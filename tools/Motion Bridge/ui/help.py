"""Help texts of the window: one entry per control, shown on hover."""
import dearpygui.dearpygui as dpg


HELP = {
    'room': 'A room holds everything measured and drawn for one venue: sensor positions, their scans, '
            'area with its base line, exclusions and barriers. Switch rooms here; each is kept in rooms/<name>/.',
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
    'show_seen': 'Fills, live, what each position with a sensor sees: from the sensor up to the first point each '
                 'ray hits, at most to a barrier, only inside the fan. Everything that stays dark is shadow: '
                 'behind people and objects, behind barriers, outside the fans. There the bridge does not know '
                 'whether something is there. Several positions add up; a further sensor belongs where a '
                 'shadow matters.',
    'ref_place': 'Puts a reference marker in the view with the next click. Place it on the reference object '
                 '(e.g. a pole) in a scan that is already aligned.',
    'ref_snap': 'Moves the selected position so that the reference object in its scan sits on the marker. '
                'Then turn it (with "Turn around reference") until the walls match.\nThe object must stand '
                'free, at least 0.5 m away from walls, and be in the room while each position learns its scan. '
                'A tube of 10 cm or more is found reliably.',
    'area_draw': 'Draws the area: click its corners in the view, then "Finish" (or click the first corner). '
                 'Only foreground inside the area counts, and the field covers it. The edge from the first to the '
                 'second corner becomes the base line; "Choose base line" changes it. Double-click an edge to add a corner, '
                 'double-click a corner to delete it.\nWithout an area everything '
                 'in view counts in the window, but zeros are sent.',
    'base_pick': 'Click an edge of the area: it becomes the base line, row 0 of the texture. The texture is the '
                 'smallest rectangle along the base line that holds the whole area (grey outline in the view); '
                 'columns run from the left end (marked 0) to the right end, seen from the area looking at the base '
                 'line. The arrow points into the area. Cells outside the area stay 0.',
    'panel_toggle': 'Opens or closes the settings column (also F2). Closed, the map fills the window: for the '
                    'performance. The bridge keeps running and sending either way.',
    'undo': 'Takes back the last change to the area, the base line, exclusions, barriers or the reference marker '
            '(drawn, dragged, added or deleted by double click, removed). Also Ctrl+Z. While drawing, it cancels the drawing.',
    'area_clear': 'Removes the area and its base line. Zeros are sent until a new one is drawn.',
    'show_field': 'Shows the field in the view as it is sent: red = occupied, green = moving, yellow = both. '
                  'Occupied and still stays red; motion fades out over Motion fade.',
    'show_fg': 'Diagnosis: shows the single foreground points (red: counts, dark red: outside the area or in an '
               'exclusion) and the foreground objects (white rings) instead of joining them to the live scan.',
    'field': 'The image the patches get, enlarged: bottom = base line (row 0), left = left end of the base line. '
             'Red = occupancy, green = motion, blue = seen by a sensor (black is shadow or outside the area; there '
             'occupancy 0 means "unknown", not "empty"). The grey band on top is the status band; it counts up '
             'every frame.\nThe cells are square: one metre is the same number of pixels in both directions.',
    'reset_fields': 'Sets the field settings back to the recommended values: cell size 0.10 m, Motion fade 0.5 s, '
                    'Motion threshold 0.3.',
    'cell_mm': 'Size of one cell in the room, the same along the base line and into the area. The texture size '
               'follows: a 4 m wide field at 0.10 m is 40 pixels wide. Patches do not depend on it. After a '
               'change, reload the Spout clip in Arena so it takes the new size.',
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
                   'can drag. Double-click an edge to add a corner, double-click a corner to delete it.',
    'countdown_s': 'Time between the click and the measurement, so you can leave the scan plane. A tone marks the '
                   'start and the end.',
    'sim': 'Adds objects you can drag in the view with the mouse. They count like real foreground and appear '
           'in the field, so patches can be tested without walking around or without a sensor.',
    'sim_add': 'Adds a simulated object 1.5 m into the area from the base line (or in front of the sensor). Drag '
               'the yellow point in the view. Its point B, where it walks to with Move on, starts 2 m to the right.',
    'sim_clear': 'Removes all simulated objects.',
    'sim_move': 'Every simulated object walks from its point A to its point B and back, pausing at each end. '
                'Standing still and moving can so be tested in one run. Drag the points to set the way; '
                'switching it on starts every object at A. This run only, not saved.',
    'sim_speed': 'Walking speed of the simulated objects. A person walks at about 1 m/s.',
    'sim_pause': 'How long a simulated object stands still at A and at B before it walks on.',
    'spout': 'The field leaves the bridge as a Spout texture named "Motion Bridge": one image per frame, 30 per '
             'second. In Arena: load it as a clip (Sources > Spout), set Resize to Stretch, and put the patch on it '
             'as an effect. Red = occupancy, green = motion, blue = visibility. The top sixteenth of the image is '
             'a status band whose pixels count up every frame, so a patch can tell a live field from a frozen '
             'one.\nNeeds the SpoutGL package (Windows).',
    'tol_mm': 'How much closer than the empty room a point must be to count as foreground. '
              'Raise it if walls or furniture flicker red.',
    'gap_mm': 'Foreground points further apart than this start a new blob.',
    'min_pts': 'Blobs with fewer points are ignored (noise, small objects).',
    'merge_mm': 'Blobs closer than this count as one object. Objects are shown in the view with "Foreground '
                'points" on; the patches get the field, not objects.',
    'reset_adv': 'Restores the detection defaults.',
    'port': 'Serial port of the lidar adapter (Silicon Labs CP210x). After "Find sensor" only ports with lidar '
            'data are listed, with the rotation the sensor reports.',
    'assign': 'The sensor on this port stands at the selected position. Another position holding the port '
              'gives it up. The bridge reconnects by itself after a cable pull.',
    'release': 'The selected position keeps its scan but gets no live data.',
    'log': 'Writes one line per revolution to logs/quality_*.csv (speed, points, valid share, CRC errors, objects).',
    'scale': 'Size of text and controls.',
    'fullscreen': 'Fullscreen on or off (also F11).',
    'fit': 'Shows the whole room: all scans, the area and the field.',
    'cancel': 'Stops the countdown or measurement.',
    'find': 'Listens about half a second on every serial port and lists only the ports that deliver lidar data '
            '(valid STL-27L packets). It only reads: nothing is sent to any device. The port the bridge reads '
            'itself is not opened; it shows as "in use". Ports that hang when opened (Bluetooth) are skipped '
            'after two seconds. All ports are checked at the same time, so a search takes at most about 2.5 s.',
    'show_all': 'Lists every serial port with its Windows description, also without a sensor.',
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
    'st_map': 'The area says what counts; its base line gives the texture its frame.\n'
              'Green: both set; field size, cell size and texture size in pixels.\n'
              'Red: no area or no base line; zeros are sent. See step 4.',
    'st_output': 'The field texture leaving the bridge over Spout: sender name, size in pixels and frames per '
                 'second (30 is normal).\nRed: nothing is sent. Install SpoutGL (pip install SpoutGL) or read '
                 'logs/bridge.log.',
    'st_objects': 'Foreground objects seen right now, including simulated ones. Grey: nothing there.',
    'view': 'The room from above, metres. Mouse wheel zooms, right-drag pans.\n\n'
            'Coloured lines: the measured empty room of each position; square: the sensor, short line: where it '
            'looks; outline: its fan, round handle: points the fan.\n'
            'Grey dots: what the sensor sees now; dark dots: ignored (outside the fan, behind a barrier, outside '
            'the area).\n'
            'Light blue: what the sensors see. Dark: shadow.\n'
            'Red / green / yellow cells: the field as it is sent (occupied / moving / both).\n'
            'Green line: the area. White line: its base line, 0 at its left end, the arrow points into the area. '
            'Thin grey outline: the texture rectangle.\n'
            'Red: exclusions, orange: barriers, yellow cross: reference marker.',
}


def tip(item, key):
    """Attaches the help text `key` to a dearpygui item and returns the item."""
    with dpg.tooltip(item):
        dpg.add_text(HELP[key], wrap=380)
    return item
