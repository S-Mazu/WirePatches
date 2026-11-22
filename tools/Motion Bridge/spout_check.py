"""Checks from outside what Arena sees: reads the bridge's own Spout sender like any receiver.

Once per CHECK_S a receiver in its own thread reads the texture and samples the status band
(README, Output: every pixel of it changes every frame). If the band changes, the sender can be
read; if it stands still or cannot be read, Arena would see a frozen or black picture.

States: 'starting' (no reading yet), 'ok' (band changed within STALL_S), 'stalled' (readable, band
stands still), 'unreadable' (no image from the sender).
"""
import array
import logging
import threading
import time

log = logging.getLogger(__name__)

CHECK_S = 1.0           # one reading per second
STALL_S = 2.0           # the band must change within this time

try:
    import SpoutGL
    from SpoutGL.enums import GL_RGBA
    AVAILABLE = True
except ImportError:
    SpoutGL = None
    GL_RGBA = None
    AVAILABLE = False


class SpoutReader:
    """Thin wrapper around a SpoutGL receiver: read() -> (ok, samples) with samples a tuple of band pixels."""

    def __init__(self, name):
        self._r = SpoutGL.SpoutReceiver()
        self._r.setReceiverName(name)
        self._buf = None

    def read(self):
        ok = self._r.receiveImage(self._buf, GL_RGBA, False, 0)
        if self._r.isUpdated():                             # first contact or a new size: allocate, read next time
            w, h = self._r.getSenderWidth(), self._r.getSenderHeight()
            self._buf = array.array('B', bytes(max(w * h * 4, 4)))
            return False, None
        if not ok or self._buf is None:
            return False, None
        w, h = self._r.getSenderWidth(), self._r.getSenderHeight()
        if w < 3 or h < 2:
            return False, None
        b = self._buf
        # the band is the first or the last row, depending on the sender's flip: sample both
        rows = (0, h - 1)
        return True, tuple(b[(r * w + x) * 4 + c] for r in rows for x in (1, w // 2, w - 2) for c in (0, 1, 2))

    def close(self):
        try:
            self._r.releaseReceiver()
        except Exception as e:
            log.debug('Spout receiver release: %s', e)


class SpoutCheck(threading.Thread):
    def __init__(self, name, reader_factory=None, clock=time.time):
        super().__init__(daemon=True)
        self.name_ = name
        self._factory = reader_factory or (SpoutReader if AVAILABLE else None)
        self._clock = clock
        self.running = True
        self.state = 'starting'
        self.since = clock()            # time the state began
        self.last_change_t = None       # last time the band changed
        self._last = None

    def step(self, reader):
        """One reading; updates state. Separate from run() for tests."""
        now = self._clock()
        try:
            ok, samples = reader.read()
        except Exception as e:
            ok, samples = False, None
            log.debug('Spout check read: %s', e)
        if ok and samples != self._last:
            self.last_change_t = now
        if ok:
            self._last = samples
        if self.last_change_t is not None and now - self.last_change_t <= STALL_S:
            new = 'ok'
        elif ok:
            new = 'stalled'
        else:
            new = 'unreadable'
        if self.state == 'starting' and new != 'ok' and now - self.since < STALL_S:
            return                       # give the first contact time
        if new != self.state:
            (log.info if new == 'ok' else log.warning)('Spout check: %s -> %s', self.state, new)
            self.state, self.since = new, now

    def bad_for(self, now=None):
        """Seconds the check has been failing (0 while ok or starting)."""
        if self.state in ('ok', 'starting'):
            return 0.0
        return (now if now is not None else self._clock()) - self.since

    def run(self):
        if self._factory is None:
            self.state = 'unavailable'
            return
        reader = None
        while self.running:
            try:
                if reader is None:
                    reader = self._factory(self.name_)
                self.step(reader)
            except Exception:
                log.exception('Spout check failed')
                if reader is not None:
                    reader.close()
                reader = None
            time.sleep(CHECK_S)
        if reader is not None:
            reader.close()

    def stop(self):
        self.running = False
