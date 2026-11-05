"""Background thread: reads the Kinect, builds both masks and sends them via Spout."""
import threading
import time
from pathlib import Path

import numpy as np

import masks
from kinect_source import KinectSource, SourceLost
from spout_out import MaskSender

BODY_SENDER = "Kinect Body"
DEPTH_SENDER = "Kinect Depth"
ROOM_FILE = Path(__file__).parent / "empty_room.npy"
ROOM_FRAMES = 30            # about 1 s at 30 FPS
ROOM_DELAY_S = 5            # time to leave the picture after the click
RETRY_S = 2
LOST_AFTER_TIMEOUTS = 3     # with a 1 s frame timeout: about 3 s without frames


def load_room():
    """Return the saved empty room, or None if there is none or it is unusable."""
    try:
        room = np.load(ROOM_FILE)
    except (OSError, ValueError):
        return None
    return room if room.shape == (masks.HEIGHT, masks.WIDTH) else None


class Engine(threading.Thread):
    def __init__(self, settings):
        super().__init__(daemon=True, name="Kinect engine")
        self._lock = threading.Lock()
        self._stop_event = threading.Event()
        self._settings = settings.sanitized()
        self._room = load_room()
        self._room_at = None        # time when the room capture starts
        self._room_frames = []
        self._body = masks.MaskFilter()
        self._depth = masks.MaskFilter()
        self._preview = None
        self._seq = 0
        self._sensor_state = "Starting"
        self._room_state = "Saved room loaded" if self._room is not None else "No room captured"
        self._fps = 0.0
        self._frame_ms = 0.0

    # --- called from the window ------------------------------------------------
    def set_settings(self, settings):
        with self._lock:
            self._settings = settings.sanitized()

    def capture_room(self):
        with self._lock:
            self._room_at = time.monotonic() + ROOM_DELAY_S
            self._room_frames = []

    def stop(self):
        self._stop_event.set()

    def status(self):
        with self._lock:
            state = self._room_state
            if self._room_at is not None:
                left = self._room_at - time.monotonic()
                state = f"Leave the picture: {left:.0f} s" if left > 0 else "Capturing empty room"
            return {
                "sensor": self._sensor_state,
                "room": state,
                "fps": self._fps,
                "frame_ms": self._frame_ms,
                "seq": self._seq,
                "preview": self._preview,
            }

    # --- thread -----------------------------------------------------------------
    def run(self):
        senders = MaskSender(BODY_SENDER), MaskSender(DEPTH_SENDER)
        try:
            while not self._stop_event.is_set():
                try:
                    with KinectSource() as source:
                        self._read_loop(source, senders)
                except SourceLost as error:
                    print(f"Kinect: {error}")
                self._set_sensor("Kinect not connected")
                self._send_empty(senders)
                self._stop_event.wait(RETRY_S)
        finally:
            for sender in senders:
                sender.close()

    def _read_loop(self, source, senders):
        timeouts = 0
        counted, t0 = 0, time.monotonic()
        while not self._stop_event.is_set():
            frame = source.read()
            if frame is None:
                timeouts += 1
                self._set_sensor("Waiting for frames")
                self._send_empty(senders)
                if timeouts >= LOST_AFTER_TIMEOUTS and not source.is_available:
                    return
                continue
            timeouts = 0
            start = time.perf_counter()
            self._process(*frame, senders)
            took_ms = (time.perf_counter() - start) * 1000
            counted += 1
            now = time.monotonic()
            with self._lock:
                self._sensor_state = "Running"
                self._frame_ms = took_ms
                if now - t0 >= 1:
                    self._fps = counted / (now - t0)
                    counted, t0 = 0, now

    def _process(self, body_index, depth, senders):
        with self._lock:
            settings = self._settings
            room = self._room
            capturing = self._room_at is not None and time.monotonic() >= self._room_at
        if capturing:
            self._collect_room(depth)
        area = settings.min_area_px
        small_body = masks.clean(masks.body_mask(body_index), area)
        small_depth = masks.clean(masks.depth_mask(depth, room, settings), area)
        big_body = self._body.process(small_body, settings)
        big_depth = self._depth.process(small_depth, settings)
        senders[0].send(big_body)
        senders[1].send(big_depth)
        with self._lock:
            self._preview = (big_body, big_depth)
            self._seq += 1

    def _collect_room(self, depth):
        self._room_frames.append(depth.copy())
        if len(self._room_frames) < ROOM_FRAMES:
            return
        room = masks.room_from_frames(self._room_frames)
        try:
            np.save(ROOM_FILE, room)
            state = "Empty room captured"
        except OSError as error:
            state = f"Room not saved: {error}"
        with self._lock:
            self._room = room
            self._room_at = None
            self._room_frames = []
            self._room_state = state

    def _send_empty(self, senders):
        """Replace the last picture, so no frozen silhouette stays on the projection."""
        for sender in senders:
            sender.send_empty()
        with self._lock:
            self._fps = 0.0

    def _set_sensor(self, state):
        with self._lock:
            self._sensor_state = state
