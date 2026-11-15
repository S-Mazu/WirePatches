"""Motion Bridge: 2D lidar -> people in a room map -> OSC for Wire patches.

Usage:
  .venv\\Scripts\\python.exe bridge.py                 window (default)
  .venv\\Scripts\\python.exe bridge.py --headless      no window, runs until Ctrl+C
  Options: --port COM19   --osc 127.0.0.1:7000 (repeatable)

Settings are made in the window: detection and output in settings.json, everything about
the room (positions, screen, areas, barriers) in rooms/<name>/. OSC interface: README.md.
"""
import argparse
import math
import os
import sys
import time

import numpy as np

from engine import DEFAULTS, SLOTS, Engine
from room import Room

HERE = os.path.dirname(os.path.abspath(__file__))
PANEL_W = 450
FAN_HANDLE_MM = 1200.0

SLOT_COLORS = [(255, 210, 60), (90, 200, 255), (255, 120, 200), (140, 255, 140),
               (255, 160, 80), (180, 140, 255), (240, 240, 240), (120, 255, 230)]
STATION_COLORS = [(80, 140, 255), (90, 220, 150), (200, 130, 255), (255, 170, 70), (255, 110, 150), (90, 220, 230)]
OK, WARN, BAD, DIM = (110, 220, 130), (255, 190, 80), (255, 95, 85), (140, 140, 155)
SCREEN_C, AREA_C, EXCL_C, BAR_C, REF_C = (255, 255, 255), (90, 210, 120), (255, 95, 85), (255, 150, 40), (255, 230, 90)

HELP = {
    'room': 'A room holds everything measured and drawn for one venue: sensor positions, their scans, '
            'screen, areas and barriers. Switch rooms here; each is kept in rooms/<name>/.',
    'new_room': 'Creates an empty room with one position. The connected sensor moves into it.',
    'positions': 'Each position is one place where a sensor stood or stands and its scan of the empty room. '
                 'The position marked "sensor" delivers live data.\nClick a position to select it; its controls '
                 'are below. In the view, drag the coloured square to move it.',
    'learn': 'Learns what the empty room looks like from the selected position. Everything closer than '
             'the empty room later counts as a person.\nClick, leave the scan plane during the countdown and '
             'stay out until the second tone (about 5 s).\nLearn again whenever the sensor or the room changes.',
    'moved': 'You moved the sensor to another place: creates a new position that takes over the sensor, '
             'placed 1 m next to the current one. Learn the empty room there, then align its scan with the '
             'others (drag, rotate, or snap to the reference).',
    'remove_pos': 'Deletes the selected position and its scan.',
    'xy': 'Position in the room map, metres. Also by dragging the coloured square in the view.',
    'rotation': 'Turns the scan of this position until walls and objects lie on those of the other positions. '
                'Degrees, counter-clockwise.',
    'nudge': 'Turns the position in small steps.',
    'pivot': 'Turns the position around the reference marker instead of around the sensor. Use it after '
             'snapping: the reference stays in place while the walls swing into line.',
    'flip': 'Mirrors the scan of this position. Tick it if the map shows the room mirrored '
            '(e.g. a sensor mounted upside down).',
    'fan': 'Only measurements inside the fan count, the rest is ignored. 360 = all around. Point the fan '
           'with the round handle in the view.',
    'show_scans': 'Shows the empty-room scans of all positions; off shows only the selected one.',
    'ref_place': 'Puts a reference marker in the view with the next click. Place it on the reference object '
                 '(e.g. a pole) in a scan that is already aligned.',
    'ref_snap': 'Moves the selected position so that the reference object in its scan sits on the marker. '
                'Then turn it (with "Turn around reference") until the walls match.\nThe object must stand '
                'free, at least 0.5 m away from walls, and be in the room while each position learns its scan. '
                'A tube of 10 cm or more is found reliably.',
    'screen_left': 'Stand alone at the LEFT end of the screen (seen from the room), close to it, and stay '
                   'still until the second tone.',
    'screen_right': 'Stand alone at the RIGHT end of the screen (seen from the room), close to it, and stay '
                    'still until the second tone.',
    'screen_draw': 'Draws the screen in the view: click the left end, then the right end (seen from the room). '
                   'The ends can be dragged afterwards.',
    'screen_swap': 'Swaps the ends: the room side changes to the other side of the screen.',
    'screen_clear': 'Removes the screen. Without a screen nothing is sent.',
    'depth': 'Sets the Depth Range at both ends of the screen. depth = 0 at the screen, 1 at the far edge; '
             'people further away send 1.\nFor a trapezoid, drag the far corners (F) in the view: each end gets '
             'its own depth.',
    'field': 'What the patches get as fields, seen from the room: left to right along the screen, bottom = at the '
             'screen, top = far edge.\nOccupancy: where something is (white = a surface crosses the whole cell).\n'
             'Motion: where the occupancy changes (white = a cell is taken or freed within 0.25 s). Standing still '
             'fades out.',
    'reset_fields': 'Sets the field settings back to the recommended values: 32 x 16 cells, Motion fade 0.5 s, '
                    'Motion threshold 0.3.',
    'field_cols': 'Cells along the screen (at most 32, one OSC message per row).',
    'field_rows': 'Cells from the screen to the far edge.',
    'field_fade_s': 'How long motion takes to fade out after it stops.',
    'field_threshold': 'Motion below this value is ignored: sensor noise at the edges of still objects. '
                       'Recommended and default: 0.3 (measured noise up to 0.28, walking 0.5 to 1). '
                       'Lower it only in a very quiet setup; raise it if still objects show motion.',
    'draw_area': 'Draws an area: click the corners in the view, then "Finish" (or click the first corner). '
                 'With at least one area, only people inside an area count.',
    'draw_excl': 'Draws an exclusion: nobody inside it counts (a bar, a stage edge, a door).',
    'draw_bar': 'Draws a barrier: click its two ends. Sensors do not look through it; everything behind it, '
                'seen from a sensor, is ignored.',
    'finish': 'Closes the area being drawn.',
    'cancel_draw': 'Stops drawing; nothing is added.',
    'area_screen': 'Adds an area covering the screen width and the Depth Range, as a start. Tick "Edit points" '
                   'to drag its corners.',
    'edit_points': 'Shows the corners of areas and the ends of barriers as handles you can drag.',
    'countdown': 'Time between the click and the measurement, so you can walk to your place '
                 '(out of the room, or to a screen end). A tone marks the start and the end.',
    'sim': 'Adds people you can drag in the view with the mouse. They are sent like real people, '
           'so patches can be tested without walking around or without a sensor.',
    'sim_add': 'Adds a simulated person 1.5 m in front of the screen. Drag the yellow point in the view.',
    'sim_clear': 'Removes all simulated people.',
    'targets': 'Programs that receive the OSC data. Arena listens on 7000 and Wire on 8000 by default '
               '(Preferences > OSC). A green dot means a local program holds that port.',
    'add_target': 'host:port, for example 127.0.0.1:7000 or 192.168.1.20:7000.',
    'tol_mm': 'How much closer than the empty room a point must be to count as foreground. '
              'Raise it if walls or furniture flicker red.',
    'gap_mm': 'Foreground points further apart than this start a new blob.',
    'min_pts': 'Blobs with fewer points are ignored (noise, small objects).',
    'merge_mm': 'Blobs closer than this become one person (arm and body; the same person seen by two sensors).',
    'assoc_mm': 'A person found this close to a tracked one continues that track (same slot).',
    'smooth': 'Share of the new position taken each revolution. Lower = calmer, higher = faster.',
    'hold_s': 'A lost person keeps its slot this long before fading out.',
    'fade_s': 'Time for a person to fade in or out (active 0 -> 1).',
    'reset_adv': 'Restores the detection defaults.',
    'port': 'Serial port of the lidar adapter (Silicon Labs CP210x).',
    'assign': 'The sensor on this port stands at the selected position. Another position holding the port '
              'gives it up. The bridge reconnects by itself after a cable pull.',
    'release': 'The selected position keeps its scan but gets no live data.',
    'log': 'Writes one line per revolution to logs/quality_*.csv (speed, points, valid share, CRC errors, people).',
    'scale': 'Size of text and controls.',
    'fullscreen': 'Fullscreen on or off (also F11).',
    'fit': 'Shows the whole room: all scans and the screen.',
    'cancel': 'Stops the countdown or measurement.',
    'refresh': 'Reads the list of serial ports again, e.g. after plugging in the adapter.',
    'st_sensor': 'Connection to the lidar.\n'
                 'Green: data arrives.\n'
                 'Yellow: CRC errors are rising. These are damaged data packets; a few right after '
                 'plugging in are normal. If they keep rising, check the wiring and the cable.\n'
                 'Red: no data. Check the USB cable, the port under "Sensor" and that the motor spins.',
    'st_signal': 'Quality of the scan over the last 5 seconds. Green = fine, yellow = keep an eye on it, '
                 'red = act.\n\n'
                 'rev/s: turns of the sensor per second. 10 is normal (green 9 to 11). Lower or unsteady '
                 'usually means weak USB power.\n\n'
                 'valid: share of measurements that hit something within range (green from 90 %). It drops '
                 'with black or shiny surfaces, glass, open doors, direct sunlight or a dirty sensor window. '
                 'In a large open hall a lower value can be normal: compare with a quiet moment.\n\n'
                 'points/rev: measurements per turn, about 2160 at 10 rev/s. Much less means data is lost '
                 'on the way (cable, USB).\n\n'
                 'The grey line below sums up the whole run since the start.',
    'st_room': 'The empty-room scan of the selected position is its reference: anything closer counts as '
               'foreground.\n\n'
               'coverage: share of directions in which the sensor sees a wall or an object. Open sides '
               'lower it; that is fine.\n'
               'noise: typical jitter of the room measurement; below 20 mm is good.\n\n'
               'Yellow: not learned yet. Learn again whenever the sensor or the room changes.',
    'st_map': 'The screen turns positions into x and depth for the patches.\n'
              'Green: screen set.\n'
              'Red: no screen; nothing is sent. See step 3.',
    'st_osc': 'Data sent to the patches. The bridge sends one update per sensor turn, so about 10 '
              'updates/s is normal and gives smooth motion. Below 8 motion gets jerky; 0 means nothing '
              'is sent.\n\n'
              'listening: targets on this computer where a program holds the port. If fewer are listening '
              'than there are targets, switch OSC input on in Arena or Wire (see Output). Targets on other '
              'computers cannot be checked.',
    'st_people': 'People counted right now, including simulated ones. Grey: nobody there.',
    'view': 'The room from above, metres. Mouse wheel zooms, right-drag pans.\n\n'
            'Coloured dots: empty-room scan of each position; square: the sensor, line: its front; '
            'outline: its fan, round handle: points the fan.\n'
            'Grey: live scan, red: closer than the empty room, dark: ignored (outside fan or area, behind a '
            'barrier).\n'
            'White: screen with its ends L and R; the arrow points into the room; the thin outline reaches '
            'to the Depth Range.\n'
            'Green: areas, red: exclusions, orange: barriers, yellow cross: reference marker.\n'
            'Coloured rings: tracked people with slot, x and depth.',
}


def list_ports():
    try:
        from serial.tools import list_ports as lp
        return [p.device for p in lp.comports()] or ['COM19']
    except Exception:
        return ['COM19']


def station_color(room, sid):
    k = next((i for i, s in enumerate(room.stations) if s.sid == sid), 0)
    return STATION_COLORS[k % len(STATION_COLORS)]


# ---------------------------------------------------------------- window
def run_window(eng, snapshot=None, sim=0):
    import dearpygui.dearpygui as dpg

    dpg.create_context()
    font = None
    fonts_dir = os.path.join(os.environ.get('WINDIR', ''), 'Fonts')
    for name in ('segoeui.ttf', 'arial.ttf'):
        path = os.path.join(fonts_dir, name)
        if os.path.exists(path):
            with dpg.font_registry():
                font = dpg.add_font(path, 18)
            break

    def tip(item, key):
        with dpg.tooltip(item):
            dpg.add_text(HELP[key], wrap=380)
        return item

    theme_cache = {}

    def themed(color, size, marker=None, ring=False):
        key = (tuple(color), size, marker, ring)
        if key in theme_cache:
            return theme_cache[key]
        with dpg.theme() as th:
            with dpg.theme_component(dpg.mvScatterSeries):
                dpg.add_theme_color(dpg.mvPlotCol_MarkerFill, (0, 0, 0, 0) if ring else color,
                                    category=dpg.mvThemeCat_Plots)
                if ring:
                    dpg.add_theme_style(dpg.mvPlotStyleVar_MarkerWeight, 3, category=dpg.mvThemeCat_Plots)
                dpg.add_theme_color(dpg.mvPlotCol_MarkerOutline, color, category=dpg.mvThemeCat_Plots)
                dpg.add_theme_color(dpg.mvPlotCol_Line, color, category=dpg.mvThemeCat_Plots)
                dpg.add_theme_style(dpg.mvPlotStyleVar_Marker, marker or dpg.mvPlotMarker_Circle,
                                    category=dpg.mvThemeCat_Plots)
                dpg.add_theme_style(dpg.mvPlotStyleVar_MarkerSize, size, category=dpg.mvThemeCat_Plots)
            with dpg.theme_component(dpg.mvLineSeries):
                dpg.add_theme_color(dpg.mvPlotCol_Line, color, category=dpg.mvThemeCat_Plots)
                dpg.add_theme_style(dpg.mvPlotStyleVar_LineWeight, size, category=dpg.mvThemeCat_Plots)
        theme_cache[key] = th
        return th

    state = {'unlock': 2, 'last_snap': None, 'sel': None, 'mode': None, 'draft': [], 'press': None,
             'dynamic': [], 'handles': {}, 'last_set': {}, 'rebuild': True, 'room_name': None}
    sim_points = []

    def room():
        return eng.room

    def sel():
        st = room().station(state['sel'])
        if st is None:
            st = eng.live_station() or room().stations[0]
            state['sel'] = st.sid
        return st

    def changed(rebuild=False):
        eng.room_changed()
        if rebuild:
            state['rebuild'] = True

    # ---- callbacks: rooms and positions
    def on_room(sender, value):
        eng.load_room(value)
        state['sel'] = None
        state['rebuild'] = True
        on_fit()

    def on_new_room():
        name = dpg.get_value('new_room_name')
        if eng.new_room(name):
            dpg.set_value('new_room_name', '')
            state['sel'] = None
            state['rebuild'] = True
            on_fit()
        else:
            eng.notify('Room name empty or already used.', 'error')

    def on_select(sender, value):
        sid = value.split(' ', 1)[0]
        state['sel'] = sid

    def on_moved():
        st = sel()
        new = eng.add_position(st.sid if st else None)
        state['sel'] = new.sid
        state['rebuild'] = True
        eng.notify(f'{new.name} created and holds the sensor. Learn the empty room there, then align it.', 'ok')

    def on_remove_pos():
        st = sel()
        if st and eng.remove_position(st.sid):
            state['sel'] = None
            state['rebuild'] = True
        else:
            eng.notify('The last position cannot be removed.', 'error')

    def on_xy(sender, value, axis):
        st = sel()
        with eng.lock:
            setattr(st, axis, float(value) * 1000.0)
        changed()

    def set_rotation(value):
        st = sel()
        pivot = room().ref if dpg.get_value('pivot') and room().ref else None
        with eng.lock:
            room().rotate_station(st, float(value), pivot)
        changed()

    def on_rot(sender, value):
        set_rotation(value)

    def on_nudge(sender, app_data, step):
        set_rotation(sel().rot + step)

    def on_flip(sender, value):
        with eng.lock:
            sel().flip = bool(value)
        changed()

    def on_fan(sender, value):
        with eng.lock:
            sel().fan_span = float(value)
        changed()

    def on_snap():
        st = sel()
        if room().ref is None:
            eng.notify('Place the reference marker first.', 'error')
            return
        with eng.lock:
            ok = room().snap_to_reference(st)
        if ok:
            changed()
            eng.notify(f'{st.name} snapped to the reference. Now turn it until the walls match.', 'ok')
        else:
            eng.notify('No free-standing object found near the marker in this scan. Drag the position '
                       'closer and try again.', 'error')

    # ---- callbacks: screen, shapes
    def set_mode(mode):
        state['mode'] = mode
        state['draft'] = []

    def on_swap():
        if room().screen:
            with eng.lock:
                s = room().screen
                room().screen = {'left': s.get('right'), 'right': s.get('left')}
            changed(True)

    def on_screen_clear():
        with eng.lock:
            room().screen = None
        changed(True)

    def on_depth(sender, value):
        with eng.lock:
            room().depth_l_mm = room().depth_r_mm = float(value) * 1000.0
        changed()

    def on_area_screen():
        if not room().has_screen():
            eng.notify('Set the screen first.', 'error')
            return
        with eng.lock:
            room().area_from_screen()
        changed(True)

    def finish_draft():
        mode, pts = state['mode'], state['draft']
        with eng.lock:
            if mode in ('area', 'exclude') and len(pts) >= 3:
                room().areas.append({'kind': 'include' if mode == 'area' else 'exclude', 'pts': pts})
            elif mode == 'barrier' and len(pts) == 2:
                room().barriers.append(pts)
            elif mode == 'screen' and len(pts) == 2:
                room().screen = {'left': pts[0], 'right': pts[1]}
            elif mode == 'ref' and len(pts) == 1:
                room().ref = pts[0]
        set_mode(None)
        changed(True)

    def on_remove_shape(sender, app_data, what):
        kind, i = what
        with eng.lock:
            if kind == 'area':
                del room().areas[i]
            else:
                del room().barriers[i]
        changed(True)

    def on_plot_click(x_m, y_m):
        mode = state['mode']
        if not mode:
            return
        p = [x_m * 1000.0, y_m * 1000.0]
        d = state['draft']
        if mode in ('area', 'exclude') and len(d) >= 3 and math.hypot(p[0] - d[0][0], p[1] - d[0][1]) < 250.0:
            finish_draft()
            return
        d.append(p)
        need = {'barrier': 2, 'screen': 2, 'ref': 1}.get(mode)
        if need and len(d) >= need:
            finish_draft()

    def on_press(sender, app_data):
        if dpg.is_item_hovered('plot'):
            state['press'] = (dpg.get_mouse_pos(local=False), dpg.get_plot_mouse_pos())
        else:
            state['press'] = None

    def on_release(sender, app_data):
        pr = state['press']
        state['press'] = None
        if pr is None or not dpg.is_item_hovered('plot'):
            return
        m = dpg.get_mouse_pos(local=False)
        if math.hypot(m[0] - pr[0][0], m[1] - pr[0][1]) < 5:
            on_plot_click(*pr[1])

    # ---- callbacks: simulation, output, sensor, display
    def on_slider(sender, value, key):
        eng.set(key, value)

    def on_sim_add():
        k = len(sim_points)
        if room().has_screen():
            u, n, length = room().frame()
            c = (np.array(room().screen['left']) + u * length * (0.3 + 0.2 * (k % 3)) + n * 1500.0) / 1000.0
            pos = (float(c[0]), float(c[1]))
        else:
            st = sel()
            mx, my = st.to_map(np.array([(k - 1) * 500.0]), np.array([1500.0]))
            pos = (float(mx[0]) / 1000.0, float(my[0]) / 1000.0)
        tag = dpg.add_drag_point(parent='plot', default_value=pos, color=(255, 210, 60, 255),
                                 thickness=2, label=f'sim {k + 1}', show_label=True)
        sim_points.append(tag)

    def on_sim_clear():
        for tag in sim_points:
            dpg.delete_item(tag)
        sim_points.clear()

    def on_add_target():
        t = dpg.get_value('new_target').strip()
        host, _, port = t.rpartition(':')
        if not host or not port.isdigit():
            eng.notify('Target must be host:port, e.g. 127.0.0.1:7000.', 'error')
            return
        if t not in eng.s['osc_targets']:
            eng.set_targets(eng.s['osc_targets'] + [t])
        dpg.set_value('new_target', '')
        rebuild_targets()

    def on_remove_target(sender, app_data, t):
        eng.set_targets([x for x in eng.s['osc_targets'] if x != t])
        rebuild_targets()

    def rebuild_targets():
        dpg.delete_item('targets', children_only=True)
        for t in eng.s['osc_targets']:
            with dpg.group(horizontal=True, parent='targets'):
                dpg.add_text('●', tag=f'dot_{t}', color=DIM)
                dpg.add_text(t)
                b = dpg.add_button(label='remove', small=True, callback=on_remove_target, user_data=t)
                with dpg.tooltip(b):
                    dpg.add_text(f'Stop sending to {t}.')

    def on_reset_fields():
        for k in ('field_cols', 'field_rows', 'field_fade_s', 'field_threshold'):
            eng.set(k, DEFAULTS[k])
            dpg.set_value(k, DEFAULTS[k])

    def on_reset_adv():
        eng.reset_advanced()
        for k in ('tol_mm', 'gap_mm', 'min_pts', 'merge_mm', 'assoc_mm', 'smooth', 'hold_s', 'fade_s'):
            dpg.set_value(k, eng.s[k])

    def on_assign():
        eng.set_port(sel().sid, dpg.get_value('port'))
        state['rebuild'] = True

    def on_release_port():
        eng.set_port(sel().sid, None)
        state['rebuild'] = True

    def on_refresh_ports():
        dpg.configure_item('port', items=list_ports())

    def on_scale(sender, value):
        dpg.set_global_font_scale(value)
        eng.set('ui_scale', value)

    def on_fit():
        xs, ys = [-3.0, 3.0], [-3.0, 3.0]
        for st in room().stations:
            mx, my = room().scan_map(st)
            if len(mx):
                xs += [float(np.percentile(mx, 2)) / 1000.0, float(np.percentile(mx, 98)) / 1000.0]
                ys += [float(np.percentile(my, 2)) / 1000.0, float(np.percentile(my, 98)) / 1000.0]
            xs.append(st.x / 1000.0)
            ys.append(st.y / 1000.0)
        if room().has_screen():
            bx, by = room().screen_band()
            xs += [v / 1000.0 for v in bx]
            ys += [v / 1000.0 for v in by]
        pad = 0.5
        dpg.set_axis_limits('xaxis', min(xs) - pad, max(xs) + pad)
        dpg.set_axis_limits('yaxis', min(ys) - pad, max(ys) + pad)
        state['unlock'] = 2

    with dpg.colormap_registry():
        dpg.add_colormap([[20, 20, 28, 255], [255, 255, 255, 255]], False, tag='cm_field')   # 0 dark .. 1 white

    # ---- layout
    with dpg.window(tag='main', no_scrollbar=True):
        with dpg.group(horizontal=True):
            with dpg.plot(tag='plot', width=-PANEL_W - 10, height=-1, equal_aspects=True, no_menus=True,
                          no_box_select=True, pan_button=dpg.mvMouseButton_Right,
                          fit_button=dpg.mvMouseButton_Middle):
                dpg.add_plot_axis(dpg.mvXAxis, label='x (m)', tag='xaxis')
                with dpg.plot_axis(dpg.mvYAxis, label='y (m)', tag='yaxis'):
                    dpg.add_line_series([], [], tag='l_band')
                    dpg.add_scatter_series([], [], tag='s_out')
                    dpg.add_scatter_series([], [], tag='s_pts')
                    dpg.add_scatter_series([], [], tag='s_fg')
                    dpg.add_line_series([], [], tag='l_screen')
                    dpg.add_line_series([], [], tag='l_normal')
                    dpg.add_line_series([], [], tag='l_draft')
                    dpg.add_scatter_series([], [], tag='s_draft')
                    for k in range(SLOTS):
                        dpg.add_scatter_series([], [], tag=f's_slot{k}')
                for k in range(SLOTS):
                    dpg.add_plot_annotation(tag=f'a_slot{k}', default_value=(0.0, 0.0),
                                            offset=(18, -18) if k % 2 == 0 else (18, 22),
                                            color=SLOT_COLORS[k] + (255,), show=False)
            with dpg.child_window(width=PANEL_W, height=-1, tag='panel'):
                with dpg.group(horizontal=True):
                    dpg.add_text('Motion Bridge', color=(255, 255, 255))
                    tip(dpg.add_text('(?) the view', color=(120, 170, 255)), 'view')
                with dpg.group(horizontal=True):
                    tip(dpg.add_button(label='Fullscreen (F11)', callback=lambda: dpg.toggle_viewport_fullscreen()),
                        'fullscreen')
                    tip(dpg.add_button(label='Fit view', callback=on_fit), 'fit')
                dpg.add_separator()

                with dpg.collapsing_header(label='Status', default_open=True):
                    for key in ('st_sensor', 'st_signal', 'st_room', 'st_map', 'st_osc', 'st_people'):
                        with dpg.group(horizontal=True):
                            tip(dpg.add_text('●', tag=key + '_dot', color=DIM), key)
                            tip(dpg.add_text('', tag=key, wrap=PANEL_W - 60), key)
                        if key == 'st_signal':
                            tip(dpg.add_text('', tag='st_session', color=DIM, indent=22, wrap=PANEL_W - 60),
                                'st_signal')
                    dpg.add_text('', tag='st_msg', wrap=PANEL_W - 30)
                    with dpg.group(tag='mode_group', show=False):
                        dpg.add_text('', tag='mode_text', color=WARN, wrap=PANEL_W - 30)
                        with dpg.group(horizontal=True):
                            tip(dpg.add_button(label='Finish', tag='finish_btn', callback=finish_draft), 'finish')
                            tip(dpg.add_button(label='Cancel drawing', callback=lambda: set_mode(None)), 'cancel_draw')
                    with dpg.group(tag='task_group', show=False):
                        dpg.add_text('', tag='task_text', color=WARN, wrap=PANEL_W - 30)
                        dpg.add_progress_bar(tag='task_bar', width=-1)
                        tip(dpg.add_button(label='Cancel', callback=lambda: eng.cancel()), 'cancel')

                with dpg.collapsing_header(label='1  Room', default_open=True):
                    tip(dpg.add_combo(Room.names(eng.rooms_dir), tag='room_combo', default_value=eng.room.name,
                                      width=-1, callback=on_room), 'room')
                    with dpg.group(horizontal=True):
                        dpg.add_input_text(tag='new_room_name', hint='name of a new room', width=220)
                        tip(dpg.add_button(label='New room', callback=on_new_room), 'new_room')

                with dpg.collapsing_header(label='2  Sensor positions', default_open=True):
                    tip(dpg.add_radio_button([], tag='pos_list', callback=on_select), 'positions')
                    tip(dpg.add_slider_float(label='Countdown', tag='countdown', min_value=0, max_value=60,
                                             default_value=float(eng.s['countdown_s']), format='%.0f s',
                                             callback=on_slider, user_data='countdown_s'), 'countdown')
                    tip(dpg.add_button(label='Learn empty room here', width=-1,
                                       callback=lambda: eng.request('background', sel().sid)), 'learn')
                    with dpg.group(horizontal=True):
                        tip(dpg.add_button(label='Sensor moved: new position', callback=on_moved), 'moved')
                        tip(dpg.add_button(label='Remove', callback=on_remove_pos), 'remove_pos')
                    dpg.add_text('', tag='sel_title', color=DIM)
                    with dpg.group(horizontal=True):
                        tip(dpg.add_input_float(label='X', tag='pos_x', width=110, step=0, format='%.2f m',
                                                on_enter=True, callback=on_xy, user_data='x'), 'xy')
                        tip(dpg.add_input_float(label='Y', tag='pos_y', width=110, step=0, format='%.2f m',
                                                on_enter=True, callback=on_xy, user_data='y'), 'xy')
                    tip(dpg.add_slider_float(label='Rotation', tag='rot', min_value=-180, max_value=180,
                                             format='%.1f deg', callback=on_rot), 'rotation')
                    with dpg.group(horizontal=True):
                        for step in (-5.0, -1.0, -0.1, 0.1, 1.0, 5.0):
                            tip(dpg.add_button(label=f'{step:+g}', callback=on_nudge, user_data=step), 'nudge')
                    with dpg.group(horizontal=True):
                        tip(dpg.add_checkbox(label='Turn around reference', tag='pivot'), 'pivot')
                        tip(dpg.add_checkbox(label='Mirrored', tag='flip', callback=on_flip), 'flip')
                    tip(dpg.add_slider_float(label='Fan', tag='fan', min_value=10, max_value=360,
                                             format='%.0f deg', callback=on_fan), 'fan')
                    tip(dpg.add_checkbox(label='Show scans of all positions', tag='show_scans', default_value=True),
                        'show_scans')
                    dpg.add_text('Reference object (e.g. a pole):', color=DIM)
                    with dpg.group(horizontal=True):
                        tip(dpg.add_button(label='Place marker', callback=lambda: set_mode('ref')), 'ref_place')
                        tip(dpg.add_button(label='Snap position to marker', callback=on_snap), 'ref_snap')

                with dpg.collapsing_header(label='3  Screen', default_open=True):
                    with dpg.group(horizontal=True):
                        tip(dpg.add_button(label='Draw in view', callback=lambda: set_mode('screen')), 'screen_draw')
                        tip(dpg.add_button(label='Swap ends', callback=on_swap), 'screen_swap')
                        tip(dpg.add_button(label='Remove', callback=on_screen_clear), 'screen_clear')
                    with dpg.group(horizontal=True):
                        tip(dpg.add_button(label='Stand at left end', callback=lambda: eng.request('left')),
                            'screen_left')
                        tip(dpg.add_button(label='Stand at right end', callback=lambda: eng.request('right')),
                            'screen_right')
                    tip(dpg.add_slider_float(label='Depth Range', tag='depth', min_value=0.5, max_value=30.0,
                                             format='%.1f m', callback=on_depth), 'depth')

                with dpg.collapsing_header(label='4  Areas and barriers', default_open=True):
                    with dpg.group(horizontal=True):
                        tip(dpg.add_button(label='Draw area', callback=lambda: set_mode('area')), 'draw_area')
                        tip(dpg.add_button(label='Draw exclusion', callback=lambda: set_mode('exclude')), 'draw_excl')
                        tip(dpg.add_button(label='Draw barrier', callback=lambda: set_mode('barrier')), 'draw_bar')
                    with dpg.group(horizontal=True):
                        tip(dpg.add_button(label='Area from screen', callback=on_area_screen), 'area_screen')
                        tip(dpg.add_checkbox(label='Edit points', tag='edit_points',
                                             callback=lambda: state.update(rebuild=True)), 'edit_points')
                    dpg.add_group(tag='shapes')

                with dpg.collapsing_header(label='5  Advanced detection', default_open=False):
                    spec = [('tol_mm', 'Tolerance', 25, 2000, '%.0f mm'), ('gap_mm', 'Blob gap', 50, 2000, '%.0f mm'),
                            ('merge_mm', 'Merge distance', 0, 3000, '%.0f mm'),
                            ('assoc_mm', 'Track distance', 100, 5000, '%.0f mm'),
                            ('smooth', 'Smoothing', 0.05, 1.0, '%.2f'), ('hold_s', 'Hold', 0.0, 5.0, '%.1f s'),
                            ('fade_s', 'Fade', 0.02, 5.0, '%.2f s')]
                    for key, label, lo, hi, fmt in spec:
                        tip(dpg.add_slider_float(label=label, tag=key, min_value=lo, max_value=hi,
                                                 default_value=float(eng.s[key]), format=fmt,
                                                 callback=on_slider, user_data=key), key)
                    tip(dpg.add_slider_int(label='Min points', tag='min_pts', min_value=1, max_value=100,
                                           default_value=int(eng.s['min_pts']), callback=on_slider,
                                           user_data='min_pts'), 'min_pts')
                    tip(dpg.add_button(label='Reset to defaults', callback=on_reset_adv), 'reset_adv')

                with dpg.collapsing_header(label='Fields', default_open=True):
                    tip(dpg.add_text('Occupancy                      Motion', color=DIM), 'field')
                    with dpg.group(horizontal=True):
                        for tag in ('occ', 'mot'):
                            with dpg.plot(tag=f'fp_{tag}', width=(PANEL_W - 40) // 2, height=150, no_menus=True,
                                          no_mouse_pos=True, no_inputs=True, no_title=True):
                                dpg.add_plot_axis(dpg.mvXAxis, tag=f'fx_{tag}', no_tick_labels=True,
                                                  no_gridlines=True)
                                with dpg.plot_axis(dpg.mvYAxis, tag=f'fy_{tag}', no_tick_labels=True,
                                                   no_gridlines=True):
                                    dpg.add_heat_series([0.0], 1, 1, tag=f'fh_{tag}', scale_min=0.0, scale_max=1.0,
                                                        bounds_min=(0.0, 0.0), bounds_max=(1.0, 1.0), format='')
                            dpg.bind_colormap(f'fp_{tag}', 'cm_field')
                    for key, label, lo, hi, fmt in (('field_cols', 'Columns', 1, 32, None),
                                                    ('field_rows', 'Rows', 1, 64, None),
                                                    ('field_fade_s', 'Motion fade', 0.0, 5.0, '%.2f s'),
                                                    ('field_threshold', 'Motion threshold', 0.0, 0.9, '%.2f')):
                        if fmt is None:
                            tip(dpg.add_slider_int(label=label, tag=key, min_value=lo, max_value=hi,
                                                   default_value=int(eng.s[key]), callback=on_slider,
                                                   user_data=key), key)
                        else:
                            tip(dpg.add_slider_float(label=label, tag=key, min_value=lo, max_value=hi,
                                                     default_value=float(eng.s[key]), format=fmt,
                                                     callback=on_slider, user_data=key), key)
                    tip(dpg.add_button(label='Reset to recommended', callback=on_reset_fields), 'reset_fields')

                with dpg.collapsing_header(label='Output (OSC)', default_open=True):
                    tip(dpg.add_text('Targets:', color=DIM), 'targets')
                    dpg.add_group(tag='targets')
                    with dpg.group(horizontal=True):
                        tip(dpg.add_input_text(tag='new_target', hint='host:port', width=200, on_enter=True,
                                               callback=on_add_target), 'add_target')
                        tip(dpg.add_button(label='Add', callback=on_add_target), 'add_target')
                    with dpg.table(header_row=True, borders_innerH=True, policy=dpg.mvTable_SizingStretchSame):
                        for h in ('slot', 'x', 'depth', 'active'):
                            dpg.add_table_column(label=h)
                        for k in range(SLOTS):
                            with dpg.table_row():
                                for c in ('n', 'x', 'd', 'a'):
                                    dpg.add_text(str(k) if c == 'n' else '', tag=f'cell_{c}{k}', color=DIM)

                with dpg.collapsing_header(label='Simulation', default_open=False):
                    tip(dpg.add_text('Drag the yellow points in the view.', color=DIM), 'sim')
                    with dpg.group(horizontal=True):
                        tip(dpg.add_button(label='Add person', callback=on_sim_add), 'sim_add')
                        tip(dpg.add_button(label='Remove all', callback=on_sim_clear), 'sim_clear')

                with dpg.collapsing_header(label='Sensor', default_open=False):
                    with dpg.group(horizontal=True):
                        live = eng.live_station()
                        tip(dpg.add_combo(list_ports(), tag='port', default_value=live.port if live else 'COM19',
                                          width=110), 'port')
                        tip(dpg.add_button(label='Refresh', callback=on_refresh_ports), 'refresh')
                    with dpg.group(horizontal=True):
                        tip(dpg.add_button(label='Sensor is at selected position', callback=on_assign), 'assign')
                        tip(dpg.add_button(label='Release', callback=on_release_port), 'release')
                    tip(dpg.add_checkbox(label='Write quality log (CSV)', default_value=bool(eng.s['log_quality']),
                                         callback=on_slider, user_data='log_quality'), 'log')
                    dpg.add_text('', tag='log_path', color=DIM, wrap=PANEL_W - 30)

                with dpg.collapsing_header(label='Display', default_open=False):
                    tip(dpg.add_slider_float(label='UI scale', min_value=0.7, max_value=2.5,
                                             default_value=float(eng.s['ui_scale']), format='%.2f',
                                             callback=on_scale), 'scale')

    # ---- static themes
    for tag, color, size in [('s_out', (70, 70, 85), 1.5), ('s_pts', (175, 175, 185), 1.5),
                             ('s_fg', (255, 80, 70), 2.5), ('l_screen', SCREEN_C, 4.0),
                             ('l_normal', SCREEN_C, 2.0), ('l_band', (150, 150, 165), 1.0),
                             ('l_draft', (255, 255, 120), 2.0), ('s_draft', (255, 255, 120), 5.0)]:
        dpg.bind_item_theme(tag, themed(color, size))
    for k in range(SLOTS):
        dpg.bind_item_theme(f's_slot{k}', themed(SLOT_COLORS[k], 16, dpg.mvPlotMarker_Circle, ring=True))
    rebuild_targets()

    with dpg.handler_registry():
        dpg.add_key_press_handler(dpg.mvKey_F11, callback=lambda: dpg.toggle_viewport_fullscreen())
        dpg.add_key_press_handler(dpg.mvKey_Escape, callback=lambda: set_mode(None))
        dpg.add_mouse_click_handler(button=dpg.mvMouseButton_Left, callback=on_press)
        dpg.add_mouse_release_handler(button=dpg.mvMouseButton_Left, callback=on_release)

    dpg.create_viewport(title='Motion Bridge', width=1550, height=1000, resizable=True)
    dpg.setup_dearpygui()
    if font:
        dpg.bind_font(font)
    dpg.set_global_font_scale(float(eng.s['ui_scale']))
    dpg.set_primary_window('main', True)
    dpg.show_viewport()

    # ---- view items that depend on the room
    def add_handle(tag, get, setf, color, label, select=None):
        dpg.add_drag_point(tag=tag, parent='plot', default_value=get(), color=color + (255,), thickness=2,
                           show_label=False,
                           callback=(lambda s, a, u: state.update(sel=u)) if select else None, user_data=select)
        state['dynamic'].append(tag)
        if label:                       # drag points draw no label; an annotation follows the handle
            dpg.add_plot_annotation(tag=f'ann_{tag}', parent='plot', default_value=get(), label=label,
                                    offset=(14, -14), color=tuple(int(v * 0.6) for v in color) + (255,))
            state['dynamic'].append(f'ann_{tag}')
        state['handles'][tag] = (get, setf)
        state['last_set'][tag] = None

    def add_series(tag, kind, color, size):
        fn = dpg.add_scatter_series if kind == 'scatter' else dpg.add_line_series
        fn([], [], tag=tag, parent='yaxis')
        dpg.bind_item_theme(tag, themed(color, size))
        state['dynamic'].append(tag)

    def rebuild():
        for tag in state['dynamic']:
            if dpg.does_item_exist(tag):
                dpg.delete_item(tag)
        state['dynamic'], state['handles'], state['last_set'] = [], {}, {}
        r = room()
        edit = dpg.get_value('edit_points')
        for i, a in enumerate(r.areas):
            add_series(f'area_{i}', 'line', AREA_C if a['kind'] == 'include' else EXCL_C, 2.0)
            if edit:
                for j in range(len(a['pts'])):
                    add_handle(f'h_v_{i}_{j}', lambda i=i, j=j: tuple(v / 1000.0 for v in room().areas[i]['pts'][j]),
                               lambda x, y, i=i, j=j: room().areas[i]['pts'].__setitem__(j, [x * 1000.0, y * 1000.0]),
                               AREA_C if a['kind'] == 'include' else EXCL_C, '')
        for i in range(len(r.barriers)):
            add_series(f'bar_{i}', 'line', BAR_C, 4.0)
            if edit:
                for j in (0, 1):
                    add_handle(f'h_b_{i}_{j}', lambda i=i, j=j: tuple(v / 1000.0 for v in room().barriers[i][j]),
                               lambda x, y, i=i, j=j: room().barriers[i].__setitem__(j, [x * 1000.0, y * 1000.0]),
                               BAR_C, '')
        for st in r.stations:
            c = station_color(r, st.sid)
            add_series(f'scan_{st.sid}', 'scatter', c, 1.5)
            add_series(f'fan_{st.sid}', 'line', tuple(int(v * 0.7) for v in c), 1.0)
            add_series(f'head_{st.sid}', 'line', c, 3.0)
            add_handle(f'h_st_{st.sid}', lambda sid=st.sid: (room().station(sid).x / 1000.0, room().station(sid).y / 1000.0),
                       lambda x, y, sid=st.sid: (setattr(room().station(sid), 'x', x * 1000.0),
                                                 setattr(room().station(sid), 'y', y * 1000.0)),
                       c, st.sid + (' (sensor)' if st.port else ''), select=st.sid)
            if st.fan_span < 360.0:
                add_handle(f'h_fan_{st.sid}',
                           lambda sid=st.sid: tuple(v / 1000.0 for v in room().station(sid).fan_handle(FAN_HANDLE_MM)),
                           lambda x, y, sid=st.sid: room().station(sid).set_fan_towards(x * 1000.0, y * 1000.0),
                           c, '', select=st.sid)
        if r.screen and r.screen.get('left') and r.screen.get('right'):
            for end, lab in (('left', 'L'), ('right', 'R')):
                add_handle(f'h_{end}', lambda end=end: tuple(v / 1000.0 for v in room().screen[end]),
                           lambda x, y, end=end: room().screen.__setitem__(end, [x * 1000.0, y * 1000.0]),
                           SCREEN_C, lab)
            if r.has_screen():
                for end, lab in (('left', 'F'), ('right', 'F')):
                    add_handle(f'h_far_{end}', lambda end=end: tuple(v / 1000.0 for v in room().far_corner(end)),
                               lambda x, y, end=end: room().set_far_corner(end, x * 1000.0, y * 1000.0),
                               (150, 150, 165), lab)
        if r.ref:
            add_handle('h_ref', lambda: tuple(v / 1000.0 for v in room().ref),
                       lambda x, y: setattr(room(), 'ref', [x * 1000.0, y * 1000.0]), REF_C, 'ref')
        # panel lists
        names = [f'{s.sid}  {s.name}' + ('  (sensor)' if s.port else '') for s in r.stations]
        cur = sel()
        dpg.configure_item('pos_list', items=names)
        dpg.set_value('pos_list', next((n for n in names if n.split(' ', 1)[0] == cur.sid), names[0]))
        dpg.delete_item('shapes', children_only=True)
        for i, a in enumerate(r.areas):
            with dpg.group(horizontal=True, parent='shapes'):
                dpg.add_text(f"{'Area' if a['kind'] == 'include' else 'Exclusion'} {i + 1} ({len(a['pts'])} corners)",
                             color=AREA_C if a['kind'] == 'include' else EXCL_C)
                dpg.add_button(label='remove', small=True, callback=on_remove_shape, user_data=('area', i))
        for i in range(len(r.barriers)):
            with dpg.group(horizontal=True, parent='shapes'):
                dpg.add_text(f'Barrier {i + 1}', color=BAR_C)
                dpg.add_button(label='remove', small=True, callback=on_remove_shape, user_data=('bar', i))
        dpg.configure_item('room_combo', items=Room.names(eng.rooms_dir))
        dpg.set_value('room_combo', r.name)
        state['room_name'] = r.name
        state['rebuild'] = False

    def sync_handles():
        moved = False
        for tag, (get, setf) in state['handles'].items():
            cur = tuple(dpg.get_value(tag)[:2])
            last = state['last_set'].get(tag)
            if last is not None and (abs(cur[0] - last[0]) > 1e-7 or abs(cur[1] - last[1]) > 1e-7):
                with eng.lock:
                    setf(*cur)
                state['last_set'][tag] = cur
                moved = True
            else:
                m = get()
                if last is None or abs(m[0] - last[0]) > 1e-7 or abs(m[1] - last[1]) > 1e-7:
                    dpg.set_value(tag, m)
                    state['last_set'][tag] = m
            if dpg.does_item_exist(f'ann_{tag}'):
                dpg.configure_item(f'ann_{tag}', default_value=state['last_set'][tag])
        if moved:
            eng.room_changed()

    def draw_room():
        r = room()
        cur = sel()
        all_scans = dpg.get_value('show_scans')
        for st in r.stations:
            if all_scans or st.sid == cur.sid:
                mx, my = r.scan_map(st)
                dpg.set_value(f'scan_{st.sid}', [list(mx / 1000.0), list(my / 1000.0)])
            else:
                dpg.set_value(f'scan_{st.sid}', [[], []])
            fx, fy = st.fan_outline(FAN_HANDLE_MM if st.fan_span < 360.0 else 400.0)
            dpg.set_value(f'fan_{st.sid}', [[v / 1000.0 for v in fx], [v / 1000.0 for v in fy]])
            hx, hy = st.to_map(np.array([0.0, 0.0]), np.array([0.0, 600.0]))
            dpg.set_value(f'head_{st.sid}', [list(hx / 1000.0), list(hy / 1000.0)])
        for i, a in enumerate(r.areas):
            pts = a['pts'] + a['pts'][:1]
            dpg.set_value(f'area_{i}', [[p[0] / 1000.0 for p in pts], [p[1] / 1000.0 for p in pts]])
        for i, b in enumerate(r.barriers):
            dpg.set_value(f'bar_{i}', [[b[0][0] / 1000.0, b[1][0] / 1000.0], [b[0][1] / 1000.0, b[1][1] / 1000.0]])
        if r.has_screen():
            l, rr = r.screen['left'], r.screen['right']
            dpg.set_value('l_screen', [[l[0] / 1000.0, rr[0] / 1000.0], [l[1] / 1000.0, rr[1] / 1000.0]])
            bx, by = r.screen_band()
            dpg.set_value('l_band', [[v / 1000.0 for v in bx], [v / 1000.0 for v in by]])
            u, n, length = r.frame()
            mid = (np.array(l) + np.array(rr)) / 2.0
            tipp = mid + n * 700.0
            wing = 180.0
            pts = [mid, tipp, tipp - n * wing + u * wing, tipp, tipp - n * wing - u * wing]
            dpg.set_value('l_normal', [[p[0] / 1000.0 for p in pts], [p[1] / 1000.0 for p in pts]])
        else:
            for tag in ('l_screen', 'l_band', 'l_normal'):
                dpg.set_value(tag, [[], []])
        d = state['draft']
        if state['mode'] and d:
            pts = list(d)
            if dpg.is_item_hovered('plot'):
                mp = dpg.get_plot_mouse_pos()
                pts = pts + [[mp[0] * 1000.0, mp[1] * 1000.0]]
            dpg.set_value('l_draft', [[p[0] / 1000.0 for p in pts], [p[1] / 1000.0 for p in pts]])
            dpg.set_value('s_draft', [[p[0] / 1000.0 for p in d], [p[1] / 1000.0 for p in d]])
        else:
            dpg.set_value('l_draft', [[], []])
            dpg.set_value('s_draft', [[], []])

    def panel_values():
        st = sel()
        dpg.set_value('sel_title', f'Selected: {st.sid}  {st.name}' + ('  (sensor)' if st.port else '  (scan only)'))
        for tag, val in (('pos_x', st.x / 1000.0), ('pos_y', st.y / 1000.0), ('rot', st.rot),
                         ('fan', st.fan_span), ('flip', st.flip), ('depth', max(room().depth_l_mm, room().depth_r_mm) / 1000.0)):
            if not dpg.is_item_active(tag):
                dpg.set_value(tag, val)
        if st.fan_span < 360.0 and f'h_fan_{st.sid}' not in state['handles'] or \
                st.fan_span >= 360.0 and f'h_fan_{st.sid}' in state['handles']:
            state['rebuild'] = True
        labels = {'area': 'Drawing an area: click the corners in the view, then Finish (or click the first corner).',
                  'exclude': 'Drawing an exclusion: click the corners in the view, then Finish.',
                  'barrier': 'Drawing a barrier: click its two ends in the view.',
                  'screen': 'Drawing the screen: click the LEFT end, then the RIGHT end (seen from the room).',
                  'ref': 'Click the reference object in the view.'}
        mode = state['mode']
        dpg.configure_item('mode_group', show=mode is not None)
        if mode:
            dpg.set_value('mode_text', labels[mode] + '  (Esc cancels)')
            dpg.configure_item('finish_btn', show=mode in ('area', 'exclude'))

    # ---- frame update
    def update():
        if state['rebuild'] or state['room_name'] != room().name:
            rebuild()
        if state['unlock'] > 0:
            state['unlock'] -= 1
            if state['unlock'] == 0:
                dpg.set_axis_limits_auto('xaxis')
                dpg.set_axis_limits_auto('yaxis')

        sync_handles()
        panel_values()
        draw_room()
        eng.sim = [[v * 1000.0 for v in dpg.get_value(tag)[:2]] for tag in sim_points]

        snap = eng.snapshot()
        rd = eng.reader
        now = time.time()
        st = sel()

        # status lines
        rq = eng.quality.recent()
        live_st = eng.live_station()
        port = live_st.port if live_st else None
        live = rd is not None and rd.connected and rq and rq['age'] < 1.0
        if rd is None:
            set_status('st_sensor', BAD, 'Sensor: no position holds a port (see Sensor)')
            set_status('st_signal', DIM, 'Signal: no data')
        elif live:
            crc_col = WARN if rq['crc_new'] > 0 else OK
            set_status('st_sensor', crc_col, f'Sensor {port} at {live_st.sid}: running, {rd.crc_bad} CRC errors'
                       + (' (rising)' if rq['crc_new'] > 0 else ''))
            exp_pts = 21600.0 / max(rq['hz'], 0.1)          # STL-27L: 21600 measurements per second
            c_hz = OK if 9.0 <= rq['hz'] <= 11.0 else (WARN if 7.0 <= rq['hz'] <= 13.0 else BAD)
            c_val = OK if rq['valid'] >= 90.0 else (WARN if rq['valid'] >= 70.0 else BAD)
            ratio = rq['points'] / exp_pts
            c_pts = OK if ratio >= 0.93 else (WARN if ratio >= 0.8 else BAD)
            worst = BAD if BAD in (c_hz, c_val, c_pts) else (WARN if WARN in (c_hz, c_val, c_pts) else OK)
            set_status('st_signal', worst, f"Signal: {rq['hz']:.1f} rev/s, valid {rq['valid']:.0f} %, "
                                           f"{rq['points']:.0f} points/rev")
        else:
            set_status('st_sensor', BAD, f'Sensor {port} at {live_st.sid}: '
                       + ('connected, no data' if rd.connected else 'not connected')
                       + (f' ({rd.last_error[:60]})' if rd.last_error and not rd.connected else ''))
            set_status('st_signal', DIM, 'Signal: no data')
        q = eng.quality.summary()
        dpg.set_value('st_session', f"Since start: {q['minutes']:.0f} min, {q['revs']} revolutions, "
                                    f"lowest valid {q['valid_min']:.0f} %" if q else '')
        dpg.set_value('log_path', f"Log: logs/{os.path.basename(eng.quality.log_path)}"
                      if eng.quality.log else '')
        if st.bg is not None and st.bg.ok:
            cover, n_med, _ = st.bg.stats()
            when = time.strftime('%d.%m. %H:%M', time.localtime(st.bg.learned_at)) if st.bg.learned_at else '?'
            set_status('st_room', OK, f'{st.sid}: empty room learned {when}, {cover:.0f} % coverage, '
                                      f'noise {n_med:.1f} mm')
        else:
            set_status('st_room', WARN, f'{st.sid}: empty room not learned (step 2)')
        r = room()
        if r.has_screen():
            extra = (f', {len(r.areas)} area{"" if len(r.areas) == 1 else "s"}' if r.areas else '') + \
                    (f', {len(r.barriers)} barrier{"" if len(r.barriers) == 1 else "s"}' if r.barriers else '')
            dl, dr = r.depth_l_mm / 1000.0, r.depth_r_mm / 1000.0
            dtxt = f'{dl:.1f} m' if abs(dl - dr) < 0.05 else f'{dl:.1f} m left, {dr:.1f} m right'
            set_status('st_map', OK, f'Screen {r.frame()[2] / 1000:.2f} m wide, depth {dtxt}{extra}')
        else:
            set_status('st_map', BAD, 'No screen: nothing is sent (step 3)')
        listening = eng.listening
        ok_targets = [t for t in eng.s['osc_targets'] if listening.get(t)]
        osc_color = OK if ok_targets else (WARN if eng.s['osc_targets'] else BAD)
        n_t = len(eng.s['osc_targets'])
        rate = eng.osc_rate()
        if rate < 8.0 and osc_color == OK:
            osc_color = WARN if rate > 0 else BAD
        set_status('st_osc', osc_color, f'OSC: {rate:.0f} updates/s to {n_t} target{"" if n_t == 1 else "s"}, '
                                        f'{len(ok_targets)} listening')
        for t in eng.s['osc_targets']:
            if dpg.does_item_exist(f'dot_{t}'):
                v = listening.get(t)
                dpg.configure_item(f'dot_{t}', color=OK if v else (DIM if v is None else BAD))
        n_tr = len(snap['tracks']) if snap else 0
        n_sim = len(sim_points)
        set_status('st_people', OK if n_tr else DIM, f'{n_tr} {"person" if n_tr == 1 else "people"} tracked'
                   + (f' ({n_sim} simulated)' if n_sim else ''))

        text, level, t_msg = eng.message
        if text and now - t_msg < 12:
            dpg.set_value('st_msg', text)
            dpg.configure_item('st_msg', color={'ok': OK, 'error': BAD}.get(level, (220, 220, 220)))
        else:
            dpg.set_value('st_msg', '')
        ts = eng.task_state()
        dpg.configure_item('task_group', show=ts is not None)
        if ts:
            dpg.set_value('task_text', ts[0])
            dpg.set_value('task_bar', ts[1])

        if snap is None or snap is state['last_snap']:
            return
        state['last_snap'] = snap

        # live points of all positions with a sensor
        px, py, fx, fy, ox, oy = [], [], [], [], [], []
        for v in snap['views'].values():
            ok = v['valid']
            a = ok & ~v['fg'] & v['keep']
            b = ok & v['fg'] & v['keep']
            c = ok & ~v['keep']
            px += list(v['mx'][a] / 1000.0)
            py += list(v['my'][a] / 1000.0)
            fx += list(v['mx'][b] / 1000.0)
            fy += list(v['my'][b] / 1000.0)
            ox += list(v['mx'][c] / 1000.0)
            oy += list(v['my'][c] / 1000.0)
        dpg.set_value('s_pts', [px, py])
        for tag, key in (('occ', 'occ'), ('mot', 'motion')):
            f = snap.get(key)
            if f is not None and f.size:
                rows, cols = f.shape
                dpg.configure_item(f'fh_{tag}', rows=rows, cols=cols)
                dpg.set_value(f'fh_{tag}', [[float(v) for v in f[::-1].ravel()]])   # row 0 (at the screen) at the bottom
        dpg.set_value('s_fg', [fx, fy])
        dpg.set_value('s_out', [ox, oy])

        for k in range(SLOTS):
            tr = snap['tracks'].get(k)
            if tr:
                tx, ty = tr['x'] / 1000.0, tr['y'] / 1000.0
                dpg.set_value(f's_slot{k}', [[tx], [ty]])
                dpg.configure_item(f'a_slot{k}', show=True, default_value=(tx, ty),
                                   label=f"{k}: x {snap['x'][k]:.2f}  d {snap['depth'][k]:.2f}")
            else:
                dpg.set_value(f's_slot{k}', [[], []])
                dpg.configure_item(f'a_slot{k}', show=False)
            act = snap['active'][k] > 0
            col = SLOT_COLORS[k] if act else DIM
            dpg.set_value(f'cell_x{k}', f"{snap['x'][k]:.2f}" if act else '-')
            dpg.set_value(f'cell_d{k}', f"{snap['depth'][k]:.2f}" if act else '-')
            dpg.set_value(f'cell_a{k}', f"{snap['active'][k]:.2f}" if act else '-')
            for c in ('n', 'x', 'd', 'a'):
                dpg.configure_item(f'cell_{c}{k}', color=col)

    def set_status(tag, color, text):
        dpg.set_value(tag, text)
        dpg.configure_item(tag + '_dot', color=color)

    rebuild()
    on_fit()
    for tag in ('occ', 'mot'):
        dpg.set_axis_limits(f'fx_{tag}', 0.0, 1.0)
        dpg.set_axis_limits(f'fy_{tag}', 0.0, 1.0)
    for _ in range(sim):
        on_sim_add()
    t_start = time.time()
    while dpg.is_dearpygui_running():
        update()
        dpg.render_dearpygui_frame()
        if snapshot and time.time() - t_start > 4.0:
            dpg.output_frame_buffer(file=snapshot)
            dpg.render_dearpygui_frame()
            dpg.render_dearpygui_frame()
            break
    eng.flush_room()
    dpg.destroy_context()


# ---------------------------------------------------------------- main
def main():
    ap = argparse.ArgumentParser(description='2D lidar to OSC for Wire patches')
    ap.add_argument('--port', help='serial port of the position with the sensor (this run only)')
    ap.add_argument('--osc', action='append', help='host:port, repeatable (this run only; default Arena 7000 and Wire 8000)')
    ap.add_argument('--headless', action='store_true', help='no window, run until Ctrl+C')
    ap.add_argument('--snapshot', help='save a PNG of the window after 4 s and quit (documentation, tests)')
    ap.add_argument('--sim', type=int, default=0, help='start with this many simulated people')
    a = ap.parse_args()
    overrides = {}
    if a.port:
        overrides['port'] = a.port
    if a.osc:
        overrides['osc_targets'] = a.osc
    eng = Engine(HERE, overrides)
    eng.start()
    try:
        if a.headless:
            live = eng.live_station()
            print(f"Motion Bridge running headless: room {eng.room.name}, sensor "
                  f"{live.port if live else '-'} -> {', '.join(eng.s['osc_targets'])}. Ctrl+C to stop.")
            while True:
                time.sleep(1)
        else:
            run_window(eng, a.snapshot, a.sim)
    except KeyboardInterrupt:
        pass
    eng.stop()
    eng.join(timeout=2)
    sys.exit(0)


if __name__ == '__main__':
    main()
