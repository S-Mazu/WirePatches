"""Occupancy and motion field over the area in front of the front line.

The field is a grid over front width x area depth: columns run along the front (0 = left
end), rows into the room (0 = at the front, last = at the far edge of the area). Cells whose
centre lies outside the area stay 0. Each cell holds two values 0..1:

- occupancy: how much of the cell is taken by something that is not part of the empty
  room. A 2D lidar sees the surface of a body facing the sensor, so occupancy is the share
  of the cell size covered by that surface (a body crossing a cell gives about 1).
- motion: how fast the occupancy changes. A cell that gets taken or freed within
  REF_S seconds reaches 1; standing still gives 0. Fades out over Motion fade.
"""
import math

import numpy as np

REF_S = 0.25            # a cell that changes completely within this time gives motion 1
SMOOTH = 0.5            # share of the new occupancy taken each update (sensor jitter)
SIM_RADIUS_MM = 220.0   # simulated objects are discs of this radius


class Field:
    def __init__(self):
        self.cols = self.rows = 0
        self.occ = self.motion = None
        self.last_t = None

    def configure(self, cols, rows):
        cols, rows = int(cols), int(rows)
        if (cols, rows) != (self.cols, self.rows):
            self.cols, self.rows = cols, rows
            self.occ = np.zeros((rows, cols), np.float32)
            self.motion = np.zeros((rows, cols), np.float32)
            self.last_t = None

    def _raw(self, room, views, sim):
        """Occupancy of this moment, before smoothing."""
        rows, cols = self.rows, self.cols
        occ = np.zeros((rows, cols), np.float32)
        if not room.ready():
            return occ
        _, _, length = room.frame()
        depth = room.depth_mm()
        extent = 0.5 * (length / cols + depth / rows)           # typical length of a surface across a cell (mm)
        for v in views:
            m = v['valid'] & v['fg'] & v['keep']
            if not m.any():
                continue
            x01, d01 = room.to_field(v['mx'][m], v['my'][m])
            inside = (x01 >= 0.0) & (x01 < 1.0) & (d01 >= 0.0) & (d01 < 1.0)
            if not inside.any():
                continue
            step = 2.0 * math.pi / max(len(v['valid']), 1)       # angle between two points of a turn
            weight = v['dist'][m][inside] * step                 # surface length one point stands for (mm)
            c = np.minimum((x01[inside] * cols).astype(int), cols - 1)
            r = np.minimum((d01[inside] * rows).astype(int), rows - 1)
            covered = np.zeros((rows, cols), np.float32)
            np.add.at(covered, (r, c), weight)
            occ = np.maximum(occ, np.minimum(covered / extent, 1.0))   # several sensors: no double count
        if sim:
            cx, cy = room.field_cells(cols, rows)
            for sx, sy in sim:
                dist = np.hypot(cx - sx, cy - sy)
                occ = np.maximum(occ, np.clip((SIM_RADIUS_MM - dist) / extent + 0.5, 0.0, 1.0))
        return occ * room.field_mask(cols, rows)

    def update(self, room, views, sim, t, fade_s, threshold):
        raw = self._raw(room, views, sim)
        dt = 0.1 if self.last_t is None else min(max(t - self.last_t, 1e-3), 0.5)
        self.last_t = t
        occ = self.occ + SMOOTH * (raw - self.occ)
        rate = np.abs(occ - self.occ) / dt * REF_S              # 1 = full change within REF_S
        th = min(max(float(threshold), 0.0), 0.95)
        inst = np.clip((rate - th) / (1.0 - th), 0.0, 1.0)
        keep = math.exp(-dt / max(float(fade_s), 1e-3))
        self.motion = np.maximum(inst, self.motion * keep).astype(np.float32)
        self.occ = occ.astype(np.float32)
