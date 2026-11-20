"""The field: occupancy, motion and visibility over the area, and the texture that carries them.

The field is a grid over front width x area depth with square cells of `cell_mm`: columns
run along the front (0 = left end), rows into the room (0 = at the front, last = at the far
edge of the area). Cells whose centre lies outside the area stay 0. Each cell holds values 0..1:

- occupancy: how much of the cell is taken by something that is not part of the empty
  room. A 2D lidar sees the surface of a body facing the sensor, so occupancy is the share
  of the cell size covered by that surface (a body crossing a cell gives about 0.8 to 1).
- motion: how fast the occupancy changes. A cell that gets taken or freed within
  REF_S seconds reaches 1; standing still gives 0. Fades out over Motion fade.
- visibility: 1 where at least one sensor sees the cell centre, 0 in the shadow (behind a body or a
  barrier, outside every fan) and without a sensor (visibility.py). Simulated objects cast no shadow.

The texture (README.md, Output) is RGBA 8 bit: R occupancy, G motion, B visibility, A 255.
The field takes the lower fifteen sixteenths of the rows; the upper sixteenth is a status
band in which every pixel carries a counter that changes every frame, so a patch can tell a
live field from a frozen one after any resampling by Arena.
"""
import math

import numpy as np

REF_S = 0.25            # a cell that changes completely within this time gives motion 1
SMOOTH = 0.5            # share of the new occupancy taken each update (sensor jitter)
SIM_RADIUS_MM = 220.0   # simulated objects are discs of this radius
MAX_CELLS = 1020        # columns or field rows at most (a 20 m front at 0.1 m is 200 columns)
FIELD_SHARE = 15        # field rows per band row: the band is the top sixteenth of the texture
EMPTY_SIZE = (8, FIELD_SHARE, 1)    # (cols, rows, band) of the black field sent without area and front


def grid_size(length_mm, depth_mm, cell_mm):
    """(cols, rows, band) for a field of this size: cells about cell_mm, band exactly one sixteenth.

    The rows are a multiple of FIELD_SHARE and the band a fifteenth of them, so the band is
    exactly the top sixteenth of the texture at any size. Depth cells are then up to 7 %
    flatter than wide; patches sample normalized, so they never notice.
    """
    cell = max(float(cell_mm), 1.0)
    cols = min(max(int(round(length_mm / cell)), 1), MAX_CELLS)
    needed = min(max(int(round(depth_mm / cell)), 1), MAX_CELLS)
    band = -(-needed // FIELD_SHARE)                        # ceil
    return cols, FIELD_SHARE * band, band


class Field:
    def __init__(self):
        self.cols = self.rows = self.band = 0
        self.occ = self.motion = self.vis = None
        self.last_update_t = None       # time of the last occupancy update (motion rate)
        self.last_t = None              # time of the last fade step

    def configure(self, cols, rows, band=1):
        cols, rows, band = int(cols), int(rows), int(band)
        if (cols, rows, band) != (self.cols, self.rows, self.band):
            self.cols, self.rows, self.band = cols, rows, band
            self.occ = np.zeros((rows, cols), np.float32)
            self.motion = np.zeros((rows, cols), np.float32)
            self.vis = np.zeros((rows, cols), np.float32)
            self.last_update_t = self.last_t = None

    def configure_for(self, room, cell_mm):
        """Grid of the room's field at this cell size; a small empty grid without area and front."""
        if room.ready():
            self.configure(*grid_size(room.frame()[2], room.depth_mm(), cell_mm))
        else:
            self.configure(*EMPTY_SIZE)

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

    def _fade(self, t, fade_s):
        """Lets motion fade since the last step; returns the time since then."""
        dt = 0.0 if self.last_t is None else min(max(t - self.last_t, 0.0), 0.5)
        self.last_t = t
        if dt > 0.0:
            self.motion *= math.exp(-dt / max(float(fade_s), 1e-3))
        return dt

    def update(self, room, views, sim, t, fade_s, threshold):
        """New measurement: occupancy follows, motion is where it changed."""
        raw = self._raw(room, views, sim)
        self._fade(t, fade_s)
        dt = 0.1 if self.last_update_t is None else min(max(t - self.last_update_t, 1e-3), 0.5)
        self.last_update_t = t
        occ = self.occ + SMOOTH * (raw - self.occ)
        rate = np.abs(occ - self.occ) / dt * REF_S              # 1 = full change within REF_S
        th = min(max(float(threshold), 0.0), 0.95)
        inst = np.clip((rate - th) / (1.0 - th), 0.0, 1.0)
        self.motion = np.maximum(inst, self.motion).astype(np.float32)
        self.occ = occ.astype(np.float32)

    def decay(self, t, fade_s):
        """Between measurements: motion keeps fading, so frames between updates are smooth."""
        self._fade(t, fade_s)

    def texture(self, counter):
        """RGBA 8-bit image (rows + band, cols, 4): R occupancy, G motion, B visibility, A 255.

        Row 0 is at the front, row rows - 1 at the far edge of the area. The last `band` rows
        (the top sixteenth) are the status band: every pixel holds `counter` (0..255) in R, G
        and B.
        """
        rows, cols = self.rows, self.cols
        tex = np.zeros((rows + self.band, cols, 4), np.uint8)
        tex[:rows, :, 0] = np.clip(self.occ * 255.0 + 0.5, 0, 255)
        tex[:rows, :, 1] = np.clip(self.motion * 255.0 + 0.5, 0, 255)
        tex[:rows, :, 2] = np.clip(self.vis * 255.0 + 0.5, 0, 255)
        tex[:, :, 3] = 255
        tex[rows:, :, :3] = int(counter) % 256
        return tex
