"""Drawing a shape into the room map with clicks: area, exclusion, barrier, front, reference marker.

No dearpygui here, so it can be tested: the view feeds clicks in map millimetres, the draft
says when the shape is complete, and `apply` writes it into the room.
"""
import math

CLOSE_MM = 250.0            # a click this close to the first corner closes a polygon
POLYGONS = ('area', 'exclude')
LINES = {'barrier': 2, 'front': 2, 'ref': 1}        # clicks that complete these shapes

LABELS = {'area': 'Drawing the area: click its corners in the view, then Finish (or click the first corner).',
          'exclude': 'Drawing an exclusion: click the corners in the view, then Finish.',
          'barrier': 'Drawing a barrier: click its two ends in the view.',
          'front': 'Drawing the front: click the LEFT end, then the RIGHT end (seen from the area).',
          'ref': 'Click the reference object in the view.'}


class Draft:
    def __init__(self):
        self.mode = None
        self.points = []

    @property
    def active(self):
        return self.mode is not None

    @property
    def needs_finish(self):
        """Polygons are closed with the Finish button (or a click on the first corner)."""
        return self.mode in POLYGONS

    @property
    def label(self):
        return LABELS[self.mode] + '  (Esc cancels)' if self.mode else ''

    def start(self, mode):
        self.mode, self.points = mode, []

    def cancel(self):
        self.mode, self.points = None, []

    def click(self, x_mm, y_mm):
        """Adds a click; True when the shape is complete."""
        if not self.mode:
            return False
        p = [float(x_mm), float(y_mm)]
        if self.needs_finish:
            if len(self.points) >= 3 and math.hypot(p[0] - self.points[0][0], p[1] - self.points[0][1]) < CLOSE_MM:
                return True
            self.points.append(p)
            return False
        self.points.append(p)
        return len(self.points) >= LINES[self.mode]

    def complete(self):
        if self.needs_finish:
            return len(self.points) >= 3
        return self.mode in LINES and len(self.points) == LINES[self.mode]

    def apply(self, room):
        """Writes the shape into the room and ends the draft. False if the shape is not complete."""
        mode, pts = self.mode, self.points
        if not self.complete():
            self.cancel()
            return False
        if mode == 'area':
            room.area = pts
        elif mode == 'exclude':
            room.exclusions.append(pts)
        elif mode == 'barrier':
            room.barriers.append(pts)
        elif mode == 'front':
            room.front = {'left': pts[0], 'right': pts[1]}
        elif mode == 'ref':
            room.ref = pts[0]
        self.cancel()
        return True
