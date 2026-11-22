# Horse Personalities

A Red Dead Redemption 2 story-mode mod that gives every horse you own a hidden personality, natural stat differences and learned skills, without changing how breeds work. PC, single player, built on ScriptHookRDR2DotNet V2.

The design is in [docs/BUILD_PLAN.md](docs/BUILD_PLAN.md). Progress per phase is in [docs/phase-reports](docs/phase-reports).

**Status:** Phase 0 (hello world) and Phase 1 (trigger spike) are written and compile against the hook's API, but have not been run in game yet. Nothing in the game changes yet: the mod only reports what it sees.

## Repository layout

| Path | What it is |
| --- | --- |
| `src/HorsePersonalities.Core` | All logic: breed data, settings, JSON, timing, the `IGameApi` interface. No game references, so it runs in unit tests. |
| `src/HorsePersonalities.Game` | The script the game loads. Builds `HorsePersonalities.dll`. `RdrGameApi.cs` is the only place that calls natives; `Natives.cs` lists every one. |
| `src/HorsePersonalities.DevTools` | Phase 1 trigger spike and detectors. Builds `HorsePersonalities.DevTools.dll`. Never shipped. |
| `tests/HorsePersonalities.Tests` | xUnit tests for Core and the sheet converter. |
| `tools/sheet-to-json` | Turns the breed sheet's CSV export into `data/breeds.json`. |
| `data/sheet-export/` | The sheet's Breeds and Classes tabs as CSV. |
| `data/horse-models.csv` | Every `a_c_horse_` model and the breed it belongs to. |
| `data/breeds.json` | Generated. Do not edit by hand. |
| `lib/` | Put `ScriptHookRDRNetAPI.dll` here (not committed; see [lib/README.md](lib/README.md)). |

Core's source files are compiled into the Game and DevTools DLLs rather than referenced, so the released mod is a single DLL.

## Build

You need Visual Studio 2022 or newer with the .NET desktop workload (or the .NET 8 SDK on its own).

1. Copy `ScriptHookRDRNetAPI.dll` from your ScriptHookRDR2DotNet V2 release into `lib/`. Use the one from the same release as the `.asi` in your game folder.
2. Open `HorsePersonalities.sln` and build (Release or Debug), or from a terminal:

```bash
dotnet build HorsePersonalities.sln -c Release
```

Output:

- `src/HorsePersonalities.Game/bin/Release/HorsePersonalities.dll` and `HorsePersonalities/breeds.json`
- `src/HorsePersonalities.DevTools/bin/Release/HorsePersonalities.DevTools.dll`

### Build straight into the game

Set an environment variable to your game's `scripts` folder and every build copies the DLLs and `breeds.json` there:

```bash
setx RDR2_SCRIPTS_DIR "C:\Program Files (x86)\Steam\steamapps\common\Red Dead Redemption 2\scripts"
```

Restart Visual Studio after setting it. In game, press Insert to reload scripts.

## Install for testing

Into `Red Dead Redemption 2\scripts\`:

```
HorsePersonalities.dll
HorsePersonalities.DevTools.dll     (Phase 1 testing only)
HorsePersonalities\breeds.json
```

On first run the mod writes `HorsePersonalities.ini` and `HorsePersonalities.DevTools.ini` next to the DLLs, and logs to `HorsePersonalities\log.txt`.

## Run the tests

No game needed. From Visual Studio's Test Explorer, or:

```bash
dotnet test tests/HorsePersonalities.Tests
```

## Change breed data

1. Edit the [breed sheet](https://claude.ai/artifact/37Yfwtn7EsEtr69NiM3kyp).
2. Download its Breeds and Classes tabs as CSV into `data/sheet-export/` as `Breeds.csv` and `Classes.csv`.
3. If a model belongs to a different breed, edit `data/horse-models.csv`.
4. Regenerate and check:

```bash
dotnet run --project tools/sheet-to-json
```

```bash
dotnet test tests/HorsePersonalities.Tests
```

The tool finds columns by their header names, so adding or moving columns in the sheet is fine. It refuses to write the file if a class, quirk or breed name doesn't match, and lists every problem at once. A test fails if `breeds.json` is out of date with the CSVs.
