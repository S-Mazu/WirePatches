"""Every module loads: a syntax error in the window code must not wait until the bridge is started."""
import importlib
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


class ImportTest(unittest.TestCase):
    def test_all_modules_load(self):
        try:
            import dearpygui  # noqa: F401
        except ImportError:
            self.skipTest('dearpygui not installed')
        for name in ('bridge', 'engine', 'spout_check', 'spout_out', 'ui.help', 'ui.theme', 'ui.drafting',
                     'ui.view', 'ui.panel', 'ui.status', 'ui.window'):
            with self.subTest(module=name):
                importlib.import_module(name)


if __name__ == '__main__':
    unittest.main()
