"""Guided measurements with a countdown and tones: learn the empty room, set an end of the front.

A measurement is a task: countdown, then measuring over a number of revolutions or
samples, then done or failed with a message. The engine drives it once per processing
step; the window only shows its state and can cancel it.
"""
import logging
import statistics
import threading
import time

from scene import Background

log = logging.getLogger(__name__)

CAL_REVS = 50                # revolutions for the empty room (5 s)
END_SAMPLES = 10             # samples (about 1 s) when an end of the front is set
END_MIN_SHARE = 0.7          # share of samples that must show exactly one object


def beep(kind: str) -> None:
    """Short signal tones without blocking (Windows only; silent elsewhere)."""
    def run():
        try:
            import winsound
            seq = {'start': [(880, 150)], 'done': [(988, 120), (1319, 180)], 'fail': [(330, 400)]}[kind]
            for f, d in seq:
                winsound.Beep(f, d)
        except ImportError:
            log.debug('no winsound: tone %s skipped', kind)
        except Exception as e:                  # a tone must never stop a measurement
            log.debug('tone %s failed: %s', kind, e)
    threading.Thread(target=run, daemon=True).start()


class Calibration:
    """One measurement at a time.

    countdown: callable -> seconds between the request and the measurement.
    notify(text, level): message for the window.
    on_done(): called after the room was changed (the engine saves it).
    """

    def __init__(self, countdown, notify, on_done):
        self._countdown, self._notify, self._on_done = countdown, notify, on_done
        self.task: dict | None = None
        self._samples: list = []

    @property
    def sid(self):
        """Position of the running task, if it is about a position."""
        return self.task.get('sid') if self.task else None

    def request(self, kind: str, sid=None) -> None:
        """kind: 'background' (for position sid), 'left' or 'right' (end of the front)."""
        self.task = {'kind': kind, 'sid': sid, 'phase': 'countdown', 'at': time.time() + float(self._countdown())}
        if float(self._countdown()) <= 0:
            self._start(None)

    def cancel(self) -> None:
        self.task = None
        self._notify('Cancelled.')

    def _start(self, reader) -> None:
        beep('start')
        self.task['phase'] = 'measure'
        self.task['rev0'] = reader.rev_count if reader else 0
        self._samples = []

    def step(self, room, reader, objects) -> None:
        """One processing step. reader: the reader of the task's position, or None. objects: what counts now."""
        task = self.task
        if task is None:
            return
        if task['phase'] == 'countdown':
            if time.time() >= task['at']:
                self._start(reader)
            return
        if task['kind'] == 'background':
            self._step_background(room, reader)
        else:
            self._step_end(room, objects)

    def _step_background(self, room, reader) -> None:
        task = self.task
        st = room.station(task['sid'])
        if reader is None or not reader.connected or st is None:
            self.task = None
            beep('fail')
            self._notify('No sensor at this position: cannot learn the room.', 'error')
        elif reader.rev_count - task['rev0'] >= CAL_REVS:
            bg = Background(room.scan_path(st.sid))
            bg.learn(reader.latest(CAL_REVS))
            bg.save()
            st.bg = bg
            self.task = None
            beep('done')
            cover = bg.stats()[0]
            log.info('empty room learned at %s: %.0f %% coverage', st.name, cover)
            self._notify(f'Empty room learned at {st.name} ({cover:.0f} % coverage).', 'ok')

    def _step_end(self, room, objects) -> None:
        task = self.task
        self._samples.append(list(objects))
        if len(self._samples) < END_SAMPLES:
            return
        singles = [s[0] for s in self._samples if len(s) == 1]
        self.task = None
        if len(singles) < END_MIN_SHARE * END_SAMPLES:
            beep('fail')
            counts = sorted({len(s) for s in self._samples})
            self._notify(f'Need exactly one object in view (saw {counts}). Try again.', 'error')
            return
        x = statistics.median(o['x'] for o in singles)
        y = statistics.median(o['y'] for o in singles)
        front = dict(room.front or {})
        front[task['kind']] = [x, y]
        room.front = front
        self._on_done()
        beep('done')
        msg = f"Front: {task['kind']} end set."
        if room.has_front():
            msg += f' {room.frame()[2] / 1000:.2f} m wide.'
        log.info(msg)
        self._notify(msg, 'ok')

    def state(self, room, reader):
        """(text, progress 0..1) for the window, or None."""
        task = self.task
        if task is None:
            return None
        st = room.station(task.get('sid'))
        name = {'background': f'Learning the empty room at {st.name if st else "?"}',
                'left': 'Setting the left end', 'right': 'Setting the right end'}[task['kind']]
        if task['phase'] == 'countdown':
            left = max(0.0, task['at'] - time.time())
            total = max(float(self._countdown()), 1e-3)
            where = 'leave the scan plane' if task['kind'] == 'background' else 'stand there alone'
            return f'{name}: {where} - {left:.0f} s', 1.0 - left / total
        if task['kind'] == 'background':
            done = (reader.rev_count - task['rev0']) / CAL_REVS if reader else 0.0
        else:
            done = len(self._samples) / END_SAMPLES
        return f'{name}: measuring, keep still', min(1.0, done)
