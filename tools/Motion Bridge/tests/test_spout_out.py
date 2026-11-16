"""Spout output without SpoutGL: the bridge runs, says so, and sends nothing."""
import os
import sys
import unittest

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import spout_out  # noqa: E402
from spout_out import SpoutOut  # noqa: E402


class SpoutOutTest(unittest.TestCase):
    def test_without_spoutgl(self):
        if spout_out.AVAILABLE:
            self.skipTest('SpoutGL is installed: the real sender is not tested here')
        out = SpoutOut()
        self.assertFalse(out.sending)
        self.assertIn('SpoutGL not installed', out.status)
        out.send(np.zeros((9, 8, 4), np.uint8))
        self.assertEqual(out.size, (8, 9))
        self.assertEqual(out.frames, 0)
        out.close()


if __name__ == '__main__':
    unittest.main()
