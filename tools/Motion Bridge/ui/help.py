"""Help texts of the window: one entry per control, shown on hover."""
import dearpygui.dearpygui as dpg


HELP = {
    'room': 'A room holds everything measured and drawn for one venue: sensor positions, their scans, '
            'area, front, exclusions and barriers. Switch rooms here; each is kept in rooms/<name>/.',
    'new_room': 'Creates an empty room with one position. The connected sensor moves into it.',
    'positions': 'Each position is one place where a sensor stood or stands and its scan of the empty room. '
                 'The position marked "sensor" delivers live data.\nClick a position to select it; its controls '
                 'are below. In the view, drag the coloured square to move it.',
    'learn': 'Learns what the empty room looks like from the selected position. Everything closer than '
             'the empty room later counts as foreground.\nClick, leave the scan plane during the countdown and '
             'stay out until the second tone (about 5 s).\nLearn again whenever the sensor or the room changes.',
    'moved': 'You moved the sensor to another place: creates a new position that takes over the sensor, '
             'placed 1 m next to the current one. Learn the empty room there, then align its scan with the '
             'others (drag, rotate, or snap to the reference).',
    'remove_pos': 'Deletes the selected position and its scan.',
    'xy': 'Position in the room map, metres. Also by dragging the coloured square in the view.',
    'rotation': 'Turns the scan of this position until walls and objects lie on those of the other positions. '
                'Degrees, counter-clockwise.',
    'nudge': 'Turns the position in small steps.',
    'pivot': 'Turns the position around the reference marker instead of around the sensor. Use it after '
             'snapping: the reference stays in place while the walls swing into line.',
    'flip': 'Mirrors the scan of this position. Tick it if the map shows the room mirrored '
            '(e.g. a sensor mounted upside down).',
    'fan': 'Only measurements inside the fan count, the rest is ignored. 360 = all around. Point the fan '
           'with the round handle in the view.',
    'show_scans': 'Shows the empty-room scans of all positions; off shows only the selected one.',
    'ref_place': 'Puts a reference marker in the view with the next click. Place it on the reference object '
                 '(e.g. a pole) in a scan that is already aligned.',
    'ref_snap': 'Moves the selected position so that the reference object in its scan sits on the marker. '
                'Then turn it (with "Turn around reference") until the walls match.\nThe object must stand '
                'free, at least 0.5 m away from walls, and be in the room while each position learns its scan. '
                'A tube of 10 cm or more is found reliably.',
    'area_draw': 'Draws the area: click its corners in the view, then "Finish" (or click the first corner). '
                 'Only foreground inside the area counts, and the field covers it. Tick "Edit points" to drag the '
                 'corners later.\nWithout an area everything in view counts in the window, but zeros are sent.',
    'area_front': 'Makes the area a rectangle in front of the front, 3 m deep, as a start. Tick "Edit points" and '
                  'drag its corners.',
    'area_clear': 'Removes the area. Zeros are sent until a new one is drawn.',
    'front_draw': 'Draws the front in the view: click the LEFT end, then the RIGHT end, as seen from the area. '
                  'The front is where the projection is. For the patches, x runs along it from 0 (left) to 1 '
                  '(right) and depth from 0 at the front to 1 at the far edge of the area.\nThe ends can be '
                  'dragged afterwards.',
    'front_left': 'Stand alone at the LEFT end of the front (seen from the area), close to it, and stay still '
                  'until the second tone.',
    'front_right': 'Stand alone at the RIGHT end of the front (seen from the area), close to it, and stay still '
                   'until the second tone.',
    'front_swap': 'Swaps the ends. The arrow must point into the area; use this when it points away from it or '
                  'the picture is mirrored.',
    'front_clear': 'Removes the front. Zeros are sent until a new one is drawn.',
    'field': 'What the patches get, seen from the area: left to right along the front, bottom = at the '
             'front, top = far edge of the area. Cells outside the area stay 0.\n'
             'Occupancy: where something is (white = a surface crosses the whole cell).\n'
             'Motion: where the occupancy changes (white = a cell is taken or freed within 0.25 s). Standing still '
             'fades out.\nThe patches receive both as one Spout texture (red = occupancy, green = motion).',
    'reset_fields': 'Sets the field settings back to the recommended values: cell size 0.10 m, Motion fade 0.5 s, '
                    'Motion threshold 0.3.',
    'cell_mm': 'Size of one cell in the room, the same along the front and into the area. The texture size follows: '
               'a 4 m front at 0.10 m is 40 pixels wide. Patches do not depend on it.',
    'field_fade_s': 'How long motion takes to fade out after it stops.',
    'field_threshold': 'Motion below this value is ignored: sensor noise at the edges of still objects. '
                       'Recommended and default: 0.3 (measured noise up to 0.28, walking 0.5 to 1). '
                       'Lower it only in a very quiet setup; raise it if still objects show motion.',
    'draw_excl': 'Draws an exclusion: click its corners in the view, then "Finish". Nothing inside it counts '
                 '(a bar, a stage edge, a door).',
    'draw_bar': 'Draws a barrier: click its two ends. Sensors do not look through it; everything behind it, '
                'seen from a sensor, is ignored.',
    'finish': 'Closes the polygon being drawn.',
    'cancel_draw': 'Stops drawing; nothing is added.',
    'edit_points': 'Shows the corners of the area and the exclusions, and the ends of barriers, as handles you '
                   'can drag.',
    'countdown_s': 'Time between the click and the measurement, so you can walk to your place '
                 '(out of the room, or to an end of the front). A tone marks the start and the end.',
    'sim': 'Adds objects you can drag in the view with the mouse. They count like real foreground and appear '
           'in the field, so patches can be tested without walking around or without a sensor.',
    'sim_add': 'Adds a simulated object 1.5 m in front of the front (or of the sensor). Drag the yellow point '
               'in the view.',
    'sim_clear': 'Removes all simulated objects.',
    'spout': 'The field leaves the bridge as a Spout texture named "Motion Bridge": one image per frame, 30 per '
             'second. In Arena: load it as a clip (Sources > Spout), set Resize to Stretch, and put the patch on it '
             'as an effect. Red = occupancy, green = motion, blue = visibility. The top sixteenth of the image is '
             'a status band whose pixels count up every frame, so a patch can tell a live field from a frozen '
             'one.\nNeeds the SpoutGL package (Windows).',
    'tol_mm': 'How much closer than the empty room a point must be to count as foreground. '
              'Raise it if walls or furniture flicker red.',
    'gap_mm': 'Foreground points further apart than this start a new blob.',
    'min_pts': 'Blobs with fewer points are ignored (noise, small objects).',
    'merge_mm': 'Blobs closer than this count as one object (arm and body; the same body seen by two sensors). '
                'Objects are shown in the view and used when you stand at an end of the front; the patches get '
                'the field, not objects.',
    'reset_adv': 'Restores the detection defaults.',
    'port': 'Serial port of the lidar adapter (Silicon Labs CP210x).',
    'assign': 'The sensor on this port stands at the selected position. Another position holding the port '
              'gives it up. The bridge reconnects by itself after a cable pull.',
    'release': 'The selected position keeps its scan but gets no live data.',
    'log': 'Writes one line per revolution to logs/quality_*.csv (speed, points, valid share, CRC errors, objects).',
    'scale': 'Size of text and controls.',
    'fullscreen': 'Fullscreen on or off (also F11).',
    'fit': 'Shows the whole room: all scans, the area and the front.',
    'cancel': 'Stops the countdown or measurement.',
    'refresh': 'Reads the list of serial ports again, e.g. after plugging in the adapter.',
    'st_sensor': 'Connection to the lidar.\n'
                 'Green: data arrives.\n'
                 'Yellow: CRC errors are rising. These are damaged data packets; a few right after '
                 'plugging in are normal. If they keep rising, check the wiring and the cable.\n'
                 'Red: no data. Check the USB cable, the port under "Sensor" and that the motor spins.',
    'st_signal': 'Quality of the scan over the last 5 seconds. Green = fine, yellow = keep an eye on it, '
                 'red = act.\n\n'
                 'rev/s: turns of the sensor per second. 10 is normal (green 9 to 11). Lower or unsteady '
                 'usually means weak USB power.\n\n'
                 'valid: share of measurements that hit something within range (green from 90 %). It drops '
                 'with black or shiny surfaces, glass, open doors, direct sunlight or a dirty sensor window. '
                 'In a large open hall a lower value can be normal: compare with a quiet moment.\n\n'
                 'points/rev: measurements per turn, about 2160 at 10 rev/s. Much less means data is lost '
                 'on the way (cable, USB).\n\n'
                 'The grey line below sums up the whole run since the start.',
    'st_room': 'The empty-room scan of the selected position is its reference: anything closer counts as '
               'foreground.\n\n'
               'coverage: share of directions in which the sensor sees a wall or an object. Open sides '
               'lower it; that is fine.\n'
               'noise: typical jitter of the room measurement; below 20 mm is good.\n\n'
               'Yellow: not learned yet. Learn again whenever the sensor or the room changes.',
    'st_map': 'The area says what counts; the front turns positions into x and depth for the patches.\n'
              'Green: both set.\n'
              'Yellow: the front faces away from the area; click "Swap ends".\n'
              'Red: no area or no front; zeros are sent. See step 3.',
    'st_output': 'The field texture leaving the bridge over Spout: sender name, size in pixels and frames per '
                 'second (30 is normal).\nRed: nothing is sent. Install SpoutGL (pip install SpoutGL) or read '
                 'logs/bridge.log.',
    'st_objects': 'Foreground objects seen right now, including simulated ones. Grey: nothing there.',
    'view': 'The room from above, metres. Mouse wheel zooms, right-drag pans.\n\n'
            'Coloured dots: empty-room scan of each position; square: the sensor, line: its front; '
            'outline: its fan, round handle: points the fan.\n'
            'Grey: live scan, red: foreground (closer than the empty room), dark red: foreground that does not '
            'count (outside the area, in an exclusion), dark: ignored (outside the fan, behind a barrier).\n'
            'White: the front with its ends L and R; the arrow points into the area. Thin grey outline: the '
            'field, front width x area depth.\n'
            'Green: the area, red: exclusions, orange: barriers, yellow cross: reference marker.\n'
            'White rings: foreground objects (blobs merged by Merge distance).',
}


def tip(item, key):
    """Attaches the help text `key` to a dearpygui item and returns the item."""
    with dpg.tooltip(item):
        dpg.add_text(HELP[key], wrap=380)
    return item
