#!/usr/bin/env python3
"""End-to-end smoke test of the AcmTools command line, used by the CI and by the release.

    python smoke_test.py --cli "path/to/acmconverter" [--with-ffmpeg]

--cli is the command that starts the tool (it may contain several words, e.g. "dotnet out/AcmTools.Cli.dll").
Only the Python standard library is used.
"""

import argparse
import array
import math
import os
import shlex
import shutil
import subprocess
import sys
import tempfile
import wave

RATE = 22050


def tone(frames, left_hz, right_hz=None, amplitude=9000):
    """Interleaved 16-bit samples: mono when right_hz is None, stereo otherwise."""
    data = array.array("h")
    for n in range(frames):
        fade = min(1.0, n / 500.0)
        data.append(int(math.sin(2 * math.pi * left_hz * n / RATE) * amplitude * fade))
        if right_hz is not None:
            data.append(int(math.sin(2 * math.pi * right_hz * n / RATE) * amplitude * fade))
    return data


def write_wav(path, samples, channels):
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(samples.tobytes())


def read_wav(path):
    with wave.open(path, "rb") as w:
        data = array.array("h")
        data.frombytes(w.readframes(w.getnframes()))
        return w.getnchannels(), w.getnframes(), data


def snr_db(reference, decoded, channels, skip_frames=4096):
    """Worst signal-to-noise ratio over the channels, ignoring the start of the file."""
    worst = 999.0
    for channel in range(channels):
        signal = noise = 0.0
        for i in range(skip_frames * channels + channel, min(len(reference), len(decoded)), channels):
            error = decoded[i] - reference[i]
            signal += reference[i] * reference[i]
            noise += error * error
        worst = min(worst, 10 * math.log10(max(signal, 1.0) / max(noise, 1e-9)))
    return worst


class Failure(Exception):
    pass


class SmokeTest:
    def __init__(self, cli, folder, with_ffmpeg):
        self.cli = shlex.split(cli, posix=(os.name != "nt"))
        self.folder = folder
        self.with_ffmpeg = with_ffmpeg
        self.passed = 0

    def path(self, name):
        return os.path.join(self.folder, name)

    def run(self, *args, expect=0):
        result = subprocess.run(
            self.cli + [str(a) for a in args],
            capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=300,
        )
        if result.returncode != expect:
            raise Failure(
                "%s -> exit %d (expected %d)\n--- stdout\n%s\n--- stderr\n%s"
                % (" ".join(map(str, args)), result.returncode, expect, result.stdout, result.stderr)
            )
        return result.stdout + result.stderr

    def check(self, condition, message):
        if not condition:
            raise Failure(message)

    def step(self, name, function):
        try:
            function()
        except Failure as failure:
            print("FAIL  %s\n%s" % (name, failure))
            sys.exit(1)
        self.passed += 1
        print("ok    %s" % name)

    # --- scenarios -------------------------------------------------------------------------------------------

    def help_and_version(self):
        self.check("inspect" in self.run("--help"), "--help does not list the commands")
        self.check(self.run("--version").startswith("AcmTools "), "--version does not print the version")

    def short_stereo_becomes_mono(self):
        samples = tone(RATE * 8, 440, 660)
        write_wav(self.path("short.wav"), samples, 2)
        self.run("to-acm", self.path("short.wav"), self.path("short.acm"), "--lang", "en")
        info = self.run("inspect", self.path("short.acm"), "--lang", "en")
        self.check(any("Effective channels" in l and l.split("│")[2].strip() == "1" for l in info.splitlines() if "│" in l),
                   "a short stereo source should be written as mono:\n" + info)
        self.run("to-audio", self.path("short.acm"), self.path("short.out.wav"))
        channels, frames, decoded = read_wav(self.path("short.out.wav"))
        self.check(channels == 1 and frames == RATE * 8, "unexpected mono round trip: %d channels, %d frames" % (channels, frames))

    def long_stereo_is_music(self):
        samples = tone(RATE * 70, 330, 495)
        write_wav(self.path("music.wav"), samples, 2)
        out = self.run("to-acm", self.path("music.wav"), self.path("music.acm"), "--lang", "en")
        self.check("Kept both channels" in out, "a 70 s stereo source should keep both channels:\n" + out)
        self.run("to-audio", self.path("music.acm"), self.path("music.out.wav"))
        channels, frames, decoded = read_wav(self.path("music.out.wav"))
        self.check(channels == 2 and frames == RATE * 70, "unexpected stereo round trip: %d channels, %d frames" % (channels, frames))
        snr = snr_db(samples, decoded, 2)
        self.check(snr > 22, "stereo round trip SNR too low: %.1f dB" % snr)

    def forced_channels(self):
        samples = tone(RATE * 5, 300, 700)
        write_wav(self.path("forced.wav"), samples, 2)
        self.run("to-acm", self.path("forced.wav"), self.path("forced.acm"), "--channels", "stereo")
        self.run("to-audio", self.path("forced.acm"), self.path("forced.out.wav"), "--channels", "stereo")
        channels, frames, decoded = read_wav(self.path("forced.out.wav"))
        self.check(channels == 2 and frames == RATE * 5, "--channels stereo did not keep two channels")
        self.run("to-audio", self.path("forced.acm"), self.path("forced.mono.wav"), "--channels", "mono")
        channels, frames, _ = read_wav(self.path("forced.mono.wav"))
        self.check(channels == 1 and frames == RATE * 5 * 2, "--channels mono should read the interleaved values as mono")

    def lossless_linear_mode(self):
        samples = tone(RATE * 3, 523, amplitude=20000)
        write_wav(self.path("lossless.wav"), samples, 1)
        self.run("to-acm", self.path("lossless.wav"), self.path("lossless.acm"), "--level", "0")
        self.run("to-audio", self.path("lossless.acm"), self.path("lossless.out.wav"))
        _, _, decoded = read_wav(self.path("lossless.out.wav"))
        self.check(list(decoded) == list(samples), "--level 0 must round trip exactly")

    def batch_with_a_failure(self):
        source = self.path("batch-in")
        os.makedirs(os.path.join(source, "sub"), exist_ok=True)
        for index, (folder, hz) in enumerate([("", 220), ("", 330), ("sub", 440)]):
            write_wav(os.path.join(source, folder, "t%d.wav" % index), tone(RATE * 2, hz), 1)
        with open(os.path.join(source, "broken.wav"), "wb") as f:
            f.write(b"this is not a wav file")
        out = self.run("to-acm", source, self.path("batch-out"), "--recursive", "--jobs", "2", "--lang", "en", expect=1)
        self.check("3 converted" in out and "1 failed" in out, "unexpected batch summary:\n" + out)
        for name in ("t0.acm", "t1.acm", os.path.join("sub", "t2.acm")):
            self.check(os.path.exists(os.path.join(self.path("batch-out"), name)), "missing batch output " + name)
        out = self.run("to-acm", source, self.path("batch-out"), "--recursive", "--no-overwrite", "--lang", "en", expect=1)
        self.check("3 skipped" in out, "--no-overwrite should skip existing files:\n" + out)

    def errors_and_languages(self):
        self.run("to-acm", "--no-such-option", expect=1)
        self.run("--lang", "xx", "inspect", self.path("short.acm"), expect=1)
        french = self.run("--lang", "fr", "inspect", self.path("short.acm"))
        self.check("chantillons" in french and "Dur" in french, "the French inspection table is missing:\n" + french)

    def ffmpeg_formats(self):
        samples = tone(RATE * 4, 400, amplitude=12000)
        write_wav(self.path("ff.wav"), samples, 1)
        self.run("to-acm", self.path("ff.wav"), self.path("ff.acm"))
        self.run("to-audio", self.path("ff.acm"), self.path("ff.ogg"))
        self.check(os.path.getsize(self.path("ff.ogg")) > 1000, "FFmpeg did not write the ogg file")
        self.run("to-acm", self.path("ff.ogg"), self.path("from-ogg.acm"))
        self.check(os.path.getsize(self.path("from-ogg.acm")) > 100, "FFmpeg import to ACM failed")

    def run_all(self):
        self.step("help and version", self.help_and_version)
        self.step("short stereo source is written as mono", self.short_stereo_becomes_mono)
        self.step("stereo source longer than 60 s keeps both channels", self.long_stereo_is_music)
        self.step("--channels forces mono or stereo", self.forced_channels)
        self.step("--level 0 is lossless", self.lossless_linear_mode)
        self.step("batch conversion reports failures and skips existing files", self.batch_with_a_failure)
        self.step("errors and translations", self.errors_and_languages)
        if self.with_ffmpeg:
            self.step("FFmpeg formats", self.ffmpeg_formats)
        print("%d checks passed" % self.passed)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--cli", required=True, help="command that starts the tool")
    parser.add_argument("--with-ffmpeg", action="store_true", help="also test the formats that need FFmpeg")
    args = parser.parse_args()
    if args.with_ffmpeg and shutil.which("ffmpeg") is None:
        sys.exit("ffmpeg was requested but is not installed")

    folder = tempfile.mkdtemp(prefix="acmtools-smoke-")
    try:
        SmokeTest(args.cli, folder, args.with_ffmpeg).run_all()
    finally:
        shutil.rmtree(folder, ignore_errors=True)


if __name__ == "__main__":
    main()