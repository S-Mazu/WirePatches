"""Simulated objects that walk between two points."""
import math
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from simpath import parse_path, path_position  # noqa: E402


class PathTest(unittest.TestCase):
    A, B = (0.0, 0.0), (2000.0, 0.0)        # 2 m; at 1 m/s 2 s of walking

    def at(self, t, speed=1000.0, pause=1.0):
        return path_position(self.A, self.B, speed, pause, t)

    def test_cycle(self):
        # pause 1 s at A, walk 2 s, pause 1 s at B, walk back 2 s: period 6 s
        self.assertEqual(self.at(0.5), (0.0, 0.0))
        self.assertAlmostEqual(self.at(2.0)[0], 1000.0)
        self.assertEqual(self.at(3.5), (2000.0, 0.0))
        self.assertAlmostEqual(self.at(5.0)[0], 1000.0)
        self.assertAlmostEqual(self.at(6.5)[0], 0.0)        # next period, at A again

    def test_continuous_at_the_ends(self):
        for t in (1.0, 3.0, 4.0, 6.0):
            self.assertAlmostEqual(self.at(t - 1e-6)[0], self.at(t + 1e-6)[0], places=2)

    def test_degenerate_input_stays_at_a(self):
        for speed, pause, t in ((0.0, 1.0, 3.0), (-1.0, 1.0, 3.0), (math.nan, 1.0, 3.0), (1000.0, 1.0, math.nan),
                                (1000.0, math.inf, 3.0)):
            x, y = path_position(self.A, self.B, speed, pause, t)
            self.assertTrue(math.isfinite(x) and math.isfinite(y))
        self.assertEqual(path_position(self.A, self.A, 1000.0, 1.0, 3.0), (0.0, 0.0))
        self.assertAlmostEqual(self.at(3.0, pause=0.0)[0], 1000.0)   # no pause: ping-pong

    def test_parse(self):
        self.assertEqual(parse_path('0.2,0.3, 0.8,0.3'), ((0.2, 0.3), (0.8, 0.3)))
        for bad in ('1,2,3', 'a,b,c,d', '0,0,nan,1'):
            with self.assertRaises(ValueError):
                parse_path(bad)


if __name__ == '__main__':
    unittest.main()
