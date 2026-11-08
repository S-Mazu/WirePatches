"""Tests for masks.py. No Kinect needed. Run: py -3.12 -m unittest"""
import unittest

import numpy as np

import masks
from masks import HEIGHT, OUT_HEIGHT, OUT_WIDTH, WIDTH, Settings


def filled(value, dtype):
    return np.full((HEIGHT, WIDTH), value, dtype)


class BodyMask(unittest.TestCase):
    def test_people_are_white_and_background_is_black(self):
        frame = filled(255, np.uint8)
        frame[10, 10] = 0
        frame[20, 20] = 5
        mask = masks.body_mask(frame)
        self.assertEqual(mask[10, 10], 255)
        self.assertEqual(mask[20, 20], 255)
        self.assertEqual(mask[0, 0], 0)


class DepthMask(unittest.TestCase):
    def setUp(self):
        self.room = filled(3000, np.int32)
        self.settings = Settings()

    def mask_at(self, mm):
        return masks.depth_mask(filled(mm, np.uint16), self.room, self.settings)[0, 0]

    def test_no_room_gives_an_empty_mask(self):
        self.assertFalse(masks.depth_mask(filled(1000, np.uint16), None, self.settings).any())

    def test_closer_than_room_is_foreground(self):
        self.assertEqual(self.mask_at(2000), 255)

    def test_within_tolerance_is_background(self):
        self.assertEqual(self.mask_at(2950), 0)

    def test_outside_the_distance_band_is_background(self):
        self.assertEqual(self.mask_at(100), 0)

    def test_no_depth_reading_is_background(self):
        self.assertEqual(self.mask_at(0), 0)

    def test_room_without_reading_counts_as_far_away(self):
        self.room[:] = 0
        self.assertEqual(self.mask_at(4000), 255)


class Room(unittest.TestCase):
    def test_median_ignores_a_single_outlier(self):
        frames = [filled(2000, np.uint16), filled(2000, np.uint16), filled(0, np.uint16)]
        self.assertEqual(masks.room_from_frames(frames)[0, 0], 2000)


class Clean(unittest.TestCase):
    def test_small_blob_is_removed_and_large_blob_stays(self):
        mask = filled(0, np.uint8)
        mask[10:14, 10:14] = 255
        mask[100:200, 100:200] = 255
        result = masks.clean(mask, 400)
        self.assertEqual(result[12, 12], 0)
        self.assertEqual(result[150, 150], 255)

    def test_empty_mask_stays_empty(self):
        self.assertFalse(masks.clean(filled(0, np.uint8), 400).any())


class Filter(unittest.TestCase):
    def full_mask(self):
        mask = filled(0, np.uint8)
        mask[100:300, 200:300] = 255
        return mask

    def test_output_size(self):
        out = masks.MaskFilter().process(self.full_mask(), Settings(calm=0))
        self.assertEqual(out.shape, (OUT_HEIGHT, OUT_WIDTH))
        self.assertEqual(out.dtype, np.uint8)

    def test_inside_is_white_and_outside_is_black(self):
        out = masks.MaskFilter().process(self.full_mask(), Settings(calm=0))
        self.assertEqual(out[600, 750], 255)
        self.assertEqual(out[0, 0], 0)

    def test_edge_stays_in_place_after_upscaling(self):
        out = masks.MaskFilter().process(self.full_mask(), Settings(calm=0, edge_softness=0))
        row = out[600]
        edge = int(np.argmax(row > 127))
        self.assertAlmostEqual(edge, 200 * masks.UPSCALE, delta=masks.UPSCALE)

    def test_calm_delays_a_new_shape(self):
        f = masks.MaskFilter()
        settings = Settings(calm=0.9)
        first = f.process(self.full_mask(), settings)
        self.assertEqual(first[600, 750], 0)
        for _ in range(60):
            last = f.process(self.full_mask(), settings)
        self.assertEqual(last[600, 750], 255)

    def test_slider_extremes(self):
        for calm in (0, 0.95):
            for softness in (0, 1):
                out = masks.MaskFilter().process(self.full_mask(), Settings(calm=calm, edge_softness=softness))
                self.assertEqual(out.shape, (OUT_HEIGHT, OUT_WIDTH))


class Rgba(unittest.TestCase):
    def test_colour_equals_alpha(self):
        rgba = masks.to_rgba(np.array([[0, 128, 255]], np.uint8))
        self.assertEqual(rgba.shape, (1, 3, 4))
        self.assertTrue((rgba[..., 0] == rgba[..., 3]).all())


class SettingsValues(unittest.TestCase):
    def test_invalid_values_fall_back_to_defaults(self):
        s = Settings(calm=float("nan"), tolerance_mm=float("inf")).sanitized()
        self.assertEqual(s.calm, Settings().calm)
        self.assertEqual(s.tolerance_mm, Settings().tolerance_mm)

    def test_out_of_range_values_are_limited(self):
        self.assertEqual(Settings(calm=5).sanitized().calm, 0.95)

    def test_loading_ignores_unknown_keys_and_wrong_types(self):
        s = Settings.from_dict({"calm": "x", "nonsense": 1, "near_mm": 800})
        self.assertEqual(s.calm, Settings().calm)
        self.assertEqual(s.near_mm, 800)


if __name__ == "__main__":
    unittest.main()
