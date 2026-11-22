"""The control panel on the right: room, positions, area and base line, shapes, detection, field, output, sensor."""
import contextlib
import logging

import dearpygui.dearpygui as dpg
import numpy as np

import port_scan
from room import Room
from settings import ADVANCED, DEFAULT_PORT, FIELD, LIMITS

from .help import tip
from .theme import BAD, BAR_C, DIM, EXCL_C, OK

log = logging.getLogger(__name__)

ADVANCED_SLIDERS = [('tol_mm', 'Tolerance', '%.0f mm'), ('gap_mm', 'Blob gap', '%.0f mm'),
                    ('merge_mm', 'Merge distance', '%.0f mm')]
FIELD_SLIDERS = [('cell_mm', 'Cell size', '%.0f mm'),
                 ('field_fade_s', 'Motion fade', '%.2f s'), ('field_threshold', 'Motion threshold', '%.2f')]
NUDGES = (-5.0, -1.0, -0.1, 0.1, 1.0, 5.0)
PREVIEW_MAX_H = 260         # pixels: the texture preview is at most this high


class Panel:
    def __init__(self, eng, state, draft, view, panel_w):
        self.eng, self.state, self.draft, self.view = eng, state, draft, view
        self.panel_w = panel_w
        self._ports = port_scan.list_ports()        # PortInfo per serial port
        self._scanner = None                        # port_scan.Scanner while a scan runs
        self._scanned = False                       # a scan has finished: the list can show only sensors
        self._build()

    def _sel(self):
        return self.state.selected(self.eng)

    def _slider(self, key, label, fmt):
        """A slider bound to a setting, with the setting's limits."""
        lo, hi = LIMITS[key]
        if fmt is None:
            return tip(dpg.add_slider_int(label=label, tag=key, min_value=lo, max_value=hi,
                                          default_value=int(self.eng.s[key]), callback=self._on_setting, user_data=key), key)
        return tip(dpg.add_slider_float(label=label, tag=key, min_value=lo, max_value=hi,
                                        default_value=float(self.eng.s[key]), format=fmt,
                                        callback=self._on_setting, user_data=key), key)

    # ------------------------------------------------ build
    def _build(self):
        w = self.panel_w
        eng = self.eng
        dpg.add_text('SET UP THE ROOM  (once per venue, before the show)', color=DIM)
        with self._section('1  Room', 'One room per venue. Everything below is saved in it.'):
            tip(dpg.add_combo(Room.names(eng.rooms_dir), tag='room_combo', default_value=eng.room.name,
                              width=-1, callback=self._on_room), 'room')
            with dpg.group(horizontal=True):
                dpg.add_input_text(tag='new_room_name', hint='name of a new room', width=220)
                tip(dpg.add_button(label='New room', callback=self._on_new_room), 'new_room')

        with self._section('2  Sensor', 'Which serial port the lidar is on, and which position it stands at.'):
            live = eng.live_station()
            tip(dpg.add_combo(self._port_items(), tag='port', default_value=self._label_of(live.port if live
                                                                                           else DEFAULT_PORT),
                              width=-1), 'port')
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Find sensor', tag='find_sensor', callback=self._on_find_sensor), 'find')
                tip(dpg.add_checkbox(label='Show all ports', tag='show_all', callback=self._fill_ports), 'show_all')
            dpg.add_text('Find sensor listens briefly on every port and lists those that deliver lidar data.',
                         tag='scan_text', color=DIM, wrap=w - 30)
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Sensor is at selected position', callback=self._on_assign), 'assign')
                tip(dpg.add_button(label='Release', callback=self._on_release_port), 'release')
            tip(dpg.add_checkbox(label='Write quality log (CSV)', default_value=bool(eng.s['log_quality']),
                                 callback=self._on_setting, user_data='log_quality'), 'log')
            dpg.add_text('', tag='log_path', color=DIM, wrap=w - 30)

        with self._section('3  Sensor positions', 'Learn the empty room at each place the sensor stands, then '
                                                  'align the scans so that they lie on each other.'):
            tip(dpg.add_radio_button([], tag='pos_list', callback=self._on_select), 'positions')
            self._slider('countdown_s', 'Countdown', '%.0f s')
            tip(dpg.add_button(label='Learn empty room here', width=-1,
                               callback=lambda: eng.request('background', self._sel().sid)), 'learn')
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Sensor moved: new position', callback=self._on_moved), 'moved')
                tip(dpg.add_button(label='Remove', callback=self._on_remove_pos), 'remove_pos')
            dpg.add_text('', tag='sel_title', color=DIM)
            with dpg.group(horizontal=True):
                tip(dpg.add_input_float(label='X', tag='pos_x', width=110, step=0, format='%.2f m',
                                        on_enter=True, callback=self._on_xy, user_data='x'), 'xy')
                tip(dpg.add_input_float(label='Y', tag='pos_y', width=110, step=0, format='%.2f m',
                                        on_enter=True, callback=self._on_xy, user_data='y'), 'xy')
            tip(dpg.add_slider_float(label='Rotation', tag='rot', min_value=-180, max_value=180,
                                     format='%.1f deg', callback=lambda s, v: self._set_rotation(v)), 'rotation')
            with dpg.group(horizontal=True):
                for step in NUDGES:
                    tip(dpg.add_button(label=f'{step:+g}', callback=self._on_nudge, user_data=step), 'nudge')
            with dpg.group(horizontal=True):
                tip(dpg.add_checkbox(label='Turn around reference', tag='pivot'), 'pivot')
                tip(dpg.add_checkbox(label='Mirrored', tag='flip', callback=self._on_flip), 'flip')
            tip(dpg.add_slider_float(label='Fan', tag='fan', min_value=10, max_value=360,
                                     format='%.0f deg', callback=self._on_fan), 'fan')
            dpg.add_text('Reference object (e.g. a pole):', color=DIM)
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Place marker', callback=lambda: self.draft.start('ref')), 'ref_place')
                tip(dpg.add_button(label='Snap position to marker', callback=self._on_snap), 'ref_snap')

        with self._section('4  Area and base line', 'The area is what counts; its base line is row 0 of the '
                                                    'texture. In the view: drag a corner to move it, double-click '
                                                    'an edge to add a corner, double-click a corner to delete it.'):
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Draw area', callback=lambda: self.draft.start('area')), 'area_draw')
                tip(dpg.add_button(label='Choose base line', callback=lambda: self.draft.start('base')), 'base_pick')
                tip(dpg.add_button(label='Remove', callback=self._on_area_clear), 'area_clear')
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Undo (Ctrl+Z)', callback=self.undo), 'undo')
                tip(dpg.add_checkbox(label='Move corners', tag='edit_points', default_value=True,
                                     callback=lambda: setattr(self.state, 'rebuild', True)), 'edit_points')

        with self._section('5  Exclusions and barriers', 'Optional: places where nothing counts, and walls the '
                                                         'sensors do not look through.'):
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Draw exclusion', callback=lambda: self.draft.start('exclude')), 'draw_excl')
                tip(dpg.add_button(label='Draw barrier', callback=lambda: self.draft.start('barrier')), 'draw_bar')
            dpg.add_group(tag='shapes')

        with self._section('6  Field', 'What the patches get: the texture as it is sent, and how motion is '
                                       'measured.', open=True):
            tip(dpg.add_text('Texture as sent (bottom = base line):', tag='tex_title', color=DIM), 'field')
            with dpg.texture_registry():
                dpg.add_dynamic_texture(1, 1, [0.0, 0.0, 0.0, 1.0], tag='tex_prev')
            self._prev_size = (1, 1)
            dpg.add_group(tag='tex_holder')
            dpg.add_image('tex_prev', parent='tex_holder', tag='tex_img', width=1, height=1)
            for key, label, fmt in FIELD_SLIDERS:
                self._slider(key, label, fmt)
            tip(dpg.add_button(label='Reset to recommended', callback=self._on_reset_fields), 'reset_fields')
            tip(dpg.add_text('', tag='spout_text', wrap=w - 30), 'spout')

        dpg.add_spacer(height=6)
        dpg.add_text('VIEW AND TESTS', color=DIM)
        with self._section('View', 'What the map shows.'):
            tip(dpg.add_checkbox(label='Show the field', tag='show_field', default_value=True), 'show_field')
            tip(dpg.add_checkbox(label='Show what the sensors see (shadows stay dark)', tag='show_seen',
                                 default_value=True), 'show_seen')
            tip(dpg.add_checkbox(label='Show scans of all positions', tag='show_scans', default_value=True),
                'show_scans')
            tip(dpg.add_checkbox(label='Foreground points (diagnosis)', tag='show_fg', default_value=False),
                'show_fg')
            tip(dpg.add_slider_float(label='UI scale', min_value=LIMITS['ui_scale'][0], max_value=LIMITS['ui_scale'][1],
                                     default_value=float(eng.s['ui_scale']), format='%.2f',
                                     callback=self._on_scale), 'scale')

        with self._section('Simulation', 'Objects you drag in the view; they count like real foreground.'):
            tip(dpg.add_text('Drag the yellow points in the view.', color=DIM), 'sim')
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Add object', callback=lambda: self.view.add_sim()), 'sim_add')
                tip(dpg.add_button(label='Remove all', callback=lambda: self.view.clear_sim()), 'sim_clear')
            tip(dpg.add_checkbox(label='Move between A and B', tag='sim_move', default_value=self.view.sim_move,
                                 callback=lambda s, a: self.view.set_sim_move(a)), 'sim_move')
            tip(dpg.add_slider_float(label='Speed (m/s)', tag='sim_speed', default_value=self.view.sim_speed_mps,
                                     min_value=0.1, max_value=5.0, format='%.1f', width=200,
                                     callback=lambda s, a: setattr(self.view, 'sim_speed_mps', max(float(a), 0.0))),
                'sim_speed')
            tip(dpg.add_slider_float(label='Pause (s)', tag='sim_pause', default_value=self.view.sim_pause_s,
                                     min_value=0.0, max_value=60.0, format='%.0f', width=200,
                                     callback=lambda s, a: setattr(self.view, 'sim_pause_s', max(float(a), 0.0))),
                'sim_pause')

        with self._section('Advanced detection', 'Fine-tuning of what counts as foreground. Rarely needed.'):
            for key, label, fmt in ADVANCED_SLIDERS:
                self._slider(key, label, fmt)
            self._slider('min_pts', 'Min points', None)
            tip(dpg.add_button(label='Reset to defaults', callback=self._on_reset_adv), 'reset_adv')

    @contextlib.contextmanager
    def _section(self, label, intro, open=False):
        """A collapsible section that starts with one line saying what it is for."""
        with dpg.collapsing_header(label=label, default_open=open):
            with dpg.group(indent=8):
                dpg.add_text(intro, color=DIM, wrap=self.panel_w - 40)
                yield
            dpg.add_spacer(height=4)

    # ------------------------------------------------ callbacks: rooms and positions
    def _on_room(self, sender, value):
        self.eng.load_room(value)
        self.state.sel = None
        self.state.rebuild = True
        self.view.fit()

    def _on_new_room(self):
        name = dpg.get_value('new_room_name')
        if self.eng.new_room(name):
            dpg.set_value('new_room_name', '')
            self.state.sel = None
            self.state.rebuild = True
            self.view.fit()
        else:
            self.eng.notify('Room name empty or already used.', 'error')

    def _on_select(self, sender, value):
        self.state.sel = value.split(' ', 1)[0]

    def _on_moved(self):
        new = self.eng.add_position(self._sel().sid)
        self.state.sel = new.sid
        self.state.rebuild = True
        self.eng.notify(f'{new.name} created and holds the sensor. Learn the empty room there, then align it.', 'ok')

    def _on_remove_pos(self):
        if self.eng.remove_position(self._sel().sid):
            self.state.sel = None
            self.state.rebuild = True
        else:
            self.eng.notify('The last position cannot be removed.', 'error')

    def _on_xy(self, sender, value, axis):
        sid = self._sel().sid
        self.eng.edit(lambda r: setattr(r.station(sid), axis, float(value) * 1000.0))

    def _set_rotation(self, value):
        sid = self._sel().sid
        pivot = self.eng.room.ref if dpg.get_value('pivot') and self.eng.room.ref else None
        self.eng.edit(lambda r: r.rotate_station(r.station(sid), float(value), pivot))

    def _on_nudge(self, sender, app_data, step):
        self._set_rotation(self._sel().rot + step)

    def _on_flip(self, sender, value):
        sid = self._sel().sid
        self.eng.edit(lambda r: setattr(r.station(sid), 'flip', bool(value)))

    def _on_fan(self, sender, value):
        sid = self._sel().sid
        self.eng.edit(lambda r: setattr(r.station(sid), 'fan_span', float(value)))

    def _on_snap(self):
        st = self._sel()
        if self.eng.room.ref is None:
            self.eng.notify('Place the reference marker first.', 'error')
            return
        if self.eng.edit(lambda r: r.snap_to_reference(r.station(st.sid))):
            self.eng.notify(f'{st.name} snapped to the reference. Now turn it until the walls match.', 'ok')
        else:
            self.eng.notify('No free-standing object found near the marker in this scan. Drag the position '
                            'closer and try again.', 'error')

    # ------------------------------------------------ callbacks: area, base line, shapes
    def _on_area_clear(self):
        self.eng.edit(lambda r: (setattr(r, 'area', None), setattr(r, 'base', None)), undo=True)
        self.state.rebuild = True

    def _on_remove_shape(self, sender, app_data, what):
        kind, i = what
        self.eng.edit(lambda r: (r.exclusions if kind == 'excl' else r.barriers).__delitem__(i), undo=True)
        self.state.rebuild = True

    def undo(self, *_):
        if self.draft.active:
            self.draft.cancel()
            return
        if self.eng.undo():
            self.state.rebuild = True
        else:
            self.eng.notify('Nothing to undo.')

    # ------------------------------------------------ callbacks: settings, output, sensor, display
    def _on_setting(self, sender, value, key):
        self.eng.set(key, value)

    def _on_reset_fields(self):
        self.eng.reset(FIELD)
        for k in FIELD:
            dpg.set_value(k, self.eng.s[k])

    def _on_reset_adv(self):
        self.eng.reset(ADVANCED)
        for k in ADVANCED:
            dpg.set_value(k, self.eng.s[k])

    def _on_assign(self):
        port = port_scan.device_of(dpg.get_value('port'))
        if port:
            self.eng.set_port(self._sel().sid, port)
            self.state.rebuild = True

    # ------------------------------------------------ the port list and the sensor search
    def _label_of(self, device):
        p = next((p for p in self._ports if p.device == device), None)
        return p.label() if p else (device or '')

    def _port_items(self):
        """Only ports with a sensor after a scan, else all ports with their description."""
        found = [p.label() for p in self._ports if p.sensor]
        if self._scanned and found and not (dpg.does_item_exist('show_all') and dpg.get_value('show_all')):
            return found
        return [p.label() for p in self._ports] or [DEFAULT_PORT]

    def _fill_ports(self, *_):
        current = port_scan.device_of(dpg.get_value('port'))
        items = self._port_items()
        dpg.configure_item('port', items=items)
        dpg.set_value('port', next((i for i in items if port_scan.device_of(i) == current), items[0]))

    def _on_find_sensor(self):
        if self._scanner and not self._scanner.done:
            return
        self._ports = port_scan.list_ports()
        in_use = {port: rd.packets > 0 for port, rd in list(self.eng.readers.items())}
        self._scanner = port_scan.Scanner(self._ports, in_use)
        self._scanner.start()
        dpg.configure_item('find_sensor', enabled=False)

    def _scan_frame(self):
        sc = self._scanner
        if sc is None:
            return
        if not sc.done:
            dpg.set_value('scan_text', f'Listening on {sc.current or "the ports"} ...')
            return
        self._scanner = None
        self._scanned = True
        dpg.configure_item('find_sensor', enabled=True)
        found = sc.found
        if found:
            names = ', '.join(p.device for p in found)
            dpg.set_value('scan_text', f'Sensor data on {names}. Select the port and assign it to a position.')
        else:
            dpg.set_value('scan_text', f'No lidar data on {len(sc.ports)} ports. Check that the sensor has power '
                                       'and spins, and the USB cable. All ports are listed.')
        self._fill_ports()
        if len(found) == 1:
            dpg.set_value('port', found[0].label())

    def _on_release_port(self):
        self.eng.set_port(self._sel().sid, None)
        self.state.rebuild = True

    def _on_scale(self, sender, value):
        dpg.set_global_font_scale(value)
        self.eng.set('ui_scale', value)

    # ------------------------------------------------ lists that depend on the room
    def rebuild(self):
        r = self.eng.room
        names = [f'{s.sid}  {s.name}' + ('  (sensor)' if s.port else '') for s in r.stations]
        cur = self._sel()
        dpg.configure_item('pos_list', items=names)
        dpg.set_value('pos_list', next((n for n in names if n.split(' ', 1)[0] == cur.sid), names[0]))
        dpg.delete_item('shapes', children_only=True)
        for i, e in enumerate(r.exclusions):
            with dpg.group(horizontal=True, parent='shapes'):
                dpg.add_text(f'Exclusion {i + 1} ({len(e)} corners)', color=EXCL_C)
                dpg.add_button(label='remove', small=True, callback=self._on_remove_shape, user_data=('excl', i))
        for i in range(len(r.barriers)):
            with dpg.group(horizontal=True, parent='shapes'):
                dpg.add_text(f'Barrier {i + 1}', color=BAR_C)
                dpg.add_button(label='remove', small=True, callback=self._on_remove_shape, user_data=('bar', i))
        dpg.configure_item('room_combo', items=Room.names(self.eng.rooms_dir))
        dpg.set_value('room_combo', r.name)

    # ------------------------------------------------ every frame
    def frame(self):
        self._scan_frame()
        st = self._sel()
        dpg.set_value('sel_title', f'Selected: {st.sid}  {st.name}' + ('  (sensor)' if st.port else '  (scan only)'))
        for tag, val in (('pos_x', st.x / 1000.0), ('pos_y', st.y / 1000.0), ('rot', st.rot),
                         ('fan', st.fan_span), ('flip', st.flip)):
            if not dpg.is_item_active(tag):
                dpg.set_value(tag, val)
        has_handle = f'h_fan_{st.sid}' in self.view.handles
        if (st.fan_span < 360.0) != has_handle:
            self.state.rebuild = True
        sp = self.eng.spout
        ok, text = self.eng.output_status()
        frames = f' {sp.frames} frames sent.' if sp is not None else ''
        dpg.set_value('spout_text', text + '.' + frames)
        dpg.configure_item('spout_text', color=OK if ok else BAD)

    def show(self, snap):
        """The texture as it was last sent, row 0 (on the base line) at the bottom, with its true proportions."""
        tex = self.eng.last_tex
        if tex is None:
            return
        h, w = tex.shape[:2]
        if (w, h) != self._prev_size:                   # a new size needs a new texture of that size
            self._prev_size = (w, h)
            scale = min((self.panel_w - 40) / w, PREVIEW_MAX_H / h)
            dw, dh = max(int(w * scale), 1), max(int(h * scale), 1)
            self._prev_idx = ((np.arange(dh) * h // dh)[::-1], np.arange(dw) * w // dw)   # nearest, row 0 at the bottom
            dpg.delete_item('tex_img')
            dpg.delete_item('tex_prev')
            with dpg.texture_registry():
                dpg.add_dynamic_texture(dw, dh, [0.0] * (dw * dh * 4), tag='tex_prev')
            dpg.add_image('tex_prev', parent='tex_holder', tag='tex_img', width=dw, height=dh)
            dpg.set_value('tex_title', f'Texture as sent, {w} x {h} pixels (bottom = base line):')
        rows, cols = self._prev_idx
        dpg.set_value('tex_prev', (tex[rows][:, cols].astype(np.float32) / 255.0).ravel())
