# Phase 2 and Phase 4 report: horse identity and the save file

Written Nov 24, 2026. Built before Phase 1 passed, by Navod's decision (the plan's rule 1). The Core logic is done and unit tested; both in-game checks wait until Phase 1 passes. If Phase 1 shows the saddle-swap or wild-horse detectors don't work, only the game wiring changes.

## What was built

**Phase 2: who gets a personality, and keeping track of it**

- **Fingerprint** (`Core/Identity/HorseFingerprint.cs`): model, sex and coat, written as `a_c_horse_turkoman_gold|male|default`. The game has no horse id a script can read, and no way to read a horse's current coat outfit, so the coat is `default` for now.
- **Registry** (`Core/Identity/HorseRegistry.cs`) applies the plan's rules:
  - A horse with no breed row gets nothing (vanilla in v1).
  - Mounting someone else's horse gives it a **temporary** personality for the session, kept by the horse's handle. It's never saved.
  - When your saddle goes on a horse, that same personality becomes **permanent**, kept by fingerprint. A wild horse keeps its Wild origin when you saddle it.
  - A saddle horse the mod hasn't seen before (your starting horse, a new purchase) is rolled once, as a stable horse.
  - Two owned horses with the same fingerprint share a personality, and the log warns once when that happens.
- **Game wiring** (`HorsePersonalitiesScript.cs`):
  - On mount: the horse's fingerprint, whether it's your saddle horse, and Wild origin if the game sent `EVENT_PLAYER_MOUNT_WILD_HORSE` for it. The taming state is logged as well, to check in game whether it also tells wild horses apart.
  - Every 250 ms: a check for a new saddle horse, which covers saddle swaps, purchases and your horse on load.
  - The overlay now shows the fingerprint and whether the personality is temporary or permanent.
- **DevTools trigger 35, *Read horse identity***: prints model, sex, the model's outfit count, meta ped type, taming state, and the saddle, active and temporary horse handles. Run it on two horses of the same breed and coat to see whether anything tells them apart.

**Phase 4: the save file**

- **`horses.json`** in the plan's format, one record per permanent horse: breed, origin, status, quality, traits, quirks, natural stats, breaking, plus the roll's `seed` and `rollerVersion`. Numbers are saved at full precision, so a reload gives back exactly the same horse. Skills join in Phase 8.
- **Written only on events:** when a horse becomes permanent, when a kept horse is seen on a new day (its `lastSeen` date), and after an upgrade or a recovery from the backup. It was also meant to write when the script stops, but in game (Nov 25) the hook never tells the script the game is exiting: no "Script stopped" line reached the log in any session. Never every frame.
- **Safe writes:** each write goes to a temporary file first and then replaces `horses.json`, so a crash mid-write leaves the old file. The previous file is kept as `horses.backup.json`.
- **Recovery:** a broken or unreadable `horses.json` is kept as `horses.broken-<time>.json` (never deleted) and the backup loads instead. If both are broken, the mod starts empty without crashing. One bad record is skipped with a log line and the rest load.
- **`ResetAllHorses = true`:** on the next load the save is moved aside as `horses.reset-<time>.json` and the mod starts empty. The setting then turns itself back to false in the INI, so it only resets once.

## Unit test results

`dotnet test tests/HorsePersonalities.Tests`: **307 passed, 0 failed.**

New tests cover the plan's Phase 2 and Phase 4 checks without the game:

- A mounted NPC horse gets a temporary personality, a reload forgets it, and saddling it makes it permanent with the same values.
- The same horse comes back with a new handle after a reload.
- Two coats of one breed get different ids, and the starting horse is rolled once.
- A round trip through the file keeps every value exactly. 5 horses survive 3 save-and-load sessions.
- A broken, empty or wrong-version file recovers from the backup, and a deleted file falls back to the backup.
- A broken file plus a broken backup starts empty, and one bad record doesn't sink the others.
- Reset moves the save aside, and the INI edit turns reset off without touching other lines or comments.

The 5 Phase 3 preview tests went away with the preview code; the registry replaced it.

## Known issues and things only the game can answer

1. **Is the saddle horse what we think?** "Owned" means `_GET_SADDLE_HORSE_FOR_PLAYER`. Phase 1's saddle-swap detector checks it changes when you swap saddles. If it doesn't, the trigger for "permanent" has to move.
2. **Wild horses.** Wild origin depends on `EVENT_PLAYER_MOUNT_WILD_HORSE`, which Phase 1 checks. If it never fires, the logged taming state may do instead.
3. **Identical horses collide.** Two owned horses of the same model and sex share one personality until the game shows a way to tell them apart. DevTools trigger 35 looks for one.
4. **Game saves aren't detected.** The plan also wants a write on game save. No native for that is known, so the mod writes on its own events instead (not on exit; see above). Quitting loses at most the last session's `lastSeen` dates, never a personality: those are written the moment they become permanent.
5. **Two playthroughs share one file.** There's no way yet to tell save slots apart. `ResetAllHorses` covers a new playthrough, as the plan says.

## In-game test steps (after Phase 1 passes)

1. Build and copy `HorsePersonalities.dll`, `HorsePersonalities.DevTools.dll` and `HorsePersonalities/breeds.json` into `scripts`, as before. Back up your save.
2. Load your save and press **F9**. Mount your own horse: the overlay says **permanent**, and `horses.json` now exists in `scripts\HorsePersonalities\`.
3. Mount an NPC's horse: the overlay says **temporary**. Note its values. Reload (Insert, or quit and load): mount it again and the values are different.
4. Mount an NPC's horse again, put your saddle on it at a stable or with the saddle prompt: the overlay says **permanent**, with the same values as before saddling. Reload: same values.
5. Two horses of one breed but different coats should show different fingerprints.
6. Quit and reload 3 times: every owned horse shows the same values each time.
7. Quit, open `horses.json` in Notepad, delete a few characters to break it, and load again: no crash, the log says the backup loaded, and a `horses.broken-...json` file appears.
8. Set `ResetAllHorses = true` in `HorsePersonalities.ini` and load: your horses roll new values, a `horses.reset-...json` file appears, and the INI line is back to false.
9. Run DevTools trigger 35 on two horses of the same breed and coat, and send me the lines.

**Done when** (Phase 2) the overlay shows a stable fingerprint for every owned horse across 3 reloads, and (Phase 4) 5 owned horses keep their personalities across 3 sessions.
