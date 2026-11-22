# Motion Bridge

Motion Bridge is a tool: it reads a 2D lidar and sends patches a Spout texture, 30 times per second, that says where in a defined area something is and where something moves. In Arena the texture is a clip, and a patch sits on that clip as a clip effect.

Last updated: 2026-11-22

## Purpose

Makes projections react to motion in the room. The bridge learns the empty room and treats everything closer than the room as foreground. It maps the foreground onto the **area**, a free polygon in the room; one edge of the area is the **base line**, row 0 of the texture: along it (`x`) and away from it into the area (`depth`). Patches get one thing: the **field**, a grid over the area, as a texture. *Occupancy* says where something is, *motion* says where the occupancy changes, so standing still fades out, *visibility* says where a sensor sees at all: behind a body, behind a barrier and outside every fan lies shadow, where occupancy 0 means "unknown", not "empty". The field knows nothing about people: an arm, a group or a running child all just change cells.

The bridge interprets nothing for a patch: what a value means in the picture is up to the patch.

Everything happens in one room map seen from above, in metres. Several sensor positions (several sensors, or one sensor moved around) are aligned in this map; the area with its base line, exclusions and barriers are drawn into it with the mouse and can be edited afterwards. Rooms and areas of any size and shape work: nothing assumes a rectangle. Patches only receive the normalized texture, so they work on any surface and with any sensor that delivers it.

Patches using it: [Motion Bridge - Autumn Wind](../../effects/Motion%20Bridge%20-%20Autumn%20Wind/), [Motion Bridge - Fabric Wind](../../effects/Motion%20Bridge%20-%20Fabric%20Wind/), [Motion Bridge - Fireflies](../../effects/Motion%20Bridge%20-%20Fireflies/). The example [Field Viewer](../../examples/Field%20Viewer/) shows the field as it arrives.

## How it works and why

1. **Read the lidar** (`stl27l.py`): about 10 turns per second, about 2160 distances per turn, checked by CRC. The reader reconnects by itself after a cable pull, so a show survives a loose plug.
2. **Learn the empty room** (`scene.py`): per position, the median distance and the noise per 0.5° direction while nobody is in the scan plane. Everything clearly closer than this background is foreground; a point is compared with the nearest empty-room distance within ±1° around it. Why: walls, columns and furniture never count as motion, without drawing them by hand; and at the edge of a fixed object the beam hits the object in one turn and the wall behind it in the next, so measured against its own direction only, such a point flickered as foreground (4 flickering directions in 20 s in the test room before, 0 after, 2026-11-22).
3. **Put all positions into one room map** (`room.py`): each position has a place and a rotation in a top view in metres; the scans are aligned by eye, by dragging, rotating and an optional reference object. Why: several sensors, or one sensor measured from several places, must describe the same room.
4. **Filter** (`room.py`): per position a fan (only these directions count), barriers (a sensor does not look through them), the area (only inside it counts) and exclusions (never inside them). Why: rooms are irregular; the person setting up decides what is relevant, not a rectangle in the code.
5. **Map onto the base line** (`room.py`, `field.py`): the area is the polygon that counts, one of its edges the base line. The field is the smallest rectangle aligned with the base line that holds the whole area: `x` runs along the base line from the left end of the rectangle to its right end, as seen from the area looking at the base line; `depth` from the base line to the point of the area farthest from it. Nothing of the area is cut off; parts of the rectangle outside the area stay 0. Why: the bridge does not know where and how the projection lies (a curtain, a floor); it hands over the area as it is, without distortion, and a patch turns and places it. The bridge never adapts to a patch.
6. **Field** (`field.py`): the field covers the rectangle in square cells of about *Cell size* (0.1 m): one metre is the same number of cells in both directions. The cells shrink by up to a fifth so that the rows end exactly at the far side (the row count is a multiple of 15 for the status band). Occupancy = share of a cell covered by foreground surface; motion = how fast the occupancy changes, with a threshold against sensor noise and a fade. Why: projections should react to motion, not to "people"; a field carries any motion (an arm, a group, a run) without interpreting it.
7. **Visibility** (`visibility.py`): per position and revolution, the seen region is, for each 0.5° direction, the distance up to the nearest point hit (without a return: the empty room's distance there, or 8 m), at most up to the first barrier, nothing outside the fan. Behind lies shadow. Several positions add up. A field cell counts as seen when its centre lies in a seen region. Why: a lidar sees only the first surface of each ray; the person setting up must see where the shadows are (a further sensor belongs there), and a patch must be able to tell "empty" from "not seen".
8. **Send** (`field.py`, `spout_out.py`): the field is rendered as an RGBA image and shared as a Spout texture 30 times per second; motion fades between measurements, so the frames are smooth. A status band at the top counts up every frame, so a patch can tell a live field from a frozen one. Why a texture: a field is an image, and a texture is what Wire and Arena read natively. Any resolution, one frame per update, nothing to reassemble in the patch. Why not OSC: a list of numbers forces every patch to hard-wire the grid size and to rebuild the image. Why a band and not a pixel: everything that passes through Arena is resampled (clip resize, interpolation); a band of one sixteenth survives that, a single pixel does not.

Foreground blobs are also merged into *objects* (`scene.py`) for the window (*Foreground points*). Patches do not get them; a tracker in fixed slots was tried and dropped, it is the wrong model for dancing.

## Status (2026-11-22)

Tested on the show machine on 2026-11-22 with the live sensor and a moving simulated object (base line and editing, #135):
- The room `Default` with the old front was converted on load: the area edge along the front became the base line; field 3.15 × 4.85 m, cells 0.08 m, texture 39 × 64.
- In the window: *Choose base line* on another edge (field 4.90 × 4.50 m, texture 49 × 48, the 0 at the correct end, the arrow into the area), Ctrl+Z back; a corner added and deleted by double click (10 → 11 → 10 corners) and both undone, a corner dragged and the drag undone; the settings column closed and opened (strip, F2); *Foreground points* on and off; the field (red = occupied, green = moving) over the map follows the object; the texture preview shows the field with the band on top and the base line at the bottom.
- Arena 7.29 received the new texture as a Spout clip (39 × 64, band at the top, the area in blue, the object in it), like the preview.
- Edge flicker against the empty room, 20 s live, the same revolutions with both tests: directions that flicker (foreground in 2 to 60 % of the turns) 4 before, 0 after.

Tested in Arena 7.29.0 on 2026-11-17 with the live sensor, two simulated objects, the example [Field Viewer](../../examples/Field%20Viewer/) and Bulging Lines (now [Motion Bridge - Fabric Wind](../../effects/Motion%20Bridge%20-%20Fabric%20Wind/)) as clip effects on the Spout clip (composition 1920 × 1080, texture 19 × 48; setup as under "In Arena"):
- The field arrives in the patch stretched over the frame (input = render size, 1920 × 1080), pixel for pixel the same picture as the Spout clip without the patch.
- With `FLIP = True` the band arrives at the top: row 0 (the front) at the bottom, as the contract says.
- The band's counter reaches the patch at 29 steps per second.
- Bridge stopped: the patch gets a black frame of the composition size, no NaN and no frozen picture. Arena takes the sender up again by itself when the bridge restarts.
- The clip must have the texture's own size and the layer *Auto Size* must be *Stretch*; then the clip's Transform scales the field to the composition and a clip effect after the Transform gets it at full size. With the clip at composition size (1920 × 1080) the field stays small and unstretched in the middle (2026-11-16). Clip *Resize* is a one-time button, not a setting (Resolume manual, Clips). A freshly loaded Spout clip takes the texture's size by itself.
- Several clips of the same Spout source, each with its own effect settings, in different columns: each gets the field (Field Viewer and Bulging Lines measured one after the other in the same layer).
- A Wire patch loaded as a source does not receive Spout inside Arena: `Texture In` gives a black 1 × 1 image (2026-11-16).
- python.exe of the venv and Arena.exe must run on the same graphics adapter (Windows graphics settings, "high performance"); before, Arena knew the sender's size but showed no image (2026-11-16).

Tested on the show machine on 2026-11-20: visibility with the live sensor. The room map tints what the sensor sees; an object next to the sensor shaded most of the area, further objects in the room cast narrow shadow wedges, and the field image *Seen* showed the same. Unit tests cover shadows behind objects and barriers, the fan and the B channel. Not checked yet: barriers with the live sensor, several physical sensors.

Tested in Arena on 2026-11-20: the B channel in a patch. [Motion Bridge - Fabric Wind](../../effects/Motion%20Bridge%20-%20Fabric%20Wind/) holds the state of cells the sensor does not see (`Shadow Hold`); its `Show Field` showed the held shadows where the room map showed them, with the live sensor at 1920 × 1080.

Tested on the show machine on 2026-11-20: *Find sensor* (Sensor section) with nine serial ports. It found the STL-27L on COM19 (958 valid packets in 0.5 s, 10 turns/s), listed only that port, and was done in 2.5 s although four Bluetooth ports (COM11 to COM14) did not answer. With the port in use by the bridge it shows "in use" without opening it.

Tested on the show machine on 2026-11-15, before these changes:
- Reading the STL-27L: 9.9–10 turns/s, 0 CRC errors over minutes.
- Room map with one sensor: empty room, fan, barrier, areas (live sensor and simulation); room files and the takeover of the older single-sensor setup.
- Fields with the live sensor (walk test): walking gives motion 0.5 to 1 over 5 to 13 cells, still objects stay below 0.3, so the default *Motion threshold* is 0.3.

Not tested:
- Whether the clip follows when the texture size changes (another area or cell size); the clip keeps its *Width* and *Height*.
- Whether column 0 arrives at the left (needs an object at a known place).
- Other composition sizes.
- The archived Kinect tool once delivered 0 × 0 pixels during a performance, cause unknown; the status band would expose a frozen texture.
- More than one physical sensor at the same time (only one sensor was available; several positions were tested by moving it).
- Exclusions and barriers: corners and ends were not edited in the test of 2026-11-22 (the code is shared with the area).

Learned the hard way (2026-11-15):
- A field sent as OSC rows (one `Read OSC` with 16 instances, `Join`, `Texture from Data`) rendered correctly in Wire with a test pattern but did not react to real motion in Arena; the cause was not found. That path is replaced by the texture.
- Wire crashed or hung three times while a `.fs` with different inputs was swapped under an existing ISF node: the `.wire` keeps a copy of the shader's input list. Safe way: a fresh ISF node for a new shader, connect its image inputs first, save and reload the patch as a test. Wire also compiles an ISF shader only if images are read directly in `main()`, and reads shader pass targets flipped vertically.
- 2026-11-17: I first concluded, without measuring, that a clip effect renders only at the clip's 19 × 48 and documented a layer effect, then a Video Router construction, as the recipe. Both were impractical for a performance and the conclusion was wrong: measured, a clip effect on the Spout clip gets the field at full size. Claims about Arena are measured with the Field Viewer before they are written down.
- 2026-11-17: the first Field Viewer was written as JSON by hand and missed `Texture In` in the patch's input list; Arena then gave it a black 1 × 1 texture, which looked like a Spout problem. Patches are built in Wire.
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
| `field.py` | The grid over the field rectangle; occupancy, motion and visibility, and the texture with the status band |
| `visibility.py` | What each position sees (up to the first point of each ray, barriers, fan) and which field cells are seen |
| `spout_out.py` | Spout sender around SpoutGL; without SpoutGL the bridge runs and says so |
| `port_scan.py` | *Find sensor*: lists the serial ports with their description and finds the ones that deliver STL-27L packets |
| `calibration.py` | Guided measurement with countdown and tones: learn the empty room |
| `quality.py` | Scan quality statistics and the optional CSV log |
| `room.py` | Room map: sensor positions, fans, area with base line and corner editing, field rectangle, exclusions, barriers, reference snap, saving, conversion of older room files |
| `scene.py` | Empty-room background (with the edge reference against flicker), foreground points, blobs, objects |
| `stl27l.py` | Serial reader for the STL-27L (packets, CRC, revolutions, reconnect) |
| `requirements.txt` | Python packages with versions |
| `tests/` | Unit tests with synthetic data (room map, field and texture, foreground at edges, settings, measurements, drafting, engine steps and undo, port scan) |

Created at runtime and not committed: `.venv/`, `settings.json` (detection and field settings), `rooms/<name>/` (one folder per room: `room.json` and a scan per position, `scan_<id>.npy`), `logs/` (`bridge.log` with what the bridge did and what failed, and the quality logs). On the first start an older `background.npy` and surface ends are taken over into the room `Default`. Older room files are converted when the room is loaded: before 2026-11-22 a front line, whose nearest area edge becomes the base line; before 2026-11-16 a screen, Depth Ranges and include areas (the first include area, or else the screen trapezoid, becomes the area, the screen its base line).

## Installation

1. Copy this whole folder to any location.
2. Install the CP210x driver from Silicon Labs; the adapter then appears as `Silicon Labs CP210x USB to UART Bridge (COMx)` in the Device Manager.
3. In this folder:
   ```
   py -3.13 -m venv .venv
   .venv\Scripts\python.exe -m pip install -r requirements.txt
   ```
4. Windows Settings > System > Display > Graphics: add `.venv\Scripts\python.exe` of this folder and `Arena.exe`, both set to *High performance*. Spout shares textures only between programs on the same graphics adapter; on a laptop with two adapters the Python process otherwise lands on the integrated one and Arena sees the sender's name and size but no image. Nothing to switch on in Arena or Wire: Spout sources are always listed.
5. Copy the patches that use the bridge (e.g. [Motion Bridge - Fabric Wind](../../effects/Motion%20Bridge%20-%20Fabric%20Wind/)) and, for checking, the example [Field Viewer](../../examples/Field%20Viewer/) into the Wire patches folder (`Documents/Resolume Wire/Patches/`). Set them up in Arena as under Usage, "In Arena".

## Usage

```
.venv\Scripts\python.exe bridge.py
```

The window shows the room map on the left and the settings column on the right. The thin strip between them (or F2) closes the column: during the performance the map fills the window, the bridge keeps sending. The column is grouped in the order of the setup (steps 1 to 6), then *View*, *Simulation* and *Advanced detection*; each section says in one line what it is for, and every control explains itself when you hover over it; `(?) the view` explains the picture. F11 or the button switches fullscreen; the mouse wheel zooms the view, right-drag pans it, left-drag moves handles.

1. **Room**: pick a room or create one per venue. Everything below is saved in it.
2. **Sensor**:
   - Which port is the sensor? Click *Find sensor*. The bridge listens half a second on every serial port and then lists only the ports that deliver lidar data, e.g. `COM19  STL-27L, 10 turns/s`. Select it and click *Sensor is at selected position*. It only reads; nothing is sent to other devices. *Show all ports* lists every port with its Windows description.
3. **Sensor positions**:
   - *Learn empty room here*: set the countdown, click, leave the scan plane before the first tone and stay out until the second tone.
   - Another sensor place: put the sensor there, click *Sensor moved: new position* and learn the empty room again. The new position takes over the sensor; the old one keeps its scan.
   - Align the positions so that their scans overlap: drag the coloured square, set *Rotation* (buttons for fine steps).
   - With a reference object (a free-standing pole or tube, 10 cm or more, standing in the room while each position learns): *Place marker* and click it in an aligned scan, select the other position, *Snap position to marker*, tick *Turn around reference* and turn until the walls match. Afterwards remove the object and learn the empty room of the live position again.
   - *Fan*: how wide this position looks (360 = all around); point it with the round handle in the view. Tick *Mirrored* if the map shows the room mirrored.
4. **Area and base line** (done once per venue, before the show):
   - *Draw area*: click the corners, then *Finish* or click the first corner. The edge from the first to the second corner becomes the base line.
   - *Choose base line*: click another edge of the area. The white line is the base line, `0` marks its left end (seen from the area), the arrow points into the area; the thin grey outline is the texture rectangle.
   - Edit afterwards in the view: drag a corner (*Move corners* on), double-click an edge to add a corner, double-click a corner to delete it. *Undo* or Ctrl+Z takes back each change to the shapes.
   - Without area and base line, a black field is sent. The status line says what is missing, and gives the field size, the cell size and the texture size.
   - After changing the area or the cell size, reload the Spout clip in Arena (Usage, "In Arena", step 2): the texture has a new size.
5. **Exclusions and barriers** (optional):
   - *Draw exclusion*: nothing inside counts.
   - *Draw barrier*: two clicks; sensors do not look through it.
   - Esc cancels drawing. Each shape can be removed in the list. Exclusion corners are added, deleted and dragged like the area's.
6. **Field**: the texture as it is sent, cell size and motion settings (see Configuration). *Advanced detection* fine-tunes detection for the room.
7. **Shadows**: *Show what the sensors see* (View) tints, live, what each position with a sensor sees. Everything that stays dark is shadow: behind people and objects, behind barriers, outside the fans. Walk through the area and watch where your shadow falls; where shadows matter, a further sensor position helps (step 3). The blue channel of the texture preview (*Field*) shows the same per cell.
8. **Check**: the status lines should be green (yellow = keep an eye on it, red = act); hover over a line to see what it means and which values are good. The view shows the measured room as solid lines, the live scan as dots and the field as it is sent: red = occupied, green = moving, yellow = both. *Field* shows the texture as it is sent, enlarged, base line at the bottom. *Foreground points* (View) shows the single foreground points and objects instead, for diagnosis. *Output (Spout)* shows the sender name, the texture size and the frame rate.

**Simulation**: *Add object* creates a yellow point in the view that you drag with the mouse. It counts like real foreground and appears in the field, also without a sensor, so patches can be developed and checked anywhere.

With *Move between A and B* every simulated object walks from its point A to its point B (which appears 2 m to the right) and back, at *Speed* (m/s, default 1, about walking pace), pausing *Pause* seconds at each end (default 5). Standing still (occupancy without motion) and moving (motion) are so tested in one run, without anyone walking in front of the sensor. The walk test with the sensor stays the final check. Switching it on starts every object at A; these settings are for this run only.

For shows without a window: `.venv\Scripts\python.exe bridge.py --headless` uses the saved settings.

Options (this run only, not saved): `--port COM19` (port of the position with the sensor), `--sim 2` (start with simulated objects), `--sim-path 0.1,0.2,0.9,0.7` (start with an object that walks between A and B, given in field shares: x 0..1 along the base line, y 0..1 into the area; repeat for more objects; needs a room with area and base line), `--sim-speed 1` (m/s), `--sim-pause 8` (s), `--snapshot file.png` (save a picture of the window after 4 s and quit).

### From the room to the picture

| Step | Where | What happens |
|---|---|---|
| 1 | Sensor | The lidar measures about 2160 distances per turn, 10 turns per second, in one plane at chest height. |
| 2 | Bridge | Foreground = everything clearly closer than the learned empty room, inside the area and not excluded. |
| 3 | Bridge | The foreground is mapped onto the field rectangle at the base line: `x` 0..1 along the base line, `depth` 0..1 away from it into the area. |
| 4 | Bridge | Per cell of the field: occupancy (how much is covered), motion (how fast that changes, fading out) and visibility (whether a sensor sees the cell or it lies in a shadow). |
| 5 | Bridge | The field becomes an RGBA image with the status band and is sent as the Spout texture `Motion Bridge`, 30 times per second. |
| 6 | Arena | A Spout clip receives the texture; the layer's *Auto Size* *Stretch* scales it to the composition. |
| 7 | Patch | A Wire patch on the clip (clip effect) reads the field with normalized coordinates and draws its own picture; without a live status band it calms down. |

What a patch does with the field is up to the patch; see its README (e.g. Motion Bridge - Fabric Wind, "How it works").

### In Arena

Set up once per composition, tested in Arena 7.29.0:

1. Start the bridge and check that *Output (Spout)* is green and shows the texture size and about 30 frames/s.
2. Drag `Motion Bridge` from Sources > Spout into a clip slot. A fresh Spout clip takes the texture's own size (clip *Width* / *Height*, e.g. 19 × 48); if they show anything else (e.g. after the composition size was applied), click the resize button next to the source name in the clip panel until they match.
3. In the layer panel, set *Auto Size* (Video) to *Stretch*. This scales the small field to the whole composition.
4. Drag the patch (e.g. Motion Bridge - Fabric Wind) onto the clip. It must sit after the clip's *Transform* (the default when it is dropped on the clip).
5. Set the layer's blend mode so the patch lies over the content of the layers below; for a patch with a transparent background (Motion Bridge - Fabric Wind) *Alpha*.
6. For more looks: duplicate the clip into other columns or layers and change the patch's settings there. Every clip is an ordinary clip: triggered, faded and automated like any other. Each one receives the same Spout texture.
7. To check what arrives: a further Spout clip with the example Field Viewer as clip effect (see its README).

Notes:
- Trigger order does not matter: Arena connects to the sender when the clip plays, and takes it up again by itself when the bridge restarts.
- When the area or the cell size changes, the texture size changes. Reload the clip or check its *Width* / *Height* (step 2); the room is set up once before the show.
- The Spout clip itself is never seen; the patch replaces its picture.

**Tests** (no sensor, no window, no Spout; need only numpy and pyserial):
```
.venv\Scripts\python.exe -m unittest discover -s tests
```

## Output

The interface to the patches is one Spout texture. This is the contract; patches rely on it and nothing else.

- **Sender**: Spout, name `Motion Bridge`, on the same computer as Arena and Wire.
- **How a patch gets it** (tested, see Status and Usage, "In Arena"): the patch is an **effect** (category `effect`) on a Spout clip of `Motion Bridge`. The clip has the texture's own size, the layer's *Auto Size* is *Stretch*, the patch sits after the clip's Transform; its `Texture In` is then the field stretched to the composition. A Wire *source* patch cannot pick up Spout inside Arena. In the Wire editor, `Texture In` set to Spout shows the same texture for building the patch.
- **Format**: RGBA, 8 bits per channel. 30 frames per second; the measurement behind it is 10 per second, motion fades between measurements.
- **Layout**: the lower fifteen sixteenths of the rows are the field, the upper sixteenth is the status band (see below). The field covers the smallest rectangle aligned with the base line that holds the whole area, in square cells: one metre is the same number of pixels in both directions. Field rows = rectangle depth / cell, a multiple of 15; band = field rows / 15; width = rectangle width / cell, rounded up. The cell is about the bridge setting *Cell size* (default 0.1 m) and shrinks by up to a fifth so that the rows end at the far side; a 4 × 3 m area gives 40 × 32 pixels: 30 field rows and a band of 2. A patch must not depend on the size: it samples the texture with normalized coordinates, 0..1 along the base line, and 0..15/16 into the area (depth `d` is at 15/16 · `d`). The aspect of the texture is the aspect of the area; a patch that needs equal metres on screen uses it.
- **Channels**, each 0..255 for 0..1:
  - R: occupancy, how much of the cell is covered by a foreground surface. The lidar sees the side of a body facing the sensor, so a body crossing a cell gives about 0.8 to 1; cells hidden behind a body stay 0 (shadow).
  - G: motion, how fast the occupancy changes. A cell taken or freed within 0.25 s gives 1; it fades out over *Motion fade* (0.5 s); changes below *Motion threshold* are ignored (0.3: sensor noise at still objects measured up to 0.28, walking 0.5 to 1).
  - B: visibility, 1 where at least one sensor sees the cell, 0 in the shadow (behind a body or a barrier, outside every fan), outside the area and without a sensor. Where B is 0, R = 0 means "unknown", not "empty". Simulated objects cast no shadow.
  - A: always 255.
- **Orientation**: column 0 is at the left end of the field rectangle, column width − 1 at the right end, as seen from the area looking at the base line. Row 0 is on the base line, the last field row at the far edge of the area, the band above it. The bridge does not know where the projection lies (a curtain, a floor); a patch turns and places the field as it needs. In a patch, normalized y = 0 is the base line, at the bottom of the frame (`FLIP = True` in `spout_out.py`; measured in Arena 2026-11-17).
- **Status band**: the top sixteenth of the texture carries no field data. Every pixel of it holds a counter in R, G and B that changes every frame (0..255, wraps). A patch samples it anywhere in the band, e.g. at 31/32 of the height, and fades out when the value stops changing, so the picture stays calm without the bridge (docs/PATCH_RULES.md, "Patches that use a tool"). The band is a sixteenth so that it survives Arena's resampling at any size. The example [Field Viewer](../../examples/Field%20Viewer/) shows how to keep the last value and measure the time since it changed.
- **Without the bridge**: the patch gets a black frame (measured); a patch must not show the band and must fade out when the counter stops.
- **Without area and base line**: an 8 × 16 black texture; the band keeps counting.
- **Simulated objects** count as discs of 0.44 m.

## Configuration

All settings are made in the window. Detection and field settings are saved in `settings.json`, the room in `rooms/<name>/`:

- Countdown (default 10 s): time between a click and the measurement.
- Room: positions, rotation, fan, area with base line, exclusions, barriers, reference: see Usage.
- *Field*: Cell size (100 mm), Motion fade (0.5 s), Motion threshold (0.3, recommended). *Reset to recommended* restores them.
- Advanced detection, with tooltips: Tolerance (150 mm), Blob gap (250 mm), Min points (6), Merge distance (450 mm). *Reset to defaults* restores them.
- *Sensor*: the port of the position with the sensor (COM19), *Find sensor*, *Show all ports*; quality log (off). *View*: UI scale (1.0). The settings column remembers whether it was open.

## Troubleshooting

- Anything odd: read `logs/bridge.log`; it says which room and readers were started and what failed.
- Sensor not connected, no COM port: CP210x driver missing or USB cable. With a port but no data: check the wiring above; the motor must spin audibly.
- Which port is it? *Find sensor* (step 2, Sensor). It finds nothing: no power, the motor does not spin, or another program holds the port (it then shows as an error in `logs/bridge.log`, `port scan:` lines).
- Walls or furniture show up in the field: learn the empty room again, or raise *Tolerance*.
- One body gives two rings: raise *Merge distance*. With two positions: align them better.
- The field appears in the wrong place or mirrored in the patch: check the base line (*Choose base line*; the `0` marks column 0) and *Mirrored* of the position; the patch turns the field as it needs.
- *Output* is red: SpoutGL is not installed (`pip install SpoutGL` in the venv) or the sender failed; the log says which.
- The texture does not appear in Arena or Wire, or is 0 × 0: the bridge and Arena may run on different graphics adapters; set both to the same one in the Windows graphics settings.
- The picture in the patch stands still although the status shows frames: the patch reads a frozen texture; check the status band in the patch.
- The patch shows nothing or a tiny field although the Spout clip shows the field: the patch is a source, the clip is not at the texture's size, or the layer's *Auto Size* is not *Stretch* (Usage, "In Arena"). The example Field Viewer shows what arrives.

## Known issues

See [open issues for this tool](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Atool%3Amotion_bridge).

## Changelog

- 2026-11-22: Corners are added and deleted by double click in the view (on an edge: new corner, on a corner: deleted), undoable; the buttons are gone. The settings column can be closed (strip or F2) for the performance and is grouped in the order of the setup, each section with a line saying what it is for; view options are collected in *View*. Tested with the live sensor in the window.
- 2026-11-22: The bridge window is a setup tool (#135). The front is replaced by a base line, one edge of the area (*Choose base line*); the field is the smallest rectangle along it that holds the whole area, with square cells, so nothing is cut off and one metre is the same in both directions. Corners can be added, deleted and dragged; Undo (Ctrl+Z) for every change to the shapes. The view shows the measured room as solid lines, the live scan as dots and the field as it is sent (red = occupied, green = moving); foreground points only with *Foreground points*. *Field* shows the texture as it is sent instead of three images. Points at the edges of fixed objects no longer flicker as foreground (comparison within ±1°). *Stand at left/right end* and *Area from front* are removed. Older rooms are converted. Tested with the live sensor, in the window and in Arena (see Status).
- 2026-11-21: New patch using the bridge: Motion Bridge - Autumn Wind (#70).
- 2026-11-21: Moving simulated objects (#130): *Move between A and B*, *Speed* and *Pause* in the Simulation section, `--sim-path`, `--sim-speed` and `--sim-pause` on the command line. Tested on the show machine with the live sensor and Arena (1920 × 1080, Motion Bridge - Autumn Wind with `Show Field`): the object stands at A as occupancy, leaves a motion trail while it walks, stands at B and walks back.
- 2026-11-21: New patch using the bridge: Motion Bridge - Fireflies (#127).
- 2026-11-21: Documentation: the B channel is now checked in a patch in Arena (Motion Bridge - Fabric Wind).
- 2026-11-20: Visibility (#113): the room map tints what each sensor sees, live, so shadows behind people, objects and barriers and outside the fans stay dark (*Show what the sensors see*). The field gets a third image, *Seen*, and the Spout texture carries visibility in B. Tested with the live sensor in the window; B in Arena not checked yet.
- 2026-11-20: *Find sensor* in the Sensor section: listens briefly on all serial ports at once and lists only the ports with valid STL-27L packets, with the rotation; the port list shows the Windows description of each port. Read-only, DTR and RTS off, ports that hang are given up after 2 s. Tested with the real sensor and nine ports.
- 2026-11-17: Tested in Arena with the example Field Viewer (#115): `FLIP = True`, so the band arrives at the top and the front at the bottom, as the contract says. Setup: the patch is a clip effect on the Spout clip, the clip at the texture's size, the layer's *Auto Size* *Stretch* (Usage, "In Arena", with the data flow from the room to the picture). Measured 29 frames per second at the patch, black when the bridge stops, automatic reconnect.
- 2026-11-16: First test on the show machine (see Status): the texture reaches Arena and built-in effects, a Wire effect gets an empty Texture In; the band arrives at the bottom; Width/Height alone does not stretch. Nothing changed in the code yet.
- 2026-11-16: Status band instead of a counter pixel (#115): the top sixteenth of the texture counts up every frame, so the sign of life survives Arena's resampling. Patches are effects on the Spout clip with Resize Stretch; a source patch cannot read Spout inside Arena (measured). Untested beyond that.
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
