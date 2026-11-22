"""Drawing shapes with clicks."""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from room import Room  # noqa: E402
from ui.drafting import Draft, toggle_corner  # noqa: E402


class DraftTest(unittest.TestCase):
    def setUp(self):
        self.room = Room('', 'T')
        self.d = Draft()

    def draw_rect(self):
        self.d.start('area')
        for p in ((0, 0), (4000, 0), (4000, -3000), (0, -3000)):
            self.d.click(*p)
        self.d.apply(self.room)

    def test_new_area_takes_its_first_edge_as_base_line(self):
        self.draw_rect()
        self.assertEqual(self.room.base, 0)
        self.assertTrue(self.room.ready())

    def test_choose_base_line_by_clicking_an_edge(self):
        self.draw_rect()
        self.d.start('base')
        self.assertTrue(self.d.click(4100, -1500))                     # one click completes it
        self.assertTrue(self.d.apply(self.room))
        self.assertEqual(self.room.base, 1)
        self.d.start('base')
        self.d.click(2000, -1500)                                       # middle of the area: no edge near
        self.assertFalse(self.d.apply(self.room))
        self.assertIn('edge', self.d.error)
        self.assertEqual(self.room.base, 1)

    def test_double_click_adds_and_deletes_corners(self):
        self.draw_rect()
        self.room.exclusions = [[[1000, -1000], [2000, -1000], [2000, -2000], [1000, -2000]]]
        self.assertEqual(toggle_corner(self.room, (2000, 50), 200.0), (True, ''))      # on the area's first edge
        self.assertEqual(self.room.area[1], [2000.0, 50.0])
        self.assertEqual(self.room.base, 0)
        toggle_corner(self.room, (1500, -1020), 200.0)                                 # on the exclusion's edge
        self.assertEqual(len(self.room.exclusions[0]), 5)
        toggle_corner(self.room, (2010, 40), 200.0)                                    # on the new corner: gone
        self.assertEqual(len(self.room.area), 4)
        self.assertEqual(toggle_corner(self.room, (3000, -2500), 200.0), (False, ''))  # nothing near
        tri = [[0, 0], [1000, 0], [0, -1000]]
        self.room.exclusions = [tri]
        changed, why = toggle_corner(self.room, (1000, 0), 200.0)
        self.assertFalse(changed)
        self.assertIn('three corners', why)

    def test_barrier_and_reference(self):
        self.d.start('barrier')
        self.d.click(0, 0)
        self.assertTrue(self.d.click(0, 2000))
        self.d.apply(self.room)
        self.assertEqual(self.room.barriers, [[[0.0, 0.0], [0.0, 2000.0]]])
        self.d.start('ref')
        self.assertTrue(self.d.click(500, 500))
        self.d.apply(self.room)
        self.assertEqual(self.room.ref, [500.0, 500.0])

    def test_cancel_and_clicks_without_a_mode(self):
        self.assertFalse(self.d.click(1, 1))
        self.d.start('area')
        self.d.click(0, 0)
        self.d.cancel()
        self.assertFalse(self.d.active)
        self.assertEqual(self.d.points, [])
        self.assertEqual(self.d.label, '')


if __name__ == '__main__':
    unittest.main()
