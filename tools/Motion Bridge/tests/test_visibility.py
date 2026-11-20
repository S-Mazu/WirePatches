"""Visibility: the seen region of a position, shadows behind objects and barriers, the fan, field cells."""
import os
import sys
import tempfile
import unittest

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from room import Room, Station  # noqa: E402
from scene import BINS  # noqa: E402
from visibility import CENTRES, field_visibility, reach, seen  # noqa: E402

FRONT = {'left': [0.0, 0.0], 'right': [4000.0, 0.0]}
RECT = [[0.0, 0.0], [4000.0, 0.0], [4000.0, -3000.0], [0.0, -3000.0]]


def make_room():
    room = Room(tempfile.mkdtemp(), 'T')
    st = Station('A', 'Position A', x=2000.0, y=0.0, rot=180.0)     # at the front, looking into the area (-y)
    room.stations = [st]
    room.area, room.front = RECT, FRONT
    return room, st


def revolution(obstacle_mm=1000.0, half_width_deg=5.0):
    """Walls at 3 m all around, an obstacle straight ahead of the sensor (sensor angle 0)."""
    dist = np.full(BINS, 3000.0)
    ahead = (CENTRES < half_width_deg) | (CENTRES > 360.0 - half_width_deg)
    dist[ahead] = obstacle_mm
    return CENTRES.copy(), dist


class Reach(unittest.TestCase):
    def test_shadow_behind_an_obstacle(self):
        _, st = make_room()
        r = reach(st, *revolution())
        self.assertTrue(seen(st, r, 2000.0, -500.0))          # in front of the obstacle
        self.assertFalse(seen(st, r, 2000.0, -2000.0))        # behind it
        self.assertTrue(seen(st, r, 3000.0, -1000.0))         # beside it, up to the wall

    def test_no_return_uses_the_empty_room_or_far(self):
        _, st = make_room()
        a, d = revolution()
        d[:] = 0.0                                            # no return anywhere
        r = reach(st, a, d)
        self.assertTrue(np.all(r == 8000.0))                  # no empty room learned: up to FAR_MM

    def test_barrier_casts_a_shadow(self):
        _, st = make_room()
        barrier = [[[2100.0, -300.0], [3500.0, -300.0]]]
        r = reach(st, *revolution(), barriers=barrier)
        self.assertTrue(seen(st, r, 2200.0, -200.0))          # before the barrier
        self.assertFalse(seen(st, r, 3000.0, -1000.0))        # behind it

    def test_outside_the_fan_nothing_is_seen(self):
        _, st = make_room()
        st.fan_dir, st.fan_span = 0.0, 90.0
        r = reach(st, *revolution())
        self.assertTrue(seen(st, r, 2000.0, -500.0))
        self.assertFalse(seen(st, r, 2000.0, 1000.0))         # behind the sensor


class FieldVisibility(unittest.TestCase):
    def test_cells_behind_the_obstacle_are_not_seen(self):
        room, st = make_room()
        vis = field_visibility(room, {'A': reach(st, *revolution())}, 40, 30)
        self.assertEqual(vis.shape, (30, 40))
        self.assertEqual(vis[20, 20], 0.0)                    # 2 m straight ahead, behind the obstacle at 1 m
        self.assertEqual(vis[5, 20], 1.0)                     # 0.5 m ahead
        self.assertEqual(vis[10, 5], 1.0)                     # to the side

    def test_without_a_sensor_nothing_is_seen(self):
        room, _ = make_room()
        self.assertFalse(field_visibility(room, {}, 40, 30).any())


if __name__ == '__main__':
    unittest.main()
