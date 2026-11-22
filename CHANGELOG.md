# Changelog

## Unreleased

### Phase 0: Setup and hello world
- Solution with Core, Game, DevTools, Tests and the sheet-to-json tool.
- The game script shows "Horse Personalities loaded" once the game is playable.
- On every mount it shows the horse's breed, model name, sex and bond level, and logs them.
- `HorsePersonalities.ini` is written with defaults on first run; bad values fall back to defaults with a log line.
- `breeds.json` generated from the breed sheet: 19 breeds, 89 models mapped to them, 57 other horse models listed by name.

### Phase 1: Trigger spike
- DevTools panel (F10) with 34 triggers to try on your horse: sounds, look-at, rear, buck, agitate, dismount, quick stop, bolt, kick candidates, a horse-action explorer, grazing scenarios, follow, stat rank changes, bond and stat readouts.
- Results recorded to `HorsePersonalities/phase1-results.csv` for the sheet's Quirks tab.
- Detectors for pat, brush and feed, calming, saddle swap, mission flag, screen fades, wild-horse breaking, taming state and bond XP.
