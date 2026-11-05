"""Mask processing: Kinect body index or depth in, smooth white mask out.

This module uses no Kinect and no Spout, so it can be tested without hardware.
"""
import math
from dataclasses import asdict, dataclass

import cv2
import numpy as np

WIDTH, HEIGHT = 512, 424    # Kinect v2 depth and body index size (fixed in the hardware)
UPSCALE = 3
OUT_WIDTH, OUT_HEIGHT = WIDTH * UPSCALE, HEIGHT * UPSCALE
BODY_COUNT = 6              # body index values 0..5 are people, 255 is background

_KERNEL_OPEN = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
_KERNEL_CLOSE = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9))
_BLUR_SIGMA = 1.2


@dataclass
class Settings:
    """Everything the sliders control. Ranges are wide on purpose; see sanitized()."""
    near_mm: float = 500        # Depth mask: nearest distance that counts
    far_mm: float = 4500        # Depth mask: farthest distance that counts
    tolerance_mm: float = 120   # Depth mask: this much closer than the empty room = foreground
    min_area_px: float = 400    # blobs smaller than this are dropped (Kinect pixels)
    calm: float = 0.5           # temporal smoothing: 0 = none, 0.95 = very calm but laggy
    edge_softness: float = 0.15 # width of the soft edge: 0 = hard, 1 = very soft

    def sanitized(self):
        """Return a copy with invalid values replaced, so no slider value can break a frame."""
        defaults = Settings()
        def value(name, low, high):
            v = getattr(self, name)
            if not isinstance(v, (int, float)) or not math.isfinite(v):
                v = getattr(defaults, name)
            return float(min(max(v, low), high))
        return Settings(
            near_mm=value("near_mm", 0, 8000),
            far_mm=value("far_mm", 0, 8000),
            tolerance_mm=value("tolerance_mm", 0, 4000),
            min_area_px=value("min_area_px", 0, WIDTH * HEIGHT),
            calm=value("calm", 0, 0.95),
            edge_softness=value("edge_softness", 0, 1),
        )

    def to_dict(self):
        return asdict(self)

    @staticmethod
    def from_dict(data):
        """Build settings from loaded data; unknown keys and wrong types are ignored."""
        known = Settings.__dataclass_fields__
        return Settings(**{k: v for k, v in data.items() if k in known}).sanitized()


def body_mask(body_index):
    """Body index frame (uint8) -> mask: 255 where a person is, else 0."""
    return ((body_index < BODY_COUNT) * 255).astype(np.uint8)


def depth_mask(depth, room, settings):
    """Depth frame in mm -> mask: 255 where something is closer than the empty room.

    Without a captured room the mask is empty, because everything would count as foreground.
    A room pixel of 0 means the sensor had no reading there.
    """
    if room is None:
        return np.zeros(depth.shape, np.uint8)
    d = depth.astype(np.int32)
    valid = (d >= settings.near_mm) & (d <= settings.far_mm)
    closer = (room == 0) | (room - d > settings.tolerance_mm)
    return ((valid & closer) * 255).astype(np.uint8)


def room_from_frames(frames):
    """Depth frames of the empty room -> one room picture (median; 0 = no reading)."""
    return np.median(np.stack(frames), axis=0).astype(np.int32)


def clean(mask, min_area_px):
    """Remove specks and small holes, then drop blobs smaller than min_area_px."""
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, _KERNEL_OPEN)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, _KERNEL_CLOSE)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    keep = np.zeros(count, np.uint8)
    keep[1:] = (stats[1:, cv2.CC_STAT_AREA] >= min_area_px) * 255
    return keep[labels]


def _edge_table(softness):
    """Lookup table that turns a blurred mask into a clean edge of the given softness."""
    half_width = max(1.0, softness * 127.0)
    x = np.clip((np.arange(256, dtype=np.float32) - 127.5 + half_width) / (2 * half_width), 0, 1)
    return (x * x * (3 - 2 * x) * 255).astype(np.uint8)


class MaskFilter:
    """Temporal smoothing and upscaling for one mask. Keeps state between frames."""

    def __init__(self):
        self._state = np.zeros((HEIGHT, WIDTH), np.float32)
        self._softness = None
        self._table = None

    def process(self, mask, settings):
        """Clean mask (WIDTH x HEIGHT) -> smooth uint8 mask (OUT_WIDTH x OUT_HEIGHT)."""
        cv2.accumulateWeighted(mask, self._state, 1.0 - settings.calm)
        small = cv2.GaussianBlur(self._state, (0, 0), _BLUR_SIGMA)
        big = cv2.resize(small, (OUT_WIDTH, OUT_HEIGHT), interpolation=cv2.INTER_LINEAR)
        if settings.edge_softness != self._softness:
            self._softness = settings.edge_softness
            self._table = _edge_table(settings.edge_softness)
        return cv2.LUT(big.astype(np.uint8), self._table)


def to_rgba(mask):
    """Mask -> RGBA: white person, transparent background.

    Colour is premultiplied by alpha, so the background is black and transparent. It stays
    clean whether the receiver treats alpha as premultiplied or straight.
    """
    return cv2.merge((mask, mask, mask, mask))
