"""The output is watched: failed sends count as failed, the check notices a frozen or unreadable sender,
the engine recreates the sender and keeps running after an error, the status says when nothing goes out.
Uses a fake SpoutGL, so it runs anywhere and never touches a real sender."""
import os
import sys
import time
import unittest
from unittest import mock

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import engine as engine_mod  # noqa: E402
import spout_check  # noqa: E402
import spout_out  # noqa: E402
from spout_check import STALL_S, SpoutCheck  # noqa: E402
from test_engine import make_engine  # noqa: E402

TEX = np.zeros((16, 8, 4), np.uint8)


class FakeSender:
    created = 0
    result = True

    def __init__(self):
        FakeSender.created += 1

    def setSenderName(self, name):
        pass

    def sendImage(self, *args):
        return FakeSender.result

    def setFrameSync(self, name):
        pass

    def releaseSender(self):
        pass


class FakeSpoutGL:
    SpoutSender = FakeSender


def fake_out():
    with mock.patch.object(spout_out, 'SpoutGL', FakeSpoutGL), mock.patch.object(spout_out, 'AVAILABLE', True):
        return spout_out.SpoutOut()


class SpoutOutTest(unittest.TestCase):
    def setUp(self):
        FakeSender.created, FakeSender.result = 0, True
        self.p = [mock.patch.object(spout_out, 'SpoutGL', FakeSpoutGL), mock.patch.object(spout_out, 'AVAILABLE', True)]
        for p in self.p:
            p.start()

    def tearDown(self):
        for p in self.p:
            p.stop()

    def test_only_frames_really_sent_count(self):
        out = spout_out.SpoutOut()
        out.send(TEX)
        self.assertEqual(out.frames, 1)
        self.assertIsNotNone(out.last_ok_t)
        FakeSender.result = False                       # sendImage fails without an exception
        t = out.last_ok_t
        out.send(TEX)
        self.assertEqual(out.frames, 1)
        self.assertEqual(out.last_ok_t, t)
        self.assertFalse(out.sending)
        self.assertIn('returned False', out.status)

    def test_recreate(self):
        out = spout_out.SpoutOut()
        out.recreate()
        self.assertEqual(FakeSender.created, 2)
        self.assertEqual(out.recreations, 1)
        self.assertTrue(out.sending)


class Reader:
    def __init__(self):
        self.ok, self.samples = True, (1,)

    def read(self):
        return self.ok, self.samples


class CheckTest(unittest.TestCase):
    def setUp(self):
        self.t = [100.0]
        self.ck = SpoutCheck('x', reader_factory=lambda n: Reader(), clock=lambda: self.t[0])
        self.r = Reader()

    def tick(self, dt, samples=None, ok=True):
        self.t[0] += dt
        self.r.ok = ok
        if samples is not None:
            self.r.samples = samples
        self.ck.step(self.r)

    def test_changing_band_is_ok_a_frozen_one_stalls(self):
        for k in range(3):
            self.tick(1.0, (k,))
        self.assertEqual(self.ck.state, 'ok')
        for _ in range(3):
            self.tick(1.0)                               # the same samples: frozen
        self.assertEqual(self.ck.state, 'stalled')
        self.assertGreater(self.ck.bad_for(self.t[0] + 1.0), 0.5)
        self.tick(1.0, (9,))
        self.assertEqual(self.ck.state, 'ok')
        self.assertEqual(self.ck.bad_for(), 0.0)

    def test_unreadable(self):
        self.tick(1.0, (1,))
        for _ in range(4):
            self.tick(1.0, ok=False)
        self.assertEqual(self.ck.state, 'unreadable')

    def test_first_contact_gets_time(self):
        self.tick(0.5, ok=False)
        self.assertEqual(self.ck.state, 'starting')
        self.tick(STALL_S + 0.1, ok=False)
        self.assertEqual(self.ck.state, 'unreadable')


class EngineWatchTest(unittest.TestCase):
    def setUp(self):
        FakeSender.created, FakeSender.result = 0, True
        self.eng = make_engine()
        self.eng.spout = fake_out()
        self.eng.check = SpoutCheck('x', reader_factory=lambda n: Reader())
        self.eng.check.state = 'ok'

    def test_failed_sends_recreate_the_sender(self):
        eng, now = self.eng, time.time()
        with mock.patch.object(spout_out, 'SpoutGL', FakeSpoutGL), mock.patch.object(spout_out, 'AVAILABLE', True):
            eng.frame(now)
            self.assertTrue(eng.output_status()[0])
            FakeSender.result = False
            eng.frame(now)
            eng.spout.last_ok_t = now - engine_mod.HEAL_S - 1.0
            eng.watch(now)
        self.assertEqual(eng.spout.recreations, 1)
        self.assertFalse(eng.output_status()[0])

    def test_a_frozen_check_recreates_and_shows_red(self):
        eng, now = self.eng, time.time()
        with mock.patch.object(spout_out, 'SpoutGL', FakeSpoutGL), mock.patch.object(spout_out, 'AVAILABLE', True):
            eng.frame(now)
            eng.check.state, eng.check.since = 'stalled', now - engine_mod.HEAL_S - 1.0
            eng.watch(now)
            self.assertEqual(eng.spout.recreations, 1)
            eng.watch(now + 1.0)                        # at most one recreation per HEAL_GAP_S
            self.assertEqual(eng.spout.recreations, 1)
        ok, text = eng.output_status()
        self.assertFalse(ok)
        self.assertIn('NOT READABLE', text)

    def test_no_frames_means_stopped(self):
        eng = self.eng
        eng.frame(time.time())
        eng.last_frame_t = time.time() - 5.0
        ok, text = eng.output_status()
        self.assertFalse(ok)
        self.assertIn('STOPPED', text)

    def test_an_error_in_the_loop_does_not_stop_the_engine(self):
        eng = self.eng
        calls = []

        def boom():
            calls.append(1)
            if len(calls) == 1:
                raise RuntimeError('test')
            eng.running = False

        with mock.patch.object(eng, '_loop_once', boom), mock.patch.object(engine_mod, 'SpoutOut', fake_out), \
                mock.patch.object(engine_mod, 'SpoutCheck', lambda name: SpoutCheck(name, reader_factory=lambda n: Reader())), \
                self.assertLogs('engine', level='ERROR'):
            eng.run()
        self.assertEqual(len(calls), 2)
        self.assertEqual(eng.loop_errors, 1)


if __name__ == '__main__':
    unittest.main()
