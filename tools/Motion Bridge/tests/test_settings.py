"""Settings: defaults, file, limits, overrides that are never written back."""
import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from settings import ADVANCED, DEFAULTS, Settings  # noqa: E402


class SettingsTest(unittest.TestCase):
    def path(self, content=None):
        p = os.path.join(tempfile.mkdtemp(), 'settings.json')
        if content is not None:
            with open(p, 'w') as f:
                json.dump(content, f)
        return p

    def test_defaults_without_a_file(self):
        s = Settings(self.path())
        self.assertEqual(s['tol_mm'], DEFAULTS['tol_mm'])
        self.assertFalse(os.path.exists(s.path))                        # nothing written until set

    def test_file_values_and_legacy_keys(self):
        p = self.path({'tol_mm': 200.0, 'port': 'COM7', 'left': [0, 0], 'unknown': 1})
        s = Settings(p)
        self.assertEqual(s['tol_mm'], 200.0)
        self.assertEqual(s.raw['port'], 'COM7')                         # taken over by the first start
        self.assertNotIn('unknown', s)

    def test_set_clamps_and_writes(self):
        s = Settings(self.path())
        s.set('field_cols', 99)
        s.set('smooth', -1)
        self.assertEqual(s['field_cols'], 32)
        self.assertEqual(s['smooth'], 0.05)
        with open(s.path) as f:
            self.assertEqual(json.load(f)['field_cols'], 32)
        with self.assertRaises(KeyError):
            s.set('nonsense', 1)

    def test_override_is_used_but_not_saved(self):
        p = self.path({'osc_targets': ['10.0.0.1:7000']})
        s = Settings(p, {'osc_targets': ['127.0.0.1:9000'], 'port': 'COM3'})
        self.assertEqual(s['osc_targets'], ['127.0.0.1:9000'])
        s.set('tol_mm', 180.0)
        with open(p) as f:
            data = json.load(f)
        self.assertEqual(data['osc_targets'], ['10.0.0.1:7000'])
        self.assertEqual(data['tol_mm'], 180.0)

    def test_reset_group(self):
        s = Settings(self.path())
        s.set('gap_mm', 600.0)
        s.reset(ADVANCED)
        self.assertEqual(s['gap_mm'], DEFAULTS['gap_mm'])

    def test_broken_file_falls_back_to_defaults(self):
        p = self.path()
        with open(p, 'w') as f:
            f.write('{not json')
        s = Settings(p)
        self.assertEqual(s['hold_s'], DEFAULTS['hold_s'])


if __name__ == '__main__':
    unittest.main()
