"""Guided measurements: learning the empty room from a fake reader, setting an end of the front."""
import os
import sys
import tempfile
import unittest

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from calibration import CAL_REVS, END_SAMPLES, Calibration  # noqa: E402
from room import Room, Station  # noqa: E402
from stl27l import Revolution  # noqa: E402

POINTS = 2160


def wall_revolution(dist_mm=3000.0):
    """A turn that sees a round wall at dist_mm everywhere."""
    angle = (np.arange(POINTS) * 360.0 / POINTS).astype(np.float32)
    return Revolution(0.0, angle, np.full(POINTS, dist_mm, np.float32), np.zeros(POINTS, np.uint8), 10.0)


class FakeReader:
    def __init__(self, revs):
        self.connected = True
        self.rev_count = 0
        self._revs = revs

    def latest(self, n=1):
        return self._revs[-n:]


class CalibrationTest(unittest.TestCase):
    def setUp(self):
        self.room = Room(tempfile.mkdtemp(), 'T')
        self.room.stations = [Station('A', 'Position A', port='COMX')]
        self.messages = []
        self.saved = 0
        self.cal = Calibration(lambda: 0.0, lambda text, level='info': self.messages.append((text, level)),
                               lambda: setattr(self, 'saved', self.saved + 1))

    def test_background_is_learned_after_enough_revolutions(self):
        reader = FakeReader([wall_revolution()] * CAL_REVS)
        self.cal.request('background', 'A')
        self.assertEqual(self.cal.task['phase'], 'measure')             # countdown 0: measuring at once
        self.cal.step(self.room, reader, [])
        self.assertIsNotNone(self.cal.task)                             # nothing learned yet
        reader.rev_count = CAL_REVS
        self.cal.step(self.room, reader, [])
        self.assertIsNone(self.cal.task)
        st = self.room.stations[0]
        self.assertTrue(st.bg.ok)
        self.assertAlmostEqual(float(np.nanmedian(st.bg.dist)), 3000.0)
        self.assertTrue(os.path.exists(self.room.scan_path('A')))
        self.assertEqual(self.messages[-1][1], 'ok')

    def test_background_fails_without_a_sensor(self):
        self.cal.request('background', 'A')
        self.cal.step(self.room, None, [])
        self.assertIsNone(self.cal.task)
        self.assertEqual(self.messages[-1][1], 'error')

    def test_front_end_from_one_object(self):
        self.cal.request('left')
        for _ in range(END_SAMPLES):
            self.cal.step(self.room, None, [{'x': 100.0, 'y': -200.0, 'n': 20}])
        self.assertIsNone(self.cal.task)
        self.assertEqual(self.room.front['left'], [100.0, -200.0])
        self.assertEqual(self.saved, 1)

    def test_front_end_fails_with_two_objects(self):
        self.cal.request('right')
        two = [{'x': 0.0, 'y': 0.0, 'n': 20}, {'x': 1000.0, 'y': 0.0, 'n': 20}]
        for _ in range(END_SAMPLES):
            self.cal.step(self.room, None, two)
        self.assertIsNone(self.cal.task)
        self.assertIsNone(self.room.front)
        self.assertEqual(self.messages[-1][1], 'error')

    def test_state_text_and_progress(self):
        self.assertIsNone(self.cal.state(self.room, None))
        self.cal.request('left')
        text, progress = self.cal.state(self.room, None)
        self.assertIn('Setting the left end', text)
        self.assertEqual(progress, 0.0)
        self.cal.cancel()
        self.assertIsNone(self.cal.task)


if __name__ == '__main__':
    unittest.main()
