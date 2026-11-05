"""Tests for bones.py. No Kinect needed. Run: py -3.12 -m unittest"""
import unittest

import numpy as np

import bones
from masks import OUT_HEIGHT, OUT_WIDTH, UPSCALE, Settings


def skeleton(**points):
    """Skeleton with only the given joints known, e.g. skeleton(j3=(100, 50), j2=(100, 90))."""
    joints = np.full((bones.JOINT_COUNT, 2), np.nan, np.float32)
    for name, point in points.items():
        joints[int(name[1:])] = point
    return joints


def at(image, x, y):
    """Value of the picture at a depth pixel."""
    return image[int((y + 0.5) * UPSCALE), int((x + 0.5) * UPSCALE)]


class Bones(unittest.TestCase):
    def test_every_joint_number_is_valid_and_used(self):
        used = {joint for bone in bones.BONES for joint in bone}
        self.assertEqual(used, set(range(bones.JOINT_COUNT)))


class Draw(unittest.TestCase):
    def test_picture_has_the_size_of_the_masks(self):
        self.assertEqual(bones.draw([], 1).shape, (OUT_HEIGHT, OUT_WIDTH))

    def test_no_people_is_black(self):
        self.assertEqual(bones.draw([], 1).max(), 0)

    def test_bone_connects_its_two_joints(self):
        image = bones.draw([skeleton(j3=(100, 50), j2=(100, 90))], 1)
        self.assertEqual(at(image, 100, 70), 255)
        self.assertEqual(at(image, 200, 70), 0)

    def test_bone_with_an_unknown_joint_is_not_drawn(self):
        self.assertEqual(bones.draw([skeleton(j3=(100, 50))], 1).max(), 0)

    def test_width_follows_the_setting(self):
        thin = bones.draw([skeleton(j3=(100, 50), j2=(100, 90))], 1)
        wide = bones.draw([skeleton(j3=(100, 50), j2=(100, 90))], 5)
        self.assertEqual(at(thin, 110, 70), 0)
        self.assertEqual(at(wide, 110, 70), 255)

    def test_points_far_outside_are_safe(self):
        image = bones.draw([skeleton(j3=(1e30, -1e30), j2=(100, 90))], 1)
        self.assertEqual(image.shape, (OUT_HEIGHT, OUT_WIDTH))

    def test_infinite_points_are_safe(self):
        image = bones.draw([skeleton(j3=(np.inf, -np.inf), j2=(100, 90))], 1)
        self.assertEqual(image.shape, (OUT_HEIGHT, OUT_WIDTH))


class Filter(unittest.TestCase):
    def setUp(self):
        self.filter = bones.BoneFilter()
        self.first = skeleton(j3=(100, 50), j2=(100, 90))
        self.moved = skeleton(j3=(300, 50), j2=(300, 90))

    def test_new_person_appears_at_once(self):
        image = self.filter.process({1: self.first}, Settings(calm=0.9))
        self.assertEqual(at(image, 100, 70), 255)

    def test_without_calm_the_bone_follows_at_once(self):
        self.filter.process({1: self.first}, Settings(calm=0))
        image = self.filter.process({1: self.moved}, Settings(calm=0))
        self.assertEqual(at(image, 300, 70), 255)
        self.assertEqual(at(image, 100, 70), 0)

    def test_calm_moves_only_part_of_the_way(self):
        self.filter.process({1: self.first}, Settings(calm=0.5))
        image = self.filter.process({1: self.moved}, Settings(calm=0.5))
        self.assertEqual(at(image, 200, 70), 255)

    def test_person_who_left_disappears(self):
        self.filter.process({1: self.first}, Settings())
        self.assertEqual(self.filter.process({}, Settings()).max(), 0)

    def test_missing_body_data_keeps_the_picture(self):
        self.filter.process({1: self.first}, Settings())
        image = self.filter.process(None, Settings())
        self.assertEqual(at(image, 100, 70), 255)

    def test_joint_that_was_unknown_appears_at_once(self):
        self.filter.process({1: skeleton(j3=(100, 50))}, Settings(calm=0.9))
        image = self.filter.process({1: self.first}, Settings(calm=0.9))
        self.assertEqual(at(image, 100, 70), 255)

    def test_extreme_slider_values_are_safe(self):
        for width in (float("nan"), float("inf"), -5, 1e9):
            image = self.filter.process({1: self.first}, Settings(bone_width=width).sanitized())
            self.assertEqual(image.shape, (OUT_HEIGHT, OUT_WIDTH))


if __name__ == "__main__":
    unittest.main()
