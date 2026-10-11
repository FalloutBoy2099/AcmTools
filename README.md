# AcmTools

.NET 10 library and CLI for Fallout/Interplay ACM audio conversion.

## Features

- `ACM -> audio`: native ACM decoder, WAV writer, and FFmpeg export for MP3, AAC, FLAC, OGG/Vorbis, Opus, and WMA. APE/MPC export is attempted through FFmpeg when the local build provides an encoder.
- `audio -> ACM`: WAV reader or FFmpeg import for MP3, AAC, FLAC, APE, OGG/Vorbis, Opus, WMA, and MPC, mono downmix by default, and native ACM writer.
- `inspection`: header and effective playback metadata.

Fallout ACM files are treated as mono by default because several files are tagged with channel counts that make playback too fast when trusted blindly.

## CLI

```powershell
dotnet run --project .\AcmTools.Cli
dotnet run --project .\AcmTools.Cli -- inspect "C:\Fallout2\master.dat\sound\sfx\ammo.acm"
dotnet run --project .\AcmTools.Cli -- to-audio "C:\Fallout2\master.dat\sound\sfx\ammo.acm" ".\out\ammo.wav"
dotnet run --project .\AcmTools.Cli -- to-audio ".\voice.acm" ".\voice.ogg"
dotnet run --project .\AcmTools.Cli -- to-acm ".\voice.flac" ".\voice.acm" --rate 22050
```

Without arguments, the CLI opens an interactive Spectre.Console menu when the terminal is interactive. In redirected/non-interactive mode, it prints command-line help instead.

Use `--trust-header-channels` only for non-Fallout ACM files where the channel count is known to be reliable.

## Languages

The CLI (menu, prompts, help, errors, inspection table) follows the system UI language: English, French, Spanish, Italian, German, Portuguese, Polish, Czech, Swedish, Chinese, and Russian. English and French are complete; the other languages translate the interactive menu and common prompts and fall back to English for the rest. Use "Change language" in the interactive menu to switch at runtime. Texts live in `AcmTools.Cli/Localization`.

## Library

```csharp
using AcmTools.Conversion;

AcmConverter converter = new();
await converter.ConvertAcmToAudioAsync("voice.acm", "voice.wav");
await converter.ConvertAudioToAcmAsync("voice.mp3", "voice.acm");
```

## Notes

The integrated ACM encoder writes a simple mono linear profile. It favors compatibility and predictable Fallout playback over matching the compression ratio of original Interplay assets.
