# Kinect (Discontinued)

> **Discontinued since 2026-11-08.** This tool is no longer developed. We will rebuild the idea with a LIDAR.

Made projections in Arena interactive with a Kinect v2: a Python tool sends the outlines and skeletons of people to Arena and Wire via Spout.

Last updated: 2026-11-13

**Status:** masks, bones and head were tested with people in front of the sensor. Pulling the sensor cable while running was not tested.

**Last known problem:** on 2026-11-06 the tool showed the shapes correctly, but nothing arrived in Arena. On 2026-11-08 the tool's own Spout senders, sending a test mask without a Kinect, arrived correctly in Arena on the show machine. No bug was found in the code and the cause is unknown. The machine has an Intel and an NVIDIA graphics adapter, so a different adapter for the tool and Arena is possible, but this was not confirmed.

## Usage

```
py -3.12 kinect_app.py
```

1. In the control window, click **Capture empty room**, then leave the picture within 5 seconds. The room is saved and loaded again on the next start.
2. In Arena, add the sources `Kinect Body`, `Kinect Depth` and `Kinect Bones` from the Spout section.
3. On each clip, set **Resize** to **Fit**. The pictures are 1536×1272; with the default setting Arena crops them on a 1080p composition.

Capture the room again whenever the sensor or the room changes. Slider values are saved when the window closes.

All three are white on transparent, so you can color them and add effects in Arena. In Wire, a `Texture In` node can receive it via Spout.

| Control | What it does |
|---|---|
| Near, Far | Depth mask: the distance range that counts |
| Room tolerance | Depth mask: how much closer than the empty room something must be |
| Min blob size | Drops smaller specks |
| Calm | Steadies outlines and bones; higher values lag behind movement |
| Edge softness | Masks: from a hard edge to a soft one |
| Bone width | Bones: line width in percent of the picture height |
| Head size | Bones: size of the head; 1 is about a real head, 0 turns it off |

Tests (no Kinect needed): `py -3.12 -m unittest`

## Goal

- Get the outlines of people into Arena as a white mask that can be colored and processed there (e.g. Trails, DelayRGB).
- Use more of what the Kinect offers, step by step.

## Decisions

| Topic | Decision | Reason |
|---|---|---|
| Language | Python | Fast to iterate. The sensor limits the frame rate to 30 FPS, and the mask takes only a few ms of each frame. |
| Kinect library | [`kinect-next`](https://github.com/Liidioteee/kinect-next) | Runs on the show machine. Young project with one author, so it stays behind a thin layer. |
| Spout library | [`SpoutGL`](https://github.com/jlai/Python-SpoutGL) | Thin wrapper, runs on the show machine. |
| Image processing | OpenCV | Standard library, fast native code. |
| User interface | [Dear PyGui](https://github.com/hoffstadt/DearPyGui) | Clickable, with live preview and sliders. |

## First version

- **Three pictures at the same time**, each with its own Spout sender:
  - `Kinect Body`: people detected by the Kinect (up to 6).
  - `Kinect Depth`: everything closer than the captured empty room.
  - `Kinect Bones`: stick figures of the tracked people (up to 6), in line with the masks. The Kinect delivers only one point for the head, so the head is an estimated disc that shrinks with the distance to the sensor.
- **Control window:** preview, "Capture empty room" button, sliders, sensor status and FPS. No keyboard shortcuts.
- **Smooth edges:** masks are upscaled from 512×424 to 1536×1272.

### The two masks

| Mask | Strength | Weakness |
|---|---|---|
| `Kinect Body` | No setup; ignores furniture | Detects only people, up to 6 |
| `Kinect Depth` | Detects anything that enters the room | Needs an empty-room capture; the room and the sensor must stay unchanged |

### Resolution limit

Depth and body data are 512×424 in the Kinect hardware. Upscaling gives smoother edges, not more detail.

## Later

- One mask per person
- Depth and infrared images as Spout sources
- Joint positions and hand states via OSC to Arena and Wire
## Trying alternatives

To compare another library or approach, copy this folder under a name that says how it differs (e.g. `Kinect Native`) and run both side by side. Compare them with real people in front of the sensor, then keep one.

## Installation

Requires Windows 10 or 11 and a Kinect v2 on USB 3.0. Run these commands in Command Prompt (`cmd`), one after the other.

Python 3.12 (skip if installed; open a new terminal afterwards):

```
winget install --id Python.Python.3.12 -e
```

Kinect for Windows SDK 2.0 (skip if installed; the installer opens a window):

```
curl.exe -L -o "%TEMP%\KinectSDK.exe" https://download.microsoft.com/download/F/2/D/F2D1012E-3BC6-49C5-B8B3-5ACFF58AF7B8/KinectSDK-v2.0_1409-Setup.exe && "%TEMP%\KinectSDK.exe"
```

Python libraries:

```
py -3.12 -m pip install kinect-next SpoutGL opencv-python numpy dearpygui
```

## Changelog

- 2026-11-13: Moved from `Kinect - Discontinued/` to `tools/Kinect/`; the status is now in this README and the root README (#88).
- 2026-11-08: Discontinued.
