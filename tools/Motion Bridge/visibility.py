"""What the sensors see: per position the region up to the first obstacle of each ray, and per field cell
whether any position sees it.

A ray of the lidar ends at the first surface it hits. Behind that point the sensor knows nothing: the
cell may be empty or taken, it lies in the shadow. The seen region of one position is, for each 0.5
degree direction, the distance up to

- the nearest measured point in that direction (a body, a wall, a pole: anything physical),
- or, without a return in that direction, the empty room's distance there (FAR_MM if the empty room had
  no return either: nothing within the sensor's range),
- but at most up to the first barrier (a sensor does not look through barriers),
- and nothing outside the position's fan.

Several positions see the union of their regions. Simulated objects cast no shadow.
"""
import numpy as np

from field import area_mask
from scene import BINS, FAR_MM, MIN_MM, bin_of, to_xy

CENTRES = (np.arange(BINS) + 0.5) * (360.0 / BINS)     # sensor angle of each bin, degrees


def ray_barrier(st, barriers):
    """Distance (mm) along each bin's ray to the first barrier, inf where none is hit."""
    out = np.full(BINS, np.inf)
    if not barriers:
        return out
    sx, sy = to_xy(CENTRES, np.ones(BINS))
    mx, my = st.to_map(sx, sy)
    dx, dy = mx - st.x, my - st.y                       # unit ray directions in the map
    for (ax, ay), (bx, by) in barriers:
        ex, ey = bx - ax, by - ay
        den = dx * ey - dy * ex
        ok = np.abs(den) > 1e-12
        den = np.where(ok, den, 1.0)
        qx, qy = ax - st.x, ay - st.y
        t = (qx * ey - qy * ex) / den                   # along the ray (mm)
        u = (qx * dy - qy * dx) / den                   # along the barrier (0..1)
        hit = ok & (t > 0.0) & (u >= 0.0) & (u <= 1.0)
        out = np.where(hit, np.minimum(out, t), out)
    return out


def reach(st, angle, dist, barriers=()):
    """Seen distance (mm) per 0.5 degree bin of one position, from one revolution."""
    angle, dist = np.asarray(angle, float), np.asarray(dist, float)
    r = np.full(BINS, np.inf)
    ok = dist >= MIN_MM
    if ok.any():
        np.minimum.at(r, bin_of(angle[ok]), dist[ok])
    empty = ~np.isfinite(r)
    bg = st.bg.dist if st.bg is not None and st.bg.ok else np.full(BINS, np.nan)
    fallback = np.where(np.isfinite(bg), bg, FAR_MM)
    r = np.where(empty, fallback, r)
    r = np.minimum(r, ray_barrier(st, barriers))
    r[~st.fan_mask(CENTRES)] = 0.0
    return r.astype(np.float32)


def seen(st, r, mx, my, margin_mm=0.0):
    """True where map points lie inside the seen region of one position (arrays of any shape)."""
    mx, my = np.asarray(mx, float), np.asarray(my, float)
    sx, sy = st.to_sensor(mx, my)
    ang = np.degrees(np.arctan2(sx, sy)) % 360.0
    d = np.hypot(sx, sy)
    return d <= np.asarray(r)[bin_of(ang)] + margin_mm


def field_visibility(room, reaches, grid, cols, rows):
    """1 for field cells that at least one position sees, else 0; shape (rows, cols).

    `reaches` maps station ids to reach() arrays; `grid` is the field's Grid (None without a ready room).
    A cell counts as seen when its centre is seen; the surface that ends a ray lies in a seen cell
    (margin of half a cell).
    """
    vis = np.zeros((rows, cols), np.float32)
    if grid is None or not room.ready() or not reaches:
        return vis
    cx, cy = grid.centres()
    half = 0.5 * grid.cell
    for sid, r in reaches.items():
        st = room.station(sid)
        if st is not None:
            vis = np.maximum(vis, seen(st, r, cx, cy, half).astype(np.float32))
    return vis * area_mask(room, grid)
