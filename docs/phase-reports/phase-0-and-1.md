# Phase 0 and Phase 1 report

Written Nov 22, 2026, before any in-game testing. Both phases need you in game to be marked done.

## What was built

**Phase 0: Setup and hello world**

- The solution from the plan: `HorsePersonalities.Core` (no game references), `HorsePersonalities.Game` (builds `HorsePersonalities.dll`), `HorsePersonalities.DevTools`, `HorsePersonalities.Tests` and `tools/sheet-to-json`.
- The game script waits for the loading screen to end, then shows **"Horse Personalities loaded (19 breeds)"**.
- On every mount it shows **breed - model - sex - bond N**, for example `Turkoman - a_c_horse_turkoman_gold - female - bond 2`, and writes the same line to `log.txt` with the bond XP and model hash.
- Horses with no breed show `No breed (Mule)`, `No breed (Gang member's horse ...)`, or `Unknown breed - 0x1234ABCD` for a model nobody has listed.
- Settings: `HorsePersonalities.ini` is written with every default on first run. Missing or bad values fall back to the default with a log line.
- Every feature runs inside a guard that catches errors, logs them and turns off only that feature after 3 failures.
- Per-frame cost is measured and logged once a minute at the Debug log level.

**Breed data**

- The sheet's Breeds and Classes tabs are saved as CSV in `data/sheet-export/`.
- `data/horse-models.csv` maps all 146 `a_c_horse_` models from femga's ped list. 89 map to the 19 breeds by name. The other 57 are kept by name so the mod can still say what they are.
- `tools/sheet-to-json` builds `data/breeds.json` from those three files, using the same validation the mod runs on load.
- Learning rates follow the sheet's formula and match it for every breed (Turkoman 1.17 / 1.35, Mustang 0.81 / 1.35, Missouri Fox Trotter 1.17 / 0.99). No breed overrides it today.

**Phase 1: Trigger spike**

- `HorsePersonalities.DevTools.dll`, a separate script. **F10** opens a panel of 34 triggers. **PageUp / PageDown** select, **End** fires at your horse, **Home** records *Works*, **Delete** records *Doesn't work*. All keys can be changed in `HorsePersonalities.DevTools.ini`.
- The target horse is the one you ride, otherwise your last mount, otherwise your saddle horse.
- Each trigger shows what it is for in the plan and how to test it. Results go to `scripts/HorsePersonalities/phase1-results.csv`.
- Detectors run the whole time and print the last 6 things they saw on the panel (and everything to `log.txt`): pat, brush and feed; calming; saddle swap; mission flag; screen fade; player control; wild horse mounted, breaking and broken; taming state; bond rank and bond XP changes.

| Plan item | Triggers |
| --- | --- |
| Whinny or snort | 8 sound names (editable list) |
| Look at a target | Look at you; look at nearest NPC |
| Rear | Horse action 1; horse action 5 |
| Buck you off / force a dismount | Throw rider (actions 2 and 10); agitate and throw; force dismount |
| Kick | Combat task on nearest NPC; horse action explorer (ids 0-20) |
| Graze | 5 scenarios (grazing, resting, drinking, wallowing) |
| Restless state / raise agitation | Agitate, keep rider |
| Change a stat rank | Speed, Health, Stamina rank +2 and restore |
| Read bond level and bond XP | Read bond; read all stats |
| Extra, for later phases | Quick stop, skid, bolt away (spooks); follow you (Shadow) |

## Unit test results

`dotnet test tests/HorsePersonalities.Tests`: **87 passed, 0 failed.**

They cover the JSON reader and writer, the game's hash function (checked against a known Rockstar value), loading and validating breeds.json, learning rates (including the plan's Turkoman and Mustang numbers), the sheet converter (including a check that `breeds.json` matches the CSVs), settings defaults and bad values, intervals, cooldowns, the error guard, mount tracking and the Phase 0 subtitle text.

The Game and DevTools projects compile with no warnings against ScriptHookRDR2DotNet V2's API, built from its current source. That proves the code matches the hook's API, not that the natives behave as hoped.

## Known issues and things only the game can answer

1. **Not run in game.** Everything game-side is untested.
2. **Hook version.** I compiled against the hook's current source (API 2.2.0.0). Build against the `ScriptHookRDRNetAPI.dll` from the release you install, so the two match. If that release is older and the build fails, tell me the error.
3. **Bond level scale.** Bond comes from `GET_ATTRIBUTE_RANK(horse, PA_BONDING)`. I don't know yet whether a level 1 horse reads 0 or 1. Phase 0's subtitle shows the raw number; compare it with the game's bond level.
4. **Sound names are guesses.** The native DB doesn't list horse sound names. Add names to `Vocalizations` in the DevTools ini to try more without rebuilding.
5. **No known kick native.** The candidates are a combat task against an NPC and the horse-action explorer. A kick near people may count as a crime; test it away from towns.
6. **No known piaffe trigger** (Show-off). The explorer may find one.
7. **Pat detection.** Brush and feed have known interaction names. The pat may arrive under a name I don't have, in which case the detector prints a hex hash. Send me the hash and I'll name it.
8. **Cutscenes.** The RDR2 native DB has no "is a cutscene playing" call. The mission guard will use the mission flag, screen fades and player control. The detectors print all three so you can see whether they cover missions and cutscenes.
9. **Native arguments.** Some arguments aren't documented in the DB. They're marked "unverified" in `RdrGameApi.cs` and taken from RedM scripts. A trigger that does nothing may just need different arguments, so note "does nothing" rather than only "doesn't work".
10. **Stat changes may be saved by the game.** The stat triggers change the horse's bonus rank. Press each one twice to restore it, and use a backed-up save.

## Questions for you

1. **Stat tier.** Answered: the breed's Handling class (Heavy, Standard, Race, Elite) is its tier. breeds.json now rejects any other value.
2. **Demeanor.** The sheet's Demeanor column is a sentence. Phase 7 needs it as behaviors the code can act on (for example "fidgets when kept standing"). That can wait until Phase 7, but it will need new columns.
3. **Special models.** 25 models have no breed: gang horses (`a_c_horse_gang_*`), Buell, Eagle Flies' horse, the Murfree Brood mangy horses, `a_c_horse_winter02_01` and the epilogue horses. This ties into the plan's open question about story horses. For now they get no personality.
4. **Online-only breeds.** Breton, Criollo, Gypsy Cob, Kladruber and Norfolk Roadster exist as models but aren't in the sheet, so they get no personality. Fine for v1?
5. **New setting.** I added `LogLevel` (Debug, Info, Warn, Error; default Info) to the INI. It's in the plan's settings table now.
6. **License.** Answered: MIT.

## In-game test steps

### Before you start

1. Finish the plan's *Before coding* checklist, including backing up your save folder.
2. Copy `ScriptHookRDRNetAPI.dll` from your hook release into `lib/`, then build in Visual Studio (Release).
3. Copy into the game's `scripts` folder: `HorsePersonalities.dll`, `HorsePersonalities.DevTools.dll` and the `HorsePersonalities` folder with `breeds.json`. (Or set `RDR2_SCRIPTS_DIR` and the build does it; see the README.)

### Phase 0

1. Start the game and load your test save. When the loading screen ends you should see **Horse Personalities loaded (19 breeds)**.
2. Press **F8**: the hook's console should list both scripts with no errors.
3. Mount your own horse. Check the subtitle: is the breed right? Is the bond number the game's bond level, or one lower?
4. Press **Insert** to reload scripts. The loaded message shows again, and mounting still works.
5. Mount two more horses of different breeds (buy one, or spawn one with Rampage). Check each breed.
6. Ride for 10 minutes. No crash, no stutter.
7. Send me `scripts/HorsePersonalities/log.txt`.

**Done when** the right breed shows for 3 different horses. Also tell me what your starting horse shows: if it says *Unknown breed* with a hex number, send the number.

### Phase 1

Use a test save away from towns and lawmen.

1. Press **F10** to open the panel. Each row says what it's for and how to test it.
2. For each trigger: set up as the *How* line says, press **End**, then **Home** if the behavior happened or **Delete** if not. Note anything odd: a different animation, a delay, or the effect not ending.
3. For the stat triggers, check the horse's stat bars after the first press, press again to restore, then save and reload to see whether the change stuck.
4. For the explorer, press **End** repeatedly and write down any id that does something new: a kick, a piaffe, head tossing.
5. Detectors, with the panel open so you can watch the list:
   - Pat, brush and feed your horse. Each should print an *Interaction* line. Note any that print a hex hash.
   - Calm your horse while it's spooked.
   - Change saddles at a stable: a *Saddle horse changed* line.
   - Start a story mission and watch the *Mission flag* line. Watch a cutscene and see which of *Mission flag*, *Screen faded* and *Player control* change.
   - Mount and break a wild horse: wild horse, breaking and taming lines.
6. Send me `phase1-results.csv`, `log.txt` and your notes. I'll fill the sheet's Quirks tab, or you can.

**Done when** every trigger has a result. Before Phase 2, I'll propose a replacement or a cut for each failure.
