"""Kinect v2 -> Spout masks and bones for Arena and Wire, with a clickable control window.

Start: py -3.12 kinect_app.py
"""
import json
import sys
from pathlib import Path

import cv2
import dearpygui.dearpygui as dpg
import numpy as np

import engine as eng
from masks import Settings

SETTINGS_FILE = Path(__file__).parent / "settings.json"
PREVIEW_W, PREVIEW_H = 384, 318
SLIDERS = (
    # field, label, min, max, format
    ("near_mm", "Near (mm)", 0, 8000, "%.0f"),
    ("far_mm", "Far (mm)", 0, 8000, "%.0f"),
    ("tolerance_mm", "Room tolerance (mm)", 0, 1000, "%.0f"),
    ("min_area_px", "Min blob size", 0, 5000, "%.0f"),
    ("calm", "Calm", 0, 0.95, "%.2f"),
    ("edge_softness", "Edge softness", 0, 1, "%.2f"),
    ("bone_width", "Bone width", 0.05, 20, "%.2f"),
    ("head_size", "Head size", 0, 10, "%.2f"),
)


def load_settings():
    try:
        return Settings.from_dict(json.loads(SETTINGS_FILE.read_text(encoding="utf-8")))
    except (OSError, ValueError, TypeError, AttributeError):
        return Settings().sanitized()


def save_settings(settings):
    try:
        SETTINGS_FILE.write_text(json.dumps(settings.to_dict(), indent=2), encoding="utf-8")
    except OSError as error:
        print(f"Settings not saved: {error}")


def preview_pixels(mask):
    """Mask -> flat RGBA float array for the preview texture."""
    small = cv2.resize(mask, (PREVIEW_W, PREVIEW_H), interpolation=cv2.INTER_AREA)
    grey = small.astype(np.float32) / 255.0
    pixels = np.empty((PREVIEW_H, PREVIEW_W, 4), np.float32)
    pixels[..., :3] = grey[..., None]
    pixels[..., 3] = 1.0
    return pixels.ravel()


def build_window(settings, on_change, on_capture):
    blank = np.zeros(PREVIEW_W * PREVIEW_H * 4, np.float32)
    with dpg.texture_registry():
        dpg.add_raw_texture(PREVIEW_W, PREVIEW_H, blank, format=dpg.mvFormat_Float_rgba, tag="tex_body")
        dpg.add_raw_texture(PREVIEW_W, PREVIEW_H, blank, format=dpg.mvFormat_Float_rgba, tag="tex_depth")
        dpg.add_raw_texture(PREVIEW_W, PREVIEW_H, blank, format=dpg.mvFormat_Float_rgba, tag="tex_bones")

    with dpg.window(tag="main"):
        dpg.add_text("Starting", tag="status_sensor")
        dpg.add_spacer(height=4)
        with dpg.group(horizontal=True):
            with dpg.group():
                dpg.add_text(f"Spout: {eng.BODY_SENDER}")
                dpg.add_image("tex_body")
            with dpg.group():
                dpg.add_text(f"Spout: {eng.DEPTH_SENDER}")
                dpg.add_image("tex_depth")
            with dpg.group():
                dpg.add_text(f"Spout: {eng.BONES_SENDER}")
                dpg.add_image("tex_bones")
        dpg.add_spacer(height=8)
        with dpg.group(horizontal=True):
            dpg.add_button(label="Capture empty room", width=180, height=32, callback=on_capture)
            dpg.add_text("", tag="status_room")
        dpg.add_spacer(height=8)
        for field, label, low, high, fmt in SLIDERS:
            dpg.add_slider_float(
                label=label, tag=f"slider_{field}", width=500, min_value=low, max_value=high,
                default_value=getattr(settings, field), format=fmt, callback=on_change,
            )


def read_sliders():
    return Settings(**{field: dpg.get_value(f"slider_{field}") for field, *_ in SLIDERS}).sanitized()


def main():
    settings = load_settings()
    engine = eng.Engine(settings)

    dpg.create_context()

    def on_change():
        engine.set_settings(read_sliders())

    build_window(settings, on_change, lambda: engine.capture_room())
    dpg.create_viewport(title="Kinect", width=1210, height=695)
    dpg.setup_dearpygui()
    dpg.show_viewport()
    dpg.set_primary_window("main", True)
    engine.start()

    # Optional for automated runs: quit after this many seconds.
    quit_at = None
    if len(sys.argv) > 1:
        quit_at = dpg.get_total_time() + float(sys.argv[1])

    last_seq = -1
    while dpg.is_dearpygui_running():
        status = engine.status()
        fps = f"{status['fps']:.1f} FPS, {status['frame_ms']:.1f} ms per frame"
        dpg.set_value("status_sensor", f"Kinect: {status['sensor']}  |  {fps}")
        dpg.set_value("status_room", status["room"])
        if status["seq"] != last_seq and status["preview"] is not None:
            last_seq = status["seq"]
            dpg.set_value("tex_body", preview_pixels(status["preview"][0]))
            dpg.set_value("tex_depth", preview_pixels(status["preview"][1]))
            dpg.set_value("tex_bones", preview_pixels(status["preview"][2]))
        dpg.render_dearpygui_frame()
        if quit_at is not None and dpg.get_total_time() >= quit_at:
            break

    save_settings(read_sliders())
    engine.stop()
    engine.join(timeout=5)
    dpg.destroy_context()


if __name__ == "__main__":
    main()
