"""The room map: scans, live points, area, front, shapes, drag handles, drafts and simulated objects.

Everything here is in map millimetres on the engine side and metres on the plot. The view
changes the room only through Engine.edit().
"""
import math
import time

import dearpygui.dearpygui as dpg
import numpy as np

from simpath import path_position
from visibility import seen

from .theme import (AREA_C, BAR_C, DRAFT_C, EXCL_C, FG_C, FG_OUT_C, FIELD_C, FRONT_C, OBJ_C, OUT_C, REF_C, SCAN_C,
                    SIM_C, station_color)

FAN_HANDLE_MM = 1200.0      # distance of the fan handle from the sensor
FAN_RING_MM = 400.0         # radius of the circle drawn for a position that looks all around
HEAD_MM = 600.0             # length of the line that shows where a sensor looks
ARROW_MM, WING_MM = 700.0, 180.0    # the arrow from the front into the area
SIM_AHEAD_MM = 1500.0       # a new simulated object is put this far in front of the front (or the sensor)
SIM_PATH_MM = 2000.0        # its second point B lies this far to the right of A, along the front
CLICK_PX = 5                # press and release closer than this is a click, not a drag
SEEN_PX = 240               # resolution of the seen-region image over the visible part of the map
SEEN_RGBA = (0.55, 0.75, 1.0, 0.16)     # tint of what the sensors see; shadows stay dark


class RoomView:
    def __init__(self, eng, state, draft, themes, width):
        self.eng, self.state, self.draft, self.themes = eng, state, draft, themes
        self.handles = {}           # tag -> (get(room) -> (x, y) m, set(room, x, y))
        self.last_set = {}          # tag -> last value given to or read from dearpygui
        self.dynamic = []           # item tags that are rebuilt with the room
        self.sim_points = []        # (tag of A, tag of B) per simulated object
        self.sim_move = False       # objects walk between A and B
        self.sim_speed_mps = 1.0
        self.sim_pause_s = 5.0
        self.sim_t0 = time.monotonic()
        self.unlock = 2             # frames until the axis limits are released after a fit
        self.press = None           # (screen pos, plot pos) of the last mouse press on the plot
        self.last_snap = None
        self._scans = {}            # sid -> (key, lists): scan points in metres, converted once
        self._build(width)

    # ------------------------------------------------ build
    def _build(self, width):
        with dpg.texture_registry():
            dpg.add_dynamic_texture(SEEN_PX, SEEN_PX, [0.0] * (SEEN_PX * SEEN_PX * 4), tag='seen_tex')
        with dpg.plot(tag='plot', width=width, height=-1, equal_aspects=True, no_menus=True,
                      no_box_select=True, pan_button=dpg.mvMouseButton_Right,
                      fit_button=dpg.mvMouseButton_Middle):
            dpg.add_plot_axis(dpg.mvXAxis, label='x (m)', tag='xaxis')
            with dpg.plot_axis(dpg.mvYAxis, label='y (m)', tag='yaxis'):
                dpg.add_image_series('seen_tex', (0.0, 0.0), (0.0, 0.0), tag='seen_img')   # under everything
                dpg.add_line_series([], [], tag='l_field')
                dpg.add_scatter_series([], [], tag='s_out')
                dpg.add_scatter_series([], [], tag='s_pts')
                dpg.add_scatter_series([], [], tag='s_fg_out')
                dpg.add_scatter_series([], [], tag='s_fg')
                dpg.add_line_series([], [], tag='l_front')
                dpg.add_line_series([], [], tag='l_arrow')
                dpg.add_line_series([], [], tag='l_draft')
                dpg.add_scatter_series([], [], tag='s_draft')
                dpg.add_scatter_series([], [], tag='s_obj')
                dpg.add_line_series([], [], tag='l_sim')
                dpg.add_scatter_series([], [], tag='s_sim')
        for tag, color, size in [('s_out', OUT_C, 1.5), ('s_pts', SCAN_C, 1.5), ('s_fg', FG_C, 2.5),
                                 ('s_fg_out', FG_OUT_C, 2.0), ('l_front', FRONT_C, 4.0), ('l_arrow', FRONT_C, 2.0),
                                 ('l_field', FIELD_C, 1.0), ('l_draft', DRAFT_C, 2.0), ('s_draft', DRAFT_C, 5.0)]:
            dpg.bind_item_theme(tag, self.themes.series(color, size))
        dpg.bind_item_theme('s_obj', self.themes.series(OBJ_C, 16, dpg.mvPlotMarker_Circle, ring=True))
        dpg.bind_item_theme('l_sim', self.themes.series(tuple(int(v * 0.6) for v in SIM_C), 1.0))
        dpg.bind_item_theme('s_sim', self.themes.series(SIM_C, 6.0))

    # ------------------------------------------------ items that depend on the room
    def _add_series(self, tag, kind, color, size):
        fn = dpg.add_scatter_series if kind == 'scatter' else dpg.add_line_series
        fn([], [], tag=tag, parent='yaxis')
        dpg.bind_item_theme(tag, self.themes.series(color, size))
        self.dynamic.append(tag)

    def _add_handle(self, tag, get, setf, color, label, select=None):
        """A drag point. get(room) -> (x, y) in metres; setf(room, x, y) in metres."""
        dpg.add_drag_point(tag=tag, parent='plot', default_value=get(self.eng.room), color=color + (255,), thickness=2,
                           show_label=False,
                           callback=(lambda s, a, u: setattr(self.state, 'sel', u)) if select else None, user_data=select)
        self.dynamic.append(tag)
        if label:                       # drag points draw no label; an annotation follows the handle
            dpg.add_plot_annotation(tag=f'ann_{tag}', parent='plot', default_value=get(self.eng.room), label=label,
                                    offset=(14, -14), color=tuple(int(v * 0.6) for v in color) + (255,))
            self.dynamic.append(f'ann_{tag}')
        self.handles[tag] = (get, setf)
        self.last_set[tag] = None

    @staticmethod
    def _m(p):
        return p[0] / 1000.0, p[1] / 1000.0

    def rebuild(self):
        for tag in self.dynamic:
            if dpg.does_item_exist(tag):
                dpg.delete_item(tag)
        self.dynamic, self.handles, self.last_set = [], {}, {}
        r = self.eng.room
        edit = dpg.get_value('edit_points')
        if r.has_area():
            self._add_series('l_area', 'line', AREA_C, 2.0)
            if edit:
                for j in range(len(r.area)):
                    self._add_handle(f'h_a_{j}', lambda room, j=j: self._m(room.area[j]),
                                     lambda room, x, y, j=j: room.area.__setitem__(j, [x * 1000.0, y * 1000.0]),
                                     AREA_C, '')
        for i, e in enumerate(r.exclusions):
            self._add_series(f'excl_{i}', 'line', EXCL_C, 2.0)
            if edit:
                for j in range(len(e)):
                    self._add_handle(f'h_e_{i}_{j}', lambda room, i=i, j=j: self._m(room.exclusions[i][j]),
                                     lambda room, x, y, i=i, j=j: room.exclusions[i].__setitem__(j, [x * 1000.0, y * 1000.0]),
                                     EXCL_C, '')
        for i in range(len(r.barriers)):
            self._add_series(f'bar_{i}', 'line', BAR_C, 4.0)
            if edit:
                for j in (0, 1):
                    self._add_handle(f'h_b_{i}_{j}', lambda room, i=i, j=j: self._m(room.barriers[i][j]),
                                     lambda room, x, y, i=i, j=j: room.barriers[i].__setitem__(j, [x * 1000.0, y * 1000.0]),
                                     BAR_C, '')
        for st in r.stations:
            c = station_color(r, st.sid)
            self._add_series(f'scan_{st.sid}', 'scatter', c, 1.5)
            self._add_series(f'fan_{st.sid}', 'line', tuple(int(v * 0.7) for v in c), 1.0)
            self._add_series(f'head_{st.sid}', 'line', c, 3.0)
            self._add_handle(f'h_st_{st.sid}',
                             lambda room, sid=st.sid: (room.station(sid).x / 1000.0, room.station(sid).y / 1000.0),
                             lambda room, x, y, sid=st.sid: (setattr(room.station(sid), 'x', x * 1000.0),
                                                             setattr(room.station(sid), 'y', y * 1000.0)),
                             c, st.sid + (' (sensor)' if st.port else ''), select=st.sid)
            if st.fan_span < 360.0:
                self._add_handle(f'h_fan_{st.sid}',
                                 lambda room, sid=st.sid: self._m(room.station(sid).fan_handle(FAN_HANDLE_MM)),
                                 lambda room, x, y, sid=st.sid: room.station(sid).set_fan_towards(x * 1000.0, y * 1000.0),
                                 c, '', select=st.sid)
        if r.front and r.front.get('left') and r.front.get('right'):
            for end, lab in (('left', 'L'), ('right', 'R')):
                self._add_handle(f'h_{end}', lambda room, end=end: self._m(room.front[end]),
                                 lambda room, x, y, end=end: room.front.__setitem__(end, [x * 1000.0, y * 1000.0]),
                                 FRONT_C, lab)
        if r.ref:
            self._add_handle('h_ref', lambda room: self._m(room.ref),
                             lambda room, x, y: setattr(room, 'ref', [x * 1000.0, y * 1000.0]), REF_C, 'ref')

    # ------------------------------------------------ every frame
    def frame(self):
        if self.unlock > 0:
            self.unlock -= 1
            if self.unlock == 0:
                dpg.set_axis_limits_auto('xaxis')
                dpg.set_axis_limits_auto('yaxis')
        self._sync_handles()
        self._draw_room()
        self._draw_draft()
        self._move_sim()

    def _move_sim(self):
        """Hands the simulated objects to the engine: at A, or on their way between A and B."""
        t = time.monotonic() - self.sim_t0
        pos, lx, ly = [], [], []
        for ta, tb in self.sim_points:
            a = [v * 1000.0 for v in dpg.get_value(ta)[:2]]
            if self.sim_move:
                b = [v * 1000.0 for v in dpg.get_value(tb)[:2]]
                pos.append(list(path_position(a, b, self.sim_speed_mps * 1000.0, self.sim_pause_s, t)))
                lx += [a[0] / 1000.0, b[0] / 1000.0, float('nan')]
                ly += [a[1] / 1000.0, b[1] / 1000.0, float('nan')]
            else:
                pos.append(a)
        self.eng.sim = pos
        moving = self.sim_move and pos
        dpg.set_value('l_sim', [lx, ly] if moving else [[], []])
        dpg.set_value('s_sim', [[p[0] / 1000.0 for p in pos], [p[1] / 1000.0 for p in pos]] if moving else [[], []])

    def _sync_handles(self):
        """Handles the user dragged go into the room; handles the room moved go into the plot."""
        moved = []
        for tag, (get, setf) in self.handles.items():
            cur = tuple(dpg.get_value(tag)[:2])
            last = self.last_set.get(tag)
            if last is not None and (abs(cur[0] - last[0]) > 1e-7 or abs(cur[1] - last[1]) > 1e-7):
                moved.append((setf, cur))
                self.last_set[tag] = cur
            else:
                m = get(self.eng.room)
                if last is None or abs(m[0] - last[0]) > 1e-7 or abs(m[1] - last[1]) > 1e-7:
                    dpg.set_value(tag, m)
                    self.last_set[tag] = m
            if dpg.does_item_exist(f'ann_{tag}'):
                dpg.configure_item(f'ann_{tag}', default_value=self.last_set[tag])
        if moved:
            def apply(room):
                for setf, (x, y) in moved:
                    setf(room, x, y)
            self.eng.edit(apply)

    def _scan_lists(self, r, st):
        key = (st.x, st.y, st.rot, st.flip, st.bg.learned_at if st.bg else None)
        cached = self._scans.get(st.sid)
        if cached is None or cached[0] != key:
            mx, my = r.scan_map(st)
            self._scans[st.sid] = (key, [list(mx / 1000.0), list(my / 1000.0)])
        return self._scans[st.sid][1]

    def _draw_room(self):
        r = self.eng.room
        cur = self.state.selected(self.eng)
        all_scans = dpg.get_value('show_scans')
        for st in r.stations:
            dpg.set_value(f'scan_{st.sid}', self._scan_lists(r, st) if all_scans or st.sid == cur.sid else [[], []])
            fx, fy = st.fan_outline(FAN_HANDLE_MM if st.fan_span < 360.0 else FAN_RING_MM)
            dpg.set_value(f'fan_{st.sid}', [[v / 1000.0 for v in fx], [v / 1000.0 for v in fy]])
            hx, hy = st.to_map(np.array([0.0, 0.0]), np.array([0.0, HEAD_MM]))
            dpg.set_value(f'head_{st.sid}', [list(hx / 1000.0), list(hy / 1000.0)])
        if r.has_area() and dpg.does_item_exist('l_area'):
            pts = r.area + r.area[:1]
            dpg.set_value('l_area', [[p[0] / 1000.0 for p in pts], [p[1] / 1000.0 for p in pts]])
        for i, e in enumerate(r.exclusions):
            pts = e + e[:1]
            dpg.set_value(f'excl_{i}', [[p[0] / 1000.0 for p in pts], [p[1] / 1000.0 for p in pts]])
        for i, b in enumerate(r.barriers):
            dpg.set_value(f'bar_{i}', [[b[0][0] / 1000.0, b[1][0] / 1000.0], [b[0][1] / 1000.0, b[1][1] / 1000.0]])
        if r.has_front():
            l, rr = r.front['left'], r.front['right']
            dpg.set_value('l_front', [[l[0] / 1000.0, rr[0] / 1000.0], [l[1] / 1000.0, rr[1] / 1000.0]])
            u, n, _ = r.frame()
            mid = (np.array(l) + np.array(rr)) / 2.0
            tipp = mid + n * ARROW_MM
            pts = [mid, tipp, tipp - n * WING_MM + u * WING_MM, tipp, tipp - n * WING_MM - u * WING_MM]
            dpg.set_value('l_arrow', [[p[0] / 1000.0 for p in pts], [p[1] / 1000.0 for p in pts]])
        else:
            for tag in ('l_front', 'l_arrow'):
                dpg.set_value(tag, [[], []])
        if r.ready():
            bx, by = r.field_outline()
            dpg.set_value('l_field', [[v / 1000.0 for v in bx], [v / 1000.0 for v in by]])
        else:
            dpg.set_value('l_field', [[], []])

    def _draw_draft(self):
        d = self.draft.points
        if self.draft.active and d:
            pts = list(d)
            if dpg.is_item_hovered('plot'):
                mp = dpg.get_plot_mouse_pos()
                pts = pts + [[mp[0] * 1000.0, mp[1] * 1000.0]]
            dpg.set_value('l_draft', [[p[0] / 1000.0 for p in pts], [p[1] / 1000.0 for p in pts]])
            dpg.set_value('s_draft', [[p[0] / 1000.0 for p in d], [p[1] / 1000.0 for p in d]])
        else:
            dpg.set_value('l_draft', [[], []])
            dpg.set_value('s_draft', [[], []])

    def show(self, snap):
        """Live points of all positions with a sensor, their seen regions, and the foreground objects."""
        self._draw_seen(snap)
        px, py, fx, fy, gx, gy, ox, oy = [], [], [], [], [], [], [], []
        for v in snap['views'].values():
            ok = v['valid']
            a = ok & ~v['fg'] & v['keep']           # background that counts: grey
            b = ok & v['fg'] & v['keep']            # foreground that counts: red
            g = ok & v['fg'] & ~v['keep']           # foreground that does not count: dark red
            c = ok & ~v['fg'] & ~v['keep']          # ignored: dark
            px += list(v['mx'][a] / 1000.0)
            py += list(v['my'][a] / 1000.0)
            fx += list(v['mx'][b] / 1000.0)
            fy += list(v['my'][b] / 1000.0)
            gx += list(v['mx'][g] / 1000.0)
            gy += list(v['my'][g] / 1000.0)
            ox += list(v['mx'][c] / 1000.0)
            oy += list(v['my'][c] / 1000.0)
        dpg.set_value('s_pts', [px, py])
        dpg.set_value('s_fg_out', [gx, gy])
        dpg.set_value('s_fg', [fx, fy])
        dpg.set_value('s_out', [ox, oy])
        dpg.set_value('s_obj', [[o['x'] / 1000.0 for o in snap['objects']], [o['y'] / 1000.0 for o in snap['objects']]])

    def _draw_seen(self, snap):
        """Tints what at least one position sees, over the visible part of the map; shadows stay dark."""
        reaches = {sid: v['reach'] for sid, v in snap['views'].items() if 'reach' in v}
        if not dpg.get_value('show_seen') or not reaches:
            dpg.configure_item('seen_img', bounds_min=(0.0, 0.0), bounds_max=(0.0, 0.0))
            return
        x0, x1 = dpg.get_axis_limits('xaxis')
        y0, y1 = dpg.get_axis_limits('yaxis')
        xs = (x0 + (np.arange(SEEN_PX) + 0.5) * (x1 - x0) / SEEN_PX) * 1000.0
        ys = (y0 + (np.arange(SEEN_PX) + 0.5) * (y1 - y0) / SEEN_PX) * 1000.0
        gx, gy = np.meshgrid(xs, ys[::-1])              # texture row 0 is the top of the image
        mask = np.zeros(gx.shape, bool)
        room = self.eng.room
        for sid, r in reaches.items():
            st = room.station(sid)
            if st is not None:
                mask |= seen(st, r, gx, gy)
        img = np.zeros(gx.shape + (4,), np.float32)
        img[mask] = SEEN_RGBA
        dpg.set_value('seen_tex', img.ravel())
        dpg.configure_item('seen_img', bounds_min=(x0, y0), bounds_max=(x1, y1))

    # ------------------------------------------------ mouse
    def on_press(self):
        if dpg.is_item_hovered('plot'):
            self.press = (dpg.get_mouse_pos(local=False), dpg.get_plot_mouse_pos())
        else:
            self.press = None

    def on_release(self):
        pr = self.press
        self.press = None
        if pr is None or not dpg.is_item_hovered('plot'):
            return
        m = dpg.get_mouse_pos(local=False)
        if math.hypot(m[0] - pr[0][0], m[1] - pr[0][1]) < CLICK_PX:
            x_m, y_m = pr[1]
            if self.draft.click(x_m * 1000.0, y_m * 1000.0):
                self.finish()

    def finish(self):
        """Ends the draft: writes the shape into the room if it is complete."""
        if self.draft.complete():
            self.eng.edit(self.draft.apply)
            self.state.rebuild = True
        else:
            self.draft.cancel()

    # ------------------------------------------------ simulation
    def add_sim(self, a=None, b=None):
        """Adds a simulated object: point A (where it stands) and B (where it walks to with Move on), map mm.
        Without points it goes 1.5 m in front of the front (or the sensor), B 2 m to the right of A."""
        k = len(self.sim_points)
        r = self.eng.room
        if r.has_front():
            u, n, length = r.frame()
        else:
            st = self.state.selected(self.eng)
            u = np.array(st.to_map(np.array([1.0]), np.array([0.0]))).ravel() - np.array([st.x, st.y])
            n = None
        if a is None:
            if n is not None:
                a = np.array(r.front['left']) + u * length * (0.3 + 0.2 * (k % 3)) + n * SIM_AHEAD_MM
            else:
                mx, my = st.to_map(np.array([(k - 1) * 500.0]), np.array([SIM_AHEAD_MM]))
                a = np.array([float(mx[0]), float(my[0])])
        a = np.array(a, float)
        b = a + u / max(float(np.hypot(*u)), 1e-9) * SIM_PATH_MM if b is None else np.array(b, float)
        ta = dpg.add_drag_point(parent='plot', default_value=(a[0] / 1000.0, a[1] / 1000.0), color=SIM_C + (255,),
                                thickness=2, label=f'sim {k + 1}', show_label=True)
        tb = dpg.add_drag_point(parent='plot', default_value=(b[0] / 1000.0, b[1] / 1000.0), color=SIM_C + (255,),
                                thickness=1, label=f'sim {k + 1} B', show_label=True, show=self.sim_move)
        self.sim_points.append((ta, tb))

    def clear_sim(self):
        for ta, tb in self.sim_points:
            dpg.delete_item(ta)
            dpg.delete_item(tb)
        self.sim_points.clear()

    def set_sim_move(self, on):
        """Move on: every object starts at A and walks to B and back; off: it stands at A."""
        self.sim_move = bool(on)
        self.sim_t0 = time.monotonic()
        for _, tb in self.sim_points:
            dpg.configure_item(tb, show=self.sim_move)

    @property
    def sim_count(self):
        return len(self.sim_points)

    # ------------------------------------------------ view
    def fit(self):
        """Shows the whole room: all scans, the positions, the area and the field."""
        r = self.eng.room
        xs, ys = [-3.0, 3.0], [-3.0, 3.0]
        for st in r.stations:
            mx, my = r.scan_map(st)
            if len(mx):
                xs += [float(np.percentile(mx, 2)) / 1000.0, float(np.percentile(mx, 98)) / 1000.0]
                ys += [float(np.percentile(my, 2)) / 1000.0, float(np.percentile(my, 98)) / 1000.0]
            xs.append(st.x / 1000.0)
            ys.append(st.y / 1000.0)
        if r.ready():
            bx, by = r.field_outline()
            xs += [v / 1000.0 for v in bx]
            ys += [v / 1000.0 for v in by]
        if r.has_area():
            xs += [p[0] / 1000.0 for p in r.area]
            ys += [p[1] / 1000.0 for p in r.area]
        pad = 0.5
        dpg.set_axis_limits('xaxis', min(xs) - pad, max(xs) + pad)
        dpg.set_axis_limits('yaxis', min(ys) - pad, max(ys) + pad)
        self.unlock = 2
