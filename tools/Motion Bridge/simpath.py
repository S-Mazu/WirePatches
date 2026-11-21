"""Where a simulated object that moves between two points is at a given time.

It starts at A, pauses there, walks to B at a constant speed, pauses there, walks back, and so on.
A pause at both ends lets a patch see the object standing still (occupancy without motion) and
then moving (motion), the two things the bridge reports, without anyone walking in front of the sensor.
"""
import math


def path_position(a, b, speed_mm_s, pause_s, t):
    """Position (x, y) in mm at time t (s) on the way A -> B -> A, with a pause of pause_s at each end.

    speed_mm_s <= 0 or A == B: the object stays at A.
    """
    ax, ay = float(a[0]), float(a[1])
    bx, by = float(b[0]), float(b[1])
    dist = math.hypot(bx - ax, by - ay)
    if not (speed_mm_s > 0.0) or dist < 1e-6 or not math.isfinite(t):
        return ax, ay
    pause = max(0.0, float(pause_s)) if math.isfinite(pause_s) else 0.0
    walk = dist / speed_mm_s
    period = 2.0 * (pause + walk)
    p = math.fmod(max(t, 0.0), period)
    if p < pause:                               # at A
        f = 0.0
    elif p < pause + walk:                      # A -> B
        f = (p - pause) / walk
    elif p < 2.0 * pause + walk:                # at B
        f = 1.0
    else:                                       # B -> A
        f = 1.0 - (p - 2.0 * pause - walk) / walk
    return ax + (bx - ax) * f, ay + (by - ay) * f


def parse_path(text):
    """'x1,y1,x2,y2' (field shares) -> ((x1, y1), (x2, y2)); raises ValueError."""
    v = [float(s) for s in text.replace(' ', '').split(',')]
    if len(v) != 4 or not all(math.isfinite(x) for x in v):
        raise ValueError(f'expected x1,y1,x2,y2, got {text!r}')
    return (v[0], v[1]), (v[2], v[3])
