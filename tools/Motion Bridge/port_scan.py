"""Finds STL-27L sensors among the serial ports by listening to each one briefly.

The sensor streams packets from power-on (stl27l.py), so a port with a sensor delivers valid packets
(header 0x54 0x2C, correct CRC) within a fraction of a second. The scan only reads: it opens a port
with DTR and RTS off (so devices such as an Arduino are not reset), listens, and closes it again.
Ports used by the bridge itself are not opened; their reader reports instead.

Opening a port can hang (Bluetooth serial ports); each probe runs in its own thread and is given up
after a timeout.
"""
import logging
import statistics
import struct
import threading
import time

import serial

from stl27l import BAUD, PKT_LEN, crc8

log = logging.getLogger(__name__)

LISTEN_S = 0.5          # listening time per port
MIN_PACKETS = 5         # valid packets needed to call it a sensor (about 450 arrive in 0.5 s)
OPEN_TIMEOUT_S = 2.0    # a probe that has not finished after LISTEN_S + this is given up


def count_packets(data: bytes) -> tuple:
    """(valid packets, rotation in turns per second or 0) found in raw bytes."""
    i, n, good, speeds = 0, len(data), 0, []
    while n - i >= PKT_LEN:
        if data[i] == 0x54 and data[i + 1] == 0x2C and crc8(data[i:i + PKT_LEN - 1]) == data[i + PKT_LEN - 1]:
            good += 1
            speeds.append(struct.unpack_from('<H', data, i + 2)[0])
            i += PKT_LEN
        else:
            i += 1
    hz = statistics.median(speeds) / 360.0 if speeds else 0.0
    return good, hz


def probe(port: str, listen_s: float = LISTEN_S, opener=serial.Serial) -> tuple:
    """Listens on one port. Returns (valid packets, turns per second, error text or '')."""
    try:
        s = opener()
        s.port, s.baudrate, s.timeout = port, BAUD, 0.05
        s.dtr, s.rts = False, False
        s.open()
    except (serial.SerialException, OSError, ValueError) as e:
        return 0, 0.0, str(e).split(':')[0] or 'cannot open'
    data = bytearray()
    try:
        end = time.monotonic() + listen_s
        while time.monotonic() < end:
            data += s.read(4096)
    except (serial.SerialException, OSError) as e:
        return 0, 0.0, str(e).split(':')[0] or 'read failed'
    finally:
        try:
            s.close()
        except Exception:
            pass
    good, hz = count_packets(bytes(data))
    return good, hz, ''


class PortInfo:
    """One serial port and what the scan found on it."""

    __slots__ = ('device', 'description', 'state', 'packets', 'hz')

    def __init__(self, device, description=''):
        self.device, self.description = device, description
        self.state = 'unknown'      # sensor, in use, none, error, no answer, unknown
        self.packets, self.hz = 0, 0.0

    @property
    def sensor(self) -> bool:
        return self.state in ('sensor', 'in use')

    def label(self) -> str:
        """Text for the port list; it always starts with the device name."""
        if self.state == 'sensor':
            return f'{self.device}  STL-27L, {self.hz:.0f} turns/s'
        if self.state == 'in use':
            return f'{self.device}  STL-27L, in use by the bridge'
        return f'{self.device}  {self.description}'.rstrip()


def device_of(label: str) -> str:
    """The device name at the start of a port list entry."""
    return label.split(' ', 1)[0] if label else ''


def list_ports() -> list:
    """All serial ports Windows reports, with their description."""
    try:
        from serial.tools import list_ports as lp
        return [PortInfo(p.device, p.description if p.description and p.description != 'n/a' else '')
                for p in sorted(lp.comports(), key=lambda p: p.device)]
    except Exception as e:
        log.warning('cannot list serial ports: %s', e)
        return []


class Scanner(threading.Thread):
    """Probes the given ports in the background, all at the same time.

    `in_use` maps ports the bridge reads itself to True when their reader receives packets;
    they are not opened. Progress is read from `current`, `done` and `ports`.
    """

    def __init__(self, ports, in_use=None, listen_s=LISTEN_S, prober=probe):
        super().__init__(daemon=True)
        self.ports = ports
        self.in_use = in_use or {}
        self.listen_s, self.prober = listen_s, prober
        self.current, self.index = '', 0
        self.done = False

    def run(self):
        """All ports at once: a port that hangs when opened then costs no extra time."""
        results, threads = {}, []
        for p in self.ports:
            if p.device in self.in_use:
                p.state = 'in use' if self.in_use[p.device] else 'none'
                continue
            t = threading.Thread(target=lambda d=p.device: results.__setitem__(d, self.prober(d, self.listen_s)),
                                 daemon=True)
            t.start()
            threads.append(t)
        self.current, self.index = f'{len(threads)} ports', len(threads)
        deadline = time.monotonic() + self.listen_s + OPEN_TIMEOUT_S
        for t in threads:
            t.join(max(deadline - time.monotonic(), 0.0))
        for p in self.ports:
            if p.device in self.in_use:
                continue
            if p.device not in results:
                p.state = 'no answer'
                log.info('port scan: %s did not answer', p.device)
                continue
            p.packets, p.hz, err = results[p.device]
            p.state = 'error' if err else ('sensor' if p.packets >= MIN_PACKETS else 'none')
            log.info('port scan: %s %s (%d packets%s)', p.device, p.state, p.packets, f', {err}' if err else '')
        self.current = ''
        self.done = True

    @property
    def found(self) -> list:
        return [p for p in self.ports if p.sensor]
