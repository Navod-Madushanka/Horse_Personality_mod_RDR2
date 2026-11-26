# Changelog

## Unreleased

### Phase 6: core behaviors (built Nov 26, 2026)
- Threat spooks from Courage (predators, gunfire, explosions), each threat judged once; rearing, throwing the rider, bolting or watching, by Courage and Temper.
- Random spooks from Nerves, weighted by night, gallop and bond, times `SpookFrequency`: noticeable but rare.
- Bucks and refusals from Temper when pushed (low-stamina spur, long sprint, hard stop); kicks when you linger behind.
- Bond XP scaled by Bond affinity, slower from bond 3 to 4 for good (0.75) and prized (0.5) horses, and for Hard wild horses (0.8).
- Hard wild horses throw you twice more; Unbreakable ones every attempt.
- Cooldowns per behavior and a 20 s gap; everything held during missions and cutscenes.
- New sheet tab Behavior (37 settings) and `data/predator-models.csv`; breeds.json schema 5.

### Phase 5: stats and the bond reveal, and prized horses (built Nov 26, 2026)
- **Prized horses:** stable horses and the sheet's new Prized tab (six PC bonus coats so far) always roll good with no trait flaw; prized wild horses break Hard more often but are never Unbreakable. Shown as "(prized)".
- **Stat bars:** health and stamina base ranks get the natural difference: the first rank at bond 2, a second rank (only -2 horses) at bond 3. The bonus rank (tack) is never touched. Originals are stored in `horses.json` and re-applied after a reset.
- `RestoreVanillaStats` puts each horse back to its original for an uninstall. `RevealPerBondLevel` is retired.
- DevTools trigger 42 steps the bond level, for testing the reveal.
- breeds.json schema 4 (prized models; five new Roller tab settings). Roller version 3.

### In-game results (Nov 26, 2026)
- **Phase 1 passed.** Wanderer works as wander-then-graze; mission flag and every cutscene detector work; Show-off was cut.
- **Phases 2, 3 and 4 passed in game:** stable ids and personalities across 3 reloads, the schema 1 save upgraded to ranks, a broken save recovered from the backup.

### Plan changes, not built yet (Nov 25, 2026)
- **Prized horses:** stable-bought, special, rare and community-favorite horses always roll good, with flaws only as fun quirks. Prized wild horses break Hard more often but are never Unbreakable. The list of special, rare and favorite models lives in the breed sheet's new Prized tab.
- **Long-use growth:** any horse ridden and bonded with for a long time grows speed and acceleration 1 rank past its natural ceiling (within its tier). Community favorites grow to 2 ranks above their breed, past the tier. Earned through bond levels and riding time (Phase 8).
- **Bond 4 is earned:** levels 1 to 3 at the normal pace; the climb from 3 to 4 is slower for good horses (0.75) and slowest for prized ones (0.5). Proposed numbers in the sheet's Roller tab.

### After Phase 1 testing (Nov 25, 2026)
- **Natural stats are whole ranks.** The game's stats are ranks 0-9, one stat-bar segment each, so a fraction of a rank would never show. Each stat now rolls a rank offset: mostly 0, about a third +1, about one in seven -1, under 1% -2. Every tier climbs at most 1 rank; Elite (Arabians) never drops.
- `MaxStatVariance` (percent) is replaced by `MaxStatRankChange` (ranks, default 2; 0 turns natural stats off). An old INI key is ignored with a log warning.
- The sheet's Tiers tab holds whole ranks; Roller tab *Stat spread* 0.6 and *Stat quality shift* 0.3 are in ranks. breeds.json is schema 3.
- **Show-off is cut from v1**: the game has no piaffe a script can start. Removed from the quirk list and from the Andalusian, Kentucky Saddler and Turkoman likely quirks.
- **horses.json schema 2.** A schema 1 file is upgraded on load: traits and quirks kept, natural stats re-rolled from each horse's seed as ranks, then written back. Tested on the Nov 24 in-game save.
- Roller version 2: the same seed still gives the same traits; natural stats and quirk picks follow the new rules.
- DevTools triggers 36-41 for retesting Wanderer: grazing and resting with their conditional animation named, wander nearby, wander freely, and wander-then-graze.

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
