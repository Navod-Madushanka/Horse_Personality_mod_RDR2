# Phase 3 report: personality roller

Written Nov 24, 2026. Built before Phase 1 passed, by Navod's decision (recorded in the plan's rule 1). The Core work is done and unit tested; the in-game check (the overlay) waits until Phase 1 passes.

## What was built

- **The roller** (`src/HorsePersonalities.Core/Rolling/PersonalityRoller.cs`). From a breed, an origin and a seed it rolls:
  1. **Good or bad**, with `GoodHorseChance`.
  2. **Traits.** Each of Courage, Nerves, Temper and Bond affinity strays from the breed's leaning, toward the good side for a good horse and the bad side for a bad one, plus a random spread. A good horse keeps at most one trait clearly on the bad side of its breed; a bad horse at most one on the good side. Values stay on the sheet's -2 to +2 scale.
  3. **Quirks:** 1, or 2 with a 35% chance. The breed's likely quirks are 4 times as likely as the others; every quirk is still possible.
  4. **Natural stats** for speed, acceleration, health and stamina, as a fraction of the breed baseline. Good horses lean up, bad horses down, and nothing passes its tier's cap or `MaxStatVariance`.
  5. **Breaking difficulty**, wild horses only: the horse's own Temper minus Bond affinity. 1.5 or more breaks Hard. 2.5 or more is Unbreakable if it also wins the `UnbreakableChance` roll, otherwise Hard.
- **Same seed, same horse.** The generator is SplitMix64, not `System.Random`, so a seed gives the same horse on every .NET version, and every roll makes the same number of draws.
- **Tuning in the sheet.** Two new tabs in the breed sheet, **Roller** and **Tiers**, hold every number above. They're exported to `data/sheet-export/` and go into `breeds.json`, which is now schema 2. A bad value anywhere fails the converter and the mod's loader with a message naming it.
- **Debug overlay** (`F9`, or `DebugKey`; off by default, or on with `DebugOverlay = true`). It shows the mounted horse's breed, tier, model, sex and bond level, its preview roll and the live tick time. Each roll is also written to `log.txt`. Preview rolls are kept for the session only and rolled as wild so the breaking difficulty shows. Phase 2 adds identity and Phase 4 saves rolls.

## What the rolls look like

10,000 rolls per breed with the default settings:

| Breed | Average courage / nerves / temper / bond | Leanings | Wild: Hard / Unbreakable |
| --- | --- | --- | --- |
| Arabian | -0.66 / +1.53 / -0.31 / +1.26 | -1 / +2 / 0 / +1 | 0.8% / 0% |
| Ardennes | +1.84 / -1.25 / -1.25 / +0.31 | +2 / -1 / -1 / 0 | 0.8% / 0% |
| Mustang | +1.27 / +0.68 / +0.68 / -0.67 | +1 / +1 / +1 / -1 | 40% / 0.9% |
| Tennessee Walker | +0.32 / -1.25 / -1.84 / +1.26 | 0 / -1 / -2 / +1 | 0% / 0% |
| Turkoman | +1.27 / -0.30 / +0.68 / +0.31 | +1 / 0 / +1 / 0 | 16% / 0.3% |

- Averages sit about 0.3 on the good side of the breed, because 80% of horses roll good.
- A brave Morgan (courage 1.5 or more) is about 4% of Morgans; a jumpy Ardennes (nerves +1 or more) about 0.2%. Rare but possible, as the plan asks.
- Unbreakable is much rarer than 5%: it needs both a bad score and the 5% roll. Even Mustangs, the hardest breed, are under 1%.

## Unit test results

`dotnet test tests/HorsePersonalities.Tests`: **281 passed, 0 failed** (186 new).

Everything the plan's Phase 3 test asks for, with 10,000 rolls per breed: the good share stays within 2 points of `GoodHorseChance`, average traits follow the breed's leanings, no stat passes its tier cap, unbreakable stays under `UnbreakableChance`, and the same seed gives the same roll. Also tested: at most one flaw per good horse, only wild horses get a breaking difficulty, 1 or 2 distinct quirks with likely ones more common, `MaxStatVariance` overriding a wider tier cap, and two fixed seeds that pin the algorithm so it can't change by accident.

The Game project still builds with no warnings against the hook API.

## Known issues

1. **Tier caps are placeholders.** Whether a bad Arabian (-5% speed) still beats a good Thoroughbred (+8%) depends on the game's real baseline stats. Phase 5 reads them and will tune the Tiers tab.
2. **Unbreakable may be too rare.** At under 1% even for Mustangs, a player could break dozens of wild horses without meeting one. If you want it closer to the 5% setting, lower *Unbreakable break score* in the Roller tab (2.0 roughly doubles it). The acceptance test forces one with a DevTools key anyway.
3. **Preview rolls are not saved.** Every reload rolls new values. That's expected until Phases 2 and 4.

## In-game test steps (after Phase 1 passes)

1. Build and copy `HorsePersonalities.dll` and `HorsePersonalities/breeds.json` into `scripts`, as before.
2. Load your test save and press **F9**: the overlay opens in the top-left corner.
3. Mount three horses of different breeds. For each, check that the overlay shows the right breed and tier, a good or bad roll, four traits, 1 or 2 quirks, four natural stats and a "Breaking if wild" line.
4. Mount a mule or a gang horse: the overlay should say it stays vanilla.
5. Leave the overlay open while riding for a few minutes. The tick time should stay under 1 ms.
6. Send me `log.txt`: it holds every preview roll.

**Done when** the overlay shows rolled values in game.
