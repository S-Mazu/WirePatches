"""The field: occupancy, motion and visibility over the area, and the texture that carries them.

The field is a grid of square cells of `cell_mm` over the smallest rectangle aligned with the
base line that holds the whole area (room.py): columns run along the base line (0 = left end as
seen from the area), rows into the room (0 = on the base line, last = at the far edge of the
area). One metre is the same number of cells in both directions. Cells whose centre lies outside
the area stay 0. Each cell holds values 0..1:

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

from room import inside_polygon

REF_S = 0.25            # a cell that changes completely within this time gives motion 1
SMOOTH = 0.5            # share of the new occupancy taken each update (sensor jitter)
SIM_RADIUS_MM = 220.0   # simulated objects are discs of this radius
MAX_CELLS = 1020        # columns or field rows at most (a 20 m area at 0.1 m is 200 columns)
FIELD_SHARE = 15        # field rows per band row: the band is the top sixteenth of the texture
EMPTY_SIZE = (8, FIELD_SHARE, 1)    # (cols, rows, band) of the black field sent without area and base line
MIN_FIT = 0.8           # cells shrink to at most this share of the cell size to end exactly at the far side


def grid_size(width_mm, depth_mm, cell_mm):
    """(cols, rows, band, cell) for a rectangle of this size: square cells, band exactly one sixteenth.

    The field rows are a multiple of FIELD_SHARE so that the band is exactly the top sixteenth of the
    texture. To make the rows end at the far side of the rectangle, the cells shrink a little (to no
    less than MIN_FIT of cell_mm); a shallow rectangle that would need more keeps cell_mm and its rows
    reach beyond the far side. Columns are rounded up: the grid reaches less than a cell beyond the
    right side. The cells stay square. A rectangle that would need more than MAX_CELLS columns or
    rows gets larger cells. Returns the cell size used.
    """
    cell = max(float(cell_mm), 1.0)
    while True:
        band = max(math.ceil(math.ceil(depth_mm / cell - 1e-6) / FIELD_SHARE), 1)
        fit = depth_mm / (FIELD_SHARE * band)               # cell size at which the rows end exactly at the far side
        c = fit if fit >= MIN_FIT * cell else cell
        cols = max(math.ceil(width_mm / c - 1e-6), 1)
        if cols <= MAX_CELLS and FIELD_SHARE * band <= MAX_CELLS:
            return cols, FIELD_SHARE * band, band, c
        cell *= 1.05


class Grid:
    """Where the field lies in the map: origin (row 0, column 0 corner), unit vectors u (columns) and
    n (rows), square cells of `cell` mm."""

    def __init__(self, origin, u, n, cols, rows, cell):
        self.origin = np.asarray(origin, float)
        self.u, self.n = np.asarray(u, float), np.asarray(n, float)
        self.cols, self.rows, self.cell = int(cols), int(rows), float(cell)

    @classmethod
    def of(cls, room, cell_mm):
        """The grid of a ready room, with its band: (grid, band)."""
        origin, u, n, width, depth = room.frame()
        cols, rows, band, cell = grid_size(width, depth, cell_mm)
        return cls(origin, u, n, cols, rows, cell), band

    def key(self):
        return (tuple(self.origin), tuple(self.u), self.cols, self.rows, self.cell)

    def to_cells(self, mx, my):
        """Map points -> (column, row) as floats; integer parts are the cell indices."""
        px, py = np.asarray(mx, float) - self.origin[0], np.asarray(my, float) - self.origin[1]
        return (px * self.u[0] + py * self.u[1]) / self.cell, (px * self.n[0] + py * self.n[1]) / self.cell

    def to_map(self, c, r):
        """(column, row) as floats -> map points (mm)."""
        c, r = np.asarray(c, float), np.asarray(r, float)
        x = self.origin[0] + (self.u[0] * c + self.n[0] * r) * self.cell
        y = self.origin[1] + (self.u[1] * c + self.n[1] * r) * self.cell
        return x, y

    def from_field(self, x01, d01):
        """Field shares (x 0..1 along the base line, depth 0..1) -> map position (mm)."""
        x, y = self.to_map(x01 * self.cols, d01 * self.rows)
        return float(x), float(y)

    def centres(self):
        """Map coordinates of the cell centres, arrays of shape (rows, cols)."""
        C, R = np.meshgrid(np.arange(self.cols) + 0.5, np.arange(self.rows) + 0.5)
        return self.to_map(C, R)

    def outline(self):
        """Closed outline of the field rectangle (mm): lists of x and y."""
        c = [0, self.cols, self.cols, 0, 0]
        r = [0, 0, self.rows, self.rows, 0]
        x, y = self.to_map(c, r)
        return list(x), list(y)


_masks = {}


def area_mask(room, grid):
    """1 for cells whose centre lies inside the area, 0 outside; shape (rows, cols). Cached."""
    key = (grid.key(), tuple(map(tuple, room.area)))
    m = _masks.get(key)
    if m is None:
        cx, cy = grid.centres()
        m = inside_polygon(cx, cy, room.area).astype(np.float32)
        _masks.clear()
        _masks[key] = m
    return m


class Field:
    def __init__(self):
        self.cols = self.rows = self.band = 0
        self.grid = None                # Grid of a ready room, else None
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
        """Grid of the room's field at this cell size; a small empty grid without area and base line."""
        if room.ready():
            self.grid, band = Grid.of(room, cell_mm)
            self.configure(self.grid.cols, self.grid.rows, band)
        else:
            self.grid = None
            self.configure(*EMPTY_SIZE)

    def _raw(self, room, views, sim):
        """Occupancy of this moment, before smoothing."""
        rows, cols = self.rows, self.cols
        occ = np.zeros((rows, cols), np.float32)
        g = self.grid
        if g is None or not room.ready():
            return occ
        extent = g.cell                                         # typical length of a surface across a cell (mm)
        for v in views:
            m = v['valid'] & v['fg'] & v['keep']
            if not m.any():
                continue
            cf, rf = g.to_cells(v['mx'][m], v['my'][m])
            inside = (cf >= 0.0) & (cf < cols) & (rf >= 0.0) & (rf < rows)
            if not inside.any():
                continue
            step = 2.0 * math.pi / max(len(v['valid']), 1)       # angle between two points of a turn
            weight = v['dist'][m][inside] * step                 # surface length one point stands for (mm)
            c = np.minimum(cf[inside].astype(int), cols - 1)
            r = np.minimum(rf[inside].astype(int), rows - 1)
            covered = np.zeros((rows, cols), np.float32)
            np.add.at(covered, (r, c), weight)
            occ = np.maximum(occ, np.minimum(covered / extent, 1.0))   # several sensors: no double count
        if sim:
            cx, cy = g.centres()
            for sx, sy in sim:
                dist = np.hypot(cx - sx, cy - sy)
                occ = np.maximum(occ, np.clip((SIM_RADIUS_MM - dist) / extent + 0.5, 0.0, 1.0))
        return occ * area_mask(room, g)

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

        Row 0 is on the base line, row rows - 1 at the far edge of the field. The last `band` rows
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
