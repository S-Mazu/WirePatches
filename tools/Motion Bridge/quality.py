"""Scan quality: per-revolution statistics for the status lines and an optional CSV log."""
import csv
import logging
import os
import statistics
import time

from scene import MIN_MM

log = logging.getLogger(__name__)

KEEP = 36000                # revolutions kept in memory (about one hour)
TRIM = 6000                 # dropped at once when KEEP is reached


class Quality:
    def __init__(self, log_dir: str):
        self.log_dir = log_dir
        self.reset()
        self.log_f = None
        self.log = None
        self.log_path = None

    def reset(self) -> None:
        self.t0 = time.time()
        self.revs = 0
        self.hz = []
        self.valid = []
        self.points = []
        self.crc = []
        self.last_t = 0.0

    def add(self, rev, n_objects: int, crc_bad: int) -> None:
        v = 100.0 * float((rev.dist >= MIN_MM).mean())
        self.revs += 1
        self.hz.append(rev.hz)
        self.valid.append(v)
        self.points.append(len(rev.dist))
        self.crc.append(crc_bad)
        self.last_t = time.time()
        if len(self.hz) > KEEP:
            del self.hz[:TRIM], self.valid[:TRIM], self.points[:TRIM], self.crc[:TRIM]
        if self.log:
            self.log.writerow([f'{rev.t:.3f}', f'{rev.hz:.3f}', len(rev.dist), f'{v:.1f}', crc_bad, n_objects])

    def set_logging(self, on: bool) -> None:
        if on and not self.log:
            os.makedirs(self.log_dir, exist_ok=True)
            self.log_path = os.path.join(self.log_dir, f'quality_{time.strftime("%Y%m%d_%H%M%S")}.csv')
            self.log_f = open(self.log_path, 'w', newline='')
            self.log = csv.writer(self.log_f)
            self.log.writerow(['t', 'hz', 'points', 'valid_pct', 'crc_bad_total', 'objects'])
            log.info('quality log: %s', self.log_path)
        elif not on and self.log:
            self.log_f.close()
            self.log_f = self.log = None

    def summary(self) -> dict:
        """The whole run since the start."""
        if not self.hz:
            return {}
        return {'minutes': (time.time() - self.t0) / 60.0, 'revs': self.revs,
                'hz_mean': statistics.fmean(self.hz), 'hz_std': statistics.pstdev(self.hz),
                'valid_mean': statistics.fmean(self.valid), 'valid_min': min(self.valid),
                'points_mean': statistics.fmean(self.points)}

    def recent(self, n: int = 50) -> dict:
        """Means over the last n revolutions (about 5 s), new CRC errors, seconds since the last one."""
        if not self.hz:
            return {}
        return {'hz': statistics.fmean(self.hz[-n:]), 'valid': statistics.fmean(self.valid[-n:]),
                'points': statistics.fmean(self.points[-n:]), 'crc_new': self.crc[-1] - self.crc[-n:][0],
                'age': time.time() - self.last_t}
