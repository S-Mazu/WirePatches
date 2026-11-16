# Motion Bridge

Motion Bridge is a tool: it reads a 2D lidar and sends patches a Spout texture, 30 times per second, that says where in a defined area something is and where something moves.

Last updated: 2026-11-16

## Purpose

Makes projections react to motion in the room. The bridge learns the empty room and treats everything closer than the room as foreground. It maps the foreground onto the **area**, a free polygon in the room, seen from a **front** line where the projection is: along the front (`x`) and away from it (`depth`). Patches get one thing: the **field**, a grid over the area, as a texture. *Occupancy* says where something is, *motion* says where the occupancy changes, so standing still fades out, *visibility* will say where a sensor can see at all (#113). The field knows nothing about people: an arm, a group or a running child all just change cells.

The bridge interprets nothing for a patch: what a value means in the picture is up to the patch.

Everything happens in one room map seen from above, in metres. Several sensor positions (several sensors, or one sensor moved around) are aligned in this map; the area, the front, exclusions and barriers are drawn into it with the mouse. Rooms and areas of any size and shape work: nothing assumes a rectangle. Patches only receive the normalized texture, so they work on any surface and with any sensor that delivers it.

Patches using it: none yet. [Bulging Lines](../../sources/Bulging%20Lines/) used the earlier OSC output and is being rebuilt on the field (#115).

## How it works and why

1. **Read the lidar** (`stl27l.py`): about 10 turns per second, about 2160 distances per turn, checked by CRC. The reader reconnects by itself after a cable pull, so a show survives a loose plug.
2. **Learn the empty room** (`scene.py`): per position, the median distance and the noise per 0.5° direction while nobody is in the scan plane. Everything clearly closer than this background is foreground. Why: walls, columns and furniture never count as motion, without drawing them by hand.
3. **Put all positions into one room map** (`room.py`): each position has a place and a rotation in a top view in metres; the scans are aligned by eye, by dragging, rotating and an optional reference object. Why: several sensors, or one sensor measured from several places, must describe the same room.
4. **Filter** (`room.py`): per position a fan (only these directions count), barriers (a sensor does not look through them), the area (only inside it counts) and exclusions (never inside them). Why: rooms are irregular; the person setting up decides what is relevant, not a rectangle in the code.
5. **Map onto the front** (`room.py`): the front is a line in the map where the projection is, drawn freely; the area is the polygon that counts. Every foreground point gets `x` (0 at the left end … 1 at the right end of the front) and `depth` (0 at the front … 1 at the point of the area farthest from it). Why: patches work in projection terms, independent of room size, room shape and sensor placement. A composed projection (U-shape, mapped objects) later needs a polyline front; the free line keeps that open.
6. **Field** (`field.py`): the field covers front width × area depth in square cells of *Cell size* (0.1 m); cells outside the area stay 0. Occupancy = share of a cell covered by foreground surface; motion = how fast the occupancy changes, with a threshold against sensor noise and a fade. Why: projections should react to motion, not to "people"; a field carries any motion (an arm, a group, a run) without interpreting it.
7. **Send** (`field.py`, `spout_out.py`): the field is rendered as an RGBA image and shared as a Spout texture 30 times per second; motion fades between measurements, so the frames are smooth. A counter pixel changes every frame, so a patch can tell a live field from a frozen one. Why a texture: a field is an image, and a texture is what Wire and Arena read natively. Any resolution, one frame per update, nothing to reassemble in the patch. Why not OSC: a list of numbers forces every patch to hard-wire the grid size and to rebuild the image.

Foreground blobs are also merged into *objects* (`scene.py`) for the window and for setting the ends of the front by standing there. Patches do not get them; a tracker in fixed slots was tried and dropped, it is the wrong model for dancing.

## Status (2026-11-16)

Untested (built in a cloud session on 2026-11-16, checked only by the unit tests in `tests/` with synthetic data and by starting the window under a virtual display; no sensor, no Spout):
- Field as a Spout texture, the counter pixel, 30 frames per second, cell size instead of a fixed grid (#115). OSC and the points are gone.
- Area and front, replacing the screen, the trapezoid with two Depth Ranges and the include areas (#111). Older room files are converted on load.

To test on the show machine (#115):
- A Wire patch running as a clip in Arena receives the texture through `Texture In` (Spout). The archived Kinect tool had this working; it also once delivered 0 × 0 pixels during a performance, cause unknown. The counter pixel exposes a frozen texture.
- Orientation: whether row 0 (the front) arrives at the bottom or the top in Wire. Then `FLIP` in `spout_out.py` and this README are fixed.
- Reading the counter pixel in a patch: `texelFetch` in an ISF shader; which node does it in node flow.
- What `Texture In` shows when the sender stops: the last frame or black. Both are safe for a patch.
- A laptop with two graphics adapters: the bridge and Arena must use the same one.

Tested on the show machine on 2026-11-15, before these changes:
- Reading the STL-27L: 9.9–10 turns/s, 0 CRC errors over minutes.
- Room map with one sensor: empty room, fan, barrier, areas (live sensor and simulation); room files and the takeover of the older single-sensor setup.
- Fields with the live sensor (walk test): walking gives motion 0.5 to 1 over 5 to 13 cells, still objects stay below 0.3, so the default *Motion threshold* is 0.3.

Not tested:
- More than one physical sensor at the same time (only one sensor was available; several positions were tested by moving it).
- Window editing by mouse (drawing, dragging handles) was only checked by use, not systematically.

Learned the hard way (2026-11-15):
- A field sent as OSC rows (one `Read OSC` with 16 instances, `Join`, `Texture from Data`) rendered correctly in Wire with a test pattern but did not react to real motion in Arena; the cause was not found. That path is replaced by the texture.
- Wire crashed or hung three times while a `.fs` with different inputs was swapped under an existing ISF node: the `.wire` keeps a copy of the shader's input list. Safe way: a fresh ISF node for a new shader, connect its image inputs first, save and reload the patch as a test. Wire also compiles an ISF shader only if images are read directly in `main()`, and reads shader pass targets flipped vertically.
- Working method: changes were made before they were thought through. The bridge was first built around "people" instead of motion, and a patch-specific format was proposed before the interface was defined. The field, the contract below and this README are the result of correcting that.

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

- OS: Windows 11 (Spout and the signal tones need Windows; everything else is portable, then without output)
- Runtime: Python 3.13
- Packages: see `requirements.txt` (pyserial, numpy, dearpygui, SpoutGL)
- Resolume Wire 7.29 / Arena 7.29; the bridge and Arena on the same computer and the same graphics adapter (Spout)

## Files

| File | Purpose |
|---|---|
| `bridge.py` | Start here: command-line options, logging, window or `--headless` |
| `ui/` | The window: `window.py` (layout, frame loop), `view.py` (room map, handles, live points), `panel.py` (controls), `status.py` (status lines), `drafting.py` (drawing shapes with clicks), `help.py` (all help texts), `theme.py` |
| `engine.py` | Processing thread: readers per position, foreground, objects, field, texture frames, room and port management |
| `settings.py` | Detection and field settings: defaults, limits, `settings.json`, command-line overrides |
| `field.py` | Occupancy and motion over the area, and the texture with the counter pixel |
| `spout_out.py` | Spout sender around SpoutGL; without SpoutGL the bridge runs and says so |
| `calibration.py` | Guided measurements with countdown and tones: learn the empty room, set an end of the front |
| `quality.py` | Scan quality statistics and the optional CSV log |
| `room.py` | Room map: sensor positions, fans, area, front, field frame, exclusions, barriers, reference snap, saving, conversion of older room files |
| `scene.py` | Empty-room background, foreground points, blobs, objects |
| `stl27l.py` | Serial reader for the STL-27L (packets, CRC, revolutions, reconnect) |
| `requirements.txt` | Python packages with versions |
| `tests/` | Unit tests with synthetic data (room map, field and texture, settings, measurements, drafting, one engine step) |

Created at runtime and not committed: `.venv/`, `settings.json` (detection and field settings), `rooms/<name>/` (one folder per room: `room.json` and a scan per position, `scan_<id>.npy`), `logs/` (`bridge.log` with what the bridge did and what failed, and the quality logs). On the first start an older `background.npy` and surface ends are taken over into the room `Default`. Room files written before 2026-11-16 (screen, Depth Ranges, include areas) are converted when the room is loaded: the screen becomes the front, the first include area (or else the screen trapezoid) becomes the area.

## Installation

1. Copy this whole folder to any location.
2. Install the CP210x driver from Silicon Labs; the adapter then appears as `Silicon Labs CP210x USB to UART Bridge (COMx)` in the Device Manager.
3. In this folder:
   ```
   py -3.13 -m venv .venv
   .venv\Scripts\python.exe -m pip install -r requirements.txt
   ```
4. Nothing to switch on in Arena or Wire: Spout sources are always listed. On a laptop with two graphics adapters, run the bridge and Arena on the same one (Windows graphics settings, "high performance").

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
   - Without area and front, a black field is sent. The status line says what is missing, and warns when the front faces away from the area.
4. **Exclusions and barriers** (optional):
   - *Draw exclusion*: nothing inside counts.
   - *Draw barrier*: two clicks; sensors do not look through it.
   - Esc cancels drawing. Each shape can be removed in the list.
5. **Advanced detection**: fine-tune detection for the room (see Configuration).
6. **Check**: the status lines should be green (yellow = keep an eye on it, red = act); hover over a line to see what it means and which values are good. Foreground objects appear as white rings. *Output (Spout)* shows the sender name, the texture size and the frame rate.

**Simulation**: *Add object* creates a yellow point in the view that you drag with the mouse. It counts like real foreground and appears in the field, also without a sensor, so patches can be developed and checked anywhere.

For shows without a window: `.venv\Scripts\python.exe bridge.py --headless` uses the saved settings.

Options (this run only, not saved): `--port COM19` (port of the position with the sensor), `--sim 2` (start with simulated objects), `--snapshot file.png` (save a picture of the window after 4 s and quit).

**Tests** (no sensor, no window, no Spout; need only numpy and pyserial):
```
.venv\Scripts\python.exe -m unittest discover -s tests
```

## Output

The interface to the patches is one Spout texture. This is the contract; patches rely on it and nothing else.

- **Sender**: Spout, name `Motion Bridge`, on the same computer as Arena and Wire. In Arena it appears under Sources > Spout; in Wire a `Texture In` node set to Spout receives it, also when the patch runs as a clip in Arena (to be confirmed, see Status).
- **Format**: RGBA, 8 bits per channel. 30 frames per second; the measurement behind it is 10 per second, motion fades between measurements.
- **Size**: width = front width / cell size, height = area depth / cell size + 1. The cell size is a bridge setting (default 0.1 m, so a 4 × 3 m area gives 40 × 31 pixels). A patch must not depend on the size: it samples the texture with normalized coordinates, 0..1 along the front and 0..1 into the area.
- **Channels**, each 0..255 for 0..1:
  - R: occupancy, how much of the cell is covered by a foreground surface. The lidar sees the side of a body facing the sensor, so a body crossing a cell gives about 0.8 to 1; cells hidden behind a body stay 0 (shadow).
  - G: motion, how fast the occupancy changes. A cell taken or freed within 0.25 s gives 1; it fades out over *Motion fade* (0.5 s); changes below *Motion threshold* are ignored (0.3: sensor noise at still objects measured up to 0.28, walking 0.5 to 1).
  - B: visibility, 1 where a sensor sees the cell (#113, not implemented yet: always 0).
  - A: always 255.
- **Orientation**: column 0 is at the left end of the front, column width − 1 at the right end. Row 0 is at the front, the last row but one at the far edge of the area. Whether row 0 arrives at the bottom or the top of the texture in Wire is confirmed on the show machine and then fixed here.
- **Reserved row**: the last row carries no field data. Its first pixel holds a counter in R, G and B that changes every frame (0..255, wraps). A patch reads it with `texelFetch` and fades out when it stops changing, so the picture stays calm without the bridge (docs/PATCH_RULES.md, "Patches that use a tool"). The rest of the row is black.
- **Without area and front**: an 8 × 9 black texture; the counter keeps running.
- **Simulated objects** count as discs of 0.44 m.

## Configuration

All settings are made in the window. Detection and field settings are saved in `settings.json`, the room in `rooms/<name>/`:

- Countdown (default 10 s): time between a click and the measurement.
- Room: positions, rotation, fan, area, front, exclusions, barriers, reference: see Usage.
- *Fields*: Cell size (100 mm), Motion fade (0.5 s), Motion threshold (0.3, recommended). *Reset to recommended* restores them.
- Advanced detection, with tooltips: Tolerance (150 mm), Blob gap (250 mm), Min points (6), Merge distance (450 mm). *Reset to defaults* restores them.
- *Sensor*: the port of the position with the sensor (COM19); quality log (off), UI scale (1.0).

## Troubleshooting

- Anything odd: read `logs/bridge.log`; it says which room and readers were started and what failed.
- Sensor not connected, no COM port: CP210x driver missing or USB cable. With a port but no data: check the wiring above; the motor must spin audibly.
- Walls or furniture show red: learn the empty room again, or raise *Tolerance*.
- One body gives two rings: raise *Merge distance*. With two positions: align them better.
- Objects appear in the wrong place or mirrored: check the front ends (*Swap ends*; the status says when the front faces away from the area) and *Mirrored* of the position.
- *Output* is red: SpoutGL is not installed (`pip install SpoutGL` in the venv) or the sender failed; the log says which.
- The texture does not appear in Arena or Wire, or is 0 × 0: the bridge and Arena may run on different graphics adapters; set both to the same one in the Windows graphics settings.
- The picture in the patch stands still although the status shows frames: the patch reads a frozen texture; check the counter pixel in the patch.

## Known issues

See [open issues for this tool](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Atool%3Amotion_bridge).

## Changelog

- 2026-11-16: The field leaves the bridge as a Spout texture (#115): R occupancy, G motion, B visibility (reserved), A 255, a counter pixel in a reserved last row, 30 frames per second with motion fading between measurements. Cell size (0.1 m) replaces the fixed grid; the texture size follows the area. OSC output, the 8-slot points and their settings are removed. Output section is the contract. Untested with Spout and with a sensor.
- 2026-11-16: Window split into the package `ui/` (view, panel, status, drafting, help, theme); the drawing of shapes is a class without dearpygui and has tests. Scan points are converted for the plot only when a position or its scan changes. Checked by starting the window under a virtual display with a demo room (snapshot); untested by hand.
- 2026-11-16: Engine split into settings, OSC output, calibration, quality and tracking modules; the window changes the room only through the engine. Log file `logs/bridge.log`. Tests for settings, the OSC messages of one update (received by a local socket) and the guided measurements. Window started under a virtual display only; untested with a sensor.
- 2026-11-16: Area and front replace the screen, the trapezoid with two Depth Ranges and the include areas (#111). The area is a free polygon (what counts), the front a free line (where the projection is); the field covers front width × area depth, cells outside the area stay 0. Points and OSC addresses unchanged; `depth` reaches 1 at the far edge of the area. Older room files are converted on load. Unit tests under `tests/`. Untested in the window and with a sensor.
- 2026-11-15: README: how the bridge works and why, status with what is tested, untested and failed.
- 2026-11-15: Occupancy and motion fields over the screen field, sent per row over OSC and shown live in the window (#69). The field in front of the screen can be a trapezoid: each end has its own Depth Range. Points (slots) unchanged. Tested live: walking gives motion 0.5 to 1, still objects stay below 0.3 (the default *Motion threshold*).
- 2026-11-15: Room map (#69): rooms per venue, several sensor positions with their own scans, alignment by dragging, rotation and a reference object, fan per position, screen drawn in the view or set by standing at its ends, areas, exclusions and barriers. Sensor mode removed; without a screen nothing is sent. Untested with more than one physical sensor.
- 2026-11-15: Status explains itself on hover, with traffic lights; scan quality (revolutions per second, valid share, points per revolution) moved into the status as *Signal*; the *Sensor* section only holds port, connect and log; *Advanced detection* is step 3.
- 2026-11-14: First version (#69): STL-27L reader, empty-room background, people tracking in 8 slots, surface and sensor mode, guided calibration with countdown and tones, mouse-driven window with tooltips and fullscreen, simulated people, OSC target check, quality log.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
