"""Motion Bridge: STL-27L lidar -> people in front of a surface -> OSC for Wire patches.

Usage:
  .venv\\Scripts\\python.exe bridge.py                 window (default)
  .venv\\Scripts\\python.exe bridge.py --headless      no window, runs until Ctrl+C
  Options: --port COM19   --osc 127.0.0.1:7000 (repeatable)

All settings are made in the window and saved to settings.json next to this file.
OSC interface: see README.md, section Output.
"""
import argparse
import os
import sys
import time

import numpy as np

from engine import DEFAULTS, SLOTS, Engine

HERE = os.path.dirname(os.path.abspath(__file__))
PANEL_W = 430

SLOT_COLORS = [(255, 210, 60), (90, 200, 255), (255, 120, 200), (140, 255, 140),
               (255, 160, 80), (180, 140, 255), (240, 240, 240), (120, 255, 230)]
OK, WARN, BAD, DIM = (110, 220, 130), (255, 190, 80), (255, 95, 85), (140, 140, 155)

HELP = {
    'learn': 'Learns what the empty room looks like. Everything closer than the empty room '
             'later counts as a person.\nClick, leave the scan plane during the countdown and stay '
             'out until the second tone (about 5 s).\nLearn again whenever the sensor or the room changes.',
    'countdown': 'Time between the click and the measurement, so you can walk to your place '
                 '(out of the room, or to a surface end). A tone marks the start and the end.',
    'left': 'Stand alone at the LEFT end of the projection surface, close to it, and stay still '
            'until the second tone.\nBoth ends together define the surface: x runs from 0 (left) to '
            '1 (right).',
    'right': 'Stand alone at the RIGHT end of the projection surface, close to it, and stay still '
             'until the second tone.',
    'clear': 'Forgets the surface ends and returns to sensor mode.',
    'sensor_mode': 'Without a surface, x follows the angle in the watched half of the sensor '
                   '(90 deg left = 0, straight = 0.5, 90 deg right = 1) and depth is the distance '
                   'from the sensor.',
    'facing': 'Direction of the watched half, in sensor degrees (0 = the sensor front, clockwise). '
              'The blue line in the view shows it.',
    'turn': 'Turns the watched half by 180 degrees.',
    'mirror': 'Swaps left and right (x = 0 and x = 1). Use it when the bulge moves the opposite way.',
    'depth': 'How far from the surface (or the sensor in sensor mode) people are tracked. '
             'depth = 0 at the surface, 1 at this distance. People further away are ignored.',
    'sim': 'Adds people you can drag in the view with the mouse. They are sent like real people, '
           'so patches can be tested without walking around or without a sensor.',
    'sim_add': 'Adds a simulated person in front of the sensor. Drag the yellow point in the view.',
    'sim_clear': 'Removes all simulated people.',
    'targets': 'Programs that receive the OSC data. Arena listens on 7000 and Wire on 8000 by default '
               '(Preferences > OSC). A green dot means a local program holds that port.',
    'add_target': 'host:port, for example 127.0.0.1:7000 or 192.168.1.20:7000.',
    'tol_mm': 'How much closer than the empty room a point must be to count as foreground. '
              'Raise it if walls or furniture flicker red.',
    'gap_mm': 'Foreground points further apart than this start a new blob.',
    'min_pts': 'Blobs with fewer points are ignored (noise, small objects).',
    'merge_mm': 'Blobs closer than this become one person (two legs, arm and body).',
    'assoc_mm': 'A person found this close to a tracked one continues that track (same slot).',
    'smooth': 'Share of the new position taken each revolution. Lower = calmer, higher = faster.',
    'hold_s': 'A lost person keeps its slot this long before fading out.',
    'fade_s': 'Time for a person to fade in or out (active 0 -> 1).',
    'reset_adv': 'Restores the detection defaults.',
    'port': 'Serial port of the lidar adapter (Silicon Labs CP210x).',
    'connect': 'Connects to the selected port. The bridge reconnects by itself after a cable pull.',
    'log': 'Writes one line per revolution to logs/quality_*.csv (speed, points, valid share, CRC errors, people).',
    'scale': 'Size of text and controls.',
    'fullscreen': 'Fullscreen on or off (also F11).',
    'fit': 'Shows the area around the sensor again (6 m).',
    'cancel': 'Stops the countdown or measurement.',
    'view': 'Top view, sensor at the centre, metres. Mouse wheel zooms, right-drag pans, '
            'double-click fits. Grey: scan, red: closer than the empty room, blue: the empty room, '
            'coloured rings: tracked people with slot number.',
}


def list_ports():
    try:
        from serial.tools import list_ports as lp
        return [p.device for p in lp.comports()] or ['COM19']
    except Exception:
        return ['COM19']


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
            dpg.add_text(HELP[key], wrap=360)

    def themed(color, size, marker=None, ring=False):
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
        return th

    sim_points = []

    # ---- callbacks
    def on_slider(sender, value, key):
        eng.set(key, value)

    def on_depth(sender, value):
        eng.set('depth_mm', float(value) * 1000.0)

    def on_turn():
        eng.set('front_deg', (float(eng.s['front_deg']) + 180.0) % 360.0)
        dpg.set_value('facing', eng.s['front_deg'])

    def on_sim_add():
        k = len(sim_points)
        f = np.radians(float(eng.s['front_deg']) + (k - 1) * 25.0)
        pos = (1.5 * np.sin(f), 1.5 * np.cos(f))
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

    def on_reset_adv():
        eng.reset_advanced()
        for k in ('tol_mm', 'gap_mm', 'min_pts', 'merge_mm', 'assoc_mm', 'smooth', 'hold_s', 'fade_s'):
            dpg.set_value(k, eng.s[k])

    def on_connect():
        eng.set_port(dpg.get_value('port'))

    def on_refresh_ports():
        dpg.configure_item('port', items=list_ports())

    def on_scale(sender, value):
        dpg.set_global_font_scale(value)
        eng.set('ui_scale', value)

    def on_fit():
        dpg.set_axis_limits('xaxis', -6, 6)
        dpg.set_axis_limits('yaxis', -6, 6)
        state['unlock'] = 2

    state = {'unlock': 2, 'last_snap': None}

    # ---- layout
    with dpg.window(tag='main', no_scrollbar=True):
        with dpg.group(horizontal=True):
            with dpg.plot(tag='plot', width=-PANEL_W - 10, height=-1, equal_aspects=True, no_menus=True):
                dpg.add_plot_axis(dpg.mvXAxis, label='x (m)', tag='xaxis')
                with dpg.plot_axis(dpg.mvYAxis, label='y (m), sensor front', tag='yaxis'):
                    dpg.add_scatter_series([], [], tag='s_bg', label='empty room')
                    dpg.add_scatter_series([], [], tag='s_pts', label='scan')
                    dpg.add_scatter_series([], [], tag='s_fg', label='closer than room')
                    dpg.add_line_series([], [], tag='l_zone', label='zone')
                    dpg.add_line_series([], [], tag='l_dir', label='direction')
                    dpg.add_scatter_series([0.0], [0.0], tag='s_sensor', label='sensor')
                    for k in range(SLOTS):
                        dpg.add_scatter_series([], [], tag=f's_slot{k}', label=f'slot {k}')
                for k in range(SLOTS):
                    dpg.add_plot_annotation(tag=f'a_slot{k}', default_value=(0.0, 0.0),
                                            offset=(18, -18) if k % 2 == 0 else (18, 22),
                                            color=SLOT_COLORS[k] + (255,), show=False)
            with dpg.child_window(width=PANEL_W, height=-1, tag='panel'):
                with dpg.group(horizontal=True):
                    dpg.add_text('Motion Bridge', color=(255, 255, 255))
                    h = dpg.add_text('(?) the view', color=(120, 170, 255))
                    tip(h, 'view')
                with dpg.group(horizontal=True):
                    b = dpg.add_button(label='Fullscreen (F11)', callback=lambda: dpg.toggle_viewport_fullscreen())
                    tip(b, 'fullscreen')
                    b = dpg.add_button(label='Fit view', callback=on_fit)
                    tip(b, 'fit')
                dpg.add_separator()

                with dpg.collapsing_header(label='Status', default_open=True):
                    for key in ('st_sensor', 'st_room', 'st_map', 'st_osc', 'st_people'):
                        with dpg.group(horizontal=True):
                            dpg.add_text('●', tag=key + '_dot', color=DIM)
                            dpg.add_text('', tag=key, wrap=PANEL_W - 60)
                    dpg.add_text('', tag='st_msg', wrap=PANEL_W - 30)
                    with dpg.group(tag='task_group', show=False):
                        dpg.add_text('', tag='task_text', color=WARN, wrap=PANEL_W - 30)
                        dpg.add_progress_bar(tag='task_bar', width=-1)
                        b = dpg.add_button(label='Cancel', callback=lambda: eng.cancel())
                        tip(b, 'cancel')

                with dpg.collapsing_header(label='1  Learn the empty room', default_open=True):
                    s = dpg.add_slider_float(label='Countdown', tag='countdown', min_value=0, max_value=60,
                                             default_value=float(eng.s['countdown_s']), format='%.0f s',
                                             callback=on_slider, user_data='countdown_s')
                    tip(s, 'countdown')
                    b = dpg.add_button(label='Learn empty room', width=-1, callback=lambda: eng.request('background'))
                    tip(b, 'learn')

                with dpg.collapsing_header(label='2  Map to the surface', default_open=True):
                    dpg.add_text('Surface (both ends set):', color=DIM)
                    with dpg.group(horizontal=True):
                        b = dpg.add_button(label='Set left end', callback=lambda: eng.request('left'))
                        tip(b, 'left')
                        b = dpg.add_button(label='Set right end', callback=lambda: eng.request('right'))
                        tip(b, 'right')
                        b = dpg.add_button(label='Clear', callback=lambda: eng.clear_surface())
                        tip(b, 'clear')
                    t = dpg.add_text('Sensor mode (no surface):', color=DIM)
                    tip(t, 'sensor_mode')
                    s = dpg.add_slider_float(label='Facing', tag='facing', min_value=0, max_value=359,
                                             default_value=float(eng.s['front_deg']), format='%.0f deg',
                                             callback=on_slider, user_data='front_deg')
                    tip(s, 'facing')
                    with dpg.group(horizontal=True):
                        b = dpg.add_button(label='Turn 180 deg', callback=on_turn)
                        tip(b, 'turn')
                        c = dpg.add_checkbox(label='Mirror', default_value=bool(eng.s['mirror']),
                                             callback=on_slider, user_data='mirror')
                        tip(c, 'mirror')
                    s = dpg.add_slider_float(label='Depth Range', min_value=0.5, max_value=20.0,
                                             default_value=float(eng.s['depth_mm']) / 1000.0, format='%.1f m',
                                             callback=on_depth)
                    tip(s, 'depth')

                with dpg.collapsing_header(label='Simulation', default_open=False):
                    t = dpg.add_text('Drag the yellow points in the view.', color=DIM)
                    tip(t, 'sim')
                    with dpg.group(horizontal=True):
                        b = dpg.add_button(label='Add person', callback=on_sim_add)
                        tip(b, 'sim_add')
                        b = dpg.add_button(label='Remove all', callback=on_sim_clear)
                        tip(b, 'sim_clear')

                with dpg.collapsing_header(label='Output (OSC)', default_open=True):
                    t = dpg.add_text('Targets:', color=DIM)
                    tip(t, 'targets')
                    dpg.add_group(tag='targets')
                    with dpg.group(horizontal=True):
                        i = dpg.add_input_text(tag='new_target', hint='host:port', width=200, on_enter=True,
                                               callback=on_add_target)
                        tip(i, 'add_target')
                        b = dpg.add_button(label='Add', callback=on_add_target)
                        tip(b, 'add_target')
                    with dpg.table(header_row=True, borders_innerH=True, policy=dpg.mvTable_SizingStretchSame):
                        for h in ('slot', 'x', 'depth', 'active'):
                            dpg.add_table_column(label=h)
                        for k in range(SLOTS):
                            with dpg.table_row():
                                for c in ('n', 'x', 'd', 'a'):
                                    dpg.add_text(str(k) if c == 'n' else '', tag=f'cell_{c}{k}', color=DIM)

                with dpg.collapsing_header(label='Advanced detection', default_open=False):
                    spec = [('tol_mm', 'Tolerance', 25, 2000, '%.0f mm'), ('gap_mm', 'Blob gap', 50, 2000, '%.0f mm'),
                            ('merge_mm', 'Merge distance', 0, 3000, '%.0f mm'),
                            ('assoc_mm', 'Track distance', 100, 5000, '%.0f mm'),
                            ('smooth', 'Smoothing', 0.05, 1.0, '%.2f'), ('hold_s', 'Hold', 0.0, 5.0, '%.1f s'),
                            ('fade_s', 'Fade', 0.02, 5.0, '%.2f s')]
                    for key, label, lo, hi, fmt in spec:
                        s = dpg.add_slider_float(label=label, tag=key, min_value=lo, max_value=hi,
                                                 default_value=float(eng.s[key]), format=fmt,
                                                 callback=on_slider, user_data=key)
                        tip(s, key)
                    s = dpg.add_slider_int(label='Min points', tag='min_pts', min_value=1, max_value=100,
                                           default_value=int(eng.s['min_pts']), callback=on_slider, user_data='min_pts')
                    tip(s, 'min_pts')
                    b = dpg.add_button(label='Reset to defaults', callback=on_reset_adv)
                    tip(b, 'reset_adv')

                with dpg.collapsing_header(label='Sensor and quality', default_open=False):
                    with dpg.group(horizontal=True):
                        c = dpg.add_combo(list_ports(), tag='port', default_value=eng.s['port'], width=120)
                        tip(c, 'port')
                        b = dpg.add_button(label='Refresh', callback=on_refresh_ports)
                        b2 = dpg.add_button(label='Connect', callback=on_connect)
                        tip(b2, 'connect')
                    c = dpg.add_checkbox(label='Write quality log (CSV)', default_value=bool(eng.s['log_quality']),
                                         callback=on_slider, user_data='log_quality')
                    tip(c, 'log')
                    dpg.add_text('', tag='quality', color=DIM, wrap=PANEL_W - 30)

                with dpg.collapsing_header(label='Display', default_open=False):
                    s = dpg.add_slider_float(label='UI scale', min_value=0.7, max_value=2.5,
                                             default_value=float(eng.s['ui_scale']), format='%.2f', callback=on_scale)
                    tip(s, 'scale')

    # ---- themes
    for tag, color, size in [('s_bg', (60, 100, 200), 1.5), ('s_pts', (175, 175, 185), 1.5),
                             ('s_fg', (255, 80, 70), 2.5), ('l_zone', (80, 160, 255), 2.0),
                             ('l_dir', (80, 160, 255), 3.0), ('s_sensor', (80, 220, 120), 6.0)]:
        dpg.bind_item_theme(tag, themed(color, size))
    for k in range(SLOTS):
        dpg.bind_item_theme(f's_slot{k}', themed(SLOT_COLORS[k], 16, dpg.mvPlotMarker_Circle, ring=True))
    rebuild_targets()

    with dpg.handler_registry():
        dpg.add_key_press_handler(dpg.mvKey_F11, callback=lambda: dpg.toggle_viewport_fullscreen())

    dpg.create_viewport(title='Motion Bridge', width=1500, height=950, resizable=True)
    dpg.setup_dearpygui()
    if font:
        dpg.bind_font(font)
    dpg.set_global_font_scale(float(eng.s['ui_scale']))
    dpg.set_primary_window('main', True)
    dpg.show_viewport()
    on_fit()

    # ---- frame update
    def update():
        if state['unlock'] > 0:
            state['unlock'] -= 1
            if state['unlock'] == 0:
                dpg.set_axis_limits_auto('xaxis')
                dpg.set_axis_limits_auto('yaxis')

        eng.sim = [list(dpg.get_value(tag)[:2]) for tag in sim_points]
        eng.sim = [[x * 1000.0, y * 1000.0] for x, y in eng.sim]

        snap = eng.snapshot()
        rd = eng.reader
        now = time.time()

        # status lines
        if rd.connected:
            hz = snap['rev'].hz if snap and snap['rev'] is not None else 0.0
            pts = len(snap['rev'].dist) if snap and snap['rev'] is not None else 0
            set_status('st_sensor', OK, f'Sensor {eng.s["port"]}: running, {hz:.1f} Hz, {pts} points, '
                                        f'{rd.crc_bad} CRC errors')
        else:
            set_status('st_sensor', BAD, f'Sensor {eng.s["port"]}: not connected'
                                         + (f' ({rd.last_error[:60]})' if rd.last_error else ''))
        if eng.bg.ok:
            cover, n_med, _ = eng.bg.stats()
            when = time.strftime('%d.%m. %H:%M', time.localtime(eng.bg.learned_at)) if eng.bg.learned_at else '?'
            set_status('st_room', OK, f'Empty room learned {when}, {cover:.0f} % coverage, noise {n_med:.1f} mm')
        else:
            set_status('st_room', WARN, 'Empty room not learned: step 1')
        z = eng.zone
        if z.surface:
            set_status('st_map', OK, f'Surface {z.frame()[2] / 1000:.2f} m wide, depth {z.depth_mm / 1000:.1f} m')
        else:
            half = ' (one end set)' if (eng.s['left'] or eng.s['right']) else ''
            set_status('st_map', WARN, f'Sensor mode{half}: facing {z.front_deg:.0f} deg'
                                       + (', mirrored' if z.mirror else '') + f', depth {z.depth_mm / 1000:.1f} m')
        listening = eng.listening
        ok_targets = [t for t in eng.s['osc_targets'] if listening.get(t)]
        osc_color = OK if ok_targets else (WARN if eng.s['osc_targets'] else BAD)
        n_t = len(eng.s['osc_targets'])
        set_status('st_osc', osc_color, f'OSC {eng.rate:.0f} msg/s to {n_t} target{"" if n_t == 1 else "s"}, '
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

        q = eng.quality.summary()
        if q:
            dpg.set_value('quality', f"{q['minutes']:.1f} min, {q['revs']} revolutions, {q['hz_mean']:.2f} Hz "
                                     f"(std {q['hz_std']:.3f}), valid {q['valid_mean']:.1f} % "
                                     f"(min {q['valid_min']:.1f} %), {q['points_mean']:.0f} points/rev"
                          + (f"\nLog: logs/{os.path.basename(eng.quality.log_path)}" if eng.quality.log else ''))

        if snap is None or snap is state['last_snap']:
            return
        state['last_snap'] = snap

        # view
        if eng.bg.ok:
            bx, by = eng.bg.xy()
            dpg.set_value('s_bg', [list(bx / 1000.0), list(by / 1000.0)])
        else:
            dpg.set_value('s_bg', [[], []])
        p = eng.points_xy(snap)
        if p is not None:
            dpg.set_value('s_pts', [list(p[0]), list(p[1])])
            dpg.set_value('s_fg', [list(p[2]), list(p[3])])
        else:
            dpg.set_value('s_pts', [[], []])
            dpg.set_value('s_fg', [[], []])

        if z.surface:
            u, n, _ = z.frame()
            l, r = z.left / 1000.0, z.right / 1000.0
            d = z.depth_mm / 1000.0
            fl, fr = l + n * d, r + n * d
            xs = [l[0], r[0], fr[0], fl[0], l[0]]
            ys = [l[1], r[1], fr[1], fl[1], l[1]]
            dpg.set_value('l_zone', [xs, ys])
            dpg.set_value('l_dir', [[l[0], r[0]], [l[1], r[1]]])
        else:
            d = z.depth_mm / 1000.0
            f = z.front_deg
            angs = np.radians(f + np.arange(-90, 91, 3))
            xs = [0.0] + list(d * np.sin(angs)) + [0.0]
            ys = [0.0] + list(d * np.cos(angs)) + [0.0]
            dpg.set_value('l_zone', [xs, ys])
            fr = np.radians(f)
            dpg.set_value('l_dir', [[0.0, 0.35 * d * np.sin(fr)], [0.0, 0.35 * d * np.cos(fr)]])

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
    dpg.destroy_context()


# ---------------------------------------------------------------- main
def main():
    ap = argparse.ArgumentParser(description='STL-27L lidar to OSC for Wire patches')
    ap.add_argument('--port', help=f"serial port (saved setting, default {DEFAULTS['port']})")
    ap.add_argument('--osc', action='append', help='host:port, repeatable (saved setting, default Arena 7000 and Wire 8000)')
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
            print(f"Motion Bridge running headless: {eng.s['port']} -> {', '.join(eng.s['osc_targets'])}. Ctrl+C to stop.")
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
