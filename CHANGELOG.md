# Changelog

## Unreleased

- Handling is now each breed's stat tier (Heavy, Standard, Race, Elite); breeds.json and the sheet converter reject any other value.
- MIT license.
- Decided: horses with no breed row (gang and story-only models, mules, Online-only breeds) stay vanilla in v1.

### Phase 4: Save file (built ahead of Phase 1, by decision)
- `horses.json` in the plan's format, written when a horse becomes permanent, when a kept horse is seen on a new day, and when the script stops. Full precision, so a reload gives back the exact horse.
- Writes go through a temporary file; the previous file is kept as `horses.backup.json`.
- A broken file is kept as `horses.broken-<time>.json` and the backup loads; both broken starts empty without crashing.
- `ResetAllHorses` moves the save aside as `horses.reset-<time>.json` and turns itself back to false.

### Phase 2: Horse tracking and identity (built ahead of Phase 1, by decision)
- Fingerprint of model, sex and coat (coat is `default` until the game shows a way to read it).
- Mounting someone else's horse gives a temporary personality; your saddle makes it permanent with the same values. A new saddle horse, including the starting horse, is rolled once.
- The overlay shows the fingerprint and whether the personality is temporary or permanent; it replaces the Phase 3 preview.
- DevTools trigger 35, *Read horse identity*, for checking what the game exposes.

### Phase 3: Personality roller (Core built ahead of Phase 1, by decision)
- Rolls good or bad, four traits around the breed's leanings, 1 or 2 quirks (likely quirks weighted), natural stats within the tier caps, and breaking difficulty for wild horses.
- Same seed, same horse: a SplitMix64 generator with a fixed number of draws per roll.
- Tuning numbers come from two new sheet tabs, Roller and Tiers; breeds.json is now schema 2.
- Debug overlay (F9, off by default) shows each mounted horse's breed, tier, sex, bond and a preview roll, plus the live tick time. Preview rolls are not saved yet (Phases 2 and 4).
- 186 new unit tests, including 10,000 rolls per breed.

### Phase 0: Setup and hello world (passed in game, v0.1)
- Fixed in game: the loading-screen native reads true all session, so the banner and mount reports never ran. Mount tracking no longer waits for the banner; the banner waits for the player to be playing and the screen faded in, with a 30 s fallback.
- Solution with Core, Game, DevTools, Tests and the sheet-to-json tool.
- The game script shows "Horse Personalities loaded" once the game is playable.
- On every mount it shows the horse's breed, model name, sex and bond level, and logs them.
- `HorsePersonalities.ini` is written with defaults on first run; bad values fall back to defaults with a log line.
- `breeds.json` generated from the breed sheet: 19 breeds, 89 models mapped to them, 57 other horse models listed by name.

### Phase 1: Trigger spike
- DevTools panel (F10) with 34 triggers to try on your horse: sounds, look-at, rear, buck, agitate, dismount, quick stop, bolt, kick candidates, a horse-action explorer, grazing scenarios, follow, stat rank changes, bond and stat readouts.
- Results recorded to `HorsePersonalities/phase1-results.csv` for the sheet's Quirks tab.
- Detectors for pat, brush and feed, calming, saddle swap, mission flag, screen fades, wild-horse breaking, taming state and bond XP.
