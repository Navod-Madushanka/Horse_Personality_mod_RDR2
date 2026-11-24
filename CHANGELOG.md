# Changelog

## Unreleased

- Handling is now each breed's stat tier (Heavy, Standard, Race, Elite); breeds.json and the sheet converter reject any other value.
- MIT license.
- Decided: horses with no breed row (gang and story-only models, mules, Online-only breeds) stay vanilla in v1.

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
