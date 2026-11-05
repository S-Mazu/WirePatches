"""Thin layer around the Kinect library. To try another library, only this file changes."""
import numpy as np
from kinect_next import KinectSensor, StreamType
from kinect_next.core.enums import JOINT_COUNT, TrackingState
from kinect_next.core.exceptions import KinectError, KinectTimeoutError

FRAME_TIMEOUT_MS = 1000


class SourceLost(Exception):
    """The sensor cannot be opened or stopped working."""


class KinectSource:
    """Delivers (body_index, depth, skeletons); see read()."""

    def __init__(self):
        self._sensor = KinectSensor(streams=StreamType.DEPTH | StreamType.BODY_INDEX | StreamType.BODY)

    def __enter__(self):
        try:
            self._sensor.open()
        except (KinectError, OSError) as error:
            raise SourceLost(str(error)) from error
        return self

    def __exit__(self, *_):
        self._sensor.close()

    @property
    def is_available(self):
        return self._sensor.is_available

    def read(self):
        """Return (body_index, depth, skeletons), or None if no complete frame arrived in time.

        skeletons maps tracking id -> joints in depth pixels (see bones.py), or is None when
        this frame has no body data.
        """
        try:
            frames = self._sensor.wait_for_frames(FRAME_TIMEOUT_MS)
        except KinectTimeoutError:
            return None
        except KinectError as error:
            raise SourceLost(str(error)) from error
        if frames.body_index is None or frames.depth is None:
            return None
        return frames.body_index.data, frames.depth.data, self._skeletons(frames)

    def _skeletons(self, frames):
        if frames.floor_clip_plane is None:     # the library sets it only with a body frame
            return None
        return {body.tracking_id: self._joints(body) for body in frames.tracked_bodies}

    def _joints(self, body):
        joints = np.full((JOINT_COUNT, 2), np.nan, np.float32)
        for joint in body.joints:
            if joint.tracking_state != TrackingState.NOT_TRACKED:
                point = joint.to_depth_space(self._sensor.mapper)
                if point.is_valid():
                    joints[int(joint.joint_type)] = point.x, point.y
        return joints
