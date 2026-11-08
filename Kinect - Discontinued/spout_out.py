"""Sends a mask as a Spout source. Must be created and used in the same thread."""
import numpy as np
import SpoutGL
from SpoutGL.enums import GL_RGBA

import masks


class MaskSender:
    def __init__(self, name):
        self._name = name
        self._sender = SpoutGL.SpoutSender()
        self._sender.setSenderName(name)
        self._empty = np.zeros((masks.OUT_HEIGHT, masks.OUT_WIDTH, 4), np.uint8)

    def send(self, mask):
        self._send_rgba(masks.to_rgba(mask))

    def send_empty(self):
        self._send_rgba(self._empty)

    def _send_rgba(self, rgba):
        self._sender.sendImage(rgba, masks.OUT_WIDTH, masks.OUT_HEIGHT, GL_RGBA, False, 0)
        self._sender.setFrameSync(self._name)

    def close(self):
        self._sender.releaseSender()
