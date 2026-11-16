"""Engine: one processing step with a simulated object, without sensor or window."""
import json
import os
import sys
import tempfile
import time
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from engine import Engine  # noqa: E402
from room import Station  # noqa: E402
from spout_out import SpoutOut  # noqa: E402

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
        json.dump({'room': 'T'}, f)
    eng = Engine(folder)
    eng.spout = SpoutOut()                                              # as run() would; sends nothing here
    return eng


class EngineTest(unittest.TestCase):
    def test_simulated_object_gives_field_and_texture(self):
        eng = make_engine()
        eng.sim = [[1000.0, -750.0]]                                    # x 0.25, depth 0.25
        for _ in range(5):
            eng.process({})
        snap = eng.snapshot()
        self.assertEqual(len(snap['objects']), 1)
        self.assertEqual(snap['occ'].shape, (30, 40))                   # 4 x 3 m at 0.1 m cells
        self.assertGreater(snap['occ'][7, 10], 0.9)                     # 0.75 m into the area, 1 m along the front
        eng.frame(time.time())
        eng.frame(time.time())
        tex = eng.field.texture(eng.counter)
        self.assertEqual(tex.shape, (31, 40, 4))                        # one reserved row
        self.assertGreater(tex[7, 10, 0], 230)                          # R = occupancy
        self.assertEqual(eng.counter, 2)
        self.assertEqual(eng.spout.size, (40, 31))

    def test_object_outside_the_area_is_ignored(self):
        eng = make_engine()
        eng.sim = [[6000.0, -750.0]]
        eng.process({})
        snap = eng.snapshot()
        self.assertEqual(len(snap['objects']), 0)
        self.assertEqual(float(snap['occ'].max()), 0.0)

    def test_without_front_a_black_field_is_sent(self):
        eng = make_engine(front=None)
        eng.sim = [[1000.0, -750.0]]
        eng.process({})
        snap = eng.snapshot()
        self.assertEqual(len(snap['objects']), 1)                       # still shown in the window
        self.assertEqual(float(snap['occ'].max()), 0.0)
        tex = eng.field.texture(7)
        self.assertEqual(tex.shape, (9, 8, 4))
        self.assertEqual(int(tex[:8, :, :3].max()), 0)                 # black field
        self.assertEqual(tuple(tex[8, 0, :3]), (7, 7, 7))               # counter in the reserved row

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
