"""Sends the field texture as a Spout source (Windows, SpoutGL).

Create and use the sender in one thread (the engine thread). Without SpoutGL the bridge
runs and shows the field in the window, but sends nothing; `status` says so.
"""
import logging

log = logging.getLogger(__name__)

SENDER_NAME = 'Motion Bridge'
FLIP = False            # whether rows are sent bottom-up; fixed after the first test in Wire (README, Output)

try:
    import SpoutGL
    from SpoutGL.enums import GL_RGBA
    AVAILABLE = True
except ImportError:                     # not Windows, or not installed
    SpoutGL = None
    GL_RGBA = None
    AVAILABLE = False


class SpoutOut:
    def __init__(self, name: str = SENDER_NAME):
        self.name = name
        self.frames = 0
        self.size = (0, 0)
        self.error = ''
        self._sender = None
        if AVAILABLE:
            try:
                self._sender = SpoutGL.SpoutSender()
                self._sender.setSenderName(name)
                log.info('Spout sender %s created', name)
            except Exception as e:          # any failure here must not stop the bridge
                self.error = str(e)
                log.error('Spout sender failed: %s', e)
        else:
            log.warning('SpoutGL not installed: no Spout output (pip install SpoutGL)')

    @property
    def sending(self) -> bool:
        return self._sender is not None and not self.error

    @property
    def status(self) -> str:
        if self._sender is None:
            return 'SpoutGL not installed' if not AVAILABLE else f'Spout failed: {self.error[:60]}'
        if self.error:
            return f'Spout failed: {self.error[:60]}'
        return f'Spout {self.name}: {self.size[0]} x {self.size[1]} pixels'

    def send(self, rgba) -> None:
        """rgba: uint8 array (height, width, 4), row 0 at the front of the area."""
        h, w = rgba.shape[0], rgba.shape[1]
        self.size = (w, h)
        if self._sender is None:
            return
        try:
            self._sender.sendImage(rgba, w, h, GL_RGBA, FLIP, 0)
            self._sender.setFrameSync(self.name)
            self.frames += 1
            self.error = ''
        except Exception as e:
            if not self.error:
                log.error('Spout send failed: %s', e)
            self.error = str(e)

    def close(self) -> None:
        if self._sender is not None:
            try:
                self._sender.releaseSender()
            except Exception as e:
                log.debug('Spout release: %s', e)
            self._sender = None
