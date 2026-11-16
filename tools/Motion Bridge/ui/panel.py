"""The control panel on the right: room, positions, area and front, shapes, detection, fields, output, sensor."""
import logging

import dearpygui.dearpygui as dpg

from room import Room
from settings import ADVANCED, DEFAULT_PORT, FIELD, LIMITS

from .help import tip
from .theme import BAR_C, DIM, EXCL_C

log = logging.getLogger(__name__)

ADVANCED_SLIDERS = [('tol_mm', 'Tolerance', '%.0f mm'), ('gap_mm', 'Blob gap', '%.0f mm'),
                    ('merge_mm', 'Merge distance', '%.0f mm')]
FIELD_SLIDERS = [('cell_mm', 'Cell size', '%.0f mm'),
                 ('field_fade_s', 'Motion fade', '%.2f s'), ('field_threshold', 'Motion threshold', '%.2f')]
NUDGES = (-5.0, -1.0, -0.1, 0.1, 1.0, 5.0)


def list_ports():
    try:
        from serial.tools import list_ports as lp
        return [p.device for p in lp.comports()] or [DEFAULT_PORT]
    except Exception as e:
        log.warning('cannot list serial ports: %s', e)
        return [DEFAULT_PORT]


class Panel:
    def __init__(self, eng, state, draft, view, panel_w):
        self.eng, self.state, self.draft, self.view = eng, state, draft, view
        self.panel_w = panel_w
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
        with dpg.collapsing_header(label='1  Room', default_open=True):
            tip(dpg.add_combo(Room.names(eng.rooms_dir), tag='room_combo', default_value=eng.room.name,
                              width=-1, callback=self._on_room), 'room')
            with dpg.group(horizontal=True):
                dpg.add_input_text(tag='new_room_name', hint='name of a new room', width=220)
                tip(dpg.add_button(label='New room', callback=self._on_new_room), 'new_room')

        with dpg.collapsing_header(label='2  Sensor positions', default_open=True):
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
            tip(dpg.add_checkbox(label='Show scans of all positions', tag='show_scans', default_value=True),
                'show_scans')
            dpg.add_text('Reference object (e.g. a pole):', color=DIM)
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Place marker', callback=lambda: self.draft.start('ref')), 'ref_place')
                tip(dpg.add_button(label='Snap position to marker', callback=self._on_snap), 'ref_snap')

        with dpg.collapsing_header(label='3  Area and front', default_open=True):
            dpg.add_text('Area: what counts', color=DIM)
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Draw area', callback=lambda: self.draft.start('area')), 'area_draw')
                tip(dpg.add_button(label='Area from front', callback=self._on_area_front), 'area_front')
                tip(dpg.add_button(label='Remove', callback=self._on_area_clear), 'area_clear')
            dpg.add_text('Front: where the projection is', color=DIM)
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Draw front', callback=lambda: self.draft.start('front')), 'front_draw')
                tip(dpg.add_button(label='Swap ends', callback=self._on_swap), 'front_swap')
                tip(dpg.add_button(label='Remove', callback=self._on_front_clear), 'front_clear')
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Stand at left end', callback=lambda: eng.request('left')), 'front_left')
                tip(dpg.add_button(label='Stand at right end', callback=lambda: eng.request('right')), 'front_right')
            tip(dpg.add_checkbox(label='Edit points', tag='edit_points',
                                 callback=lambda: setattr(self.state, 'rebuild', True)), 'edit_points')

        with dpg.collapsing_header(label='4  Exclusions and barriers', default_open=True):
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Draw exclusion', callback=lambda: self.draft.start('exclude')), 'draw_excl')
                tip(dpg.add_button(label='Draw barrier', callback=lambda: self.draft.start('barrier')), 'draw_bar')
            dpg.add_group(tag='shapes')

        with dpg.collapsing_header(label='5  Advanced detection', default_open=False):
            for key, label, fmt in ADVANCED_SLIDERS:
                self._slider(key, label, fmt)
            self._slider('min_pts', 'Min points', None)
            tip(dpg.add_button(label='Reset to defaults', callback=self._on_reset_adv), 'reset_adv')

        with dpg.collapsing_header(label='Fields', default_open=True):
            tip(dpg.add_text('Occupancy                      Motion', color=DIM), 'field')
            with dpg.group(horizontal=True):
                for tag in ('occ', 'mot'):
                    with dpg.plot(tag=f'fp_{tag}', width=(w - 40) // 2, height=150, no_menus=True,
                                  no_mouse_pos=True, no_inputs=True, no_title=True):
                        dpg.add_plot_axis(dpg.mvXAxis, tag=f'fx_{tag}', no_tick_labels=True, no_gridlines=True)
                        with dpg.plot_axis(dpg.mvYAxis, tag=f'fy_{tag}', no_tick_labels=True, no_gridlines=True):
                            dpg.add_heat_series([0.0], 1, 1, tag=f'fh_{tag}', scale_min=0.0, scale_max=1.0,
                                                bounds_min=(0.0, 0.0), bounds_max=(1.0, 1.0), format='')
                    dpg.bind_colormap(f'fp_{tag}', 'cm_field')
                    dpg.set_axis_limits(f'fx_{tag}', 0.0, 1.0)
                    dpg.set_axis_limits(f'fy_{tag}', 0.0, 1.0)
            for key, label, fmt in FIELD_SLIDERS:
                self._slider(key, label, fmt)
            tip(dpg.add_button(label='Reset to recommended', callback=self._on_reset_fields), 'reset_fields')

        with dpg.collapsing_header(label='Output (Spout)', default_open=True):
            tip(dpg.add_text('', tag='spout_text', wrap=w - 30), 'spout')

        with dpg.collapsing_header(label='Simulation', default_open=False):
            tip(dpg.add_text('Drag the yellow points in the view.', color=DIM), 'sim')
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Add object', callback=lambda: self.view.add_sim()), 'sim_add')
                tip(dpg.add_button(label='Remove all', callback=lambda: self.view.clear_sim()), 'sim_clear')

        with dpg.collapsing_header(label='Sensor', default_open=False):
            with dpg.group(horizontal=True):
                live = eng.live_station()
                tip(dpg.add_combo(list_ports(), tag='port', default_value=live.port if live else DEFAULT_PORT,
                                  width=110), 'port')
                tip(dpg.add_button(label='Refresh', callback=lambda: dpg.configure_item('port', items=list_ports())),
                    'refresh')
            with dpg.group(horizontal=True):
                tip(dpg.add_button(label='Sensor is at selected position', callback=self._on_assign), 'assign')
                tip(dpg.add_button(label='Release', callback=self._on_release_port), 'release')
            tip(dpg.add_checkbox(label='Write quality log (CSV)', default_value=bool(eng.s['log_quality']),
                                 callback=self._on_setting, user_data='log_quality'), 'log')
            dpg.add_text('', tag='log_path', color=DIM, wrap=w - 30)

        with dpg.collapsing_header(label='Display', default_open=False):
            tip(dpg.add_slider_float(label='UI scale', min_value=LIMITS['ui_scale'][0], max_value=LIMITS['ui_scale'][1],
                                     default_value=float(eng.s['ui_scale']), format='%.2f',
                                     callback=self._on_scale), 'scale')

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

    # ------------------------------------------------ callbacks: area, front, shapes
    def _on_swap(self):
        if self.eng.room.front:
            self.eng.edit(lambda r: setattr(r, 'front', {'left': r.front.get('right'), 'right': r.front.get('left')}))
            self.state.rebuild = True

    def _on_front_clear(self):
        self.eng.edit(lambda r: setattr(r, 'front', None))
        self.state.rebuild = True

    def _on_area_clear(self):
        self.eng.edit(lambda r: setattr(r, 'area', None))
        self.state.rebuild = True

    def _on_area_front(self):
        if not self.eng.room.has_front():
            self.eng.notify('Draw the front first.', 'error')
            return
        self.eng.edit(lambda r: r.area_from_front())
        self.state.rebuild = True

    def _on_remove_shape(self, sender, app_data, what):
        kind, i = what
        self.eng.edit(lambda r: (r.exclusions if kind == 'excl' else r.barriers).__delitem__(i))
        self.state.rebuild = True

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
        self.eng.set_port(self._sel().sid, dpg.get_value('port'))
        self.state.rebuild = True

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
        if sp is None:
            dpg.set_value('spout_text', 'Starting.')
        elif sp.sending:
            dpg.set_value('spout_text', f'Sender "{sp.name}", {sp.size[0]} x {sp.size[1]} pixels '
                                        f'(top sixteenth is the status band), {self.eng.frame_rate:.0f} frames/s, '
                                        f'{sp.frames} frames sent.')
        else:
            dpg.set_value('spout_text', f'{sp.status}. The field is shown here but not sent.')

    def show(self, snap):
        """Field heatmaps."""
        for tag, key in (('occ', 'occ'), ('mot', 'motion')):
            f = snap.get(key)
            if f is not None and f.size:
                rows, cols = f.shape
                dpg.configure_item(f'fh_{tag}', rows=rows, cols=cols)
                dpg.set_value(f'fh_{tag}', [[float(v) for v in f[::-1].ravel()]])   # row 0 (at the front) at the bottom
