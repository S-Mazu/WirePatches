"""State shared by the parts of the window: the selected position and the rebuild flag."""


class UiState:
    def __init__(self):
        self.sel = None             # sid of the selected position
        self.rebuild = True         # the room changed in shape: lists and plot items are rebuilt
        self.room_name = None       # name of the room the plot items were built for

    def selected(self, eng):
        """The selected position; falls back to the live one, then the first."""
        st = eng.room.station(self.sel)
        if st is None:
            st = eng.live_station() or eng.room.stations[0]
            self.sel = st.sid
        return st
