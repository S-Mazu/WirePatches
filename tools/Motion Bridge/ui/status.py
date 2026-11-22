"""The status lines at the top of the panel: sensor, signal, room, map, OSC, objects; messages, drafts, tasks."""
import os
import time

import dearpygui.dearpygui as dpg

from field import Grid

from .help import tip
from .theme import BAD, DIM, OK, WARN

MESSAGE_S = 12.0            # how long a message stays visible
LIVE_S = 1.0                # data younger than this counts as live
HZ_OK, HZ_WARN = (9.0, 11.0), (7.0, 13.0)
VALID_OK, VALID_WARN = 90.0, 70.0
POINTS_OK, POINTS_WARN = 0.93, 0.8
MEASUREMENTS_PER_S = 21600.0    # STL-27L: expected points per second


class StatusLines:
    def __init__(self, eng, state, draft, on_finish, panel_w):
        self.eng, self.state, self.draft, self.on_finish = eng, state, draft, on_finish
        self._build(panel_w)

    def _build(self, panel_w):
        with dpg.collapsing_header(label='Status', default_open=True):
            for key in ('st_sensor', 'st_signal', 'st_room', 'st_map', 'st_output', 'st_objects'):
                with dpg.group(horizontal=True):
                    tip(dpg.add_text('●', tag=key + '_dot', color=DIM), key)
                    tip(dpg.add_text('', tag=key, wrap=panel_w - 60), key)
                if key == 'st_signal':
                    tip(dpg.add_text('', tag='st_session', color=DIM, indent=22, wrap=panel_w - 60), 'st_signal')
            dpg.add_text('', tag='st_msg', wrap=panel_w - 30)
            with dpg.group(tag='mode_group', show=False):
                dpg.add_text('', tag='mode_text', color=WARN, wrap=panel_w - 30)
                with dpg.group(horizontal=True):
                    tip(dpg.add_button(label='Finish', tag='finish_btn', callback=lambda: self.on_finish()), 'finish')
                    tip(dpg.add_button(label='Cancel drawing', callback=lambda: self.draft.cancel()), 'cancel_draw')
            with dpg.group(tag='task_group', show=False):
                dpg.add_text('', tag='task_text', color=WARN, wrap=panel_w - 30)
                dpg.add_progress_bar(tag='task_bar', width=-1)
                tip(dpg.add_button(label='Cancel', callback=lambda: self.eng.cancel()), 'cancel')

    @staticmethod
    def _set(tag, color, text):
        dpg.set_value(tag, text)
        dpg.configure_item(tag + '_dot', color=color)

    def frame(self, n_sim, n_objects):
        eng = self.eng
        now = time.time()
        self._sensor_and_signal()
        self._room()
        self._map()
        ok, text = eng.output_status()
        self._set('st_output', OK if ok else BAD, text)
        self._set('st_objects', OK if n_objects else DIM, f'{n_objects} {"object" if n_objects == 1 else "objects"}'
                  + (f' ({n_sim} simulated)' if n_sim else ''))
        text, level, t_msg = eng.message
        if text and now - t_msg < MESSAGE_S:
            dpg.set_value('st_msg', text)
            dpg.configure_item('st_msg', color={'ok': OK, 'error': BAD}.get(level, (220, 220, 220)))
        else:
            dpg.set_value('st_msg', '')
        dpg.configure_item('mode_group', show=self.draft.active)
        if self.draft.active:
            dpg.set_value('mode_text', self.draft.label)
            dpg.configure_item('finish_btn', show=self.draft.needs_finish)
        ts = eng.task_state()
        dpg.configure_item('task_group', show=ts is not None)
        if ts:
            dpg.set_value('task_text', ts[0])
            dpg.set_value('task_bar', ts[1])

    def _sensor_and_signal(self):
        eng = self.eng
        rd = eng.reader
        rq = eng.quality.recent()
        live_st = eng.live_station()
        port = live_st.port if live_st else None
        live = rd is not None and rd.connected and rq and rq['age'] < LIVE_S
        if rd is None:
            self._set('st_sensor', BAD, 'Sensor: no position holds a port (see Sensor)')
            self._set('st_signal', DIM, 'Signal: no data')
        elif live:
            crc_col = WARN if rq['crc_new'] > 0 else OK
            self._set('st_sensor', crc_col, f'Sensor {port} at {live_st.sid}: running, {rd.crc_bad} CRC errors'
                      + (' (rising)' if rq['crc_new'] > 0 else ''))
            exp_pts = MEASUREMENTS_PER_S / max(rq['hz'], 0.1)
            c_hz = OK if HZ_OK[0] <= rq['hz'] <= HZ_OK[1] else (WARN if HZ_WARN[0] <= rq['hz'] <= HZ_WARN[1] else BAD)
            c_val = OK if rq['valid'] >= VALID_OK else (WARN if rq['valid'] >= VALID_WARN else BAD)
            ratio = rq['points'] / exp_pts
            c_pts = OK if ratio >= POINTS_OK else (WARN if ratio >= POINTS_WARN else BAD)
            worst = BAD if BAD in (c_hz, c_val, c_pts) else (WARN if WARN in (c_hz, c_val, c_pts) else OK)
            self._set('st_signal', worst, f"Signal: {rq['hz']:.1f} rev/s, valid {rq['valid']:.0f} %, "
                                          f"{rq['points']:.0f} points/rev")
        else:
            self._set('st_sensor', BAD, f'Sensor {port} at {live_st.sid}: '
                      + ('connected, no data' if rd.connected else 'not connected')
                      + (f' ({rd.last_error[:60]})' if rd.last_error and not rd.connected else ''))
            self._set('st_signal', DIM, 'Signal: no data')
        q = eng.quality.summary()
        dpg.set_value('st_session', f"Since start: {q['minutes']:.0f} min, {q['revs']} revolutions, "
                                    f"lowest valid {q['valid_min']:.0f} %" if q else '')
        dpg.set_value('log_path', f"Log: logs/{os.path.basename(eng.quality.log_path)}" if eng.quality.log else '')

    def _room(self):
        st = self.state.selected(self.eng)
        if st.bg is not None and st.bg.ok:
            cover, n_med, _ = st.bg.stats()
            when = time.strftime('%d.%m. %H:%M', time.localtime(st.bg.learned_at)) if st.bg.learned_at else '?'
            self._set('st_room', OK, f'{st.sid}: empty room learned {when}, {cover:.0f} % coverage, '
                                     f'noise {n_med:.1f} mm')
        else:
            self._set('st_room', WARN, f'{st.sid}: empty room not learned (step 3)')

    def _map(self):
        r = self.eng.room
        if r.ready():
            n_e, n_b = len(r.exclusions), len(r.barriers)
            extra = (f', {n_e} exclusion{"" if n_e == 1 else "s"}' if n_e else '') + \
                    (f', {n_b} barrier{"" if n_b == 1 else "s"}' if n_b else '')
            g, band = Grid.of(r, self.eng.s['cell_mm'])
            txt = (f'Area {len(r.area)} corners{extra}. Field {g.cols * g.cell / 1000:.2f} x '
                   f'{g.rows * g.cell / 1000:.2f} m, cells {g.cell / 1000:.2f} m, texture {g.cols} x {g.rows + band}')
            self._set('st_map', OK, txt)
        elif r.has_area():
            self._set('st_map', BAD, 'No base line: a black field is sent (step 4, Choose base line)')
        else:
            self._set('st_map', BAD, 'No area: a black field is sent (step 4)')
