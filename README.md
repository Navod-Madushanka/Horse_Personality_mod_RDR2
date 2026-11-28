# Horse Personalities

A Red Dead Redemption 2 story-mode mod (PC) that gives every horse you own its own hidden personality, natural stat differences and learned skills, without changing how breeds work. An Arabian stays the fastest breed and a draft stays heavy, but no two horses, and no two playthroughs, feel the same.

There's no menu. A new horse looks exactly like a vanilla horse of its breed; you find out who it is by riding it.

## What it does

- **Four traits per horse:** Courage (wolves, gunfire, explosions), Nerves (spooking at nothing), Temper (bucking and kicking when pushed) and Bond affinity (how warm it is and how it takes care). Each breed leans its own way.
- **Quirks:** Chatterbox, Nosy, Shadow, Wanderer, Head-tosser, Picky eater and Kicker. A Kicker kicks whoever stands behind it, dogs included, in town too, and the law may come.
- **Every coat has a character.** All 91 horse coats in the game have a hand-written personality and signature habits: the White Arabian snorts at peasants in Saint Denis, the Silver Dapple Pinto strolls off if you leave it untied, the Grullo Dun Mustang guards your saddlebags while you're in the saloon, the Gold Turkoman rears in front of a crowd.
- **Care gets an answer:** affectionate horses nicker after a pat, aloof ones give nothing back, grumpy ones toss their heads at the brush.
- **Stats reveal themselves as you bond:** small health and stamina differences show on the stat bars at bond 2 and 3.
- **Horses learn:** gunfights make a horse steadier under fire and predators steadier around predators, each only in its own situation. Speed and acceleration grow as you gallop and sprint, and a horse kept for the whole story grows past its natural ceiling.
- **Wild horses** can be hard to break, rarely unbreakable. **Prized horses** (stable-bought, bonus coats, rare wild coats, community favorites) always roll good.
- **Nothing happens in story missions or cutscenes.**

## Requirements

- Red Dead Redemption 2 on PC, story mode.
- [Script Hook RDR2](https://www.dev-c.com/rdr2/scripthookrdr2/) by Alexander Blade (`ScriptHookRDR2.dll`, `dinput8.dll`).
- [ScriptHookRDR2DotNet V2](https://github.com/Halen84/ScriptHookRDR2DotNet-V2/releases), release **2.2.0 or newer**.

## Install

1. Install the two hooks above.
2. Copy the zip's `scripts` folder into your game folder (the one with `RDR2.exe`). You should end up with:

```
Red Dead Redemption 2\scripts\HorsePersonalities.dll
Red Dead Redemption 2\scripts\HorsePersonalities.ini
Red Dead Redemption 2\scripts\HorsePersonalities\breeds.json
Red Dead Redemption 2\scripts\HorsePersonalities\test-personalities.json
```

3. Start the game and load a save. "Horse Personalities loaded" appears once you're in. Your horses are rolled the first time the mod sees them.

The mod keeps your horses in `scripts\HorsePersonalities\horses.json` (with a backup) and logs to `scripts\HorsePersonalities\log.txt`.

## Settings

Everything is optional and set in `scripts\HorsePersonalities.ini`; delete a line to go back to its default. Press Insert in game to reload after editing.

| Setting | Default | What it changes |
| --- | --- | --- |
| `GoodHorseChance` | 80 | How often a horse rolls good overall (percent). |
| `UnbreakableChance` | 5 | Cap on unbreakable wild horses (percent). |
| `MaxStatRankChange` | 2 | How far natural stats can stray from the breed, in stat-bar segments (0 turns them off). |
| `SpookFrequency` | 1 | More or fewer random spooks (0 turns them off). |
| `QuirkCooldownSeconds` | 120 | The shortest gap between quirks on one horse. |
| `DisableDuringMissions` | true | Keeps behaviors out of story missions and cutscenes. |
| `RestoreVanillaStats` | false | Puts every horse's stats back to the game's own (for uninstalling). |
| `ResetAllHorses` | false | A fresh start: every horse is rolled again, once. |
| `DebugOverlay`, `DebugKey` | false, F9 | An on-screen panel with your horse's hidden values. |
| `TestPersonalities`, `TestPresetKey` | false, F11 | Test mode: fixed personalities instead of rolls, saved separately from your real horses. |
| `ExperienceSpeed` | 1 | Makes horses learn faster, for testing. |
| `LogLevel` | Info | How much goes in `log.txt` (Debug, Info, Warn, Error). |

A misspelled setting gets a warning in `log.txt` with the setting it probably meant.

## Uninstall

The game resets the mod's stat changes when you reload without it, so removing the files is enough. If you want to be thorough, set `RestoreVanillaStats = true`, ride or stand by each of your horses once, then delete the files.

## Known limits

- Story and gang horses stay vanilla, except Buell and Rachel. Mules and Online-only breeds stay vanilla.
- A script can't steer a horse, make it jump or change how it handles, so personalities show through sounds, looks, spooks, bucks, refusals, grazing, following and pace.
- Loved and ignored treats need each treat's item code in `breeds.json` (`data/treats.csv` in the source); until they're filled in, treat reactions fall back to the horse's general manner.

## For developers

Building, the breed sheet, tests and releases: [docs/DEVELOPING.md](docs/DEVELOPING.md). The design is in [docs/BUILD_PLAN.md](docs/BUILD_PLAN.md).

## License

[MIT](LICENSE). Red Dead Redemption 2 belongs to Rockstar Games; Script Hook RDR2 and ScriptHookRDR2DotNet V2 are their authors' own projects and are not included.
