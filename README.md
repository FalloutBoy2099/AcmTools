# AcmTools

.NET 10 library and CLI for Fallout/Interplay ACM audio conversion.

[![CI](https://github.com/FalloutBoy2099/AcmTools/actions/workflows/ci.yml/badge.svg)](https://github.com/FalloutBoy2099/AcmTools/actions/workflows/ci.yml)
[![CodeQL](https://github.com/FalloutBoy2099/AcmTools/actions/workflows/codeql.yml/badge.svg)](https://github.com/FalloutBoy2099/AcmTools/actions/workflows/codeql.yml)
[![Release](https://img.shields.io/github/v/release/FalloutBoy2099/AcmTools)](https://github.com/FalloutBoy2099/AcmTools/releases/latest)

## Features

- `ACM -> audio`: native ACM decoder, WAV writer, and FFmpeg export for MP3, AAC, FLAC, OGG/Vorbis, Opus, and WMA. APE/MPC export is attempted through FFmpeg when the local build provides an encoder.
- `audio -> ACM`: WAV reader or FFmpeg import for MP3, AAC, FLAC, APE, OGG/Vorbis, Opus, WMA, and MPC, mono or stereo (see below), and native ACM writer.
- `batch`: convert a whole folder (optionally recursive, with wildcards) in parallel; a failing file does not stop the others.
- `inspection`: header, duration and effective playback metadata (mono or stereo).

### Mono or stereo

Most speech and sound effects of the game are tagged stereo in their header although they are mono, so trusting the header makes them play twice as long. Only the music tracks are really stereo. By default (`--channels auto`) audio longer than 60 seconds is therefore treated as music and keeps its two channels, and everything shorter is mono. On the 2,501 ACM files of the game this rule classifies exactly the 19 music tracks as stereo (the longest speech file lasts 45 s); the short ambient loops `wind1.acm` and `wind2.acm` are the only exceptions and need `--channels stereo`.

`--channels mono` and `--channels stereo` force the choice, both when reading an ACM (`inspect`, `to-audio`) and when writing one (`to-acm`: a stereo source is mixed down to mono unless it lasts more than 60 s or `--channels stereo` is given). `--trust-header-channels` is kept as an alias of `--channels stereo`.

## Installing

Every [release](https://github.com/FalloutBoy2099/AcmTools/releases/latest) provides, for Windows, Linux and macOS on x64 and arm64 (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`):

| Download | What it is |
|---|---|
| `acmconverter-<version>-<system>-native.zip` | NativeAOT executable: starts in a few milliseconds, nothing to install |
| `acmconverter-<version>-<system>-standalone.zip` | Self-contained executable with the .NET runtime inside (about 30 MB) |
| `acmconverter-<version>-<system>-dotnet.zip` | Small executable (about 1 MB) that needs the .NET 10 runtime |

Other ways to get it:

```bash
dotnet tool install -g AcmTools.Cli                                  # the command is then: acmconverter
dotnet add package AcmTools                                          # the library
docker run --rm -v "$PWD:/data" ghcr.io/falloutboy2099/acmtools --help   # image with FFmpeg included, amd64 and arm64
```

Downloads are listed in `SHA256SUMS.txt` and carry a build provenance attestation (`gh attestation verify <file> --repo FalloutBoy2099/AcmTools`). The macOS executables are not notarized: if Gatekeeper blocks one, run `xattr -d com.apple.quarantine <file>`. FFmpeg is only needed to read or write formats other than WAV and ACM.

## CLI

```powershell
dotnet run --project .\AcmTools.Cli
dotnet run --project .\AcmTools.Cli -- inspect "C:\Fallout2\master.dat\sound\sfx\ammo.acm"
dotnet run --project .\AcmTools.Cli -- to-audio "C:\Fallout2\master.dat\sound\sfx\ammo.acm" ".\out\ammo.wav"
dotnet run --project .\AcmTools.Cli -- to-audio ".\voice.acm" ".\voice.ogg"
dotnet run --project .\AcmTools.Cli -- to-acm ".\voice.flac" ".\voice.acm" --rate 22050
dotnet run --project .\AcmTools.Cli -- to-audio "C:\Fallout2\master.dat\sound" ".\out" --format ogg --recursive
dotnet run --project .\AcmTools.Cli -- to-acm ".\wavs\*.wav" ".\acm" --snr 26 --jobs 4
dotnet run --project .\AcmTools.Cli -- to-acm ".\voice.wav" ".\voice.acm" --level 0
dotnet run --project .\AcmTools.Cli -- --lang fr inspect ".\voice.acm"
dotnet run --project .\AcmTools.Cli -- to-audio "C:\Fallout2\sound\music\wind1.acm" ".\wind1.wav" --channels stereo
```

Batch mode is used when the input is a folder or a file name with wildcards (`*`, `?`). The output is then a folder, the sub-folder layout is kept with `--recursive`, and existing files are skipped with `--no-overwrite`. `--jobs N` sets the number of parallel conversions. The exit code is 1 when at least one file failed, and Ctrl+C cancels cleanly (exit code 130).

Without arguments, the CLI opens an interactive Spectre.Console menu when the terminal is interactive. In redirected/non-interactive mode, it prints command-line help instead.

Like the reference decoder (libacm), a plain ACM read with `--channels stereo` is treated as stereo even when its header says mono, because the channel count they store is often wrong.

## Languages

The CLI (menu, prompts, help, errors, inspection table) follows the system UI language: English, French, Spanish, Italian, German, Portuguese, Polish, Czech, Swedish, Chinese, and Russian. English and French are complete; the other languages translate the interactive menu and common prompts and fall back to English for the rest. Use `--lang <code>` (`en`, `fr`, `es`, `it`, `de`, `pt`, `pl`, `cs`, `sv`, `zh`, `ru`) to force a language, or "Change language" in the interactive menu to switch at runtime. Texts live in `AcmTools.Cli/Localization`.

## Library

```csharp
using AcmTools.Conversion;

AcmConverter converter = new();
await converter.ConvertAcmToAudioAsync("voice.acm", "voice.wav");
await converter.ConvertAudioToAcmAsync("voice.mp3", "voice.acm");
```

`AcmDecodeOptions.Channels` and `AcmEncodeOptions.Channels` (`AcmChannelMode.Auto`, `Mono`, `Stereo`) choose the channels, with `StereoThresholdSeconds` (60 by default) as the music threshold.

Both methods accept a `CancellationToken`, run FFmpeg asynchronously (the process is killed on cancellation), and return the conversion steps as `ConversionStep` values (a `Kind` and its `Arguments`, with an English `Message`) so that front ends can translate them.

## Development and releases

GitHub Actions runs on every push and pull request (`.github/workflows`):

- `ci.yml` builds with warnings as errors on Linux, Windows and macOS, runs the unit tests when a `*.Tests.csproj` is present, publishes the CLI and runs `.github/scripts/smoke_test.py` on it (round trips mono and stereo, `--level 0` lossless, batch with a failing file, translations, and the FFmpeg formats on Linux). It also checks that the NativeAOT build works.
- `codeql.yml` runs the CodeQL security analysis; `dependabot.yml` keeps the NuGet packages, the actions and the Docker image up to date.
- `release.yml` publishes a release. Push a tag, or start the workflow by hand with a version number:

```bash
git tag v1.4.0 && git push origin v1.4.0       # v1.4.0-rc.1 gives a pre-release
```

It runs the CI first, then builds the 18 executables (6 systems x native, standalone and dotnet; the NativeAOT ones are compiled on a runner of each system), runs the smoke test on the ones that can run on the runner, packs the NuGet packages, builds the multi-architecture Docker image and creates the GitHub release with the checksums and attestations. Packages are also pushed to nuget.org when the `NUGET_API_KEY` secret is set, and to GitHub Packages. After the first Docker push, make the `acmtools` package public in the GitHub package settings if you want anyone to pull it.

`Publish.ps1` and `GitPublish.ps1` build the same executables locally with PowerShell.

## Notes

### Encoder

By default the integrated encoder works like the original Interplay one. The audio goes through the codec's sub-band transform (level 7: blocks of 16 rows of 128 columns, 2048 samples), each block gets one quantization step, and every column of coefficients is stored with the cheapest of the format's fillers. It is lossy: `--snr <dB>` (`AcmEncodeOptions.TargetSnrDb`, default 30) sets the signal-to-noise ratio the quantizer aims for in each block, and the measured ratio is usually 1 to 4 dB above it. On the files of the original game this gives 70% to 150% of the original size at the default (about 4 bits per sample for speech, the same as the original); `--snr 26` gets close to the original sizes for most sound effects.

`--level 0` writes lossless linear blocks instead: each block uses only as many bits per sample as it needs (digital silence costs almost nothing), and `--max-bits N` quantizes loud blocks to shrink the file. These files are several times bigger. `--level 1` to `10` pick another number of transform stages.

How it works, for the curious: the decoder's reconstruction is a cascade of 2-channel FIR filter banks (`y[2j] = s[j] + s[j-1] + 2 d[j-1]`, `y[2j+1] = 2 s[j] - d[j] - d[j-1]`) whose inverse is an IIR filter with one stable and one unstable pole (3 +- 2 sqrt 2). The encoder inverts it by splitting the recursion into its two modes, one run forward and the other backward, so that coefficients stay bounded (`AcmTools/Acm/AcmTransform.cs`). The decoder starts from a zero state, so the first few samples of a file that starts abruptly carry a little more error than the rest.