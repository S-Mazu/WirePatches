"""Motion Bridge: 2D lidar -> foreground in a room map -> field as a Spout texture for Wire patches.

Usage:
  .venv\\Scripts\\python.exe bridge.py                 window (default)
  .venv\\Scripts\\python.exe bridge.py --headless      no window, runs until Ctrl+C
  Options: --port COM19

Settings are made in the window: detection and output in settings.json, everything about
the room (positions, area with base line, exclusions, barriers) in rooms/<name>/. Interface to the patches: README.md.
"""
import argparse
import logging
import logging.handlers
import os
import sys
import threading
import time

from engine import Engine
from simpath import parse_path

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

    def thread_failed(args):            # a thread that dies must leave a trace in the log
        logging.getLogger('bridge').critical('thread %s died', args.thread.name if args.thread else '?',
                                             exc_info=(args.exc_type, args.exc_value, args.exc_traceback))
    threading.excepthook = thread_failed
    sys.excepthook = lambda t, v, tb: logging.getLogger('bridge').critical('uncaught error', exc_info=(t, v, tb))


def main():
    ap = argparse.ArgumentParser(description='2D lidar to a Spout field texture for Wire patches')
    ap.add_argument('--port', help='serial port of the position with the sensor (this run only)')
    ap.add_argument('--headless', action='store_true', help='no window, run until Ctrl+C')
    ap.add_argument('--snapshot', help='save a PNG of the window after 4 s and quit (documentation, tests)')
    ap.add_argument('--sim', type=int, default=0, help='start with this many simulated objects')
    ap.add_argument('--sim-path', action='append', default=[], metavar='X1,Y1,X2,Y2',
                    help='add a simulated object that walks between A (X1,Y1) and B (X2,Y2) and back; field shares: '
                         'x 0..1 along the base line, y 0..1 into the area. Repeat for more objects')
    ap.add_argument('--sim-speed', type=float, help='walking speed of simulated objects in m/s (default 1)')
    ap.add_argument('--sim-pause', type=float, help='pause of simulated objects at each end in s (default 5)')
    a = ap.parse_args()
    try:
        sim_paths = [parse_path(p) for p in a.sim_path]
    except ValueError as e:
        ap.error(str(e))
    setup_logging(HERE)
    overrides = {}
    if a.port:
        overrides['port'] = a.port
    eng = Engine(HERE, overrides)
    eng.start()
    log.info('started: room %s', eng.room.name)
    try:
        if a.headless:
            live = eng.live_station()
            print(f"Motion Bridge running headless: room {eng.room.name}, sensor "
                  f"{live.port if live else '-'} -> Spout. Ctrl+C to stop.")
            while True:
                time.sleep(1)
        else:
            from ui.window import run_window      # dearpygui is only needed with a window
            run_window(eng, a.snapshot, a.sim, sim_paths, a.sim_speed, a.sim_pause)
    except KeyboardInterrupt:
        pass
    eng.stop()
    eng.join(timeout=2)
    sys.exit(0)


if __name__ == '__main__':
    main()
