"""Room map: area, base line, field frame, corner editing, filters, files and migration."""
import json
import os
import sys
import tempfile
import unittest

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from room import MIN_SIZE_MM, Room, Station, create_default, nearest_corner, nearest_edge, polygon_area  # noqa: E402

# The area is a 4 x 3 m rectangle at negative y; its base line is the edge along the x axis (edge 0).
# Seen from the area, looking at the base line, the left end is at the origin, so n = (u.y, -u.x) = (0, -1).
RECT = [[0.0, 0.0], [4000.0, 0.0], [4000.0, -3000.0], [0.0, -3000.0]]
FRONT = {'left': [0.0, 0.0], 'right': [4000.0, 0.0]}            # rooms before 2026-11-22


def make_room(area=RECT, base=0):
    room = Room(tempfile.mkdtemp(), 'T')
    room.stations = [Station('A', 'Position A')]
    room.area = [list(p) for p in area] if area else area
    room.base = base
    return room


class FrameTest(unittest.TestCase):
    def test_ready_needs_area_and_base_line(self):
        self.assertTrue(make_room().ready())
        self.assertFalse(make_room(area=None).ready())
        self.assertFalse(make_room(base=None).ready())
        self.assertFalse(make_room(area=[[0, 0], [100, 0], [100, -3000], [0, -3000]]).ready())   # base too short

    def test_frame_of_the_rectangle(self):
        origin, u, n, width, depth = make_room().frame()
        np.testing.assert_allclose(origin, [0, 0])
        np.testing.assert_allclose(u, [1, 0])
        np.testing.assert_allclose(n, [0, -1])
        self.assertAlmostEqual(width, 4000.0)
        self.assertAlmostEqual(depth, 3000.0)

    def test_corner_order_does_not_change_left_and_right(self):
        ccw = [RECT[0], RECT[3], RECT[2], RECT[1]]                     # same rectangle, other direction
        room = make_room(area=ccw, base=3)                              # edge corner 3 -> 0 is the x-axis edge
        self.assertGreater(polygon_area(ccw), 0.0)
        origin, u, n, width, depth = room.frame()
        np.testing.assert_allclose(origin, [0, 0])
        np.testing.assert_allclose(u, [1, 0])
        np.testing.assert_allclose(n, [0, -1])

    def test_rectangle_holds_the_whole_area(self):
        # The area reaches 1.5 m left of the base line's left end and 0.5 m right of its right end.
        area = [[0, 0], [4000, 0], [4500, -2000], [-1500, -3000]]
        origin, u, n, width, depth = make_room(area=area).frame()
        np.testing.assert_allclose(origin, [-1500, 0])
        self.assertAlmostEqual(width, 6000.0)
        self.assertAlmostEqual(depth, 3000.0)

    def test_slanted_base_line(self):
        # A square of 2 m turned by 45 degrees; the base line is the edge from (0, 0) to (s, -s).
        s = 2000.0 / np.sqrt(2.0)
        area = [[0, 0], [s, -s], [0, -2 * s], [-s, -s]]
        origin, u, n, width, depth = make_room(area=area).frame()
        self.assertAlmostEqual(width, 2000.0)
        self.assertAlmostEqual(depth, 2000.0)
        np.testing.assert_allclose(u, [np.sqrt(0.5), -np.sqrt(0.5)])

    def test_minimum_depth(self):
        _, _, _, _, depth = make_room(area=[[0, 0], [4000, 0], [2000, -100]]).frame()
        self.assertAlmostEqual(depth, MIN_SIZE_MM)


class EditTest(unittest.TestCase):
    def test_insert_corner_keeps_the_base_line(self):
        room = make_room(base=2)                                        # far edge (4000,-3000) -> (0,-3000)
        room.insert_corner(0, [2000, 100])                              # into edge 0: corners after it move
        self.assertEqual(len(room.area), 5)
        self.assertEqual(room.base, 3)
        self.assertEqual(room.area[room.base], [4000.0, -3000.0])
        room.insert_corner(3, [2000, -3100])                            # into the base line itself
        self.assertEqual(room.base, 3)                                  # starts at the same corner

    def test_delete_corner(self):
        room = make_room(base=2)
        self.assertTrue(room.delete_corner(0))
        self.assertEqual(room.base, 1)
        self.assertEqual(room.area[room.base], [4000.0, -3000.0])
        self.assertFalse(room.delete_corner(0))                         # three corners remain

    def test_delete_the_corner_that_starts_the_base_line(self):
        room = make_room(base=0)
        room.delete_corner(0)
        self.assertEqual(room.base, 2)                                  # the edge from (0,-3000) to (4000,0)
        self.assertEqual(room.area[2], [0.0, -3000.0])

    def test_nearest_edge_and_corner(self):
        self.assertEqual(nearest_edge(RECT, (2000, 100))[0], 0)
        self.assertEqual(nearest_edge(RECT, (4100, -1500))[0], 1)
        k, d = nearest_corner(RECT, (3900, -3100))
        self.assertEqual(k, 2)
        self.assertAlmostEqual(d, np.hypot(100, 100))


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
        room = make_room(base=1)
        room.exclusions = [[[1, 2], [3, 4], [5, 6]]]
        room.barriers = [[[0, 0], [1, 1]]]
        room.save()
        back = Room.load(room.folder)
        self.assertEqual(back.area, RECT)
        self.assertEqual(back.base, 1)
        self.assertEqual(back.exclusions, room.exclusions)
        self.assertEqual(back.barriers, room.barriers)
        self.assertEqual([s.sid for s in back.stations], ['A'])
        with open(os.path.join(room.folder, 'room.json')) as f:
            self.assertNotIn('front', json.load(f))

    def test_front_of_older_rooms_becomes_the_base_line(self):
        folder = tempfile.mkdtemp()
        area = [[-1000, -3000], [-1000, 0], [4500, 100], [4000, -3000]]     # the front lies along edge 1
        with open(os.path.join(folder, 'room.json'), 'w') as f:
            json.dump({'stations': [], 'area': area, 'front': FRONT, 'exclusions': [], 'barriers': []}, f)
        room = Room.load(folder)
        self.assertEqual(room.base, 1)
        self.assertTrue(room.ready())

    def test_legacy_screen_and_trapezoid_become_area_and_base_line(self):
        folder = tempfile.mkdtemp()
        old = {'stations': [Station('A', 'Position A').to_dict()], 'screen': FRONT,
               'depth_l_mm': 2000.0, 'depth_r_mm': 4000.0,
               'areas': [{'kind': 'exclude', 'pts': [[1, 1], [2, 2], [3, 1]]}], 'barriers': [], 'ref': None}
        with open(os.path.join(folder, 'room.json'), 'w') as f:
            json.dump(old, f)
        room = Room.load(folder)
        np.testing.assert_allclose(room.area, [[0, 0], [4000, 0], [4000, -4000], [0, -2000]])
        self.assertEqual(room.base, 0)
        self.assertEqual(room.exclusions, [[[1, 1], [2, 2], [3, 1]]])
        self.assertAlmostEqual(room.frame()[4], 4000.0)
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
        self.assertAlmostEqual(room.frame()[4], 2500.0)
        self.assertEqual(Room.names(rooms), ['Default'])


if __name__ == '__main__':
    unittest.main()
