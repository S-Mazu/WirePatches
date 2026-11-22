"""Guided measurement with a countdown and tones: learn the empty room.

A measurement is a task: countdown, then measuring over a number of revolutions or
samples, then done or failed with a message. The engine drives it once per processing
step; the window only shows its state and can cancel it.
"""
import logging
import threading
import time

from scene import Background

log = logging.getLogger(__name__)

CAL_REVS = 50                # revolutions for the empty room (5 s)


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

    @property
    def sid(self):
        """Position of the running task, if it is about a position."""
        return self.task.get('sid') if self.task else None

    def request(self, kind: str, sid=None) -> None:
        """kind: 'background' (learn the empty room at position sid)."""
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

    def step(self, room, reader, objects) -> None:
        """One processing step. reader: the reader of the task's position, or None. objects: what counts now."""
        task = self.task
        if task is None:
            return
        if task['phase'] == 'countdown':
            if time.time() >= task['at']:
                self._start(reader)
            return
        self._step_background(room, reader)

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

    def state(self, room, reader):
        """(text, progress 0..1) for the window, or None."""
        task = self.task
        if task is None:
            return None
        st = room.station(task.get('sid'))
        name = f'Learning the empty room at {st.name if st else "?"}'
        if task['phase'] == 'countdown':
            left = max(0.0, task['at'] - time.time())
            total = max(float(self._countdown()), 1e-3)
            return f'{name}: leave the scan plane - {left:.0f} s', 1.0 - left / total
        done = (reader.rev_count - task['rev0']) / CAL_REVS if reader else 0.0
        return f'{name}: measuring, keep the room still', min(1.0, done)
