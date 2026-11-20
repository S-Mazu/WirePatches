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
        self.assertEqual(tex.shape, (32, 40, 4))                        # 30 field rows + band of 2
        self.assertGreater(tex[7, 10, 0], 230)                          # R = occupancy
        self.assertEqual(eng.counter, 2)
        self.assertEqual(eng.spout.size, (40, 32))

    def test_visibility_goes_into_the_blue_channel(self):
        """A sensor at the front looks into the area; an obstacle 1 m ahead shades the cells behind it."""
        import numpy as np
        from scene import BINS
        from stl27l import Revolution
        eng = make_engine()
        st = eng.room.stations[0]
        st.x, st.y, st.rot = 2000.0, 0.0, 180.0                       # middle of the front, looking into the area
        angle = (np.arange(BINS) + 0.5) * (360.0 / BINS)
        dist = np.full(BINS, 3000.0)
        dist[(angle < 5.0) | (angle > 355.0)] = 1000.0
        rev = Revolution(time.time(), angle.astype(np.float32), dist.astype(np.float32),
                         np.zeros(BINS, np.uint8), 10.0)
        eng.process({'A': rev})
        vis = eng.snapshot()['vis']
        self.assertEqual(vis[20, 20], 0.0)                              # 2 m ahead, behind the obstacle
        self.assertEqual(vis[5, 20], 1.0)                               # 0.5 m ahead
        self.assertEqual(vis[10, 5], 1.0)                               # beside it
        tex = eng.field.texture(0)
        self.assertEqual((tex[5, 20, 2], tex[20, 20, 2]), (255, 0))     # B = visibility

    def test_without_a_sensor_nothing_is_seen(self):
        eng = make_engine()
        eng.sim = [[1000.0, -750.0]]
        eng.process({})
        self.assertEqual(float(eng.snapshot()['vis'].max()), 0.0)

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
        self.assertEqual(tex.shape, (16, 8, 4))                         # 15 black rows + band of 1
        self.assertEqual(int(tex[:15, :, :3].max()), 0)                # black field
        self.assertEqual(tuple(tex[15, 3, :3]), (7, 7, 7))              # counter in the band

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
