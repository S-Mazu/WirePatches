# Motion Bridge

Motion Bridge is a tool: it reads a 2D lidar and sends patches over OSC, about 10 times per second, where in a defined area something is and where something moves.

Last updated: 2026-11-16

## Purpose

Makes projections react to motion in the room. The bridge learns the empty room and treats everything closer than the room as foreground. It maps the foreground onto the **area**, a free polygon in the room, seen from a **front** line where the projection is: along the front (`x`) and away from it (`depth`), both 0..1. Patches get two kinds of data:

- **Fields**: a grid over the whole field. *Occupancy* says where something is, *motion* says where the occupancy changes, so standing still fades out. Neither knows about people: an arm, a group or a running child all just change cells.
- **Points**: up to 8 tracked objects in fixed slots, for patches that need single positions.

The bridge interprets nothing for a patch: what a value means in the picture is up to the patch.

Everything happens in one room map seen from above, in metres. Several sensor positions (several sensors, or one sensor moved around) are aligned in this map; the area, the front, exclusions and barriers are drawn into it with the mouse. Rooms and areas of any size and shape work: nothing assumes a rectangle. Patches only receive these normalized values, so they work on any surface and with any sensor that delivers them.

Patches using it: [Bulging Lines](../../sources/Bulging%20Lines/) (points). No patch uses the fields yet; see Status.

## How it works and why

1. **Read the lidar** (`stl27l.py`): about 10 turns per second, about 2160 distances per turn, checked by CRC. The reader reconnects by itself after a cable pull, so a show survives a loose plug.
2. **Learn the empty room** (`scene.py`): per position, the median distance and the noise per 0.5° direction while nobody is in the scan plane. Everything clearly closer than this background is foreground. Why: walls, columns and furniture never count as motion, without drawing them by hand.
3. **Put all positions into one room map** (`room.py`): each position has a place and a rotation in a top view in metres; the scans are aligned by eye, by dragging, rotating and an optional reference object. Why: several sensors, or one sensor measured from several places, must describe the same room.
4. **Filter** (`room.py`): per position a fan (only these directions count), barriers (a sensor does not look through them), the area (only inside it counts) and exclusions (never inside them). Why: rooms are irregular; the person setting up decides what is relevant, not a rectangle in the code.
5. **Map onto the front** (`room.py`): the front is a line in the map where the projection is, drawn freely; the area is the polygon that counts. Every foreground point gets `x` (0 at the left end … 1 at the right end of the front) and `depth` (0 at the front … 1 at the point of the area farthest from it). Why: patches work in projection terms, independent of room size, room shape and sensor placement. A composed projection (U-shape, mapped objects) later needs a polyline front; the free line keeps that open.
6. **Fields** (`field.py`): the field covers front width × area depth, divided into cells (default 32 × 16); cells outside the area stay 0. Occupancy = share of a cell covered by foreground surface; motion = how fast the occupancy changes, with a threshold against sensor noise and a fade. Why: projections should react to motion, not to "people"; a field carries any motion (an arm, a group, a run) without interpreting it.
7. **Points** (`engine.py`): neighbouring foreground points form blobs, close blobs merge, blobs are followed in 8 fixed slots with fade in and out. Why: some patches need single positions; kept for them and for the existing patch.
8. **Send** (`engine.py`): fields and points over OSC once per turn, plus a heartbeat (`/lidar/alive`) so a patch can calm down when the bridge stops.

## Status (2026-11-16)

Untested (built in a cloud session on 2026-11-16, checked only by the unit tests in `tests/` with synthetic data; the window was not run):
- Area and front (#111), replacing the screen, the trapezoid with two Depth Ranges and the include areas. Older room files are converted on load.

Tested on the show machine on 2026-11-15, before that change:
- Reading the STL-27L: 9.9–10 turns/s, 0 CRC errors over minutes.
- Room map with one sensor: empty room, screen, fan, barrier, areas (live sensor and simulation); room files and the takeover of the older single-sensor setup.
- Fields with the live sensor (walk test): walking gives motion 0.5 to 1 over 5 to 13 cells, still objects stay below 0.3, so the default *Motion threshold* is 0.3.
- Trapezoid and field display in the window (simulation).
- Getting a field into Wire: one instanced `Read OSC`, `Join` and `Texture from Data` give a correct texture (checked with a test pattern in a separate Wire patch).

Not tested:
- More than one physical sensor at the same time (only one sensor was available; several positions were tested by moving it).
- Window editing by mouse (drawing, dragging handles) was only checked by use, not systematically.

Failed:
- **No patch reacts to the fields yet.** A rebuild of Bulging Lines on the motion field (branch `claude/lidar-room-map`, not merged) rendered correctly in Wire with a synthetic test field, but did not react to real motion in Arena with the live bridge. The cause was not found. Not checked: whether Arena passes the 16 rows `/lidar/motion/<r>` to an instanced `Read OSC` inside a clip, whether the values reaching the patch are above 0 (small cells, threshold 0.3), and the patch's own mapping. Start there.
- **Wire crashed or hung three times** during that rebuild. Cause: the `.wire` keeps a copy of an ISF shader's input list; swapping a `.fs` with different inputs under an existing node makes the two disagree. Safe way: give the new shader a fresh ISF node, connect its image inputs first, save and reload the patch as a test. Wire also compiles an ISF shader only if images are read directly in `main()`, and reads shader pass targets flipped vertically.
- **Working method** (Claude, who built this): changes were made before they were thought through and checked. The bridge was first built around "people" instead of motion, and a patch-specific format (and Spout) was proposed before the interface was defined for any patch. A shader was changed while the patch was loaded. The fields and this README are the result of correcting that.

## Hardware

- LDROBOT **STL-27L** 2D lidar (also sold as WayPonDEV FHL-LD19 Plus), 360°, 10 Hz, about 2160 points per turn, 0.03–25 m (black targets up to 10 m).
- CP2102 USB adapter from the kit. Wiring of the kit cable (the colours do **not** follow the usual scheme):

  | Sensor pin | Wire | Signal | Adapter |
  |---|---|---|---|
  | 1 | black | Tx (data, 3.3 V) | RXD |
  | 2 | white | PWM | not connected (internal speed control) |
  | 3 | green | GND | GND |
  | 4 | red | +5 V | +5V |

- Mount the sensor at the height that should be detected, typically 1.2 to 1.5 m on a stand, with a free view, out of reach of people.

## Requirements

- OS: Windows 11 (signal tones use Windows; everything else is portable)
- Runtime: Python 3.13
- Packages: see `requirements.txt` (pyserial, numpy, python-osc, dearpygui)
- Resolume Wire 7.29 / Arena 7.29 with OSC input enabled

## Files

| File | Purpose |
|---|---|
| `bridge.py` | Start here: window (or `--headless`), command-line options |
| `engine.py` | Processing thread: foreground per position, fields, points (tracking), calibration, OSC, quality log |
| `room.py` | Room map: sensor positions, fans, area, front, field frame, exclusions, barriers, reference snap, saving, conversion of older room files |
| `field.py` | Occupancy and motion field over the area |
| `scene.py` | Empty-room background, foreground points, blobs |
| `stl27l.py` | Serial reader for the STL-27L (packets, CRC, revolutions, reconnect) |
| `requirements.txt` | Python packages with versions |
| `tests/` | Unit tests with synthetic data (room map, field, one engine step) |

Created at runtime and not committed: `.venv/`, `settings.json` (detection and output settings), `rooms/<name>/` (one folder per room: `room.json` and a scan per position, `scan_<id>.npy`), `logs/` (quality logs). On the first start an older `background.npy` and surface ends are taken over into the room `Default`. Room files written before 2026-11-16 (screen, Depth Ranges, include areas) are converted when the room is loaded: the screen becomes the front, the first include area (or else the screen trapezoid) becomes the area.

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
3. **Area and front**:
   - *Front*: where the projection is. *Draw front* (click the left end, then the right end, as seen from the area), or stand at each end with *Stand at left end* / *Stand at right end*. The arrow points into the area; *Swap ends* turns it. The ends can be dragged with *Edit points* on.
   - *Area*: what counts. *Draw area*: click the corners, then *Finish* or click the first corner. *Area from front* gives a 3 m deep rectangle as a start. Tick *Edit points* to drag the corners. The far edge of the area is where `depth` reaches 1.
   - Without area and front, zeros are sent. The status line says what is missing, and warns when the front faces away from the area.
4. **Exclusions and barriers** (optional):
   - *Draw exclusion*: nothing inside counts.
   - *Draw barrier*: two clicks; sensors do not look through it.
   - Esc cancels drawing. Each shape can be removed in the list.
5. **Advanced detection**: fine-tune detection for the room (see Configuration).
6. **Check**: the status lines should be green (yellow = keep an eye on it, red = act); hover over a line to see what it means and which values are good. Tracked objects appear as coloured rings with their slot, x and depth.

**Simulation**: *Add object* creates a yellow point in the view that you drag with the mouse. It counts like real foreground, also without a sensor, so patches can be developed and checked anywhere.

For shows without a window: `.venv\Scripts\python.exe bridge.py --headless` uses the saved settings.

Options (this run only, not saved): `--port COM19` (port of the position with the sensor), `--osc 127.0.0.1:7000` (repeatable), `--sim 2` (start with simulated objects), `--snapshot file.png` (save a picture of the window after 4 s and quit).

**Tests** (no sensor, no window; need only numpy, pyserial and python-osc):
```
.venv\Scripts\python.exe -m unittest discover -s tests
```

## Output

- Transport: OSC over UDP, sent to every target in *Output (OSC)*; default Arena `127.0.0.1:7000` and Wire `127.0.0.1:8000`.
- Data, once per revolution (about 10 Hz), 8 slots:

  | Address | Arguments | Meaning |
  |---|---|---|
  | `/lidar/x` | 8 floats | position along the front, 0 = left end, 1 = right end (outside 0..1 beside the front, when the area reaches further) |
  | `/lidar/depth` | 8 floats | distance from the front, 0 = at the front, 1 = at the far edge of the area or further |
  | `/lidar/active` | 8 floats | 0..1, fades in and out; 0 = slot empty |
  | `/lidar/count` | 1 float | number of active slots |
  | `/lidar/alive` | 1 float | counter 0..999, changes with every message |

- Fields, `C` columns × `R` rows (window: *Fields*, default 32 × 16):

  | Address | Arguments | Meaning |
  |---|---|---|
  | `/lidar/field/size` | 2 floats | `C`, `R` |
  | `/lidar/occupancy/<r>` | `C` floats | occupancy of row `r`, 0..1 |
  | `/lidar/motion/<r>` | `C` floats | motion of row `r`, 0..1 |

  - The grid covers front width × area depth. Row `r` = 0 is at the front, `R` − 1 at the far edge of the area. Column 0 is at the left end of the front, `C` − 1 at the right end. Cells whose centre lies outside the area are always 0.
  - Occupancy: share of the cell size covered by a foreground surface. The lidar sees the side of a body facing the sensor, so a body crossing a cell gives about 1; cells hidden behind a body stay 0 (shadow).
  - Motion: how fast the occupancy changes. A cell taken or freed within 0.25 s gives 1. It fades out over *Motion fade* (0.5 s); changes below *Motion threshold* are ignored (default and recommendation 0.3: sensor noise at still objects measured up to 0.28, walking 0.5 to 1).
  - Simulated objects count as discs of 0.44 m.
- Without area and front the bridge sends zeros for everything, and `/lidar/alive` keeps counting.
- Patches read the points with `OSC In` → `Read OSC` (address as above, 8 parameters).
- Patches read a field with one `Read OSC` whose `instances` is `R`, addresses `/lidar/motion/0` … `/lidar/motion/<R-1>`, `C` parameters; `Join` (`C` channels) gives one list of `C` × `R` values, column by column; `Texture from Data` (2D, stride `R`, 16 or 32 bit float) turns it into a texture of `R` × `C` pixels for a shader.
- A patch should fade out when `/lidar/alive` stops changing, so it stays calm without the bridge (docs/PATCH_RULES.md, "Patches that use a tool").

## Configuration

All settings are made in the window. Detection and output are saved in `settings.json`, the room in `rooms/<name>/`:

- Countdown (default 10 s): time between a click and the measurement.
- Room: positions, rotation, fan, area, front, exclusions, barriers, reference: see Usage.
- *Fields*: Columns (32, at most 32), Rows (16), Motion fade (0.5 s), Motion threshold (0.3, recommended). *Reset to recommended* restores them.
- Advanced detection, with tooltips: Tolerance (150 mm), Blob gap (250 mm), Min points (6), Merge distance (450 mm), Track distance (700 mm), Smoothing (0.5), Hold (0.4 s), Fade (0.3 s). *Reset to defaults* restores them.
- *Sensor*: the port of the position with the sensor (COM19); OSC targets, quality log (off), UI scale (1.0).

## Troubleshooting

- Sensor not connected, no COM port: CP210x driver missing or USB cable. With a port but no data: check the wiring above; the motor must spin audibly.
- Walls or furniture show red: learn the empty room again, or raise *Tolerance*.
- One person gives two rings: raise *Merge distance*. With two positions: align them better.
- Objects appear in the wrong place or mirrored: check the front ends (*Swap ends*; the status says when the front faces away from the area) and *Mirrored* of the position.
- A target has a red dot: nothing listens on that port; switch OSC input on in Arena or Wire.
- The patch in Wire reacts but not the one in Arena: restart Arena after switching OSC input on.

## Known issues

See [open issues for this tool](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Atool%3Amotion_bridge).

## Changelog

- 2026-11-16: Area and front replace the screen, the trapezoid with two Depth Ranges and the include areas (#111). The area is a free polygon (what counts), the front a free line (where the projection is); the field covers front width × area depth, cells outside the area stay 0. Points and OSC addresses unchanged; `depth` reaches 1 at the far edge of the area. Older room files are converted on load. Unit tests under `tests/`. Untested in the window and with a sensor.
- 2026-11-15: README: how the bridge works and why, status with what is tested, untested and failed.
- 2026-11-15: Occupancy and motion fields over the screen field, sent per row over OSC and shown live in the window (#69). The field in front of the screen can be a trapezoid: each end has its own Depth Range. Points (slots) unchanged. Tested live: walking gives motion 0.5 to 1, still objects stay below 0.3 (the default *Motion threshold*).
- 2026-11-15: Room map (#69): rooms per venue, several sensor positions with their own scans, alignment by dragging, rotation and a reference object, fan per position, screen drawn in the view or set by standing at its ends, areas, exclusions and barriers. Sensor mode removed; without a screen nothing is sent. Untested with more than one physical sensor.
- 2026-11-15: Status explains itself on hover, with traffic lights; scan quality (revolutions per second, valid share, points per revolution) moved into the status as *Signal*; the *Sensor* section only holds port, connect and log; *Advanced detection* is step 3.
- 2026-11-14: First version (#69): STL-27L reader, empty-room background, people tracking in 8 slots, surface and sensor mode, guided calibration with countdown and tones, mouse-driven window with tooltips and fullscreen, simulated people, OSC target check, quality log.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
