"""Points output: foreground blobs merged into objects, followed in fixed slots.

Frozen: patches get the fields as the primary interface; this stays for patches that need
single positions (README.md, Points). Not extended.
"""
import math

SLOTS = 8
TICK_S = 0.1                 # assumed step when the first update has no predecessor


def merge(blobs, merge_mm):
    """Joins blobs closer than merge_mm into one object (parts of one body, the same body seen by two sensors)."""
    objects = []
    for b in blobs:
        for o in objects:
            if math.hypot(b['x'] - o['x'], b['y'] - o['y']) < merge_mm:
                n = o['n'] + b['n']
                o['x'] = (o['x'] * o['n'] + b['x'] * b['n']) / n
                o['y'] = (o['y'] * o['n'] + b['y'] * b['n']) / n
                o['n'] = n
                break
        else:
            objects.append({'x': b['x'], 'y': b['y'], 'n': b['n']})
    return objects


class Tracker:
    """Keeps objects in fixed slots; smooths positions and fades them in and out."""

    def __init__(self):
        self.tracks = {}
        self.last_t = None

    def update(self, objects, t, assoc_mm, smooth, hold_s, fade_s):
        dt = TICK_S if self.last_t is None else max(0.0, min(0.5, t - self.last_t))
        self.last_t = t
        pairs = sorted((math.hypot(o['x'] - tr['x'], o['y'] - tr['y']), s, i)
                       for s, tr in self.tracks.items() for i, o in enumerate(objects))
        used_s, used_o = set(), set()
        for d, s, i in pairs:
            if d > assoc_mm or s in used_s or i in used_o:
                continue
            tr, o = self.tracks[s], objects[i]
            tr['x'] += smooth * (o['x'] - tr['x'])
            tr['y'] += smooth * (o['y'] - tr['y'])
            tr['seen'] = t
            used_s.add(s)
            used_o.add(i)
        for i, o in enumerate(objects):
            if i in used_o:
                continue
            slot = next((k for k in range(SLOTS) if k not in self.tracks), None)
            if slot is None:
                break
            self.tracks[slot] = {'x': o['x'], 'y': o['y'], 'seen': t, 'active': 0.0}
        step = dt / max(fade_s, 1e-3)
        for s in list(self.tracks):
            tr = self.tracks[s]
            alive = t - tr['seen'] <= hold_s
            tr['active'] = min(1.0, tr['active'] + step) if alive else max(0.0, tr['active'] - step)
            if not alive and tr['active'] <= 0.0:
                del self.tracks[s]
