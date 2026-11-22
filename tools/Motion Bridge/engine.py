"""Motion Bridge engine: sensors -> room map -> foreground -> field -> Spout texture.

Threads:
- One Stl27lReader per serial port collects revolutions (stl27l.py).
- The Engine thread (this module) takes the newest revolution of every position, builds
  the foreground, the objects and the field (10 Hz, with the sensor), renders the field
  texture at FRAME_HZ and sends it over Spout (spout_out.py), and keeps a snapshot for
  the window.
- The window thread (ui) reads `snapshot()` and the room, and changes the room only
  through `edit()`; settings through `set()` and `reset()`; measurements through
  `request()` and `cancel()`.

`lock` guards the room, the readers, the field and the snapshot. Settings are plain values
that are replaced, not mutated, so reading them needs no lock.
"""
import copy
import logging
import os
import threading
import time

from calibration import Calibration
from field import Field
from quality import Quality
from room import Room, create_default
from scene import MIN_MM, blobs, foreground, merge, to_xy
from settings import DEFAULT_PORT, Settings
from spout_check import SpoutCheck
from spout_out import SpoutOut
from stl27l import Stl27lReader
from visibility import field_visibility, reach

log = logging.getLogger(__name__)

TICK_S = 0.1                 # without sensor data (simulation only) the field is still measured at 10 Hz
FRAME_HZ = 30.0              # texture frames per second; motion fades between measurements
STALE_S = 0.5                # a revolution older than this is not used
HEAL_S = 2.0                 # sending or the check failing this long: the sender is recreated
HEAL_GAP_S = 5.0             # at most one recreation per this time
STOP_S = 1.0                 # no frame for this long: the output counts as stopped
SAVE_DELAY_S = 1.0           # the room is written at most once per second while it is edited
UNDO_STEPS = 50              # shape edits that can be undone


class Engine(threading.Thread):
    def __init__(self, folder: str, overrides: dict | None = None):
        super().__init__(daemon=True)
        self.folder = folder
        self.rooms_dir = os.path.join(folder, 'rooms')
        overrides = dict(overrides or {})
        port_override = overrides.pop('port', None)
        self.s = Settings(os.path.join(folder, 'settings.json'), overrides)
        self.lock = threading.RLock()
        self.readers: dict = {}             # port -> Stl27lReader
        self._dirty_t = None                # time of the first unsaved room edit
        self._undo: list = []               # shapes before each edit, newest last
        self._port_override = None          # (position, saved port) while --port is in effect
        if not Room.names(self.rooms_dir):
            create_default(self.rooms_dir, self.s.raw, os.path.join(folder, 'background.npy'))
        names = Room.names(self.rooms_dir)
        self.room: Room = None
        self.load_room(self.s['room'] if self.s['room'] in names else names[0])
        if port_override:
            live = self.live_station() or self.room.stations[0]
            self._port_override = (live.sid, live.port)
            live.port = port_override       # this run only, see save_room
            self.sync_readers()
        self.field = Field()
        self.quality = Quality(os.path.join(folder, 'logs'))
        self.quality.set_logging(self.s['log_quality'])
        self.spout = None                   # created in run(): Spout wants one thread
        self.counter = 0                    # frame counter in the texture, 0..255
        self.last_tex = None                # the texture of the last frame (RGBA uint8, row 0 on the base line)
        self.frame_rate = 0.0               # frames really sent per second, measured
        self._frames, self._frames_t = 0, time.time()
        self.last_frame_t = None            # time of the last texture frame the engine made
        self.check = None                   # SpoutCheck: reads the sender like Arena does
        self.loop_errors = 0
        self._healed_t = 0.0
        self._minute_t, self._minute_frames = time.time(), 0   # the minute line in the log
        self._minute_revs = 0
        self.cal = Calibration(lambda: self.s['countdown_s'], self.notify, self.save_room)
        self.sim: list = []                 # simulated objects, [x_mm, y_mm] in the map
        self.running = True
        self.message = ('', 'info', 0.0)
        self.snap: dict | None = None

    # ------------------------------------------------ settings
    def set(self, key: str, value) -> None:
        self.s.set(key, value)
        if key == 'log_quality':
            self.quality.set_logging(bool(value))

    def reset(self, keys) -> None:
        self.s.reset(keys)

    def notify(self, text: str, level: str = 'info') -> None:
        self.message = (text, level, time.time())

    # ------------------------------------------------ room
    def load_room(self, name: str) -> None:
        with self.lock:
            self.flush_room()
            self._port_override = None
            self.room = Room.load(os.path.join(self.rooms_dir, name))
            self._undo = []
            self.sync_readers()
        self.s.set('room', name)
        log.info('room %s loaded', name)

    def new_room(self, name: str) -> bool:
        """A new room with one position at the origin; the connected sensor moves into it."""
        name = Room.safe_name(name)
        if not name or name in Room.names(self.rooms_dir):
            return False
        live = self.live_station()
        room = Room(os.path.join(self.rooms_dir, name), name)
        room.add_station(port=live.port if live else DEFAULT_PORT)
        room.save()
        self.load_room(name)
        return True

    def edit(self, fn, undo=False):
        """Runs fn(room) under the lock, schedules the room to be written, returns fn's result.
        undo: the shapes before the edit go onto the undo stack if the edit changed them."""
        with self.lock:
            before = self.shapes() if undo else None
            result = fn(self.room)
            if undo:
                self.push_undo(before)
            self.room_changed()
        return result

    # ------------------------------------------------ undo of shape edits
    def shapes(self):
        """The drawn shapes of the room: area, base line, exclusions, barriers, reference marker."""
        r = self.room
        return copy.deepcopy((r.area, r.base, r.exclusions, r.barriers, r.ref))

    def push_undo(self, before) -> None:
        """Keeps `before` for Undo if the shapes differ from it now."""
        with self.lock:
            if before is not None and before != self.shapes() and not (self._undo and self._undo[-1] == before):
                self._undo.append(before)
                del self._undo[:-UNDO_STEPS]

    def undo(self) -> bool:
        """Puts the shapes back to before the last change; False if there is nothing to undo."""
        with self.lock:
            if not self._undo:
                return False
            r = self.room
            r.area, r.base, r.exclusions, r.barriers, r.ref = self._undo.pop()
            self.room_changed()
        return True

    def save_room(self) -> None:
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

    def room_changed(self) -> None:
        """Called after every edit; the room is written at most once per second."""
        if self._dirty_t is None:
            self._dirty_t = time.time()

    def flush_room(self) -> None:
        if self._dirty_t is not None and self.room is not None:
            self._dirty_t = None
            with self.lock:
                self.save_room()

    def live_station(self):
        return next((s for s in self.room.stations if s.port), None)

    def sync_readers(self) -> None:
        """One reader per port that a position holds."""
        with self.lock:
            want = {s.port for s in self.room.stations if s.port}
            for port in list(self.readers):
                if port not in want:
                    self.readers.pop(port).stop()
                    log.info('reader on %s stopped', port)
            for port in want:
                if port not in self.readers:
                    rd = Stl27lReader(port)
                    rd.start()
                    self.readers[port] = rd
                    log.info('reader on %s started', port)

    @property
    def reader(self):
        """Reader of the first position with a sensor (status display, quality)."""
        st = self.live_station()
        return self.readers.get(st.port) if st else None

    def reader_of(self, sid):
        """Reader of a position, or None."""
        st = self.room.station(sid) if sid else None
        return self.readers.get(st.port) if st and st.port else None

    def set_port(self, sid, port) -> None:
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

    def remove_position(self, sid) -> bool:
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

    # ------------------------------------------------ guided measurements
    def request(self, kind: str, sid=None) -> None:
        """kind: 'background' (learn the empty room at position sid). Starts the countdown."""
        with self.lock:
            self.cal.request(kind, sid)

    def cancel(self) -> None:
        with self.lock:
            self.cal.cancel()

    def task_state(self):
        """(text, progress 0..1) for the window, or None."""
        return self.cal.state(self.room, self.reader_of(self.cal.sid))

    # ------------------------------------------------ main loop
    def run(self) -> None:
        self.spout = SpoutOut()
        self.check = SpoutCheck(self.spout.name)
        self.check.start()
        self._loop_state = {'seen': {}, 'last_tick': 0.0, 'last_frame': 0.0}
        while self.running:
            try:
                self._loop_once()
            except Exception:                   # a failure here must never stop the output silently
                self.loop_errors += 1
                log.exception('engine loop failed (%d so far)', self.loop_errors)
                time.sleep(0.1)
        self.check.stop()
        self.spout.close()
        self.flush_room()
        for rd in self.readers.values():
            rd.stop()
        self.quality.set_logging(False)

    def _loop_once(self) -> None:
        """One pass of the main loop: read the sensors, measure, send a frame when due, watch the output."""
        st_ = self._loop_state
        seen = st_['seen']
        frame_s = 1.0 / FRAME_HZ
        now = time.time()
        if self._dirty_t is not None and now - self._dirty_t > SAVE_DELAY_S:
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
                    n_objects = len(self.snap['objects']) if self.snap else 0
                    for rev in rd.latest(n):
                        self.quality.add(rev, n_objects, rd.crc_bad)
            latest = rd.latest(1)
            if latest and now - latest[0].t < STALE_S:
                revs[st.sid] = latest[0]
        if new or (now - st_['last_tick'] >= TICK_S and (self.sim or not revs)):
            self.process(revs)
            st_['last_tick'] = now
        if now - st_['last_frame'] >= frame_s:
            self.frame(now)
            st_['last_frame'] = now
            self.watch(now)
        else:
            time.sleep(0.002)

    def watch(self, now: float) -> None:
        """Self-healing and the minute log: recreate the sender when sending or the check has failed for
        HEAL_S, at most every HEAL_GAP_S; one line per minute with what went out."""
        sp, ck = self.spout, self.check
        send_bad = sp.sending is False or (sp.last_ok_t is not None and now - sp.last_ok_t > HEAL_S)
        check_bad = ck is not None and ck.bad_for(now) > HEAL_S
        if (send_bad or check_bad) and now - self._healed_t > HEAL_GAP_S and sp.available:
            log.warning('output not reaching receivers (send %s, check %s for %.1f s): recreating the sender',
                        'failing' if send_bad else 'ok', ck.state if ck else '-', ck.bad_for(now) if ck else 0.0)
            self._healed_t = now
            sp.recreate()
        if now - self._minute_t >= 60.0:
            frames = sp.frames - self._minute_frames
            rd = self.reader
            revs = (rd.rev_count if rd else 0) - self._minute_revs
            log.info('minute: %d frames sent (%.1f/s), check %s, sensor %d revolutions (%s), %d recreations, '
                     '%d loop errors', frames, frames / (now - self._minute_t), ck.state if ck else '-', revs,
                     'connected' if rd and rd.connected else 'not connected', sp.recreations, self.loop_errors)
            self._minute_t, self._minute_frames = now, sp.frames
            self._minute_revs = rd.rev_count if rd else 0

    def stop(self) -> None:
        self.running = False

    def process(self, revs: dict) -> None:
        """One measurement: the newest revolution per position -> foreground, objects, field."""
        s = self.s
        t = max((r.t for r in revs.values()), default=time.time())
        views, found, reaches = {}, [], {}
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
                    found.append(b)
                seen_r = reach(st, rev.angle, rev.dist, room.barriers)
                reaches[sid] = seen_r
                views[sid] = {'mx': mx, 'my': my, 'valid': rev.dist >= MIN_MM, 'fg': fg, 'keep': keep,
                              'dist': rev.dist, 'reach': seen_r}
            objects = merge(found, s['merge_mm'])              # foreground objects: blobs merged, plus simulated ones
            sim = [{'x': float(x), 'y': float(y), 'n': 50, 'sim': True} for x, y in self.sim]
            sim = [o for o in sim if room.counts([o['x']], [o['y']])[0]]
            objects += sim
            self.field.configure_for(room, s['cell_mm'])
            self.field.update(room, list(views.values()), [[o['x'], o['y']] for o in sim], t,
                              s['field_fade_s'], s['field_threshold'])
            self.field.vis = field_visibility(room, reaches, self.field.grid, self.field.cols, self.field.rows)
            self.cal.step(room, self.reader_of(self.cal.sid), objects)
            self.snap = {
                'views': views, 'objects': objects, 't': t,
                'occ': self.field.occ.copy(), 'motion': self.field.motion.copy(), 'vis': self.field.vis.copy(),
                'grid': self.field.grid, 'band': self.field.band,
            }

    def frame(self, now: float) -> None:
        """One texture frame: motion fades, the counter advances, the image goes out over Spout."""
        with self.lock:
            self.field.configure_for(self.room, self.s['cell_mm'])
            self.field.decay(now, self.s['field_fade_s'])
            tex = self.field.texture(self.counter)
        self.last_tex = tex                 # the window shows it as the preview of what is sent
        self.counter = (self.counter + 1) % 256
        self.spout.send(tex)
        self.last_frame_t = now
        if now - self._frames_t >= 1.0:              # frames that were really sent
            self.frame_rate = (self.spout.frames - self._frames) / (now - self._frames_t)
            self._frames, self._frames_t = self.spout.frames, now

    # ------------------------------------------------ for the user interface
    def snapshot(self):
        with self.lock:
            return self.snap

    def output_status(self):
        """(ok, text) for the status line: what leaves the bridge."""
        sp, ck = self.spout, self.check
        now = time.time()
        if sp is None or self.last_frame_t is None:
            return False, 'Output: starting'
        if now - self.last_frame_t > STOP_S:
            return False, f'Output STOPPED: no frame for {now - self.last_frame_t:.0f} s (see logs/bridge.log)'
        if not sp.sending:
            return False, f'Output: {sp.status}; nothing is sent'
        if sp.last_ok_t is None or now - sp.last_ok_t > STOP_S:
            return False, 'Output STOPPED: frames are made but not sent (see logs/bridge.log)'
        heal = f', sender recreated {sp.recreations}x' if sp.recreations else ''
        if ck is not None and ck.state in ('stalled', 'unreadable'):
            return False, (f'Output NOT READABLE for {ck.bad_for(now):.0f} s: Arena would see '
                           f'{"a frozen" if ck.state == "stalled" else "no"} picture{heal}')
        check = {'ok': 'checked', 'starting': 'check starting'}.get(ck.state if ck else '', 'not checked')
        return True, f'Output: {sp.status}, {self.frame_rate:.0f} frames/s, {check}{heal}'
