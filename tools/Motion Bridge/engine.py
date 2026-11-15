"""Motion Bridge engine: sensors -> room map -> people -> screen coordinates -> OSC.

Runs in its own thread, independent of any window. The user interface only reads
snapshots and calls the request methods.
"""
import copy
import csv
import json
import math
import os
import socket
import statistics
import threading
import time

import numpy as np
from pythonosc.udp_client import SimpleUDPClient

from field import Field
from room import BEHIND_MM, Room, create_default
from scene import MIN_MM, Background, blobs, foreground, to_xy
from stl27l import Stl27lReader

SLOTS = 8
TICK_S = 0.1                 # without sensor data (simulation only) the engine still sends at 10 Hz
CAL_REVS = 50                # revolutions for the empty room (5 s)
END_SAMPLES = 10             # samples (about 1 s) when a surface end is set

DEFAULTS = {
    'room': 'Default',
    'osc_targets': ['127.0.0.1:7000', '127.0.0.1:8000'],   # Resolume defaults: Arena 7000, Wire 8000
    'countdown_s': 10.0,
    'tol_mm': 150.0,
    'gap_mm': 250.0,
    'min_pts': 6,
    'merge_mm': 450.0,
    'assoc_mm': 700.0,
    'smooth': 0.5,
    'hold_s': 0.4,
    'fade_s': 0.3,
    'field_cols': 32,
    'field_rows': 16,
    'field_fade_s': 0.5,
    'field_threshold': 0.05,
    'log_quality': False,
    'ui_scale': 1.0,
}


# ---------------------------------------------------------------- tracking
def merge(bl, merge_mm):
    """Joins blobs closer than merge_mm into one person (legs, arm and body)."""
    people = []
    for b in bl:
        for p in people:
            if math.hypot(b['x'] - p['x'], b['y'] - p['y']) < merge_mm:
                n = p['n'] + b['n']
                p['x'] = (p['x'] * p['n'] + b['x'] * b['n']) / n
                p['y'] = (p['y'] * p['n'] + b['y'] * b['n']) / n
                p['n'] = n
                break
        else:
            people.append({'x': b['x'], 'y': b['y'], 'n': b['n']})
    return people


class Tracker:
    """Keeps people in fixed slots; smooths positions and fades them in and out."""

    def __init__(self):
        self.tracks = {}
        self.last_t = None

    def update(self, people, t, assoc_mm, smooth, hold_s, fade_s):
        dt = TICK_S if self.last_t is None else max(0.0, min(0.5, t - self.last_t))
        self.last_t = t
        pairs = sorted((math.hypot(p['x'] - tr['x'], p['y'] - tr['y']), s, i)
                       for s, tr in self.tracks.items() for i, p in enumerate(people))
        used_s, used_p = set(), set()
        for d, s, i in pairs:
            if d > assoc_mm or s in used_s or i in used_p:
                continue
            tr, p = self.tracks[s], people[i]
            tr['x'] += smooth * (p['x'] - tr['x'])
            tr['y'] += smooth * (p['y'] - tr['y'])
            tr['seen'] = t
            used_s.add(s)
            used_p.add(i)
        for i, p in enumerate(people):
            if i in used_p:
                continue
            slot = next((k for k in range(SLOTS) if k not in self.tracks), None)
            if slot is None:
                break
            self.tracks[slot] = {'x': p['x'], 'y': p['y'], 'seen': t, 'active': 0.0}
        step = dt / max(fade_s, 1e-3)
        for s in list(self.tracks):
            tr = self.tracks[s]
            alive = t - tr['seen'] <= hold_s
            tr['active'] = min(1.0, tr['active'] + step) if alive else max(0.0, tr['active'] - step)
            if not alive and tr['active'] <= 0.0:
                del self.tracks[s]


# ---------------------------------------------------------------- helpers
def beep(kind):
    """Short signal tones without blocking (Windows only; silent elsewhere)."""
    def run():
        try:
            import winsound
            seq = {'start': [(880, 150)], 'done': [(988, 120), (1319, 180)], 'fail': [(330, 400)]}[kind]
            for f, d in seq:
                winsound.Beep(f, d)
        except Exception:
            pass
    threading.Thread(target=run, daemon=True).start()


def port_listening(host, port):
    """True if a local program holds the UDP port, False if free, None for remote hosts."""
    if host not in ('127.0.0.1', 'localhost'):
        return None
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.bind(('', port))
        return False
    except OSError:
        return True
    finally:
        s.close()


class Quality:
    """Per-revolution statistics and an optional CSV log."""

    def __init__(self, log_dir):
        self.log_dir = log_dir
        self.reset()
        self.log_f = None
        self.log = None
        self.log_path = None

    def reset(self):
        self.t0 = time.time()
        self.revs = 0
        self.hz = []
        self.valid = []
        self.points = []
        self.crc = []
        self.last_t = 0.0

    def add(self, rev, n_people, crc_bad):
        v = 100.0 * float((rev.dist >= MIN_MM).mean())
        self.revs += 1
        self.hz.append(rev.hz)
        self.valid.append(v)
        self.points.append(len(rev.dist))
        self.crc.append(crc_bad)
        self.last_t = time.time()
        if len(self.hz) > 36000:            # keep about one hour
            del self.hz[:6000], self.valid[:6000], self.points[:6000], self.crc[:6000]
        if self.log:
            self.log.writerow([f'{rev.t:.3f}', f'{rev.hz:.3f}', len(rev.dist), f'{v:.1f}', crc_bad, n_people])

    def set_logging(self, on):
        if on and not self.log:
            os.makedirs(self.log_dir, exist_ok=True)
            self.log_path = os.path.join(self.log_dir, f'quality_{time.strftime("%Y%m%d_%H%M%S")}.csv')
            self.log_f = open(self.log_path, 'w', newline='')
            self.log = csv.writer(self.log_f)
            self.log.writerow(['t', 'hz', 'points', 'valid_pct', 'crc_bad_total', 'people'])
        elif not on and self.log:
            self.log_f.close()
            self.log_f = self.log = None

    def summary(self):
        if not self.hz:
            return {}
        return {'minutes': (time.time() - self.t0) / 60.0, 'revs': self.revs,
                'hz_mean': statistics.fmean(self.hz), 'hz_std': statistics.pstdev(self.hz),
                'valid_mean': statistics.fmean(self.valid), 'valid_min': min(self.valid),
                'points_mean': statistics.fmean(self.points)}

    def recent(self, n=50):
        """Means over the last n revolutions (about 5 s), new CRC errors, seconds since the last one."""
        if not self.hz:
            return {}
        return {'hz': statistics.fmean(self.hz[-n:]), 'valid': statistics.fmean(self.valid[-n:]),
                'points': statistics.fmean(self.points[-n:]), 'crc_new': self.crc[-1] - self.crc[-n:][0],
                'age': time.time() - self.last_t}


# ---------------------------------------------------------------- engine
class Engine(threading.Thread):
    def __init__(self, folder, overrides=None):
        super().__init__(daemon=True)
        self.folder = folder
        self.rooms_dir = os.path.join(folder, 'rooms')
        self.settings_path = os.path.join(folder, 'settings.json')
        self.s = copy.deepcopy(DEFAULTS)
        old = {}
        if os.path.exists(self.settings_path):
            with open(self.settings_path) as f:
                old = json.load(f)
            self.s.update({k: v for k, v in old.items() if k in DEFAULTS})
        self.saved = copy.deepcopy(self.s)            # what settings.json holds
        self.overrides = dict(overrides or {})        # command line: this run only, never saved
        port_override = self.overrides.pop('port', None)
        self.s.update(self.overrides)
        self.lock = threading.RLock()
        self.readers = {}                   # port -> Stl27lReader
        self._dirty_t = None                # time of the first unsaved room edit
        self._port_override = None          # (position, saved port) while --port is in effect
        if not Room.names(self.rooms_dir):
            create_default(self.rooms_dir, old, os.path.join(folder, 'background.npy'))
        names = Room.names(self.rooms_dir)
        self.room = None
        self.load_room(self.s['room'] if self.s['room'] in names else names[0])
        if port_override:
            live = self.live_station() or self.room.stations[0]
            self._port_override = (live.sid, live.port)
            live.port = port_override       # this run only, see save_room
            self.sync_readers()
        self.tracker = Tracker()
        self.field = Field()
        self.quality = Quality(os.path.join(folder, 'logs'))
        self.quality.set_logging(self.s['log_quality'])
        self.clients = []
        self.listening = {}
        self.sim = []                       # simulated people, [x_mm, y_mm] in the map
        self.running = True
        self.task = None                    # countdown / measurement in progress
        self.message = ('', 'info', 0.0)
        self.sent = 0
        self.rate = 0.0
        self._rate_n, self._rate_t = 0, time.time()
        self.snap = None
        self.end_samples = []
        self.set_targets(self.s['osc_targets'])

    # ------------------------------------------------ settings
    def save(self):
        data = dict(self.s)
        for k in self.overrides:                      # command-line values are not written back
            if self.s[k] == self.overrides[k]:
                data[k] = self.saved[k]
        with open(self.settings_path, 'w') as f:
            json.dump(data, f, indent=2)

    def set(self, key, value):
        with self.lock:
            self.s[key] = value
            if key == 'log_quality':
                self.quality.set_logging(bool(value))
        self.save()

    def reset_advanced(self):
        for k in ('tol_mm', 'gap_mm', 'min_pts', 'merge_mm', 'assoc_mm', 'smooth', 'hold_s', 'fade_s'):
            self.s[k] = DEFAULTS[k]
        self.save()

    def notify(self, text, level='info'):
        self.message = (text, level, time.time())

    # ------------------------------------------------ room
    def load_room(self, name):
        with self.lock:
            self.flush_room()
            self._port_override = None
            self.room = Room.load(os.path.join(self.rooms_dir, name))
            self.s['room'] = name
            self.sync_readers()
        self.save()

    def new_room(self, name):
        """A new room with one position at the origin; the connected sensor moves into it."""
        name = Room.safe_name(name)
        if not name or name in Room.names(self.rooms_dir):
            return False
        live = self.live_station()
        room = Room(os.path.join(self.rooms_dir, name), name)
        room.add_station(port=live.port if live else 'COM19')
        room.depth_l_mm, room.depth_r_mm = self.room.depth_l_mm, self.room.depth_r_mm
        room.save()
        self.load_room(name)
        return True

    def save_room(self):
        """Writes the room; a port given on the command line is not written."""
        with self.lock:
            ov = self._port_override
            st = self.room.station(ov[0]) if ov else None
            if st is None:
                self.room.save()
                return
            cur, st.port = st.port, ov[1]
            try:
                self.room.save()
            finally:
                st.port = cur

    def room_changed(self):
        """Called after every edit; the room is written at most once per second."""
        if self._dirty_t is None:
            self._dirty_t = time.time()

    def flush_room(self):
        if self._dirty_t is not None and self.room is not None:
            self._dirty_t = None
            with self.lock:
                self.save_room()

    def live_station(self):
        return next((s for s in self.room.stations if s.port), None)

    def sync_readers(self):
        """One reader per port that a position holds."""
        with self.lock:
            want = {s.port for s in self.room.stations if s.port}
            for port in list(self.readers):
                if port not in want:
                    self.readers.pop(port).stop()
            for port in want:
                if port not in self.readers:
                    rd = Stl27lReader(port)
                    rd.start()
                    self.readers[port] = rd

    @property
    def reader(self):
        """Reader of the first position with a sensor (status display, quality)."""
        st = self.live_station()
        return self.readers.get(st.port) if st else None

    def set_port(self, sid, port):
        """Assigns a serial port to a position; another position holding it gives it up."""
        with self.lock:
            self._port_override = None
            for s in self.room.stations:
                if s.port == port or s.sid == sid:
                    s.port = None
            st = self.room.station(sid)
            if st:
                st.port = port or None
            self.save_room()
            self.sync_readers()

    def add_position(self, from_sid):
        """The sensor was moved: a new position takes over its port, then learns the room there."""
        with self.lock:
            self._port_override = None      # moving the sensor makes its port part of the room
            src = self.room.station(from_sid)
            port = src.port if src else None
            if src:
                src.port = None
            st = self.room.add_station(near=src, port=port)
            self.save_room()
            self.sync_readers()
        return st

    def remove_position(self, sid):
        with self.lock:
            self._port_override = None
            if len(self.room.stations) <= 1:
                return False
            st = self.room.station(sid)
            port = st.port if st else None
            self.room.remove_station(sid)
            if port and not self.live_station():
                self.room.stations[-1].port = port
            self.save_room()
            self.sync_readers()
        return True

    # ------------------------------------------------ OSC
    def set_targets(self, targets):
        clients = []
        for t in targets:
            host, _, port = t.rpartition(':')
            clients.append((t, SimpleUDPClient(host, int(port))))
        with self.lock:
            self.clients = clients
            self.s['osc_targets'] = list(targets)
        self.save()
        self.check_targets()

    def check_targets(self):
        out = {}
        for t, _ in self.clients:
            host, _, port = t.rpartition(':')
            out[t] = port_listening(host, int(port))
        self.listening = out

    def send(self, x, depth, active, occ=None, motion=None):
        alive = float(self.sent % 1000)
        for _, c in self.clients:
            try:
                c.send_message('/lidar/x', [float(v) for v in x])
                c.send_message('/lidar/depth', [float(v) for v in depth])
                c.send_message('/lidar/active', [float(v) for v in active])
                c.send_message('/lidar/count', float((active > 0).sum()))
                if occ is not None:
                    rows, cols = occ.shape
                    c.send_message('/lidar/field/size', [float(cols), float(rows)])
                    for r in range(rows):
                        c.send_message(f'/lidar/occupancy/{r}', [float(v) for v in occ[r]])
                        c.send_message(f'/lidar/motion/{r}', [float(v) for v in motion[r]])
                c.send_message('/lidar/alive', alive)
            except OSError:
                pass
        self.sent += 1
        self._rate_n += 1
        now = time.time()
        if now - self._rate_t >= 1.0:
            self.rate = self._rate_n / (now - self._rate_t)
            self._rate_n, self._rate_t = 0, now

    def osc_rate(self):
        """Updates per second; 0 when sending has stopped."""
        return self.rate if time.time() - self._rate_t < 2.5 else 0.0

    # ------------------------------------------------ guided calibration
    def request(self, kind, sid=None):
        """kind: 'background' (for position sid), 'left' or 'right'. Starts the countdown."""
        with self.lock:
            self.task = {'kind': kind, 'sid': sid, 'phase': 'countdown',
                         'at': time.time() + float(self.s['countdown_s'])}
        if float(self.s['countdown_s']) <= 0:
            self._start_measure()

    def cancel(self):
        with self.lock:
            self.task = None
        self.notify('Cancelled.')

    def _task_reader(self):
        st = self.room.station(self.task.get('sid')) if self.task else None
        return self.readers.get(st.port) if st and st.port else None

    def _start_measure(self):
        beep('start')
        with self.lock:
            self.task['phase'] = 'measure'
            rd = self._task_reader()
            self.task['rev0'] = rd.rev_count if rd else 0
            self.end_samples = []

    def _task_step(self, people):
        task = self.task
        if task is None:
            return
        if task['phase'] == 'countdown':
            if time.time() >= task['at']:
                self._start_measure()
            return
        if task['kind'] == 'background':
            rd = self._task_reader()
            st = self.room.station(task['sid'])
            if rd is None or not rd.connected or st is None:
                self.task = None
                beep('fail')
                self.notify('No sensor at this position: cannot learn the room.', 'error')
            elif rd.rev_count - task['rev0'] >= CAL_REVS:
                bg = Background(self.room.scan_path(st.sid))
                bg.learn(rd.latest(CAL_REVS))
                bg.save()
                st.bg = bg
                self.task = None
                beep('done')
                cover = bg.stats()[0]
                self.notify(f'Empty room learned at {st.name} ({cover:.0f} % coverage).', 'ok')
            return
        self.end_samples.append(list(people))
        if len(self.end_samples) >= END_SAMPLES:
            singles = [ps[0] for ps in self.end_samples if len(ps) == 1]
            self.task = None
            if len(singles) < 0.7 * END_SAMPLES:
                beep('fail')
                counts = sorted({len(ps) for ps in self.end_samples})
                self.notify(f'Need exactly one person in view (saw {counts}). Try again.', 'error')
                return
            x = statistics.median(p['x'] for p in singles)
            y = statistics.median(p['y'] for p in singles)
            scr = dict(self.room.screen or {})
            scr[task['kind']] = [x, y]
            self.room.screen = scr
            self.save_room()
            beep('done')
            msg = f"Screen: {task['kind']} end set."
            if self.room.has_screen():
                msg += f' {self.room.frame()[2] / 1000:.2f} m wide.'
            self.notify(msg, 'ok')

    def task_state(self):
        """(text, progress 0..1) for the user interface, or None."""
        task = self.task
        if task is None:
            return None
        st = self.room.station(task.get('sid'))
        name = {'background': f'Learning the empty room at {st.name if st else "?"}',
                'left': 'Setting the left end', 'right': 'Setting the right end'}[task['kind']]
        if task['phase'] == 'countdown':
            left = max(0.0, task['at'] - time.time())
            total = max(float(self.s['countdown_s']), 1e-3)
            where = 'leave the scan plane' if task['kind'] == 'background' else 'stand there alone'
            return f'{name}: {where} - {left:.0f} s', 1.0 - left / total
        if task['kind'] == 'background':
            rd = self._task_reader()
            done = (rd.rev_count - task['rev0']) / CAL_REVS if rd else 0.0
        else:
            done = len(self.end_samples) / END_SAMPLES
        return f'{name}: measuring, keep still', min(1.0, done)

    # ------------------------------------------------ main loop
    def run(self):
        seen, last_tick, last_check = {}, 0.0, 0.0
        while self.running:
            now = time.time()
            if now - last_check > 2.0:
                self.check_targets()
                last_check = now
            if self._dirty_t is not None and now - self._dirty_t > 1.0:
                self.flush_room()
            with self.lock:
                pairs = [(st, self.readers.get(st.port)) for st in self.room.stations if st.port]
                primary = self.reader
            new, revs = False, {}
            for st, rd in pairs:
                if rd is None:
                    continue
                c = rd.rev_count
                if c != seen.get(rd.port):
                    n = max(1, min(c - seen.get(rd.port, c - 1), 50))
                    seen[rd.port] = c
                    new = True
                    if rd is primary:
                        n_people = len(self.snap['in_zone']) if self.snap else 0
                        for rev in rd.latest(n):
                            self.quality.add(rev, n_people, rd.crc_bad)
                latest = rd.latest(1)
                if latest and now - latest[0].t < 0.5:
                    revs[st.sid] = latest[0]
            if new:
                self.process(revs)
                last_tick = now
            elif now - last_tick >= TICK_S and (self.sim or not revs):
                self.process(revs)
                last_tick = now
            else:
                time.sleep(0.005)
        self.send(np.zeros(SLOTS), np.zeros(SLOTS), np.zeros(SLOTS))
        self.flush_room()
        for rd in self.readers.values():
            rd.stop()
        self.quality.set_logging(False)

    def stop(self):
        self.running = False

    def process(self, revs):
        s = self.s
        t = max((r.t for r in revs.values()), default=time.time())
        views, people = {}, []
        with self.lock:
            room = self.room
            for sid, rev in revs.items():
                st = room.station(sid)
                if st is None:
                    continue
                fg = foreground(rev, st.bg, s['tol_mm'])
                mx, my = st.to_map(*to_xy(rev.angle, rev.dist))
                keep = room.keep_points(st, rev.angle, mx, my)
                for b in blobs(rev, fg & keep, s['gap_mm'], int(s['min_pts'])):
                    b['x'], b['y'] = st.to_map(b['x'], b['y'])
                    people.append(b)
                views[sid] = {'mx': mx, 'my': my, 'valid': rev.dist >= MIN_MM, 'fg': fg, 'keep': keep,
                              'dist': rev.dist}
            people = merge(people, s['merge_mm'])
            sim = [{'x': float(x), 'y': float(y), 'n': 50, 'sim': True} for x, y in self.sim]
            sim = [p for p in sim if room.in_areas([p['x']], [p['y']])[0]]
            people += sim
            screen = room.has_screen()
            in_zone = [p for p in people if not screen or room.map_xy(p['x'], p['y'])[1] >= -BEHIND_MM]
            self.tracker.update(in_zone, t, s['assoc_mm'], s['smooth'], s['hold_s'], s['fade_s'])
            x, depth, active = np.zeros(SLOTS), np.zeros(SLOTS), np.zeros(SLOTS)
            if screen:
                for k, tr in self.tracker.tracks.items():
                    x01, d_mm = room.map_xy(tr['x'], tr['y'])
                    x[k] = x01
                    depth[k] = min(1.0, max(0.0, d_mm / float(room.depth_at(x01))))
                    active[k] = tr['active']
            self.field.configure(min(max(int(s['field_cols']), 1), 32), min(max(int(s['field_rows']), 1), 64))
            self.field.update(room, list(views.values()), [[p['x'], p['y']] for p in sim], t,
                              s['field_fade_s'], s['field_threshold'])
            occ, motion = self.field.occ.copy(), self.field.motion.copy()
        self.send(x, depth, active, occ, motion)
        with self.lock:
            self._task_step(in_zone)
            self.snap = {
                'views': views, 'people': people, 'in_zone': in_zone,
                'tracks': {k: dict(v) for k, v in self.tracker.tracks.items()},
                'x': x, 'depth': depth, 'active': active, 't': t, 'occ': occ, 'motion': motion,
            }

    # ------------------------------------------------ for the user interface
    def snapshot(self):
        with self.lock:
            return self.snap
