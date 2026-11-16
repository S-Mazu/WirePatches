"""Colours, series themes and the font of the window."""
import os

import dearpygui.dearpygui as dpg

STATION_COLORS = [(80, 140, 255), (90, 220, 150), (200, 130, 255), (255, 170, 70), (255, 110, 150), (90, 220, 230)]
OK, WARN, BAD, DIM = (110, 220, 130), (255, 190, 80), (255, 95, 85), (140, 140, 155)
FRONT_C, AREA_C, EXCL_C, BAR_C, REF_C = (255, 255, 255), (90, 210, 120), (255, 95, 85), (255, 150, 40), (255, 230, 90)
FIELD_C, FG_OUT_C, SIM_C = (150, 150, 165), (150, 60, 55), (255, 210, 60)
SCAN_C, FG_C, OUT_C, DRAFT_C = (175, 175, 185), (255, 80, 70), (70, 70, 85), (255, 255, 120)
OBJ_C = (240, 240, 240)


def station_color(room, sid):
    k = next((i for i, s in enumerate(room.stations) if s.sid == sid), 0)
    return STATION_COLORS[k % len(STATION_COLORS)]


def load_font(size=18):
    """The system font (Windows), or None for dearpygui's default."""
    fonts_dir = os.path.join(os.environ.get('WINDIR', ''), 'Fonts')
    for name in ('segoeui.ttf', 'arial.ttf'):
        path = os.path.join(fonts_dir, name)
        if os.path.exists(path):
            with dpg.font_registry():
                return dpg.add_font(path, size)
    return None


class Themes:
    """Series themes by colour and size, created once each."""

    def __init__(self):
        self._cache = {}

    def series(self, color, size, marker=None, ring=False):
        key = (tuple(color), size, marker, ring)
        if key in self._cache:
            return self._cache[key]
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
        self._cache[key] = th
        return th
