"""Motion Bridge: 2D lidar -> foreground in a room map -> fields and points over OSC for Wire patches.

Usage:
  .venv\\Scripts\\python.exe bridge.py                 window (default)
  .venv\\Scripts\\python.exe bridge.py --headless      no window, runs until Ctrl+C
  Options: --port COM19   --osc 127.0.0.1:7000 (repeatable)

Settings are made in the window: detection and output in settings.json, everything about
the room (positions, area, front, exclusions, barriers) in rooms/<name>/. OSC interface: README.md.
"""
import argparse
import logging
import logging.handlers
import os
import sys
import time

from engine import Engine

HERE = os.path.dirname(os.path.abspath(__file__))
log = logging.getLogger('bridge')


def setup_logging(folder):
    """Console and logs/bridge.log (1 MB, 3 files kept)."""
    os.makedirs(os.path.join(folder, 'logs'), exist_ok=True)
    fmt = logging.Formatter('%(asctime)s %(levelname)s %(name)s: %(message)s')
    root = logging.getLogger()
    root.setLevel(logging.INFO)
    fh = logging.handlers.RotatingFileHandler(os.path.join(folder, 'logs', 'bridge.log'),
                                              maxBytes=1_000_000, backupCount=3, encoding='utf-8')
    fh.setFormatter(fmt)
    root.addHandler(fh)
    ch = logging.StreamHandler()
    ch.setFormatter(fmt)
    root.addHandler(ch)


def main():
    ap = argparse.ArgumentParser(description='2D lidar to OSC for Wire patches')
    ap.add_argument('--port', help='serial port of the position with the sensor (this run only)')
    ap.add_argument('--osc', action='append', help='host:port, repeatable (this run only; default Arena 7000 and Wire 8000)')
    ap.add_argument('--headless', action='store_true', help='no window, run until Ctrl+C')
    ap.add_argument('--snapshot', help='save a PNG of the window after 4 s and quit (documentation, tests)')
    ap.add_argument('--sim', type=int, default=0, help='start with this many simulated objects')
    a = ap.parse_args()
    setup_logging(HERE)
    overrides = {}
    if a.port:
        overrides['port'] = a.port
    if a.osc:
        overrides['osc_targets'] = a.osc
    eng = Engine(HERE, overrides)
    eng.start()
    log.info('started: room %s, targets %s', eng.room.name, ', '.join(eng.s['osc_targets']))
    try:
        if a.headless:
            live = eng.live_station()
            print(f"Motion Bridge running headless: room {eng.room.name}, sensor "
                  f"{live.port if live else '-'} -> {', '.join(eng.s['osc_targets'])}. Ctrl+C to stop.")
            while True:
                time.sleep(1)
        else:
            from ui.window import run_window      # dearpygui is only needed with a window
            run_window(eng, a.snapshot, a.sim)
    except KeyboardInterrupt:
        pass
    eng.stop()
    eng.join(timeout=2)
    sys.exit(0)


if __name__ == '__main__':
    main()
