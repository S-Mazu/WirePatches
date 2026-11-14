"""Motion Bridge engine: sensor -> people -> surface coordinates -> OSC.

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

from scene import MIN_MM, Background, blobs, foreground, to_xy
from stl27l import Stl27lReader

SLOTS = 8
TICK_S = 0.1                 # without sensor data (simulation only) the engine still sends at 10 Hz
CAL_REVS = 50                # revolutions for the empty room (5 s)
END_SAMPLES = 10             # samples (about 1 s) when a surface end is set

DEFAULTS = {
    'port': 'COM19',
    'osc_targets': ['127.0.0.1:7000', '127.0.0.1:8000'],   # Resolume defaults: Arena 7000, Wire 8000
    'left': None,
    'right': None,
    'depth_mm': 3000.0,
    'front_deg': 0.0,
    'mirror': False,
    'countdown_s': 10.0,
    'tol_mm': 150.0,
    'gap_mm': 250.0,
    'min_pts': 6,
    'merge_mm': 450.0,
    'assoc_mm': 700.0,
    'smooth': 0.5,
    'hold_s': 0.4,
    'fade_s': 0.3,
    'log_quality': False,
    'ui_scale': 1.0,
}


# ---------------------------------------------------------------- mapping
class Zone:
    """Maps sensor coordinates (mm) to x 0..1 and a distance in mm.

    With both surface ends set: x along the surface from left (0) to right (1), distance
    away from the surface. Otherwise sensor mode: x follows the angle in the watched half
    (90 deg left = 0, straight = 0.5, 90 deg right = 1), distance from the sensor.
    """

    def __init__(self, s):
        self.left = np.array(s['left'], float) if s['left'] else None
        self.right = np.array(s['right'], float) if s['right'] else None
        self.depth_mm = float(s['depth_mm'])
        self.front_deg = float(s['front_deg'])
        self.mirror = bool(s['mirror'])

    @property
    def surface(self):
        return (self.left is not None and self.right is not None
                and float(np.linalg.norm(self.right - self.left)) > 300.0)

    def frame(self):
        axis = self.right - self.left
        length = float(np.linalg.norm(axis))
        u = axis / length
        n = np.array([-u[1], u[0]])
        if np.dot(-self.left, n) < 0:       # normal points from the surface into the room
            n = -n
        return u, n, length

    def map(self, x_mm, y_mm):
        if self.surface:
            u, n, length = self.frame()
            p = np.array([x_mm, y_mm]) - self.left
            return float(np.dot(p, u) / length), float(np.dot(p, n))
        angle = math.degrees(math.atan2(x_mm, y_mm)) - self.front_deg
        angle = (angle + 180.0) % 360.0 - 180.0
        if self.mirror:
            angle = -angle
        return 0.5 + angle / 180.0, math.hypot(x_mm, y_mm)

    def inside(self, x01, dist_mm):
        return -0.1 <= x01 <= 1.1 and -300.0 <= dist_mm <= self.depth_mm


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

    def add(self, rev, n_people, crc_bad):
        v = 100.0 * float((rev.dist >= MIN_MM).mean())
        self.revs += 1
        self.hz.append(rev.hz)
        self.valid.append(v)
        self.points.append(len(rev.dist))
        if len(self.hz) > 36000:            # keep about one hour
            del self.hz[:6000], self.valid[:6000], self.points[:6000]
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


# ---------------------------------------------------------------- engine
class Engine(threading.Thread):
    def __init__(self, folder, overrides=None):
        super().__init__(daemon=True)
        self.folder = folder
        self.settings_path = os.path.join(folder, 'settings.json')
        self.s = copy.deepcopy(DEFAULTS)
        if os.path.exists(self.settings_path):
            with open(self.settings_path) as f:
                self.s.update(json.load(f))
        self.saved = copy.deepcopy(self.s)            # what settings.json holds
        self.overrides = dict(overrides or {})        # command line: this run only, never saved
        self.s.update(self.overrides)
        self.lock = threading.RLock()
        self.zone = Zone(self.s)
        self.bg = Background(os.path.join(folder, 'background.npy'))
        self.bg.load()
        self.tracker = Tracker()
        self.quality = Quality(os.path.join(folder, 'logs'))
        self.quality.set_logging(self.s['log_quality'])
        self.reader = None
        self.clients = []
        self.listening = {}
        self.sim = []                       # simulated people, [x_mm, y_mm]
        self.running = True
        self.task = None                    # countdown / measurement in progress
        self.message = ('', 'info', 0.0)
        self.sent = 0
        self.rate = 0.0
        self._rate_n, self._rate_t = 0, time.time()
        self.snap = None
        self.end_samples = []
        self.set_port(self.s['port'])
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
            if key in ('depth_mm', 'front_deg', 'mirror', 'left', 'right'):
                self.zone = Zone(self.s)
            if key == 'log_quality':
                self.quality.set_logging(bool(value))
        self.save()

    def reset_advanced(self):
        for k in ('tol_mm', 'gap_mm', 'min_pts', 'merge_mm', 'assoc_mm', 'smooth', 'hold_s', 'fade_s'):
            self.s[k] = DEFAULTS[k]
        self.save()

    def notify(self, text, level='info'):
        self.message = (text, level, time.time())

    # ------------------------------------------------ sensor and OSC
    def set_port(self, port):
        with self.lock:
            if self.reader:
                self.reader.stop()
            self.reader = Stl27lReader(port)
            self.reader.start()
            self.s['port'] = port
        self.save()

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

    def send(self, x, depth, active):
        alive = float(self.sent % 1000)
        for _, c in self.clients:
            try:
                c.send_message('/lidar/x', [float(v) for v in x])
                c.send_message('/lidar/depth', [float(v) for v in depth])
                c.send_message('/lidar/active', [float(v) for v in active])
                c.send_message('/lidar/count', float((active > 0).sum()))
                c.send_message('/lidar/alive', alive)
            except OSError:
                pass
        self.sent += 1
        self._rate_n += 1
        now = time.time()
        if now - self._rate_t >= 1.0:
            self.rate = self._rate_n / (now - self._rate_t)
            self._rate_n, self._rate_t = 0, now

    # ------------------------------------------------ guided calibration
    def request(self, kind):
        """kind: 'background', 'left' or 'right'. Starts the countdown."""
        with self.lock:
            self.task = {'kind': kind, 'phase': 'countdown', 'at': time.time() + float(self.s['countdown_s'])}
        if float(self.s['countdown_s']) <= 0:
            self._start_measure()

    def cancel(self):
        with self.lock:
            self.task = None
        self.notify('Cancelled.')

    def clear_surface(self):
        self.set('left', None)
        self.set('right', None)
        self.notify('Surface cleared: sensor mode.')

    def _start_measure(self):
        beep('start')
        with self.lock:
            self.task['phase'] = 'measure'
            self.task['rev0'] = self.reader.rev_count
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
            if not self.reader.connected:
                self.task = None
                beep('fail')
                self.notify('Sensor not connected: cannot learn the room.', 'error')
            elif self.reader.rev_count - task['rev0'] >= CAL_REVS:
                bg = Background(self.bg.path)
                bg.learn(self.reader.latest(CAL_REVS))
                bg.save()
                self.bg = bg
                self.task = None
                beep('done')
                cover = bg.stats()[0]
                self.notify(f'Empty room learned ({cover:.0f} % coverage).', 'ok')
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
            self.set(task['kind'], [x, y])
            beep('done')
            msg = f"{task['kind'].capitalize()} end set."
            if self.zone.surface:
                msg += f' Surface {self.zone.frame()[2] / 1000:.2f} m wide.'
            self.notify(msg, 'ok')

    def task_state(self):
        """(text, progress 0..1) for the user interface, or None."""
        task = self.task
        if task is None:
            return None
        name = {'background': 'Learning the empty room', 'left': 'Setting the left end',
                'right': 'Setting the right end'}[task['kind']]
        if task['phase'] == 'countdown':
            left = max(0.0, task['at'] - time.time())
            total = max(float(self.s['countdown_s']), 1e-3)
            where = 'leave the scan plane' if task['kind'] == 'background' else 'stand there alone'
            return f'{name}: {where} - {left:.0f} s', 1.0 - left / total
        if task['kind'] == 'background':
            done = (self.reader.rev_count - task['rev0']) / CAL_REVS
        else:
            done = len(self.end_samples) / END_SAMPLES
        return f'{name}: measuring, keep still', min(1.0, done)

    # ------------------------------------------------ main loop
    def run(self):
        seen, last_tick, last_check = 0, 0.0, 0.0
        while self.running:
            now = time.time()
            if now - last_check > 2.0:
                self.check_targets()
                last_check = now
            rd = self.reader
            if rd.rev_count != seen:
                new = min(rd.rev_count - seen, 50)
                seen = rd.rev_count
                for rev in rd.latest(new):
                    self.process(rev)
                last_tick = time.time()
            elif now - last_tick >= TICK_S and (self.sim or not rd.connected):
                self.process(None)
                last_tick = now
            else:
                time.sleep(0.005)
        self.send(np.zeros(SLOTS), np.zeros(SLOTS), np.zeros(SLOTS))
        self.reader.stop()
        self.quality.set_logging(False)

    def stop(self):
        self.running = False

    def process(self, rev):
        s = self.s
        t = rev.t if rev is not None else time.time()
        if rev is not None:
            fg = foreground(rev, self.bg, s['tol_mm'])
            bl = blobs(rev, fg, s['gap_mm'], int(s['min_pts']))
            people = merge(bl, s['merge_mm'])
        else:
            fg, bl, people = None, [], []
        people += [{'x': float(x), 'y': float(y), 'n': 50, 'sim': True} for x, y in self.sim]
        in_zone = [p for p in people if self.zone.inside(*self.zone.map(p['x'], p['y']))]
        self.tracker.update(in_zone, t, s['assoc_mm'], s['smooth'], s['hold_s'], s['fade_s'])
        x, depth, active = np.zeros(SLOTS), np.zeros(SLOTS), np.zeros(SLOTS)
        for k, tr in self.tracker.tracks.items():
            x01, d_mm = self.zone.map(tr['x'], tr['y'])
            x[k] = x01
            depth[k] = min(1.0, max(0.0, d_mm / max(self.zone.depth_mm, 1.0)))
            active[k] = tr['active']
        self.send(x, depth, active)
        if rev is not None:
            self.quality.add(rev, len(in_zone), self.reader.crc_bad)
        with self.lock:
            self._task_step(in_zone)
            self.snap = {
                'rev': rev, 'fg': fg, 'people': people, 'in_zone': in_zone,
                'tracks': {k: dict(v) for k, v in self.tracker.tracks.items()},
                'x': x, 'depth': depth, 'active': active, 't': t,
            }

    # ------------------------------------------------ for the user interface
    def snapshot(self):
        with self.lock:
            return self.snap

    def points_xy(self, snap):
        rev, fg = snap['rev'], snap['fg']
        if rev is None:
            return None
        valid = rev.dist >= MIN_MM
        x, y = to_xy(rev.angle, rev.dist)
        return x[valid & ~fg] / 1000.0, y[valid & ~fg] / 1000.0, x[fg] / 1000.0, y[fg] / 1000.0
