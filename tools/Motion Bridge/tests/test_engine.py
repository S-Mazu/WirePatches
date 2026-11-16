"""Engine: one processing step with a simulated object, without sensor or window."""
import json
import os
import sys
import tempfile
import unittest

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from engine import Engine  # noqa: E402
from room import Station  # noqa: E402

FRONT = {'left': [0.0, 0.0], 'right': [4000.0, 0.0]}
RECT = [[0.0, 0.0], [4000.0, 0.0], [4000.0, -3000.0], [0.0, -3000.0]]


def make_engine(area=RECT, front=FRONT):
    folder = tempfile.mkdtemp()
    room_dir = os.path.join(folder, 'rooms', 'T')
    os.makedirs(room_dir)
    with open(os.path.join(room_dir, 'room.json'), 'w') as f:        # no port: no serial reader
        json.dump({'stations': [Station('A', 'Position A').to_dict()], 'area': area, 'front': front,
                   'exclusions': [], 'barriers': [], 'ref': None}, f)
    with open(os.path.join(folder, 'settings.json'), 'w') as f:
        json.dump({'room': 'T', 'osc_targets': []}, f)                 # no OSC targets: nothing leaves the test
    eng = Engine(folder)
    eng.osc.close()
    return eng


class EngineTest(unittest.TestCase):
    def test_simulated_object_gives_points_and_field(self):
        eng = make_engine()
        eng.sim = [[1000.0, -750.0]]                                    # x 0.25, depth 0.25
        for _ in range(5):
            eng.process({})
        snap = eng.snapshot()
        self.assertEqual(len(snap['objects']), 1)
        self.assertAlmostEqual(snap['x'][0], 0.25)
        self.assertAlmostEqual(snap['depth'][0], 0.25)
        self.assertGreater(snap['active'][0], 0.0)
        self.assertGreater(snap['occ'][4, 8], 0.9)
        self.assertEqual(snap['occ'].shape, (16, 32))

    def test_object_outside_the_area_is_ignored(self):
        eng = make_engine()
        eng.sim = [[6000.0, -750.0]]
        eng.process({})
        snap = eng.snapshot()
        self.assertEqual(len(snap['objects']), 0)
        self.assertEqual(float(np.abs(snap['active']).sum()), 0.0)

    def test_without_front_zeros_are_sent(self):
        eng = make_engine(front=None)
        eng.sim = [[1000.0, -750.0]]
        eng.process({})
        snap = eng.snapshot()
        self.assertEqual(len(snap['objects']), 1)                       # still tracked in the window
        self.assertEqual(float(np.abs(snap['x']).sum()), 0.0)
        self.assertEqual(float(snap['occ'].max()), 0.0)

    def test_front_end_set_by_standing_there(self):
        eng = make_engine(front=None)
        eng.set('countdown_s', 0.0)
        eng.sim = [[3900.0, -200.0]]
        eng.request('right')
        for _ in range(12):
            eng.process({})
        self.assertIsNone(eng.cal.task)
        self.assertEqual(eng.room.front['right'], [3900.0, -200.0])


if __name__ == '__main__':
    unittest.main()
