"""Thin layer around the Kinect library. To try another library, only this file changes."""
from kinect_next import KinectSensor, StreamType
from kinect_next.core.exceptions import KinectError, KinectTimeoutError

FRAME_TIMEOUT_MS = 1000


class SourceLost(Exception):
    """The sensor cannot be opened or stopped working."""


class KinectSource:
    """Delivers (body_index, depth) as NumPy arrays."""

    def __init__(self):
        self._sensor = KinectSensor(streams=StreamType.DEPTH | StreamType.BODY_INDEX)

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
        """Return (body_index, depth), or None if no complete frame arrived in time."""
        try:
            frames = self._sensor.wait_for_frames(FRAME_TIMEOUT_MS)
        except KinectTimeoutError:
            return None
        except KinectError as error:
            raise SourceLost(str(error)) from error
        if frames.body_index is None or frames.depth is None:
            return None
        return frames.body_index.data, frames.depth.data
