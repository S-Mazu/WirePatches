"""Room map: sensor positions, screen, areas and barriers in one top view.

Map frame: millimetres, x to the right, y up, seen from above. The first sensor position
starts at the origin. Sensor frame (scene.py): x to the right, y = sensor front.

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

BEHIND_MM = 300.0               # people further than this behind the screen are ignored


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


class Room:
    def __init__(self, folder, name):
        self.folder, self.name = folder, name
        self.stations = []
        self.screen = None                      # {'left': [x, y], 'right': [x, y]} in mm
        self.depth_l_mm = 3000.0                 # Depth Range at the left and the right end of the screen:
        self.depth_r_mm = 3000.0                 # the tracked field is a trapezoid in front of the screen
        self.areas = []                         # [{'kind': 'include'|'exclude', 'pts': [[x, y], ...]}]
        self.barriers = []                      # [[[x, y], [x, y]], ...]
        self.ref = None                         # reference marker [x, y] (e.g. a pole)

    # ------------------------------------------------ files
    @staticmethod
    def names(rooms_dir):
        if not os.path.isdir(rooms_dir):
            return []
        return sorted(n for n in os.listdir(rooms_dir) if os.path.exists(os.path.join(rooms_dir, n, 'room.json')))

    @staticmethod
    def safe_name(name):
        return re.sub(r'[^A-Za-z0-9 _-]', '', name).strip()[:40]

    def scan_path(self, sid):
        return os.path.join(self.folder, f'scan_{sid}.npy')

    @classmethod
    def load(cls, folder):
        room = cls(folder, os.path.basename(folder))
        with open(os.path.join(folder, 'room.json')) as f:
            d = json.load(f)
        room.stations = [Station(**s) for s in d.get('stations', [])]
        room.screen = d.get('screen')
        old = float(d.get('depth_mm', 3000.0))
        room.depth_l_mm = float(d.get('depth_l_mm', old))
        room.depth_r_mm = float(d.get('depth_r_mm', old))
        room.areas = d.get('areas', [])
        room.barriers = d.get('barriers', [])
        room.ref = d.get('ref')
        for st in room.stations:
            st.bg = Background(room.scan_path(st.sid))
            st.bg.load()
        return room

    def save(self):
        os.makedirs(self.folder, exist_ok=True)
        d = {'stations': [s.to_dict() for s in self.stations], 'screen': self.screen,
             'depth_l_mm': self.depth_l_mm, 'depth_r_mm': self.depth_r_mm,
             'areas': self.areas, 'barriers': self.barriers, 'ref': self.ref}
        tmp = os.path.join(self.folder, 'room.json.tmp')
        with open(tmp, 'w') as f:
            json.dump(d, f, indent=2)
        os.replace(tmp, os.path.join(self.folder, 'room.json'))

    # ------------------------------------------------ stations
    def station(self, sid):
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

    # ------------------------------------------------ screen
    def has_screen(self):
        s = self.screen
        return bool(s and s.get('left') and s.get('right')
                    and math.hypot(s['right'][0] - s['left'][0], s['right'][1] - s['left'][1]) > 300.0)

    def frame(self):
        """Unit vector along the screen (left -> right), unit normal into the room, length.

        Seen from the room, facing the screen, the left end is on the left; so the room lies
        to the right of the direction left -> right in the top view: n = (u.y, -u.x).
        """
        l, r = np.array(self.screen['left'], float), np.array(self.screen['right'], float)
        axis = r - l
        length = float(np.linalg.norm(axis))
        u = axis / length
        return u, np.array([u[1], -u[0]]), length

    def map_xy(self, mx, my):
        """Map position -> (x along the screen 0..1, distance from the screen in mm)."""
        u, n, length = self.frame()
        p = np.array([mx, my], float) - np.array(self.screen['left'], float)
        return float(np.dot(p, u) / length), float(np.dot(p, n))

    def depth_at(self, x01):
        """Depth Range at a position along the screen (mm): linear from the left to the right end."""
        t = np.clip(x01, 0.0, 1.0)
        return np.maximum(self.depth_l_mm + (self.depth_r_mm - self.depth_l_mm) * t, 1.0)

    def to_field(self, mx, my):
        """Map points -> (x 0..1 along the screen, depth 0..1 within the trapezoid), vectorised."""
        u, n, length = self.frame()
        l = np.array(self.screen['left'], float)
        px, py = np.asarray(mx, float) - l[0], np.asarray(my, float) - l[1]
        x01 = (px * u[0] + py * u[1]) / length
        d = px * n[0] + py * n[1]
        return x01, d / self.depth_at(x01)

    def far_corner(self, end):
        """Back corner of the field behind the 'left' or 'right' end of the screen (mm)."""
        u, n, _ = self.frame()
        depth = self.depth_l_mm if end == 'left' else self.depth_r_mm
        return np.array(self.screen[end], float) + n * depth

    def set_far_corner(self, end, mx, my):
        """Drags a back corner: only its distance from the screen counts (at least 0.3 m)."""
        u, n, _ = self.frame()
        d = float(np.dot(np.array([mx, my], float) - np.array(self.screen[end], float), n))
        d = max(300.0, d)
        if end == 'left':
            self.depth_l_mm = d
        else:
            self.depth_r_mm = d

    def screen_band(self):
        """Closed outline of the tracked field (screen and the far edge at Depth Range), mm."""
        u, n, _ = self.frame()
        l, r = np.array(self.screen['left'], float), np.array(self.screen['right'], float)
        fl, fr = l + n * self.depth_l_mm, r + n * self.depth_r_mm
        return [l[0], r[0], fr[0], fl[0], l[0]], [l[1], r[1], fr[1], fl[1], l[1]]

    def area_from_screen(self):
        """An area polygon covering screen x Depth Range, as a starting point for editing."""
        xs, ys = self.screen_band()
        self.areas.append({'kind': 'include', 'pts': [[float(x), float(y)] for x, y in zip(xs[:4], ys[:4])]})

    # ------------------------------------------------ filters
    def keep_points(self, st, angle, mx, my):
        """Which points of one station count: inside its fan, not behind a barrier, inside the areas."""
        keep = st.fan_mask(angle)
        if self.barriers:
            keep &= ~blocked(st.x, st.y, mx, my, self.barriers)
        return keep & self.in_areas(mx, my)

    def in_areas(self, mx, my):
        mx, my = np.asarray(mx, float), np.asarray(my, float)
        inc = [a['pts'] for a in self.areas if a['kind'] == 'include' and len(a['pts']) >= 3]
        exc = [a['pts'] for a in self.areas if a['kind'] == 'exclude' and len(a['pts']) >= 3]
        keep = np.ones(mx.shape, bool)
        if inc:
            keep = np.zeros(mx.shape, bool)
            for poly in inc:
                keep |= inside_polygon(mx, my, poly)
        for poly in exc:
            keep &= ~inside_polygon(mx, my, poly)
        return keep

    def copy(self):
        """A copy for drawing, without the scans."""
        c = Room(self.folder, self.name)
        c.stations = [copy.copy(s) for s in self.stations]
        c.screen, c.depth_l_mm, c.depth_r_mm = copy.deepcopy(self.screen), self.depth_l_mm, self.depth_r_mm
        c.areas, c.barriers, c.ref = copy.deepcopy(self.areas), copy.deepcopy(self.barriers), copy.deepcopy(self.ref)
        return c


def create_default(rooms_dir, old_settings, old_background):
    """First start: a room 'Default' with one position at the origin.

    Takes over the old single-sensor setup if there is one: port, surface ends (they were
    measured in the sensor frame, which equals the map frame of position A) and the learned
    empty room.
    """
    room = Room(os.path.join(rooms_dir, 'Default'), 'Default')
    st = room.add_station(port=old_settings.get('port') or 'COM19')
    room.depth_l_mm = room.depth_r_mm = float(old_settings.get('depth_mm') or 3000.0)
    if old_settings.get('left') and old_settings.get('right'):
        room.screen = {'left': list(old_settings['left']), 'right': list(old_settings['right'])}
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
