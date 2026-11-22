"""Foreground against the empty room: points at the edges of fixed objects do not flicker."""
import os
import sys
import unittest

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from scene import BINS, Background, edge_reference, foreground  # noqa: E402
from stl27l import Revolution  # noqa: E402


def empty_room():
    """Walls at 3 m all around, a fixed object at 1 m in bins 100 to 110 (5 degrees), no return in bins 300 to 309."""
    bg = Background('')
    bg.dist = np.full(BINS, 3000.0, np.float32)
    bg.dist[100:111] = 1000.0
    bg.dist[300:310] = np.nan
    bg.noise = np.full(BINS, 5.0, np.float32)
    bg.noise[300:310] = np.nan
    return bg


def turn(points):
    """A revolution with one point per given (bin, distance)."""
    b = np.array([p[0] for p in points], float)
    angle = ((b + 0.5) * 360.0 / BINS).astype(np.float32)
    dist = np.array([p[1] for p in points], np.float32)
    return Revolution(0.0, angle, dist, np.zeros(len(points), np.uint8), 10.0)


class EdgeTest(unittest.TestCase):
    def test_edge_reference_takes_the_nearest_around(self):
        near, noise = edge_reference(empty_room(), width=2)
        self.assertEqual(float(near[98]), 1000.0)                    # two bins before the object
        self.assertEqual(float(near[97]), 3000.0)
        self.assertTrue(np.isnan(near[305]))                         # no return around it either
        self.assertEqual(float(near[299]), 3000.0)

    def test_points_at_an_object_edge_are_not_foreground(self):
        bg = empty_room()
        rev = turn([(99, 1000.0),          # the object, seen one bin early: edge flicker
                    (99, 2000.0),          # a mixed point between object and wall
                    (50, 2000.0),          # something new in front of the wall
                    (105, 600.0),          # something new in front of the object
                    (305, 4000.0),         # in a direction without return in the empty room
                    (305, 9000.0)])
        np.testing.assert_array_equal(foreground(rev, bg, 100.0), [False, False, True, True, True, False])

    def test_cache_follows_a_new_capture(self):
        bg = empty_room()
        bg.edges()
        bg.dist = np.full(BINS, 2000.0, np.float32)
        bg._edges = None                                              # as learn() and load() do
        self.assertEqual(float(bg.edges()[0][100]), 2000.0)


if __name__ == '__main__':
    unittest.main()
