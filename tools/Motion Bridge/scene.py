"""Scene analysis for the STL-27L: empty-room background, foreground points and blobs.

Coordinates: sensor at the origin, x to the right, y to the front of the sensor, millimetres.
"""
import math
import os
import time

import numpy as np

BINS = 720                  # 0.5 degree bins for the background
MIN_MM = 120                # ignore anything closer (mount, cables, a hand on the sensor)
FAR_MM = 8000               # where the empty room had no return, closer points count as foreground


def bin_of(angle):
    return (angle * (BINS / 360.0)).astype(np.int32) % BINS


def to_xy(angle, dist):
    a = np.radians(angle)                       # 0 = sensor front, clockwise
    return dist * np.sin(a), dist * np.cos(a)   # x right, y front (mm)


class Background:
    """Median distance and noise of the empty room per 0.5 degree bin."""

    def __init__(self, path):
        self.path = path
        self.dist = np.full(BINS, np.nan, np.float32)   # nan = no return in that direction
        self.noise = np.full(BINS, np.nan, np.float32)  # robust standard deviation
        self.learned_at = None                          # time.time() of the capture

    @property
    def ok(self):
        return bool(np.isfinite(self.dist).any())

    def learn(self, revs):
        a = np.concatenate([r.angle for r in revs])
        d = np.concatenate([r.dist for r in revs])
        b = bin_of(a)
        expected = len(a) / BINS
        dist = np.full(BINS, np.nan, np.float32)
        noise = np.full(BINS, np.nan, np.float32)
        for k in range(BINS):
            v = d[(b == k) & (d >= MIN_MM)]
            if len(v) >= 0.5 * expected:
                med = np.median(v)
                dist[k] = med
                noise[k] = 1.4826 * np.median(np.abs(v - med))
        self.dist, self.noise, self.learned_at = dist, noise, time.time()

    def save(self):
        np.save(self.path, np.stack([self.dist, self.noise]))

    def load(self):
        if os.path.exists(self.path):
            self.dist, self.noise = np.load(self.path).astype(np.float32)
            self.learned_at = os.path.getmtime(self.path)

    def stats(self):
        """Coverage in %, median noise and 95th percentile noise in mm."""
        n = self.noise[np.isfinite(self.noise)]
        cover = 100.0 * np.isfinite(self.dist).mean()
        if not len(n):
            return cover, float('nan'), float('nan')
        return cover, float(np.median(n)), float(np.percentile(n, 95))

    def xy(self):
        ang = (np.arange(BINS) + 0.5) * (360.0 / BINS)
        ok = np.isfinite(self.dist)
        return to_xy(ang[ok], self.dist[ok])


def foreground(rev, bg, tol_mm):
    """True for points that are clearly closer than the empty room."""
    d, b = rev.dist, bin_of(rev.angle)
    valid = d >= MIN_MM
    if not bg.ok:
        return np.zeros_like(valid)
    ref, noise = bg.dist[b], bg.noise[b]
    thr = np.maximum(tol_mm, 4.0 * np.nan_to_num(noise, nan=0.0))
    near = np.where(np.isfinite(ref), d < ref - thr, d < FAR_MM)
    return valid & near


def blobs(rev, fg, gap_mm=250.0, min_pts=6):
    """Groups neighbouring foreground points; returns centroids sorted by distance."""
    idx = np.nonzero(fg)[0]
    if not len(idx):
        return []
    x, y = to_xy(rev.angle[idx], rev.dist[idx])
    groups, cur = [], [0]
    for k in range(1, len(idx)):
        if math.hypot(x[k] - x[k - 1], y[k] - y[k - 1]) > gap_mm:
            groups.append(cur)
            cur = []
        cur.append(k)
    groups.append(cur)
    if len(groups) > 1 and math.hypot(x[0] - x[-1], y[0] - y[-1]) <= gap_mm:   # wrap at 360
        groups[0] = groups.pop() + groups[0]
    out = []
    for g in groups:
        if len(g) < min_pts:
            continue
        gx, gy = x[g], y[g]
        cx, cy = float(gx.mean()), float(gy.mean())
        width = float(math.hypot(gx.max() - gx.min(), gy.max() - gy.min()))
        out.append({'x': cx, 'y': cy, 'w': width, 'n': len(g), 'r': math.hypot(cx, cy)})
    out.sort(key=lambda o: o['r'])
    return out
