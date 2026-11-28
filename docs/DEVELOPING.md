# Developing Horse Personalities

The design is in [BUILD_PLAN.md](BUILD_PLAN.md), progress per phase in [phase-reports](phase-reports), the in-game test plan in [testing/home-test-plan.md](testing/home-test-plan.md).

## Repository layout

| Path | What it is |
| --- | --- |
| `src/HorsePersonalities.Core` | All logic: breed data, settings, JSON, timing, the `IGameApi` interface. No game references, so it runs in unit tests. |
| `src/HorsePersonalities.Game` | The script the game loads. Builds `HorsePersonalities.dll`. `RdrGameApi.cs` is the only place that calls natives; `Natives.cs` lists every one. |
| `src/HorsePersonalities.DevTools` | Phase 1 trigger spike and detectors. Builds `HorsePersonalities.DevTools.dll`. Never shipped. |
| `tests/HorsePersonalities.Tests` | xUnit tests for Core and the sheet converter. |
| `tools/sheet-to-json` | Turns the breed sheet's CSV export into `data/breeds.json`. |
| `data/sheet-export/` | The breed sheet's tabs as CSV: Breeds, Classes, Roller, Tiers, Prized, Behavior, Demeanor, Coats, Skills. |
| `data/*.csv` | Model lists kept in the repo: horse models, predators, dogs (Kicker targets), treats (names and item codes). |
| `data/test-personalities.json` | Test mode presets and which models get which. |
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
HorsePersonalities.DevTools.dll     (testing only: the F10 trigger menu)
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
2. Download the tabs you changed as CSV into `data/sheet-export/` under the tab's name (`Coats.csv`, `Behavior.csv`, `Skills.csv`...).
3. If a model belongs to a different breed, edit `data/horse-models.csv`.
4. Regenerate and check:

```bash
dotnet run --project tools/sheet-to-json
```

```bash
dotnet test tests/HorsePersonalities.Tests
```

The tool finds columns by their header names, so adding or moving columns in the sheet is fine. It refuses to write the file if a class, quirk or breed name doesn't match, and lists every problem at once. A test fails if `breeds.json` is out of date with the CSVs.

## Make a release

On Windows, with `ScriptHookRDRNetAPI.dll` in `lib/`:

```bash
powershell -ExecutionPolicy Bypass -File tools\make-release.ps1
```

(or `tools/make-release.sh` elsewhere). It checks `breeds.json`, runs the tests, builds the mod in Release and writes `dist/HorsePersonalities-<version>.zip` with the layout players copy into the game folder. The version is in `Directory.Build.props`. DevTools is never packed.

## License

[MIT](LICENSE). Red Dead Redemption 2 belongs to Rockstar Games; Script Hook RDR2 and ScriptHookRDR2DotNet V2 are their authors' own projects and are not included here.
