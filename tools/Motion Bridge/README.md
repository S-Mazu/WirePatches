# Motion Bridge

Motion Bridge is a tool: it reads a 2D lidar, finds people in front of the projection surface and sends their positions to Wire patches over OSC, about 10 times per second.

Last updated: 2026-11-14

## Purpose

Makes projections react to people in the room. The bridge learns the empty room, treats everything closer than the room as a person, follows up to 8 people in fixed slots and maps each one onto the surface: where along the surface (`x`) and how far from it (`depth`). Patches only receive these normalized values, so they work on any surface and with any sensor that delivers them.

Patches using it: [Bulging Lines](../../sources/Bulging%20Lines/).

## Hardware

- LDROBOT **STL-27L** 2D lidar (also sold as WayPonDEV FHL-LD19 Plus), 360°, 10 Hz, about 2160 points per turn, 0.03–25 m (black targets up to 10 m).
- CP2102 USB adapter from the kit. Wiring of the kit cable (the colours do **not** follow the usual scheme):

  | Sensor pin | Wire | Signal | Adapter |
  |---|---|---|---|
  | 1 | black | Tx (data, 3.3 V) | RXD |
  | 2 | white | PWM | not connected (internal speed control) |
  | 3 | green | GND | GND |
  | 4 | red | +5 V | +5V |

- Mount the sensor at the height that should be detected (legs near the floor, bodies at about 1 m) with a free view, out of reach of people.

## Requirements

- OS: Windows 11 (signal tones use Windows; everything else is portable)
- Runtime: Python 3.13
- Packages: see `requirements.txt` (pyserial, numpy, python-osc, dearpygui)
- Resolume Wire 7.29 / Arena 7.29 with OSC input enabled

## Files

| File | Purpose |
|---|---|
| `bridge.py` | Start here: window (or `--headless`), command-line options |
| `engine.py` | Processing thread: background, people, tracking, surface mapping, calibration, OSC, quality log |
| `scene.py` | Empty-room background, foreground points, blobs |
| `stl27l.py` | Serial reader for the STL-27L (packets, CRC, revolutions, reconnect) |
| `requirements.txt` | Python packages with versions |

Created at runtime and not committed: `.venv/`, `settings.json` (all settings), `background.npy` (the learned room), `logs/` (quality logs).

## Installation

1. Copy this whole folder to any location.
2. Install the CP210x driver from Silicon Labs; the adapter then appears as `Silicon Labs CP210x USB to UART Bridge (COMx)` in the Device Manager.
3. In this folder:
   ```
   py -3.13 -m venv .venv
   .venv\Scripts\python.exe -m pip install -r requirements.txt
   ```
4. Arena: Preferences > OSC > OSC Input on, port 7000. Wire: Preferences > OSC Input on, port 8000. **Restart Arena** after switching OSC input on, otherwise patches inside clips may not receive the data.

## Usage

```
.venv\Scripts\python.exe bridge.py
```

The window shows the room from above on the left and the controls on the right. Every control explains itself when you hover over it; `(?) the view` explains the picture. F11 or the button switches fullscreen; the mouse wheel zooms the view, right-drag pans it.

1. **Learn the empty room**: set the countdown, click the button, leave the scan plane before the first tone and stay out until the second tone.
2. **Map to the surface**:
   - With a surface: click *Set left end*, walk to the left end of the projection surface and stand there alone, close to it, until the second tone. The same for *Set right end*. x then runs from 0 (left) to 1 (right).
   - Without a surface (sensor mode): x follows the angle in the watched half of the sensor. Turn the half with *Facing* or *Turn 180 deg* until the blue line points into the room; tick *Mirror* if the bulge moves the opposite way.
   - *Depth Range*: how far from the surface people count.
3. **Check**: the status lines must all be green; tracked people appear as coloured rings with their slot, x and depth.

**Simulation**: *Add person* creates a yellow point in the view that you drag with the mouse. It is sent like a real person, also without a sensor, so patches can be developed and checked anywhere.

For shows without a window: `.venv\Scripts\python.exe bridge.py --headless` uses the saved settings.

Options (this run only, not saved): `--port COM19`, `--osc 127.0.0.1:7000` (repeatable), `--sim 2` (start with simulated people), `--snapshot file.png` (save a picture of the window after 4 s and quit).

## Output

- Transport: OSC over UDP, sent to every target in *Output (OSC)*; default Arena `127.0.0.1:7000` and Wire `127.0.0.1:8000`.
- Data, once per revolution (about 10 Hz), 8 slots:

  | Address | Arguments | Meaning |
  |---|---|---|
  | `/lidar/x` | 8 floats | position along the surface, 0 = left end, 1 = right end (slightly outside is possible) |
  | `/lidar/depth` | 8 floats | distance from the surface, 0 = at the surface, 1 = Depth Range away |
  | `/lidar/active` | 8 floats | 0..1, fades in and out; 0 = slot empty |
  | `/lidar/count` | 1 float | number of active slots |
  | `/lidar/alive` | 1 float | counter 0..999, changes with every message |

- Patches read them with `OSC In` → `Read OSC` (address as above, 8 parameters). A patch should fade out when `/lidar/alive` stops changing, so it stays calm without the bridge (docs/PATCH_RULES.md, "Patches that use a tool").

## Configuration

All settings are made in the window and saved in `settings.json`:

- Countdown (default 10 s): time between a click and the measurement.
- Facing (0 deg), Mirror (off), Depth Range (3 m): see Usage.
- Advanced detection, with tooltips: Tolerance (150 mm), Blob gap (250 mm), Min points (6), Merge distance (450 mm), Track distance (700 mm), Smoothing (0.5), Hold (0.4 s), Fade (0.3 s). *Reset to defaults* restores them.
- Sensor port (COM19), OSC targets, quality log (off), UI scale (1.0).

## Troubleshooting

- Sensor not connected, no COM port: CP210x driver missing or USB cable. With a port but no data: check the wiring above; the motor must spin audibly.
- Walls or furniture show red: learn the empty room again, or raise *Tolerance*.
- One person gives two rings: raise *Merge distance*.
- A target has a red dot: nothing listens on that port; switch OSC input on in Arena or Wire.
- The patch in Wire reacts but not the one in Arena: restart Arena after switching OSC input on.

## Known issues

See [open issues for this tool](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Atool%3Amotion_bridge).

## Changelog

- 2026-11-14: First version (#69): STL-27L reader, empty-room background, people tracking in 8 slots, surface and sensor mode, guided calibration with countdown and tones, mouse-driven window with tooltips and fullscreen, simulated people, OSC target check, quality log.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
