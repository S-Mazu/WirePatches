"""The window: room map on the left, control panel on the right, one update per frame."""
import logging
import time

import dearpygui.dearpygui as dpg

from .drafting import Draft
from .help import tip
from .panel import Panel
from .state import UiState
from .status import StatusLines
from .theme import Themes, load_font
from .view import RoomView

log = logging.getLogger(__name__)

PANEL_W = 450
SNAPSHOT_AFTER_S = 4.0


def run_window(eng, snapshot=None, sim=0, sim_paths=(), sim_speed=None, sim_pause=None):
    """Runs until the window is closed. snapshot: PNG path to write after a few seconds, then quit.
    sim_paths: ((x, y), (x, y)) per moving object, A and B in field shares (x along the front, y depth)."""
    dpg.create_context()
    font = load_font()
    themes = Themes()
    state, draft = UiState(), Draft()
    with dpg.colormap_registry():
        dpg.add_colormap([[20, 20, 28, 255], [255, 255, 255, 255]], False, tag='cm_field')   # 0 dark .. 1 white

    with dpg.window(tag='main', no_scrollbar=True):
        with dpg.group(horizontal=True):
            view = RoomView(eng, state, draft, themes, width=-PANEL_W - 10)
            with dpg.child_window(width=PANEL_W, height=-1, tag='panel'):
                with dpg.group(horizontal=True):
                    dpg.add_text('Motion Bridge', color=(255, 255, 255))
                    tip(dpg.add_text('(?) the view', color=(120, 170, 255)), 'view')
                with dpg.group(horizontal=True):
                    tip(dpg.add_button(label='Fullscreen (F11)', callback=lambda: dpg.toggle_viewport_fullscreen()),
                        'fullscreen')
                    tip(dpg.add_button(label='Fit view', callback=lambda: view.fit()), 'fit')
                dpg.add_separator()
                status = StatusLines(eng, state, draft, on_finish=view.finish, panel_w=PANEL_W)
                panel = Panel(eng, state, draft, view, panel_w=PANEL_W)

    with dpg.handler_registry():
        dpg.add_key_press_handler(dpg.mvKey_F11, callback=lambda: dpg.toggle_viewport_fullscreen())
        dpg.add_key_press_handler(dpg.mvKey_Escape, callback=lambda: draft.cancel())
        dpg.add_mouse_click_handler(button=dpg.mvMouseButton_Left, callback=lambda: view.on_press())
        dpg.add_mouse_release_handler(button=dpg.mvMouseButton_Left, callback=lambda: view.on_release())

    dpg.create_viewport(title='Motion Bridge', width=1550, height=1000, resizable=True)
    dpg.setup_dearpygui()
    if font:
        dpg.bind_font(font)
    dpg.set_global_font_scale(float(eng.s['ui_scale']))
    dpg.set_primary_window('main', True)
    dpg.show_viewport()

    def rebuild():
        view.rebuild()
        panel.rebuild()
        state.room_name = eng.room.name
        state.rebuild = False

    rebuild()
    view.fit()
    for _ in range(sim):
        view.add_sim()
    if sim_speed is not None:
        view.sim_speed_mps = max(float(sim_speed), 0.0)
        dpg.set_value('sim_speed', view.sim_speed_mps)
    if sim_pause is not None:
        view.sim_pause_s = max(float(sim_pause), 0.0)
        dpg.set_value('sim_pause', view.sim_pause_s)
    if sim_paths:
        if eng.room.ready():
            for a, b in sim_paths:
                view.add_sim(eng.room.from_field(*a), eng.room.from_field(*b))
            view.set_sim_move(True)
            dpg.set_value('sim_move', True)
        else:
            log.warning('--sim-path needs a room with area and front; ignored')
    last_snap = None
    t_start = time.time()
    while dpg.is_dearpygui_running():
        if state.rebuild or state.room_name != eng.room.name:
            rebuild()
        view.frame()
        panel.frame()
        snap = eng.snapshot()
        status.frame(view.sim_count, len(snap['objects']) if snap else 0)
        if snap is not None and snap is not last_snap:
            last_snap = snap
            view.show(snap)
            panel.show(snap)
        dpg.render_dearpygui_frame()
        if snapshot and time.time() - t_start > SNAPSHOT_AFTER_S:
            dpg.output_frame_buffer(file=snapshot)
            dpg.render_dearpygui_frame()
            dpg.render_dearpygui_frame()
            log.info('snapshot written to %s', snapshot)
            break
    eng.flush_room()
    dpg.destroy_context()
