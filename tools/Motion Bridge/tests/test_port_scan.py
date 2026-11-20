"""Port scan: recognising sensor packets, probing a port, scanning several ports. No serial hardware."""
import os
import struct
import sys
import time
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import port_scan  # noqa: E402
from port_scan import PortInfo, Scanner, count_packets, device_of, probe  # noqa: E402
from stl27l import crc8  # noqa: E402


def packet(speed=3600, start=0):
    body = bytes([0x54, 0x2C]) + struct.pack('<HH', speed, start) + bytes(36) + struct.pack('<HH', start + 300, 0)
    return body + bytes([crc8(body)])


class FakeSerial:
    """Stands in for serial.Serial: returns `data` in chunks, records what was set."""

    def __init__(self, data=b'', fail_open=False):
        self.data, self.fail_open = data, fail_open
        self.port = self.baudrate = self.timeout = None
        self.dtr = self.rts = True
        self.opened = self.closed = False

    def __call__(self):
        return self

    def open(self):
        if self.fail_open:
            import serial
            raise serial.SerialException('could not open port COM9: access denied')
        self.opened = True

    def read(self, n):
        chunk, self.data = self.data[:n], self.data[n:]
        if not chunk:
            time.sleep(0.01)
        return chunk

    def close(self):
        self.closed = True


class CountPackets(unittest.TestCase):
    def test_valid_packets_in_noise(self):
        data = b'\x01\x54\x2c\x00' + packet() * 20 + b'\xff' * 10
        good, hz = count_packets(data)
        self.assertEqual(good, 20)
        self.assertAlmostEqual(hz, 10.0)

    def test_bad_crc_and_noise(self):
        bad = bytearray(packet())
        bad[-1] ^= 0xFF
        self.assertEqual(count_packets(bytes(bad) * 10)[0], 0)
        self.assertEqual(count_packets(os.urandom(4000))[0], 0)


class Probe(unittest.TestCase):
    def test_sensor_port(self):
        fake = FakeSerial(packet() * 30)
        good, hz, err = probe('COM5', listen_s=0.1, opener=fake)
        self.assertEqual((good, err), (30, ''))
        self.assertFalse(fake.dtr or fake.rts)             # does not reset devices
        self.assertTrue(fake.closed)

    def test_port_that_cannot_be_opened(self):
        good, hz, err = probe('COM9', listen_s=0.1, opener=FakeSerial(fail_open=True))
        self.assertEqual(good, 0)
        self.assertEqual(err, 'could not open port COM9')


class Scan(unittest.TestCase):
    def test_finds_sensor_skips_port_in_use_gives_up_on_hang(self):
        saved = port_scan.OPEN_TIMEOUT_S
        port_scan.OPEN_TIMEOUT_S = 0.2
        try:
            def prober(dev, listen_s):
                if dev == 'COM3':
                    time.sleep(5)                         # hangs like a Bluetooth port
                if dev == 'COM19':
                    raise AssertionError('the port in use must not be opened')
                return (200, 10.0, '') if dev == 'COM7' else (0, 0.0, '')

            ports = [PortInfo(d) for d in ('COM3', 'COM4', 'COM7', 'COM19')]
            sc = Scanner(ports, in_use={'COM19': True}, listen_s=0.05, prober=prober)
            sc.start()
            sc.join(3)
            self.assertTrue(sc.done)
            states = {p.device: p.state for p in ports}
            self.assertEqual(states, {'COM3': 'no answer', 'COM4': 'none', 'COM7': 'sensor', 'COM19': 'in use'})
            self.assertEqual([p.device for p in sc.found], ['COM7', 'COM19'])
        finally:
            port_scan.OPEN_TIMEOUT_S = saved

    def test_labels_start_with_the_device(self):
        p = PortInfo('COM19', 'Silicon Labs CP210x USB to UART Bridge (COM19)')
        self.assertEqual(device_of(p.label()), 'COM19')
        p.state, p.hz = 'sensor', 10.0
        self.assertEqual(p.label(), 'COM19  STL-27L, 10 turns/s')
        self.assertEqual(device_of(p.label()), 'COM19')


if __name__ == '__main__':
    unittest.main()
