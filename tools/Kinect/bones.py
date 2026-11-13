"""Skeleton drawing: joint positions in, smooth white stick figures out.

This module uses no Kinect and no Spout, so it can be tested without hardware.
A skeleton is a float array of shape (JOINT_COUNT, 3): x and y in depth pixels, z in metres
from the sensor. NaN = unknown.
"""
import cv2
import numpy as np

from masks import OUT_HEIGHT, OUT_WIDTH, UPSCALE

JOINT_COUNT = 25
HEAD = 3
HEAD_RADIUS_M = 0.11        # the Kinect delivers only one point for the head; this is an estimate
FOCAL_PX = 365.0            # approximate focal length of the Kinect v2 depth camera
MIN_Z_M = 0.2               # closer than this, the head stops growing
BONES = (
    # Kinect v2 joint numbers. Spine and head
    (3, 2), (2, 20), (20, 1), (1, 0),
    # Left arm and hand
    (20, 4), (4, 5), (5, 6), (6, 7), (7, 21), (6, 22),
    # Right arm and hand
    (20, 8), (8, 9), (9, 10), (10, 11), (11, 23), (10, 24),
    # Left leg
    (0, 12), (12, 13), (13, 14), (14, 15),
    # Right leg
    (0, 16), (16, 17), (17, 18), (18, 19),
)

_SHIFT = 4                                  # fractional bits for sub-pixel drawing
_LIMIT = 4 * max(OUT_WIDTH, OUT_HEIGHT)     # keeps far off-screen points in integer range


def draw(skeletons, bone_width, head_size=1.0):
    """Skeletons -> uint8 mask (OUT_WIDTH x OUT_HEIGHT).

    bone_width is % of the picture height. head_size scales the estimated head; 0 = no head.
    """
    image = np.zeros((OUT_HEIGHT, OUT_WIDTH), np.uint8)
    thickness = max(1, round(bone_width / 100 * OUT_HEIGHT))
    for joints in skeletons:
        # Pixel centres: the same mapping as the linear upscale of the masks.
        out = np.clip((joints[:, :2] + 0.5) * UPSCALE - 0.5, -_LIMIT, _LIMIT)
        for a, b in BONES:
            if np.isnan(out[a]).any() or np.isnan(out[b]).any():
                continue
            cv2.line(image, _fixed(out[a]), _fixed(out[b]), 255, thickness, cv2.LINE_AA, _SHIFT)
        _draw_head(image, out[HEAD], joints[HEAD, 2], head_size)
    return image


def _fixed(point):
    return tuple(int(v) for v in point * (1 << _SHIFT))


def _draw_head(image, centre, z, head_size):
    if np.isnan(centre).any() or np.isnan(z) or z <= 0:
        return
    radius = head_size * HEAD_RADIUS_M * FOCAL_PX / max(z, MIN_Z_M) * UPSCALE
    fixed_radius = int(min(radius, _LIMIT) * (1 << _SHIFT))
    if fixed_radius > 0:
        cv2.circle(image, _fixed(centre), fixed_radius, 255, cv2.FILLED, cv2.LINE_AA, _SHIFT)


class BoneFilter:
    """Steadies each person's joints over time. Keeps state between frames."""

    def __init__(self):
        self._state = {}

    def process(self, skeletons, settings):
        """Skeletons by tracking id -> uint8 mask. None means no new data: keep the last picture."""
        if skeletons is not None:
            self._state = {id_: self._smooth(id_, joints, settings.calm) for id_, joints in skeletons.items()}
        return draw(self._state.values(), settings.bone_width, settings.head_size)

    def _smooth(self, tracking_id, joints, calm):
        previous = self._state.get(tracking_id)
        if previous is None:
            return joints.copy()
        blended = previous + (joints - previous) * (1.0 - calm)
        return np.where(np.isnan(previous), joints, blended)
