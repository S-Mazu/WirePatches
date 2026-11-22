"""Room map: sensor positions, area with its base line, exclusions and barriers in one top view.

Map frame: millimetres, x to the right, y up, seen from above. The first sensor position
starts at the origin. Sensor frame (scene.py): x to the right, y = sensor front.

- The area is a polygon: only foreground inside it counts, and the field covers it.
- One edge of the area is the base line. The field is the smallest rectangle aligned with it
  that holds the whole area: row 0 lies on the base line, columns run from its left end to its
  right end as seen from the area (README.md, Output).
- Exclusions are polygons inside which nothing counts; barriers are walls a sensor does
  not look through.

A room lives in rooms/<name>/: room.json plus one empty-room scan per position
(scan_<id>.npy). Several positions can come from several sensors or from one sensor
that was moved; only positions with a port deliver live data.
"""
import copy
import json
import math
import os
import re

import numpy as np

from scene import Background, to_xy

MIN_BASE_MM = 300.0             # a base line shorter than this is not a base line
MIN_SIZE_MM = 300.0             # the field is at least this wide and deep
START_DEPTH_MM = 3000.0         # depth of the area made from an old front (rooms before 2026-11-16)


def _rot(x, y, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return c * x - s * y, s * x + c * y


class Station:
    """One sensor position: where it stands, where it looks, what it watches."""

    def __init__(self, sid, name, x=0.0, y=0.0, rot=0.0, flip=False, fan_dir=0.0, fan_span=360.0, port=None):
        self.sid, self.name = sid, name
        self.x, self.y, self.rot = float(x), float(y), float(rot)   # mm, degrees counter-clockwise
        self.flip = bool(flip)                                       # sensor mounted mirrored
        self.fan_dir = float(fan_dir)          # sensor degrees (0 = front, clockwise)
        self.fan_span = float(fan_span)        # degrees, 360 = all around
        self.port = port                       # serial port while the sensor stands here
        self.bg = None                         # Background, set by Room

    def to_dict(self):
        return {k: getattr(self, k) for k in ('sid', 'name', 'x', 'y', 'rot', 'flip', 'fan_dir', 'fan_span', 'port')}

    def to_map(self, sx, sy):
        """Sensor frame -> map frame (arrays or scalars, mm)."""
        if self.flip:
            sx = -sx
        mx, my = _rot(sx, sy, self.rot)
        return mx + self.x, my + self.y

    def to_sensor(self, mx, my):
        sx, sy = _rot(mx - self.x, my - self.y, -self.rot)
        return (-sx if self.flip else sx), sy

    def fan_mask(self, angle):
        """True for sensor angles inside the fan."""
        if self.fan_span >= 360.0:
            return np.ones(len(angle), bool)
        diff = (angle - self.fan_dir + 180.0) % 360.0 - 180.0
        return np.abs(diff) <= self.fan_span / 2.0

    def fan_outline(self, radius_mm, step=3.0):
        """Map points of the fan wedge (closed line), for drawing."""
        span = min(self.fan_span, 360.0)
        angs = np.arange(-span / 2.0, span / 2.0 + 1e-6, step) + self.fan_dir
        if len(angs) < 2:
            angs = np.array([self.fan_dir - span / 2.0, self.fan_dir + span / 2.0])
        sx, sy = to_xy(angs, np.full(len(angs), radius_mm))
        mx, my = self.to_map(sx, sy)
        if span >= 360.0:
            return list(mx), list(my)
        return [self.x] + list(mx) + [self.x], [self.y] + list(my) + [self.y]

    def set_fan_towards(self, mx, my):
        """Points the fan at a map position (used by the fan handle)."""
        sx, sy = self.to_sensor(mx, my)
        if math.hypot(sx, sy) > 1.0:
            self.fan_dir = math.degrees(math.atan2(sx, sy)) % 360.0

    def fan_handle(self, radius_mm):
        sx, sy = to_xy(np.array([self.fan_dir]), np.array([radius_mm]))
        mx, my = self.to_map(sx, sy)
        return float(mx[0]), float(my[0])


def inside_polygon(px, py, poly):
    """Vectorised even-odd test; poly is a list of [x, y]."""
    px, py = np.asarray(px, float), np.asarray(py, float)
    res = np.zeros(px.shape, bool)
    n = len(poly)
    if n < 3:
        return res
    for k in range(n):
        x1, y1 = poly[k]
        x2, y2 = poly[(k + 1) % n]
        if y1 == y2:
            continue
        cross = (y1 > py) != (y2 > py)
        xi = x1 + (py - y1) * (x2 - x1) / (y2 - y1)
        res ^= cross & (px < xi)
    return res


def polygon_area(poly):
    """Signed area (mm2): positive for counter-clockwise corners (map frame, y up)."""
    p = np.asarray(poly, float)
    x, y = p[:, 0], p[:, 1]
    return 0.5 * float(np.dot(x, np.roll(y, -1)) - np.dot(y, np.roll(x, -1)))


def seg_dist(p, a, b):
    """Distance (mm) of point p from the segment a-b."""
    px, py = p
    ax, ay = a
    ex, ey = b[0] - ax, b[1] - ay
    ll = ex * ex + ey * ey
    t = 0.0 if ll <= 0.0 else min(max(((px - ax) * ex + (py - ay) * ey) / ll, 0.0), 1.0)
    return math.hypot(px - ax - t * ex, py - ay - t * ey)


def nearest_edge(poly, p):
    """(index, distance mm) of the polygon edge nearest to p; edge k runs from corner k to corner k + 1."""
    best = (None, math.inf)
    for k in range(len(poly)):
        d = seg_dist(p, poly[k], poly[(k + 1) % len(poly)])
        if d < best[1]:
            best = (k, d)
    return best


def nearest_corner(poly, p):
    """(index, distance mm) of the polygon corner nearest to p."""
    best = (None, math.inf)
    for k, c in enumerate(poly):
        d = math.hypot(c[0] - p[0], c[1] - p[1])
        if d < best[1]:
            best = (k, d)
    return best


def blocked(sx, sy, px, py, barriers):
    """True where the line of sight from (sx, sy) to the point crosses a barrier."""
    px, py = np.asarray(px, float), np.asarray(py, float)
    out = np.zeros(px.shape, bool)
    dx, dy = px - sx, py - sy
    for (ax, ay), (bx, by) in barriers:
        ex, ey = bx - ax, by - ay
        den = dx * ey - dy * ex
        ok = np.abs(den) > 1e-9
        den = np.where(ok, den, 1.0)
        qx, qy = ax - sx, ay - sy
        t = (qx * ey - qy * ex) / den          # along the line of sight
        u = (qx * dy - qy * dx) / den          # along the barrier
        out |= ok & (t > 0.0) & (t < 1.0) & (u >= 0.0) & (u <= 1.0)
    return out


def _line_frame(left, right):
    """Unit vector along a line (left -> right), unit normal to its front side, length.

    Seen from the front side, facing the line, the left end is on the left; so the front
    side lies to the right of the direction left -> right in the top view: n = (u.y, -u.x).
    """
    l, r = np.array(left, float), np.array(right, float)
    axis = r - l
    length = float(np.linalg.norm(axis))
    u = axis / length
    return u, np.array([u[1], -u[0]]), length


class Room:
    def __init__(self, folder, name):
        self.folder, self.name = folder, name
        self.stations = []
        self.area = None                        # [[x, y], ...] polygon (mm): only foreground inside counts
        self.base = None                        # index k of the base line: the area edge from corner k to k + 1
        self.exclusions = []                    # [[[x, y], ...], ...] polygons inside which nothing counts
        self.barriers = []                      # [[[x, y], [x, y]], ...]
        self.ref = None                         # reference marker [x, y] (e.g. a pole)

    # ------------------------------------------------ files
    @staticmethod
    def names(rooms_dir: str) -> list[str]:
        if not os.path.isdir(rooms_dir):
            return []
        return sorted(n for n in os.listdir(rooms_dir) if os.path.exists(os.path.join(rooms_dir, n, 'room.json')))

    @staticmethod
    def safe_name(name: str) -> str:
        return re.sub(r'[^A-Za-z0-9 _-]', '', name).strip()[:40]

    def scan_path(self, sid):
        return os.path.join(self.folder, f'scan_{sid}.npy')

    @classmethod
    def load(cls, folder: str) -> 'Room':
        room = cls(folder, os.path.basename(folder))
        with open(os.path.join(folder, 'room.json')) as f:
            d = json.load(f)
        room.stations = [Station(**s) for s in d.get('stations', [])]
        if 'screen' in d or 'areas' in d:
            room.area, front, room.exclusions = cls._legacy(d)
        else:
            room.area, front = d.get('area'), d.get('front')
            room.exclusions = d.get('exclusions', [])
        room.base = d.get('base')
        if room.base is None and room.has_area():
            room.base = cls._base_from_front(room.area, front)
        room.barriers = d.get('barriers', [])
        room.ref = d.get('ref')
        for st in room.stations:
            st.bg = Background(room.scan_path(st.sid))
            st.bg.load()
        return room

    @staticmethod
    def _legacy(d):
        """Room files before 2026-11-16: screen, Depth Ranges and include/exclude areas.

        The screen becomes the front. The first include area becomes the area; without one,
        the screen trapezoid (screen width x Depth Range per end) does.
        """
        front = d.get('screen') or None
        old = float(d.get('depth_mm', START_DEPTH_MM))
        dl, dr = float(d.get('depth_l_mm', old)), float(d.get('depth_r_mm', old))
        areas = d.get('areas') or []
        inc = [a['pts'] for a in areas if a.get('kind') == 'include' and len(a.get('pts', [])) >= 3]
        exc = [a['pts'] for a in areas if a.get('kind') == 'exclude' and len(a.get('pts', [])) >= 3]
        area = inc[0] if inc else None
        if area is None and front and front.get('left') and front.get('right'):
            l, r = np.array(front['left'], float), np.array(front['right'], float)
            if np.linalg.norm(r - l) > MIN_BASE_MM:
                _, n, _ = _line_frame(l, r)
                area = [list(map(float, p)) for p in (l, r, r + n * dr, l + n * dl)]
        return area, front, exc

    @staticmethod
    def _base_from_front(area, front):
        """Rooms before 2026-11-22 had a front line: the area edge closest to its middle becomes the base line."""
        if not front or not front.get('left') or not front.get('right'):
            return 0
        mid = ((front['left'][0] + front['right'][0]) / 2.0, (front['left'][1] + front['right'][1]) / 2.0)
        return nearest_edge(area, mid)[0]

    def save(self) -> None:
        os.makedirs(self.folder, exist_ok=True)
        d = {'stations': [s.to_dict() for s in self.stations], 'area': self.area, 'base': self.base,
             'exclusions': self.exclusions, 'barriers': self.barriers, 'ref': self.ref}
        tmp = os.path.join(self.folder, 'room.json.tmp')
        with open(tmp, 'w') as f:
            json.dump(d, f, indent=2)
        os.replace(tmp, os.path.join(self.folder, 'room.json'))

    # ------------------------------------------------ stations
    def station(self, sid) -> 'Station | None':
        return next((s for s in self.stations if s.sid == sid), None)

    def add_station(self, near=None, port=None):
        """New position, placed 1 m next to `near` (or at the origin)."""
        used = {s.sid for s in self.stations}
        sid = next(chr(c) for c in range(ord('A'), ord('Z') + 1) if chr(c) not in used)
        x, y = (near.x + 1000.0, near.y) if near else (0.0, 0.0)
        st = Station(sid, f'Position {sid}', x, y, near.rot if near else 0.0, near.flip if near else False,
                     port=port)
        st.bg = Background(self.scan_path(sid))
        self.stations.append(st)
        return st

    def remove_station(self, sid):
        self.stations = [s for s in self.stations if s.sid != sid]
        p = self.scan_path(sid)
        if os.path.exists(p):
            os.remove(p)

    def rotate_station(self, st, rot, pivot=None):
        """Sets the rotation; with a pivot (map mm) the station turns around it."""
        delta = rot - st.rot
        if pivot is not None:
            vx, vy = _rot(st.x - pivot[0], st.y - pivot[1], delta)
            st.x, st.y = pivot[0] + vx, pivot[1] + vy
        st.rot = (rot + 180.0) % 360.0 - 180.0

    def scan_map(self, st):
        """The station's empty-room scan in map coordinates (mm), in angular order."""
        if st.bg is None or not st.bg.ok:
            return np.zeros(0), np.zeros(0)
        sx, sy = st.bg.xy()
        return st.to_map(sx, sy)

    def find_reference(self, st, near, radius_mm=600.0, max_width_mm=400.0, gap_mm=100.0):
        """Centre of a pole-like object in the station's scan near a map point, or None.

        The scan sees the front half of a round pole; its centre lies about width/pi behind
        the mean of the visible points.
        """
        mx, my = self.scan_map(st)
        if not len(mx):
            return None
        d = np.hypot(mx - near[0], my - near[1])
        idx = np.nonzero(d < radius_mm)[0]
        if len(idx) < 2:
            return None
        k0 = int(idx[np.argmin(d[idx])])
        n = len(mx)
        group = [k0]
        for step in (1, -1):                       # grow along the scan while neighbours are close
            k = k0
            while len(group) < n:
                j = (k + step) % n
                if math.hypot(mx[j] - mx[k], my[j] - my[k]) > gap_mm:
                    break
                group.append(j)
                k = j
        gx, gy = mx[group], my[group]
        width = math.hypot(gx.max() - gx.min(), gy.max() - gy.min())
        if len(group) < 2 or width > max_width_mm:
            return None
        cx, cy = float(gx.mean()), float(gy.mean())
        vx, vy = cx - st.x, cy - st.y
        r = math.hypot(vx, vy)
        if r < 1.0:
            return None
        off = max(width, 20.0) / math.pi
        return cx + vx / r * off, cy + vy / r * off

    def snap_to_reference(self, st):
        """Moves the station so that the pole in its scan sits on the reference marker."""
        if self.ref is None:
            return False
        c = self.find_reference(st, self.ref)
        if c is None:
            return False
        st.x += self.ref[0] - c[0]
        st.y += self.ref[1] - c[1]
        return True

    # ------------------------------------------------ area and base line
    def has_area(self) -> bool:
        return bool(self.area) and len(self.area) >= 3

    def has_base(self) -> bool:
        """The area has a base line of usable length."""
        if not self.has_area() or self.base is None or not 0 <= self.base < len(self.area):
            return False
        a, b = self.area[self.base], self.area[(self.base + 1) % len(self.area)]
        return math.hypot(b[0] - a[0], b[1] - a[1]) > MIN_BASE_MM

    def ready(self) -> bool:
        """Area and base line are set: the field has a frame and something counts."""
        return self.has_base() and abs(polygon_area(self.area)) > 1.0

    def base_ends(self):
        """(left, right) ends of the base line as arrays, seen from the area looking at it (README, Output)."""
        n = len(self.area)
        a, b = np.array(self.area[self.base], float), np.array(self.area[(self.base + 1) % n], float)
        # The inside of a counter-clockwise polygon lies left of a -> b. The frame puts the area on the side
        # n = (u.y, -u.x), right of left -> right; so for a counter-clockwise polygon left = b, right = a.
        return (b, a) if polygon_area(self.area) > 0.0 else (a, b)

    def frame(self):
        """The field rectangle: (origin, u, n, width, depth).

        origin is the corner at row 0, column 0; u the unit vector along the base line (left -> right),
        n the unit normal into the area; width and depth in mm. The rectangle is the smallest one aligned
        with the base line that holds the whole area, so nothing of the area is cut off.
        """
        left, right = self.base_ends()
        u, n, _ = _line_frame(left, right)
        pts = np.array(self.area, float) - left
        s, d = pts @ u, pts @ n
        origin = left + u * s.min() + n * d.min()
        return (origin, u, n, max(float(s.max() - s.min()), MIN_SIZE_MM),
                max(float(d.max() - d.min()), MIN_SIZE_MM))

    def set_base(self, k) -> None:
        """Makes edge k (corner k to corner k + 1) the base line."""
        if self.has_area():
            self.base = int(k) % len(self.area)

    def insert_corner(self, k, p) -> None:
        """Inserts a corner into edge k of the area (after corner k); the base line stays the same edge part."""
        self.area.insert(k + 1, [float(p[0]), float(p[1])])
        if self.base is not None and self.base > k:
            self.base += 1

    def delete_corner(self, k) -> bool:
        """Removes corner k of the area if three remain. The base line keeps its edge if it can."""
        n = len(self.area)
        if n <= 3:
            return False
        del self.area[k]
        if self.base is not None:
            if self.base == k:                  # the base line started here: the edge before takes over
                self.base = (k - 1) % (n - 1)
            elif self.base > k:
                self.base -= 1
        return True

    # ------------------------------------------------ filters
    def keep_points(self, st, angle, mx, my):
        """Which points of one station count: inside its fan, not behind a barrier, counting."""
        keep = st.fan_mask(angle)
        if self.barriers:
            keep &= ~blocked(st.x, st.y, mx, my, self.barriers)
        return keep & self.counts(mx, my)

    def counts(self, mx, my):
        """True where a map point counts: inside the area (if there is one), in no exclusion."""
        mx, my = np.asarray(mx, float), np.asarray(my, float)
        keep = inside_polygon(mx, my, self.area) if self.has_area() else np.ones(mx.shape, bool)
        for poly in self.exclusions:
            if len(poly) >= 3:
                keep &= ~inside_polygon(mx, my, poly)
        return keep

    def copy(self):
        """A copy for drawing, without the scans."""
        c = Room(self.folder, self.name)
        c.stations = [copy.copy(s) for s in self.stations]
        c.area, c.base = copy.deepcopy(self.area), self.base
        c.exclusions, c.barriers, c.ref = (copy.deepcopy(self.exclusions), copy.deepcopy(self.barriers),
                                           copy.deepcopy(self.ref))
        return c


def create_default(rooms_dir, old_settings, old_background):
    """First start: a room 'Default' with one position at the origin.

    Takes over the old single-sensor setup if there is one: port, surface ends (they were
    measured in the sensor frame, which equals the map frame of position A) as the base line
    of a rectangular area in front of it, and the learned empty room.
    """
    room = Room(os.path.join(rooms_dir, 'Default'), 'Default')
    st = room.add_station(port=old_settings.get('port') or 'COM19')
    if old_settings.get('left') and old_settings.get('right'):
        l, r = np.array(old_settings['left'], float), np.array(old_settings['right'], float)
        if np.linalg.norm(r - l) > MIN_BASE_MM:
            _, n, _ = _line_frame(l, r)
            depth = float(old_settings.get('depth_mm') or START_DEPTH_MM)
            room.area = [list(map(float, p)) for p in (l, r, r + n * depth, l + n * depth)]
            room.base = 0
    room.save()
    if old_background and os.path.exists(old_background):
        bg = Background(old_background)
        bg.load()
        bg.path = room.scan_path(st.sid)
        bg.save()
        t = os.path.getmtime(old_background)            # keep the date of the original capture
        os.utime(bg.path, (t, t))
        bg.learned_at = t
        st.bg = bg
    return room
