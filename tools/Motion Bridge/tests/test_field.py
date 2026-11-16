"""Occupancy and motion field over the area, with synthetic foreground."""
import os
import sys
import unittest

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from field import EMPTY_SIZE, Field, grid_size  # noqa: E402
from room import Room, Station  # noqa: E402

POINTS = 2160           # one STL-27L turn
FRONT = {'left': [0.0, 0.0], 'right': [4000.0, 0.0]}
RECT = [[0.0, 0.0], [4000.0, 0.0], [4000.0, -3000.0], [0.0, -3000.0]]


def make_room(area=RECT):
    room = Room('', 'T')
    room.stations = [Station('A', 'Position A')]
    room.area, room.front = area, FRONT
    return room


def view(fg_x, fg_y, dist_mm=3000.0, width_mm=300.0):
    """A sensor view as the engine builds it: one turn, foreground only along a surface.

    The surface lies along the front (x), centred on (fg_x, fg_y), seen from dist_mm, with
    the point spacing of a real turn. A cell is 125 x 187.5 mm at 32 x 16 over 4 x 3 m, and
    occupancy relates the covered length to 0.5 * (125 + 187.5) = 156 mm: a surface
    crossing a cell along x gives 125 / 156 = 0.8.
    """
    mx = np.full(POINTS, 1e6)
    my = np.full(POINTS, 1e6)
    fg = np.zeros(POINTS, bool)
    spacing = dist_mm * 2.0 * np.pi / POINTS
    n_fg = int(round(width_mm / spacing))
    mx[:n_fg] = fg_x + (np.arange(n_fg) - (n_fg - 1) / 2.0) * spacing
    my[:n_fg] = fg_y
    fg[:n_fg] = True
    return {'mx': mx, 'my': my, 'valid': np.ones(POINTS, bool), 'fg': fg,
            'keep': np.ones(POINTS, bool), 'dist': np.full(POINTS, dist_mm)}


class FieldTest(unittest.TestCase):
    def run_field(self, room, views, sim=(), ticks=1, t0=0.0, cols=32, rows=16, fade=0.5, threshold=0.3):
        f = Field()
        f.configure(cols, rows)
        for k in range(ticks):
            f.update(room, views, list(sim), t0 + 0.1 * k, fade, threshold)
        return f

    def test_surface_fills_its_cell(self):
        f = self.run_field(make_room(), [view(1062.5, -800.0)], ticks=8)      # centre of cell column 8, row 4
        self.assertGreater(f.occ[4, 8], 0.75)                                  # 0.8 crossing the cell along x
        self.assertGreater(f.occ[4, 7], 0.4)                                   # the surface reaches the neighbours
        self.assertGreater(f.occ[4, 9], 0.4)
        self.assertEqual(float(np.delete(f.occ, 4, axis=0).max()), 0.0)        # nothing in any other row

    def test_motion_appears_and_fades(self):
        room = make_room()
        f = self.run_field(room, [view(1062.5, -800.0)], ticks=1)
        self.assertGreater(f.motion[4, 8], 0.9)                                # taken within 0.1 s
        for k in range(1, 30):                                                 # stays: motion fades out
            f.update(room, [view(1062.5, -800.0)], [], 0.1 * k, 0.5, 0.3)
        self.assertLess(f.motion[4, 8], 0.05)
        self.assertGreater(f.occ[4, 8], 0.75)

    def test_nothing_without_area_or_front(self):
        room = make_room(area=None)
        f = self.run_field(room, [view(1000.0, -750.0)], ticks=3)
        self.assertEqual(float(f.occ.max()), 0.0)

    def test_cells_outside_the_area_stay_zero(self):
        room = make_room(area=[[0, 0], [4000, 0], [0, -3000]])               # triangle, far right outside
        f = self.run_field(room, [view(3500.0, -2500.0)], ticks=3)            # x 0.875, depth 0.83: outside
        self.assertEqual(float(f.occ.max()), 0.0)
        f = self.run_field(room, [view(500.0, -500.0)], ticks=3)              # inside
        self.assertGreater(float(f.occ.max()), 0.5)

    def test_simulated_object_is_a_disc(self):
        f = self.run_field(make_room(), [], sim=[[2000.0, -1500.0]], ticks=4)  # x 0.5, depth 0.5
        self.assertGreater(f.occ[8, 16], 0.9)
        self.assertGreater(f.occ[7, 15], 0.5)
        self.assertEqual(float(f.occ[0, 0]), 0.0)

    def test_two_sensors_do_not_double_count(self):
        v = view(1000.0, -750.0)
        f1 = self.run_field(make_room(), [v], ticks=6)
        f2 = self.run_field(make_room(), [v, v], ticks=6)
        np.testing.assert_allclose(f1.occ, f2.occ)


if __name__ == '__main__':
    unittest.main()


class TextureTest(unittest.TestCase):
    def test_grid_size_follows_the_cell_size_and_keeps_the_band_a_sixteenth(self):
        self.assertEqual(grid_size(4000.0, 3000.0, 100.0), (40, 30, 2))          # 30 needed: 2 x 15, band 2 of 32
        self.assertEqual(grid_size(20000.0, 8000.0, 125.0), (160, 75, 5))        # 64 needed: 5 x 15 rows
        self.assertEqual(grid_size(50.0, 50.0, 100.0), (1, 15, 1))
        cols, rows, band = grid_size(1e9, 1e9, 1.0)
        self.assertEqual((cols, rows, band), (1020, 1020, 68))
        for depth in (300.0, 1499.0, 1500.0, 1501.0, 30000.0):
            cols, rows, band = grid_size(4000.0, depth, 100.0)
            self.assertEqual(rows, 15 * band)
            self.assertGreaterEqual(rows, round(depth / 100.0))

    def test_configure_for_room(self):
        f = Field()
        f.configure_for(make_room(), 100.0)
        self.assertEqual((f.cols, f.rows, f.band), (40, 30, 2))
        f.configure_for(make_room(area=None), 100.0)
        self.assertEqual((f.cols, f.rows, f.band), EMPTY_SIZE)

    def test_texture_layout(self):
        room = make_room()
        f = Field()
        f.configure_for(room, 100.0)
        for k in range(6):
            f.update(room, [], [[2000.0, -1500.0]], 0.1 * k, 0.5, 0.3)        # disc at x 0.5, depth 0.5
        tex = f.texture(200)
        self.assertEqual(tex.shape, (32, 40, 4))                             # 30 field rows + band of 2
        self.assertEqual(tex.dtype, np.uint8)
        self.assertEqual(int(tex[..., 3].min()), 255)                        # opaque
        self.assertGreater(int(tex[15, 20, 0]), 230)                         # R occupancy at the disc
        self.assertEqual(int(tex[0, 0, 0]), 0)                               # front left corner empty
        self.assertEqual(int(tex[30:, :, :3].min()), 200)                    # the whole band carries the counter
        self.assertEqual(int(tex[30:, :, :3].max()), 200)
        self.assertEqual(int(tex[:30, :, 2].max()), 0)                       # visibility not implemented: 0
        self.assertEqual(int(f.texture(300)[31, 39, 1]), 44)                 # counter wraps at 256

    def test_motion_fades_between_measurements(self):
        room = make_room()
        f = Field()
        f.configure_for(room, 100.0)
        f.update(room, [], [[2000.0, -1500.0]], 0.0, 0.5, 0.3)
        m0 = float(f.motion.max())
        self.assertGreater(m0, 0.9)
        f.decay(0.25, 0.5)
        self.assertAlmostEqual(float(f.motion.max()), m0 * np.exp(-0.5), places=4)
        f.decay(0.5, 0.5)
        f.update(room, [], [[2000.0, -1500.0]], 0.6, 0.5, 0.3)              # same place: no new motion
        self.assertLess(float(f.motion.max()), m0 * np.exp(-1.0) + 0.05)
