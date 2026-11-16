"""Room map: area, front, field frame, filters, files and migration."""
import json
import os
import sys
import tempfile
import unittest

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from room import MIN_DEPTH_MM, Room, Station, create_default  # noqa: E402

# A front of 4 m along the x axis, the area a 4 x 3 m rectangle in front of it. Seen from the
# area the left end is at the origin, so the area lies at negative y (n = (u.y, -u.x)).
FRONT = {'left': [0.0, 0.0], 'right': [4000.0, 0.0]}
RECT = [[0.0, 0.0], [4000.0, 0.0], [4000.0, -3000.0], [0.0, -3000.0]]


def make_room(area=RECT, front=FRONT):
    room = Room(tempfile.mkdtemp(), 'T')
    room.stations = [Station('A', 'Position A')]
    room.area, room.front = area, front
    return room


class FrameTest(unittest.TestCase):
    def test_ready_needs_area_and_front(self):
        self.assertTrue(make_room().ready())
        self.assertFalse(make_room(area=None).ready())
        self.assertFalse(make_room(front=None).ready())
        self.assertFalse(make_room(front={'left': [0, 0], 'right': [100, 0]}).ready())   # too short

    def test_frame_and_depth(self):
        room = make_room()
        u, n, length = room.frame()
        np.testing.assert_allclose(u, [1, 0])
        np.testing.assert_allclose(n, [0, -1])
        self.assertAlmostEqual(length, 4000.0)
        self.assertAlmostEqual(room.depth_mm(), 3000.0)

    def test_depth_follows_the_farthest_corner(self):
        room = make_room(area=[[0, 0], [4000, 0], [2000, -5000]])
        self.assertAlmostEqual(room.depth_mm(), 5000.0)
        room = make_room(area=[[0, 0], [4000, 0], [2000, -100]])
        self.assertAlmostEqual(room.depth_mm(), MIN_DEPTH_MM)

    def test_to_front_and_to_field(self):
        room = make_room()
        x01, d_mm = room.to_front(1000.0, -750.0)
        self.assertAlmostEqual(x01, 0.25)
        self.assertAlmostEqual(d_mm, 750.0)
        x01, d01 = room.to_field(np.array([1000.0, 5000.0]), np.array([-750.0, 600.0]))
        np.testing.assert_allclose(x01, [0.25, 1.25])
        np.testing.assert_allclose(d01, [0.25, -0.2])

    def test_front_faces_area(self):
        room = make_room()
        self.assertTrue(room.front_faces_area())
        room.front = {'left': FRONT['right'], 'right': FRONT['left']}
        self.assertFalse(room.front_faces_area())

    def test_field_mask_follows_the_area(self):
        room = make_room(area=[[0, 0], [4000, 0], [0, -3000]])          # triangle: the far right is outside
        mask = room.field_mask(4, 3)
        self.assertEqual(mask.shape, (3, 4))
        self.assertEqual(mask[0, 0], 1.0)                                # at the front, left
        self.assertEqual(mask[2, 3], 0.0)                                # far edge, right
        self.assertIs(mask, room.field_mask(4, 3))                       # cached
        room.area[2] = [4000, -3000]                                     # edited in place: the cache notices
        self.assertEqual(room.field_mask(4, 3)[2, 3], 1.0)

    def test_area_from_front(self):
        room = make_room(area=None)
        room.area_from_front(2000.0)
        np.testing.assert_allclose(room.area, [[0, 0], [4000, 0], [4000, -2000], [0, -2000]])
        self.assertAlmostEqual(room.depth_mm(), 2000.0)


class FilterTest(unittest.TestCase):
    def test_counts_inside_area_and_outside_exclusions(self):
        room = make_room()
        room.exclusions = [[[1000, -1000], [2000, -1000], [2000, -2000], [1000, -2000]]]
        mx = np.array([500.0, 1500.0, 6000.0])
        my = np.array([-500.0, -1500.0, -500.0])
        np.testing.assert_array_equal(room.counts(mx, my), [True, False, False])

    def test_without_an_area_everything_counts(self):
        room = make_room(area=None)
        np.testing.assert_array_equal(room.counts([9e6], [9e6]), [True])

    def test_keep_points_with_fan_and_barrier(self):
        room = make_room()
        st = room.stations[0]
        st.x, st.y, st.fan_span, st.fan_dir = 2000.0, 500.0, 90.0, 180.0     # looks into the area (sensor y+ = map y-)
        room.barriers = [[[2500, -1000], [3500, -1000]]]
        angle = np.array([180.0, 180.0, 0.0])
        mx = np.array([2000.0, 3000.0, 2000.0])
        my = np.array([-500.0, -2000.0, 1500.0])
        np.testing.assert_array_equal(room.keep_points(st, angle, mx, my), [True, False, False])


class FileTest(unittest.TestCase):
    def test_save_and_load(self):
        room = make_room()
        room.exclusions = [[[1, 2], [3, 4], [5, 6]]]
        room.barriers = [[[0, 0], [1, 1]]]
        room.save()
        back = Room.load(room.folder)
        self.assertEqual(back.area, RECT)
        self.assertEqual(back.front, FRONT)
        self.assertEqual(back.exclusions, room.exclusions)
        self.assertEqual(back.barriers, room.barriers)
        self.assertEqual([s.sid for s in back.stations], ['A'])

    def test_legacy_screen_and_trapezoid_become_front_and_area(self):
        folder = tempfile.mkdtemp()
        old = {'stations': [Station('A', 'Position A').to_dict()], 'screen': FRONT,
               'depth_l_mm': 2000.0, 'depth_r_mm': 4000.0,
               'areas': [{'kind': 'exclude', 'pts': [[1, 1], [2, 2], [3, 1]]}], 'barriers': [], 'ref': None}
        with open(os.path.join(folder, 'room.json'), 'w') as f:
            json.dump(old, f)
        room = Room.load(folder)
        self.assertEqual(room.front, FRONT)
        np.testing.assert_allclose(room.area, [[0, 0], [4000, 0], [4000, -4000], [0, -2000]])
        self.assertEqual(room.exclusions, [[[1, 1], [2, 2], [3, 1]]])
        self.assertAlmostEqual(room.depth_mm(), 4000.0)
        room.save()
        with open(os.path.join(folder, 'room.json')) as f:
            self.assertNotIn('screen', json.load(f))

    def test_legacy_include_area_wins_over_the_trapezoid(self):
        folder = tempfile.mkdtemp()
        tri = [[0, 0], [4000, 0], [2000, -1000]]
        old = {'stations': [], 'screen': FRONT, 'depth_mm': 3000.0,
               'areas': [{'kind': 'include', 'pts': tri}], 'barriers': [], 'ref': None}
        with open(os.path.join(folder, 'room.json'), 'w') as f:
            json.dump(old, f)
        room = Room.load(folder)
        self.assertEqual(room.area, tri)

    def test_create_default_takes_over_the_old_single_sensor_setup(self):
        rooms = tempfile.mkdtemp()
        room = create_default(rooms, {'port': 'COM7', 'left': [0, 0], 'right': [4000, 0], 'depth_mm': 2500.0}, None)
        self.assertEqual(room.stations[0].port, 'COM7')
        self.assertTrue(room.ready())
        self.assertAlmostEqual(room.depth_mm(), 2500.0)
        self.assertEqual(Room.names(rooms), ['Default'])


if __name__ == '__main__':
    unittest.main()
