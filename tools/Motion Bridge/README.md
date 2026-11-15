# Motion Bridge

Motion Bridge is a tool: it reads a 2D lidar, finds people in a room map and sends their positions to Wire patches over OSC, about 10 times per second.

Last updated: 2026-11-15

## Purpose

Makes projections react to people in the room. The bridge learns the empty room, treats everything closer than the room as a person, follows up to 8 people in fixed slots and maps each one onto the screen: where along the screen (`x`) and how far from it (`depth`).

Everything happens in one room map seen from above, in metres. Several sensor positions (several sensors, or one sensor moved around) are aligned in this map; the screen, areas, exclusions and barriers are drawn into it with the mouse. Rooms of any size and shape work: nothing assumes a rectangle. Patches only receive these normalized values, so they work on any surface and with any sensor that delivers them.

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
| `engine.py` | Processing thread: background, people, tracking in the room map, calibration, OSC, quality log |
| `room.py` | Room map: sensor positions, fans, screen, areas, barriers, reference snap, saving |
| `scene.py` | Empty-room background, foreground points, blobs |
| `stl27l.py` | Serial reader for the STL-27L (packets, CRC, revolutions, reconnect) |
| `requirements.txt` | Python packages with versions |

Created at runtime and not committed: `.venv/`, `settings.json` (detection and output settings), `rooms/<name>/` (one folder per room: `room.json` and a scan per position, `scan_<id>.npy`), `logs/` (quality logs). On the first start an older `background.npy` and surface ends are taken over into the room `Default`.

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

The window shows the room map on the left and the controls on the right. Every control explains itself when you hover over it; `(?) the view` explains the picture. F11 or the button switches fullscreen; the mouse wheel zooms the view, right-drag pans it, left-drag moves handles.

1. **Room**: pick a room or create one per venue. Everything below is saved in it.
2. **Sensor positions**:
   - *Learn empty room here*: set the countdown, click, leave the scan plane before the first tone and stay out until the second tone.
   - Another sensor place: put the sensor there, click *Sensor moved: new position* and learn the empty room again. The new position takes over the sensor; the old one keeps its scan.
   - Align the positions so that their scans overlap: drag the coloured square, set *Rotation* (buttons for fine steps).
   - With a reference object (a free-standing pole or tube, 10 cm or more, standing in the room while each position learns): *Place marker* and click it in an aligned scan, select the other position, *Snap position to marker*, tick *Turn around reference* and turn until the walls match. Afterwards remove the object and learn the empty room of the live position again.
   - *Fan*: how wide this position looks (360 = all around); point it with the round handle in the view. Tick *Mirrored* if the map shows the room mirrored.
3. **Screen**: *Draw in view* (click the left end, then the right end, as seen from the room), or stand at each end with *Stand at left end* / *Stand at right end*. The arrow points into the room; *Swap ends* turns it. *Depth Range*: the distance where `depth` reaches 1. Without a screen nothing is sent.
4. **Areas and barriers** (all optional):
   - *Draw area*: click the corners, then *Finish* or click the first corner. With areas, only people inside them count. *Area from screen* gives a start that you shape with *Edit points*.
   - *Draw exclusion*: nobody inside counts.
   - *Draw barrier*: two clicks; sensors do not look through it.
   - Esc cancels drawing. Each shape can be removed in the list.
5. **Advanced detection**: fine-tune detection for the room (see Configuration).
6. **Check**: the status lines should be green (yellow = keep an eye on it, red = act); hover over a line to see what it means and which values are good. Tracked people appear as coloured rings with their slot, x and depth.

**Simulation**: *Add person* creates a yellow point in the view that you drag with the mouse. It is sent like a real person, also without a sensor, so patches can be developed and checked anywhere.

For shows without a window: `.venv\Scripts\python.exe bridge.py --headless` uses the saved settings.

Options (this run only, not saved): `--port COM19` (port of the position with the sensor), `--osc 127.0.0.1:7000` (repeatable), `--sim 2` (start with simulated people), `--snapshot file.png` (save a picture of the window after 4 s and quit).

## Output

- Transport: OSC over UDP, sent to every target in *Output (OSC)*; default Arena `127.0.0.1:7000` and Wire `127.0.0.1:8000`.
- Data, once per revolution (about 10 Hz), 8 slots:

  | Address | Arguments | Meaning |
  |---|---|---|
  | `/lidar/x` | 8 floats | position along the screen, 0 = left end, 1 = right end (outside 0..1 beside the screen; limit it with an area) |
  | `/lidar/depth` | 8 floats | distance from the screen, 0 = at the screen, 1 = Depth Range away or further |
  | `/lidar/active` | 8 floats | 0..1, fades in and out; 0 = slot empty |
  | `/lidar/count` | 1 float | number of active slots |
  | `/lidar/alive` | 1 float | counter 0..999, changes with every message |

- Patches read them with `OSC In` → `Read OSC` (address as above, 8 parameters). A patch should fade out when `/lidar/alive` stops changing, so it stays calm without the bridge (docs/PATCH_RULES.md, "Patches that use a tool").

## Configuration

All settings are made in the window. Detection and output are saved in `settings.json`, the room in `rooms/<name>/`:

- Countdown (default 10 s): time between a click and the measurement.
- Room: positions, rotation, fan, screen, Depth Range (3 m), areas, barriers, reference: see Usage.
- Advanced detection, with tooltips: Tolerance (150 mm), Blob gap (250 mm), Min points (6), Merge distance (450 mm), Track distance (700 mm), Smoothing (0.5), Hold (0.4 s), Fade (0.3 s). *Reset to defaults* restores them.
- *Sensor*: the port of the position with the sensor (COM19); OSC targets, quality log (off), UI scale (1.0).

## Troubleshooting

- Sensor not connected, no COM port: CP210x driver missing or USB cable. With a port but no data: check the wiring above; the motor must spin audibly.
- Walls or furniture show red: learn the empty room again, or raise *Tolerance*.
- One person gives two rings: raise *Merge distance*. With two positions: align them better.
- People appear in the wrong place or mirrored: check the screen ends (*Swap ends*) and *Mirrored* of the position.
- A target has a red dot: nothing listens on that port; switch OSC input on in Arena or Wire.
- The patch in Wire reacts but not the one in Arena: restart Arena after switching OSC input on.

## Known issues

See [open issues for this tool](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Atool%3Amotion_bridge).

## Changelog

- 2026-11-15: Room map (#69): rooms per venue, several sensor positions with their own scans, alignment by dragging, rotation and a reference object, fan per position, screen drawn in the view or set by standing at its ends, areas, exclusions and barriers. Sensor mode removed; without a screen nothing is sent. Untested with more than one physical sensor.
- 2026-11-15: Status explains itself on hover, with traffic lights; scan quality (revolutions per second, valid share, points per revolution) moved into the status as *Signal*; the *Sensor* section only holds port, connect and log; *Advanced detection* is step 3.
- 2026-11-14: First version (#69): STL-27L reader, empty-room background, people tracking in 8 slots, surface and sensor mode, guided calibration with countdown and tones, mouse-driven window with tooltips and fullscreen, simulated people, OSC target check, quality log.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
