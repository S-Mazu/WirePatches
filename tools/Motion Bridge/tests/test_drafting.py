"""Drawing shapes with clicks."""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from room import Room  # noqa: E402
from ui.drafting import Draft  # noqa: E402


class DraftTest(unittest.TestCase):
    def setUp(self):
        self.room = Room('', 'T')
        self.d = Draft()

    def test_front_completes_with_two_clicks(self):
        self.d.start('front')
        self.assertFalse(self.d.click(0, 0))
        self.assertTrue(self.d.click(4000, 0))
        self.assertTrue(self.d.apply(self.room))
        self.assertEqual(self.room.front, {'left': [0.0, 0.0], 'right': [4000.0, 0.0]})
        self.assertFalse(self.d.active)

    def test_area_closes_on_the_first_corner_or_finish(self):
        self.d.start('area')
        for p in ((0, 0), (4000, 0), (4000, -3000)):
            self.assertFalse(self.d.click(*p))
        self.assertFalse(self.d.click(100, -2900))                     # fourth corner, not near the first
        self.assertTrue(self.d.click(100, 100))                        # near the first corner: closed
        self.assertTrue(self.d.apply(self.room))
        self.assertEqual(len(self.room.area), 4)
        self.d.start('exclude')
        self.d.click(0, 0)
        self.d.click(1, 0)
        self.assertFalse(self.d.apply(self.room))                      # two corners are no polygon
        self.assertEqual(self.room.exclusions, [])
        self.d.start('exclude')
        for p in ((0, 0), (1000, 0), (0, 1000)):
            self.d.click(*p)
        self.assertTrue(self.d.complete())
        self.assertTrue(self.d.apply(self.room))                       # Finish button
        self.assertEqual(len(self.room.exclusions), 1)

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
