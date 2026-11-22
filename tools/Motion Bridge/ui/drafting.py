"""Clicks in the room map that draw or change a shape: area, exclusion, barrier, reference marker,
base line; a double click adds or deletes a corner.

No dearpygui here, so it can be tested: the view feeds clicks in map millimetres, the draft
says when the shape is complete, and `apply` writes it into the room.
"""
import math

from room import nearest_corner, nearest_edge

CLOSE_MM = 250.0            # a click this close to the first corner closes a polygon
PICK_MM = 600.0             # a click this close to an edge picks it (base line)
POLYGONS = ('area', 'exclude')
LINES = {'barrier': 2, 'ref': 1, 'base': 1}     # clicks that complete these

LABELS = {'area': 'Drawing the area: click its corners in the view, then Finish (or click the first corner). '
                  'The edge from the first to the second corner becomes the base line.',
          'exclude': 'Drawing an exclusion: click the corners in the view, then Finish.',
          'barrier': 'Drawing a barrier: click its two ends in the view.',
          'ref': 'Click the reference object in the view.',
          'base': 'Click the edge of the area that becomes the base line.'}


class Draft:
    def __init__(self):
        self.mode = None
        self.points = []
        self.error = ''             # why the last apply did nothing, for the window

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
        self.mode, self.points, self.error = mode, [], ''

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
        """Writes the shape into the room and ends the draft. False (and `error`) if nothing changed."""
        mode, pts = self.mode, self.points
        self.error = ''
        if not self.complete():
            self.cancel()
            return False
        ok = True
        if mode == 'area':
            room.area = pts
            room.base = 0
        elif mode == 'exclude':
            room.exclusions.append(pts)
        elif mode == 'barrier':
            room.barriers.append(pts)
        elif mode == 'ref':
            room.ref = pts[0]
        elif mode == 'base':
            ok = self._base(room, pts[0])
        self.cancel()
        return ok

    def _base(self, room, p):
        if not room.has_area():
            self.error = 'Draw the area first.'
            return False
        k, d = nearest_edge(room.area, p)
        if d > PICK_MM:
            self.error = 'No edge of the area there: click closer to an edge.'
            return False
        room.set_base(k)
        return True


def _polygons(room):
    """(kind, index, corners) of every polygon that can be edited: the area first, then the exclusions."""
    out = [('area', 0, room.area)] if room.has_area() else []
    return out + [('excl', i, e) for i, e in enumerate(room.exclusions)]


def toggle_corner(room, p, pick_mm):
    """A double click at p: deletes the corner of the area or an exclusion there, or else adds a corner to the
    edge there. Corners win over edges. Returns (changed, message why not)."""
    corners = [(nearest_corner(poly, p), kind, i) for kind, i, poly in _polygons(room)]
    best = min(corners, key=lambda b: b[0][1], default=None)
    if best is not None and best[0][1] <= pick_mm:
        (k, _), kind, i = best
        poly = room.area if kind == 'area' else room.exclusions[i]
        if len(poly) <= 3:
            return False, 'A shape keeps at least three corners; remove it instead.'
        if kind == 'area':
            room.delete_corner(k)
        else:
            del poly[k]
        return True, ''
    edges = [(nearest_edge(poly, p), kind, i) for kind, i, poly in _polygons(room)]
    best = min(edges, key=lambda b: b[0][1], default=None)
    if best is not None and best[0][1] <= pick_mm:
        (k, _), kind, i = best
        if kind == 'area':
            room.insert_corner(k, p)
        else:
            room.exclusions[i].insert(k + 1, [float(p[0]), float(p[1])])
        return True, ''
    return False, ''
