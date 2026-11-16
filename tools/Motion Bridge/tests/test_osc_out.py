"""OSC output: the messages of one update, received and parsed by a local UDP socket."""
import os
import socket
import sys
import unittest

import numpy as np
from pythonosc.osc_message import OscMessage

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from osc_out import OscOut, parse_target, port_listening  # noqa: E402
from tracking import SLOTS  # noqa: E402


class OscOutTest(unittest.TestCase):
    def setUp(self):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.bind(('127.0.0.1', 0))
        self.sock.settimeout(1.0)
        self.target = f'127.0.0.1:{self.sock.getsockname()[1]}'

    def tearDown(self):
        self.sock.close()
        if hasattr(self, 'osc'):
            self.osc.close()

    def receive(self, n):
        out = {}
        for _ in range(n):
            msg = OscMessage(self.sock.recv(65536))
            out[msg.address] = msg.params
        return out

    def test_one_update_points_fields_heartbeat(self):
        osc = self.osc = OscOut([self.target])
        self.assertTrue(osc.listening[self.target])
        x = np.linspace(0, 1, SLOTS)
        occ = np.zeros((16, 32), np.float32)
        occ[4, 8] = 0.8
        motion = np.zeros((16, 32), np.float32)
        motion[4, 8] = 1.0
        osc.send(x, np.zeros(SLOTS), np.array([1.0] + [0.0] * (SLOTS - 1)), occ, motion)
        got = self.receive(4 + 1 + 2 * 16 + 1)
        self.assertEqual(len(got['/lidar/x']), SLOTS)
        self.assertAlmostEqual(got['/lidar/x'][SLOTS - 1], 1.0)
        self.assertEqual(got['/lidar/count'], [1.0])
        self.assertEqual(got['/lidar/field/size'], [32.0, 16.0])
        self.assertEqual(len(got['/lidar/occupancy/0']), 32)
        self.assertAlmostEqual(got['/lidar/occupancy/4'][8], 0.8, places=5)
        self.assertAlmostEqual(got['/lidar/motion/4'][8], 1.0)
        self.assertEqual(got['/lidar/motion/15'], [0.0] * 32)
        self.assertEqual(got['/lidar/alive'], [0.0])
        osc.send(x, np.zeros(SLOTS), np.zeros(SLOTS))                 # points only, no fields
        got = self.receive(5)
        self.assertEqual(got['/lidar/alive'], [1.0])
        self.assertNotIn('/lidar/field/size', got)

    def test_targets_and_listening(self):
        osc = self.osc = OscOut([])
        self.assertEqual(osc.targets, [])
        osc.set_targets([self.target, '192.168.1.20:7000'])
        self.assertTrue(osc.listening[self.target])
        self.assertIsNone(osc.listening['192.168.1.20:7000'])          # remote: cannot be checked
        with self.assertRaises(ValueError):
            parse_target('nonsense')
        self.assertEqual(parse_target('127.0.0.1:7000'), ('127.0.0.1', 7000))

    def test_port_listening(self):
        self.assertTrue(port_listening('127.0.0.1', self.sock.getsockname()[1]))
        self.assertIsNone(port_listening('10.1.1.1', 7000))


if __name__ == '__main__':
    unittest.main()
