"""Detection and output settings: defaults, limits, settings.json, command-line overrides.

Everything about the room (positions, area, front, exclusions, barriers) lives in the room
files (room.py), not here.
"""
import json
import logging
import os

log = logging.getLogger(__name__)

DEFAULT_PORT = 'COM19'                  # serial port of the lidar adapter until the user picks one

DEFAULTS = {
    'room': 'Default',
    'countdown_s': 10.0,
    'tol_mm': 150.0,
    'gap_mm': 250.0,
    'min_pts': 6,
    'merge_mm': 450.0,
    'cell_mm': 100.0,                   # field cell size; the texture size follows the area
    'field_fade_s': 0.5,
    'field_threshold': 0.3,             # measured: sensor noise at still objects reaches 0.28
    'log_quality': False,
    'ui_scale': 1.0,
    'panel_open': True,                 # the settings column is open (closed during a performance)
}

# Numeric settings are clamped to these ranges when set; the window's sliders use the same.
LIMITS = {
    'countdown_s': (0.0, 60.0),
    'tol_mm': (25.0, 2000.0),
    'gap_mm': (50.0, 2000.0),
    'min_pts': (1, 100),
    'merge_mm': (0.0, 3000.0),
    'cell_mm': (20.0, 1000.0),
    'field_fade_s': (0.0, 5.0),
    'field_threshold': (0.0, 0.9),
    'ui_scale': (0.7, 2.5),
}

ADVANCED = ('tol_mm', 'gap_mm', 'min_pts', 'merge_mm')
FIELD = ('cell_mm', 'field_fade_s', 'field_threshold')


class Settings:
    """Settings with defaults, a JSON file and overrides for one run.

    Overrides (command line) take effect but are never written to the file; the file keeps
    what it held for those keys. `raw` is the file as loaded, including keys of older
    versions (port, left, right, depth_mm) that the first start takes over into a room.
    """

    def __init__(self, path: str, overrides: dict | None = None):
        self.path = path
        self.raw: dict = {}
        if os.path.exists(path):
            try:
                with open(path) as f:
                    self.raw = json.load(f)
            except (OSError, ValueError) as e:
                log.warning('settings.json not readable, using defaults: %s', e)
        self._saved = {k: self._clamp(k, self.raw[k]) for k in DEFAULTS if k in self.raw}
        self.overrides = {k: v for k, v in (overrides or {}).items() if k in DEFAULTS}
        self._values = dict(DEFAULTS)
        self._values.update(self._saved)
        self._values.update(self.overrides)

    @staticmethod
    def _clamp(key: str, value):
        if key in LIMITS:
            lo, hi = LIMITS[key]
            kind = type(DEFAULTS[key])
            try:
                return kind(min(max(kind(value), lo), hi))
            except (TypeError, ValueError):
                return DEFAULTS[key]
        return value

    def __getitem__(self, key: str):
        return self._values[key]

    def __contains__(self, key: str) -> bool:
        return key in self._values

    def get(self, key: str, default=None):
        return self._values.get(key, default)

    def set(self, key: str, value) -> None:
        """Sets one value (clamped to its limits) and writes the file."""
        if key not in DEFAULTS:
            raise KeyError(key)
        self._values[key] = self._clamp(key, value)
        if key in self.overrides:
            self.overrides.pop(key)             # the user chose: the override is over
        self.save()

    def reset(self, keys) -> None:
        """Puts the given keys back to their defaults and writes the file."""
        for k in keys:
            self._values[k] = DEFAULTS[k]
            self.overrides.pop(k, None)
        self.save()

    def save(self) -> None:
        data = dict(self._values)
        for k in self.overrides:                # command-line values are not written back
            data[k] = self._saved.get(k, DEFAULTS[k])
        tmp = self.path + '.tmp'
        try:
            with open(tmp, 'w') as f:
                json.dump(data, f, indent=2)
            os.replace(tmp, self.path)
        except OSError as e:
            log.error('cannot write %s: %s', self.path, e)
