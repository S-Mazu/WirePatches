"""OSC output: targets, a check whether something listens, and the messages per update.

The interface to the patches is documented in README.md under Output.
"""
import logging
import socket
import time

import numpy as np
from pythonosc.udp_client import SimpleUDPClient

log = logging.getLogger(__name__)

ALIVE_MOD = 1000            # /lidar/alive counts 0..999


def port_listening(host: str, port: int):
    """True if a local program holds the UDP port, False if free, None for remote hosts."""
    if host not in ('127.0.0.1', 'localhost'):
        return None
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.bind(('', port))
        return False
    except OSError:
        return True
    finally:
        s.close()


def parse_target(target: str):
    """'host:port' -> (host, port); raises ValueError."""
    host, _, port = target.rpartition(':')
    if not host or not port.isdigit():
        raise ValueError(f'target must be host:port, not {target!r}')
    return host, int(port)


class OscOut:
    def __init__(self, targets=()):
        self.targets: list[str] = []
        self.clients: list = []
        self.listening: dict = {}           # target -> True / False / None (remote)
        self.sent = 0
        self._rate = 0.0
        self._rate_n, self._rate_t = 0, time.time()
        self._failed: set = set()           # targets whose last send failed (logged once)
        self.set_targets(targets)

    def set_targets(self, targets) -> None:
        clients = []
        for t in targets:
            host, port = parse_target(t)
            clients.append((t, SimpleUDPClient(host, port)))
        self.close()
        self.targets, self.clients = list(targets), clients
        self.check()

    def check(self) -> None:
        """Refreshes `listening` (a bind test per local target)."""
        self.listening = {t: port_listening(*parse_target(t)) for t in self.targets}

    def send(self, x, depth, active, occ=None, motion=None) -> None:
        """One update: points (8 slots), the fields row by row, and the heartbeat."""
        alive = float(self.sent % ALIVE_MOD)
        for t, c in self.clients:
            try:
                c.send_message('/lidar/x', [float(v) for v in x])
                c.send_message('/lidar/depth', [float(v) for v in depth])
                c.send_message('/lidar/active', [float(v) for v in active])
                c.send_message('/lidar/count', float((np.asarray(active) > 0).sum()))
                if occ is not None:
                    rows, cols = occ.shape
                    c.send_message('/lidar/field/size', [float(cols), float(rows)])
                    for r in range(rows):
                        c.send_message(f'/lidar/occupancy/{r}', [float(v) for v in occ[r]])
                        c.send_message(f'/lidar/motion/{r}', [float(v) for v in motion[r]])
                c.send_message('/lidar/alive', alive)
                if t in self._failed:
                    self._failed.discard(t)
                    log.info('sending to %s works again', t)
            except OSError as e:
                if t not in self._failed:
                    self._failed.add(t)
                    log.warning('cannot send to %s: %s', t, e)
        self.sent += 1
        self._rate_n += 1
        now = time.time()
        if now - self._rate_t >= 1.0:
            self._rate = self._rate_n / (now - self._rate_t)
            self._rate_n, self._rate_t = 0, now

    def rate(self) -> float:
        """Updates per second; 0 when sending has stopped."""
        return self._rate if time.time() - self._rate_t < 2.5 else 0.0

    def close(self) -> None:
        for _, c in self.clients:
            c._sock.close()
        self.clients = []
