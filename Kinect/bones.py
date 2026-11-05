"""Skeleton drawing: joint positions in, smooth white stick figures out.

This module uses no Kinect and no Spout, so it can be tested without hardware.
A skeleton is a float array of shape (JOINT_COUNT, 2) in depth pixels; NaN = joint unknown.
"""
import cv2
import numpy as np

from masks import OUT_HEIGHT, OUT_WIDTH, UPSCALE

JOINT_COUNT = 25
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


def draw(skeletons, bone_width):
    """Skeletons -> uint8 mask (OUT_WIDTH x OUT_HEIGHT). bone_width is % of the picture height."""
    image = np.zeros((OUT_HEIGHT, OUT_WIDTH), np.uint8)
    thickness = max(1, round(bone_width / 100 * OUT_HEIGHT))
    for joints in skeletons:
        # Pixel centres: the same mapping as the linear upscale of the masks.
        out = np.clip((joints + 0.5) * UPSCALE - 0.5, -_LIMIT, _LIMIT)
        for a, b in BONES:
            if np.isnan(out[a]).any() or np.isnan(out[b]).any():
                continue
            p1 = tuple(int(v) for v in out[a] * (1 << _SHIFT))
            p2 = tuple(int(v) for v in out[b] * (1 << _SHIFT))
            cv2.line(image, p1, p2, 255, thickness, cv2.LINE_AA, _SHIFT)
    return image


class BoneFilter:
    """Steadies each person's joints over time. Keeps state between frames."""

    def __init__(self):
        self._state = {}

    def process(self, skeletons, settings):
        """Skeletons by tracking id -> uint8 mask. None means no new data: keep the last picture."""
        if skeletons is not None:
            self._state = {id_: self._smooth(id_, joints, settings.calm) for id_, joints in skeletons.items()}
        return draw(self._state.values(), settings.bone_width)

    def _smooth(self, tracking_id, joints, calm):
        previous = self._state.get(tracking_id)
        if previous is None:
            return joints.copy()
        blended = previous + (joints - previous) * (1.0 - calm)
        return np.where(np.isnan(previous), joints, blended)
