"""Reader for the LDROBOT STL-27L 2D lidar (also sold as WayPonDEV FHL-LD19 Plus).

The sensor streams 47-byte packets at 921600 baud from power-on and takes no commands.
Packet: 0x54 0x2C, speed (deg/s), start angle (0.01 deg), 12 x (distance mm, intensity),
end angle (0.01 deg), timestamp (ms), CRC8 (polynomial 0x4D).
Angles: 0 = sensor front, increasing clockwise.
"""
import struct
import threading
import time
from collections import deque

import numpy as np
import serial

BAUD = 921600
PKT_LEN = 47
POINTS_PER_PKT = 12


def _crc_table(poly=0x4D):
    table = []
    for i in range(256):
        c = i
        for _ in range(8):
            c = ((c << 1) ^ poly) & 0xFF if c & 0x80 else (c << 1) & 0xFF
        table.append(c)
    return bytes(table)


CRC_TABLE = _crc_table()
_POINTS = struct.Struct('<' + 'HB' * POINTS_PER_PKT)


def crc8(data):
    c = 0
    for b in data:
        c = CRC_TABLE[c ^ b]
    return c


class Revolution:
    """One full turn of the sensor."""

    __slots__ = ('t', 'angle', 'dist', 'intensity', 'hz')

    def __init__(self, t, angle, dist, intensity, hz):
        self.t = t                  # time.time() when the turn was complete
        self.angle = angle          # float32 degrees, 0 = front, clockwise
        self.dist = dist            # float32 mm, 0 = no return
        self.intensity = intensity  # uint8
        self.hz = hz                # rotation speed reported by the sensor


class Stl27lReader(threading.Thread):
    """Reads the serial port in the background and collects full revolutions.

    Reconnects on its own when the port disappears (cable pulled, adapter reset).
    """

    def __init__(self, port, baud=BAUD, keep=200):
        super().__init__(daemon=True)
        self.port, self.baud = port, baud
        self.revs = deque(maxlen=keep)
        self.lock = threading.Lock()
        self.running = True
        self.connected = False
        self.last_error = ''
        self.packets = 0
        self.crc_bad = 0
        self.bytes = 0
        self.rev_count = 0
        self.raw_sink = None        # open binary file: raw serial bytes are written to it
        self._a, self._d, self._i, self._spd = [], [], [], []
        self._last_angle = None

    def stop(self):
        self.running = False

    def latest(self, n=1):
        with self.lock:
            return list(self.revs)[-n:]

    def run(self):
        while self.running:
            try:
                with serial.Serial(self.port, self.baud, timeout=0.1) as s:
                    self.connected, self.last_error = True, ''
                    s.reset_input_buffer()
                    buf = bytearray()
                    while self.running:
                        chunk = s.read(8192)
                        if not chunk:
                            continue
                        self.bytes += len(chunk)
                        if self.raw_sink:
                            self.raw_sink.write(chunk)
                        buf += chunk
                        buf = self._parse(buf)
            except (serial.SerialException, OSError) as e:
                self.connected, self.last_error = False, str(e)
                time.sleep(1.0)

    def _parse(self, buf):
        i, n = 0, len(buf)
        while n - i >= PKT_LEN:
            if buf[i] == 0x54 and buf[i + 1] == 0x2C:
                pkt = bytes(buf[i:i + PKT_LEN])
                if crc8(pkt[:46]) == pkt[46]:
                    self._on_packet(pkt)
                    i += PKT_LEN
                    continue
                self.crc_bad += 1
            i += 1
        return buf[i:]

    def _on_packet(self, pkt):
        self.packets += 1
        speed, start = struct.unpack_from('<HH', pkt, 2)
        end = struct.unpack_from('<H', pkt, 42)[0]
        start, end = start / 100.0, end / 100.0
        step = ((end - start) % 360.0) / (POINTS_PER_PKT - 1)
        vals = _POINTS.unpack_from(pkt, 6)
        self._spd.append(speed)
        for k in range(POINTS_PER_PKT):
            a = (start + step * k) % 360.0
            if self._last_angle is not None and a < self._last_angle - 180.0:
                self._emit()
            self._last_angle = a
            self._a.append(a)
            self._d.append(vals[2 * k])
            self._i.append(vals[2 * k + 1])

    def _emit(self):
        if len(self._a) > 100:
            rev = Revolution(time.time(), np.asarray(self._a, np.float32), np.asarray(self._d, np.float32),
                             np.asarray(self._i, np.uint8), float(np.median(self._spd)) / 360.0)
            with self.lock:
                self.revs.append(rev)
            self.rev_count += 1
        self._a, self._d, self._i, self._spd = [], [], [], []
