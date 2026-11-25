# Horse Personalities Mod — Build Plan

Nov 22, 2026 · @Navod

## Overview and scope

The mod gives every horse you own in Red Dead Redemption 2 its own hidden personality, natural stat differences and learned skills, without breaking how breeds work. An Arabian stays the fastest breed and a draft stays heavy, but no two horses, and no two playthroughs, feel the same.

**v1 scope**

- PC, story mode only, single player. Script Hook already blocks Online.
- A C# script mod on ScriptHookRDR2DotNet V2, with C++ as the fallback if the C# hook fails on the current game build.
- No custom menus or UI. The player reads a horse through its behavior and the game's own stat bars.
- Breed data comes from the [breed table sheet](https://claude.ai/artifact/37Yfwtn7EsEtr69NiM3kyp), exported to JSON.

**Not in v1**

- Size or looks changes (stats and behavior only).
- Training design (skills grow from experience; deliberate training comes later).
- Red Dead Online, RedM, consoles.
- New animations or models. Everything uses animations and behaviors the game already has.

**Who does what**

- **You:** install and test in game, tune numbers in the sheet, record what works, decide when a phase is done.
- **Coding agent:** writes the code, unit tests and docs, one phase at a time, and asks instead of guessing how a native behaves.

## Locked design decisions

Every horse is built from three layers: the breed sets the limits, the individual roll sets the personality, and experience adds learned skills on top. These rules are decided; change them here first, then in code.

### Three layers

1. **Breed** (vanilla, untouched): stats tier (its Handling class), and default demeanor. The Arabian stays fastest, drafts stay heavy.
2. **Individual** (rolled once, hidden):
   - **Traits:** Courage (big threats), Nerves (small or random things), Temper (bucking, refusing), Bond affinity (how fast it bonds).
   - **Natural stats:** whole-rank ups and downs in speed, acceleration, health and stamina (usually 0 or 1 rank, rarely 2). They set the horse's ceiling.
   - **Quirks:** 1 to 2 habits from the list of 7 (Chatterbox, Kicker, Head-tosser, Nosy, Shadow, Wanderer, Picky eater). Show-off was cut on Nov 25, 2026: the game has no piaffe a script can trigger.
3. **Experience** (learned, separate from personality): gunfire, predators, handling, speed and acceleration. Learning speed comes from the breed's class.

### How traits are rolled

- Each horse first rolls **good or bad**. `GoodHorseChance` (default 80%) sets the odds.
- Traits then lean that way, with the breed's leanings from the sheet on top. A good horse can still have one flaw, and a bad horse one good trait.
- Breed leanings shift the odds, they don't lock the result. A brave Morgan or a jumpy Ardennes is rare but possible.
- Quirk odds are raised for the breed's likely quirks.
- "Leaning that way" is measured against the breed: a good horse sits on the good side of its breed's leanings, with at most one trait noticeably on the bad side; a bad horse the reverse. A jumpy Thoroughbred is still a good Thoroughbred if it is less jumpy than most.
- Every roll comes from a seed, so the same seed always gives the same horse. The seed is picked at random the first time a horse is rolled.
- The spreads, shifts and odds the roller uses live in the sheet's Roller tab, not in code.

### Prized horses

Decided Nov 25, 2026. A player who pays for a horse, hunts down a rare one or picks a community favorite should never be let down by its personality. These are **prized horses**, and they trade a better personality for a slower bond.

- **Who is prized:**
  - every horse bought from a stable (origin Stable, which also covers horses the player already owned when the mod first saw them);
  - special and rare horses (for example the bonus and pre-order coats);
  - community favorites.
  Special, rare and favorite horses are a list of models in the breed sheet's Prized tab, never in code. A prized model is prized however the player got it: bought, broken in the wild, or taken.
- **A better personality:** a prized horse always rolls good, and none of its traits sits on the bad side of its breed. The "one flaw" a normal good horse may have is not allowed.
- **Its flaws are only fun ones:** quirks such as wandering off, calling out a lot, kicking people or following you when you walk away. Never a serious problem like bucking you off for no reason.
- **The cost, slower bonding:** a prized horse earns bond XP more slowly (Roller tab: *Prized bond speed*).
- **The cost, harder to break:** a prized wild horse's breaking score is raised (Roller tab: *Prized break score bonus*), so it breaks Hard more often. It is never Unbreakable: an unusable favorite would be the very disappointment this rule prevents.
- Breed leanings still apply. A prized Arabian is still more alert than most breeds, just better than an average Arabian.

### How the player discovers it

- There is no menu. A new horse looks exactly like a vanilla horse of its breed.
- Behavior traits show from day one: you learn a horse is nervous when it spooks.
- Health and stamina differences appear as the bond grows (25% per bond level), on top of vanilla bonding.
- Speed, acceleration and handling grow with training toward the horse's natural ceiling.
- Bond affinity shows in how fast or slow bonding goes.

### Who gets a personality

- NPC and wild horses: none. Pure vanilla.
- Horses with no breed row in the sheet: none, ever, in v1. That covers gang and other story-only models (`a_c_horse_gang_*`, Buell, Eagle Flies' horse, the Murfree Brood horses, the epilogue horses), mules, and the Online-only breeds (Breton, Criollo, Gypsy Cob, Kladruber, Norfolk Roadster). They stay vanilla even when the player mounts or saddles one.
- When the player mounts one: a temporary personality, gone on reload.
- When the player puts their saddle on it: that same personality becomes permanent.
- Horses the player already owns, including the starting horse, are rolled the first time the mod sees them.
- "Owned" means the horse wearing the player's saddle (the game's saddle horse). The mod sees a stabled horse when the player takes it out.
- A horse first seen as the saddle horse, with no temporary personality, counts as a stable horse, so it is prized. A temporary personality keeps its origin when it becomes permanent, so a broken wild horse stays wild.
- A personality is matched to a horse by its fingerprint (model, sex, coat). Two owned horses with the same fingerprint share one personality; the log warns when that happens.

&#91;embedded content: horse lifecycle · 5 states, 1 decision\]

Mounting rolls a temporary personality and your saddle makes it permanent; a wild horse has to survive its breaking roll first.

### Bonding and breaking

- **Stable horses** come broken at bond level 1, like vanilla. Only the time to reach levels 2 to 4 differs. No bond limit, ever.
- **Wild horses** roll a breaking difficulty from Temper and Bond affinity: Normal (vanilla), Hard (longer break, slow bonding) or Unbreakable (never breaks, basically unusable).
- Unbreakable exists only for wild horses, and `UnbreakableChance` (default 5%) keeps it rare.
- Prized horses (see Prized horses) bond more slowly, and prized wild horses break Hard more often but are never Unbreakable.

### Skills and learning speed

- Class rates from the sheet: Superior 1.5 / 0.8, Race 1.3 / 0.7, Riding 1.0 / 1.0, Work 0.9 / 1.1, War 0.8 / 1.5, Draft 0.6 / 1.2 (physical / combat).
- Multi-class breeds take their best class, minus 10%. Turkoman: 1.17 physical, 1.35 combat.
- A skill only makes up for personality in its own situation. A timid horse can learn to handle gunfire and stay timid about everything else.
- Training design is open; v1 only grows skills from experience.

### Hard rules

- Stats and skills never push a horse past its breed tier. A trained draft never handles like an Arabian.
- The tier is the breed's Handling class from the sheet: Heavy, Standard, Race or Elite. There is no separate stats tier column.
- Natural stats are whole ranks, because the game's stats are ranks 0 to 9 (one stat-bar segment each) and a fraction of a rank wouldn't show (decided Nov 25, 2026, after Phase 1).
- Each tier caps how many ranks a natural stat can move up and down from the breed baseline (the sheet's Tiers tab), and no stat ever moves more than `MaxStatRankChange`. Every tier moves up at most 1 rank so it can't climb into the next tier; Elite never moves down so the Arabian stays fastest. A stat also never leaves the game's own 0 to max range. The caps are placeholders until Phase 5 reads real stats in game.
- Breaking difficulty (wild horses only) comes from the horse's own Temper minus Bond affinity. Past the Hard score it breaks Hard. Past the Unbreakable score it is Unbreakable only if it also wins a `UnbreakableChance` roll, so the real share stays under that setting.
- Spooks, bucks, kicks and quirks are off during story missions and cutscenes.
- Every behavior has a cooldown so nothing spams.
- Vanilla pat, brush and feed animations stay; reactions play after them.
- A debug overlay shows hidden values for development and is off by default.

## Before coding

Nothing gets coded until the game, the hooks and a test save are ready and the example script loads. All tools here are free.

### Install

- [ ] RDR2 on PC, updated to the latest build. The hooks and Rampage target the latest build.
- [ ] [Script Hook RDR2](http://www.dev-c.com/rdr2/scripthookrdr2/) by Alexander Blade. The download includes an ASI loader.
- [ ] [ScriptHookRDR2DotNet V2](https://github.com/Halen84/ScriptHookRDR2DotNet-V2): the C# layer. Needs .NET Framework 4.8 and the Visual C++ 2019 x64 redistributable. Use the .asi and API DLLs from the same release.
- [ ] Visual Studio Community (2022 or newer) with the .NET desktop workload.
- [ ] Git, and a new repo for the mod.
- [ ] [Rampage Trainer](https://www.nexusmods.com/reddeadredemption2/mods/233) for testing: spawn horses and predators, teleport, change the time.
- [ ] OpenIV (optional), to read horse stats in `pedattributes.ymt`.
- [ ] Fallback only: [ScriptHookRDR2 V2](https://www.nexusmods.com/reddeadredemption2/mods/1472) by kepmehz, if we switch to C++. It can unload and reload scripts.

### Set up

- [ ] Back up your save folder (under Documents\\Rockstar Games\\Red Dead Redemption 2) before every test session.
- [ ] Make a test save near a stable and a wild horse area, with money for horses.
- [ ] Set the game to borderless windowed, so switching to Visual Studio is quick.
- [ ] Build the example script, drop it in the `scripts` folder, and check that Insert reloads it and F8 opens the console. If this fails, stop and switch to C++.

### Reference material

- [alloc8or's RDR3 native DB](https://alloc8or.github.io/rdr3-native-db/): the game functions we call.
- [femga's rdr3\_discoveries](https://github.com/femga/rdr3_discoveries): game data, including [every horse model name](https://github.com/femga/rdr3_discoveries/blob/master/peds/peds_list.lua) (filter `a_c_horse_`).
- [gtabase horse database](https://www.gtabase.com/red-dead-redemption-2/horses/): class, handling and stats for every coat.
- [Dog Companion source](https://github.com/Shtivi/RDR2-Personal-Dog): an open-source animal companion mod to learn from.
- RedM horse scripts such as rsg-horses: Lua, but they call the same natives.
- [Unruliness mod](https://www.nexusmods.com/reddeadredemption2/mods/9590): already does wild vs stable temperament. Study it and plan for players running both.
- [Breed table sheet](https://claude.ai/artifact/37Yfwtn7EsEtr69NiM3kyp): the source of all breed data.

### Data to prepare

- [ ] Map every `a_c_horse_` model (and special coats, which can be outfits on a shared model) to its breed.
- [ ] Export the breed sheet as CSV; the agent converts it to `breeds.json`.

## Architecture

The mod has two halves: a thin game layer that talks to RDR2 through one adapter, and a Core library that holds every rule and runs in tests without the game.

&#91;embedded content: mod architecture · game layer, Core, files\]

Detectors turn game state into events, Core decides what happens, and the behavior player acts through the same adapter. Only Save and settings touches the files.

### Solution layout

- `HorsePersonalities.Core`: registry, roller, brain, revealer, skills, save and settings. No game references.
- `HorsePersonalities.Game`: the script, the `IGameApi` adapter, detectors, behavior player and overlay. Builds `HorsePersonalities.dll`.
- `HorsePersonalities.Tests`: unit tests for Core.
- `HorsePersonalities.DevTools`: the Phase 1 trigger hotkeys and test helpers. Never shipped.
- `tools/sheet-to-json`: turns the sheet's CSV export into `breeds.json`.

### Tick loop rules

- Every frame: only cheap checks, such as the current mount and hotkeys.
- Every 250 ms: threats near the horse and the horse's state.
- Every 1000 ms: bond level and whether stats need re-applying.
- Only on events: rolls, saves and file writes.
- Keep per-frame work under 1 ms; the overlay can show the measured time.

## Data model

Three files hold everything: `breeds.json` (read-only design data), `horses.json` (the save) and `HorsePersonalities.ini` (player settings). Every file carries a `schemaVersion` so old saves can be upgraded.

### breeds.json (generated from the sheet)

It also carries `prizedModels`, the special, rare and community-favorite models from the sheet's Prized tab, each with its reason.

```json
{
  "schemaVersion": 2,
  "multiClassPenalty": 0.10,
  "roller": { "traitSpread": 0.6, "qualityShift": 0.5, "flawMargin": 0.25, "secondQuirkChance": 0.35, "likelyQuirkWeight": 4,
              "statSpread": 0.5, "statQualityShift": 0.25, "hardBreakScore": 1.5, "unbreakableBreakScore": 2.5 },
  "tiers": { "Heavy": { "statUpCap": 0.04, "statDownCap": 0.10 }, "Elite": { "statUpCap": 0.10, "statDownCap": 0.05 } },
  "classes": {
    "Race": { "physical": 1.3, "combat": 0.7 },
    "War":  { "physical": 0.8, "combat": 1.5 }
  },
  "breeds": [
    {
      "name": "Turkoman",
      "models": ["a_c_horse_turkoman_gold", "a_c_horse_turkoman_silver"],
      "classes": ["Race", "War"],
      "handling": "Standard",
      "leans": { "courage": 1, "nerves": 0, "temper": 1, "bondAffinity": 0 },
      "likelyQuirks": ["HeadTosser", "PickyEater"],
      "demeanor": "ProudImpatient"
    }
  ]
}
```

Model names in the example are placeholders; the real list comes from `peds_list.lua`. `roller` comes from the sheet's Roller tab and `tiers` from its Tiers tab (all four tiers; two shown).

### horses.json (one record per known horse)

```json
{
  "schemaVersion": 2,
  "horses": {
    "a_c_horse_turkoman_gold|male|coatHash": {
      "breed": "Turkoman",
      "origin": "Stable",
      "status": "Permanent",
      "quality": "Good",
      "traits": { "courage": 0.8, "nerves": -0.2, "temper": 1.1, "bondAffinity": 0.3 },
      "quirks": ["PickyEater"],
      "naturalStats": { "speed": 1, "acceleration": 0, "health": 1, "stamina": -1 },
      "breaking": "Normal",
      "skills": { "gunfire": 0.15, "predators": 0.0, "handling": 0.30, "speed": 0.10, "acceleration": 0.05 },
      "firstSeen": "2026-11-22",
      "lastSeen": "2026-11-22"
    }
  }
}
```

- The key is the horse's fingerprint: model, sex and coat. Phase 2 confirms which parts the game exposes.
- Traits are numbers from -2 to +2, matching the sheet's scale. Natural stats are whole ranks above or below the breed baseline, capped by tier.
- Skills run from 0 (untrained) to 1 (at the horse's ceiling).
- Temporary personalities are never written to this file.
- Schema 2 (Nov 25, 2026) stores natural stats as whole ranks. A schema 1 file (natural stats as fractions) is upgraded on load: each horse keeps its traits and quirks, and its natural stats are re-rolled from its saved seed under the rank rules. A quirk that no longer exists (Show-off) is dropped.
- Each record also keeps the roll's `seed` and `rollerVersion`, so a horse can be checked against a fresh roll.
- The file is written when a personality becomes permanent and when the script stops (reload or quit), never every frame. Each write first copies the last good file to `horses.backup.json`.
- A broken `horses.json` is kept as `horses.broken-<time>.json` and the backup is loaded instead; nothing is deleted. `ResetAllHorses` moves the file aside as `horses.reset-<time>.json`, starts empty, and sets itself back to false.

### HorsePersonalities.ini (player settings)

| Key | Default | What it does |
| --- | --- | --- |
| GoodHorseChance | 80 | Percent of horses that roll good |
| UnbreakableChance | 5 | Percent cap on unbreakable wild horses |
| MaxStatRankChange | 2 | Most ranks a natural stat can move (0 turns natural stats off). Replaced MaxStatVariance on Nov 25, 2026 |
| RevealPerBondLevel | 25 | Percent of stat differences revealed per bond level |
| SpookFrequency | 1.0 | Multiplier on random spook checks |
| QuirkCooldownSeconds | 120 | Shortest gap between two quirk events on one horse |
| DisableDuringMissions | true | Turns behaviors off in missions and cutscenes |
| DebugOverlay | false | Shows hidden values on screen |
| DebugKey | F9 | Toggles the overlay |
| ResetAllHorses | false | Wipes saved personalities on next load, for a new playthrough |
| LogLevel | Info | Lowest level written to log.txt: Debug, Info, Warn or Error |

## What the player sees

Behavior is the mod's only interface: no menus, no popups, no new animations. Everything below reuses what the game already does, and only behaviors marked Works in the sheet's Quirks tab (after Phase 1) ship.

### How each trait shows

| Trait | Low | High |
| --- | --- | --- |
| Courage | Ears and head snap toward threats early, snorts, jostles, rears or bolts near predators and gunfire | Stays loose and holds position under fire |
| Nerves | Calm idle, ears up, ignores small things | Restless idle, random snorts and side-steps, spooks at nothing at night, in forests or at a gallop |
| Temper | Relaxed and tolerant when pushed | Ears back a lot, bucks when pushed or scared, kicks if you linger behind it, fights calming |
| Bond affinity | Looks away, slow to bond; wild horses can be hard or impossible to break | Perks up when you approach, bonds fast |

### Quirks (1 to 2 per horse)

| Quirk | What you see | Built from |
| --- | --- | --- |
| Chatterbox | Whinnies and snorts a lot, calls out to other horses | Horse sounds |
| Kicker | Kicks any NPC behind it while standing still, even when calm | Vanilla kick |
| Head-tosser | Constant head movement when standing | Restless state, agitation |
| Nosy | Turns to watch people, wagons and animals going past | Look-at |
| Shadow | Follows you a few steps when you walk away | Vanilla lead and follow |
| Wanderer | Won't stay put when not hitched, drifts off to graze | A wander task, then grazing named with its conditional animation (both being retested after Phase 1) |
| Picky eater | Loves one treat, doesn't care about another | Feed event plus a reaction |

### Reactions to care

Reactions come from Bond affinity and Temper, soften as the bond grows, and play right after the vanilla animation.

|  | Affectionate | Aloof | Grumpy |
| --- | --- | --- | --- |
| Pat | Ears up, soft nicker | Looks away | Ears back, head toss |
| Brush | Stands still | Fidgets | Stamps, may kick if you go behind |
| Feed | Happy sound, bigger for its favorite | Eats, no reaction | Head toss after |
| Calm when spooked | Settles fast | Settles slowly | Fights it |

### Breed demeanor

Each breed has a default body language, listed in the sheet's Demeanor column. Examples:

- **Turkoman:** impatient when kept standing, hates being dirty, ears back at cheap horses.
- **Ardennes:** stands like a rock and barely reacts to nearby gunfire.
- **Arabian:** ears and head always moving, alert to everything.
- **Drafts:** gentle giants, slow to react, lean into pats.

### The game's own stat bars

Health and stamina bars drift above or below the breed's usual values as the bond grows. Speed, acceleration and handling bars climb with training. Nothing else on screen changes.

### Debug overlay (development only)

A small text panel in a screen corner, toggled with `DebugKey` and off by default. It shows the current horse's fingerprint, breed, good or bad, traits, quirks, natural stats, skills, bond level and the last behavior event with its cause.

## Outputs

The finished mod is one DLL, one settings file and a data folder, dropped into the game's `scripts` folder and released as a zip on Nexus.

### What ships

```
Red Dead Redemption 2/
  scripts/
    HorsePersonalities.dll         the mod
    HorsePersonalities.ini         player settings
    HorsePersonalities/
      breeds.json                  breed data from the sheet
      horses.json                  saved personalities (created on first run)
      horses.backup.json           last good copy of the save
      log.txt                      what the mod did and why
```

Players install Script Hook RDR2 and ScriptHookRDR2DotNet V2 first; the README links both.

### Release package

- A zip with the folder above (no `horses.json`), plus `README.md` and `CHANGELOG.md`.
- README: what the mod does, install, settings, known conflicts, uninstall, and a note to back up saves.
- Nexus page: description, requirements, screenshots or a short clip of a spook, a quirk and a reaction.

### Development outputs (not shipped)

- `HorsePersonalities.Core`: all logic, no game references, so it can be unit tested.
- `HorsePersonalities.Tests`: unit tests for the roller, caps, skills and save file.
- `HorsePersonalities.DevTools`: the Phase 1 trigger test script with hotkeys.
- `tools/sheet-to-json`: converts the sheet's CSV export into `breeds.json`.
- The sheet's Quirks tab, filled in with what works.

## Build phases

Ten phases, each ending in something you can run in game. Phase 1 is a gate: only behaviors that work in testing get built in later phases.

&#91;embedded content: build roadmap · 10 phases, 1 gate\]

Nothing after Phase 1 uses a behavior that failed its trigger test, so a missing native costs one quirk, not the project.

### Phase 0: Setup and hello world

- **Agent builds:** the solution (Core, Game script, Tests, DevTools), a script that shows "Horse Personalities loaded", and on mount shows the horse's model name, breed and bond level.
- **You:** finish the Before coding checklist, install the build, ride for 10 minutes.
- **Test:** script loads, Insert reloads it, the breed shown matches the horse, no crash.
- **Done when:** you see the right breed for 3 different horses.

### Phase 1: Trigger spike (gate)

- **Agent builds:** a DevTools script with one hotkey per behavior on your current horse: play a whinny or snort, look at a target, rear, buck you off or force a dismount, kick, graze, restless state, raise agitation, change a stat rank, read bond level and bond XP. Plus detectors: pat, brush, feed, saddle swap, mission active.
- **You:** press each key, write Works or Doesn't work in the sheet's Quirks tab, note anything odd.
- **Done when:** every trigger has a result. The agent proposes a substitute or a cut for each failure before Phase 2.

### Phase 2: Horse tracking and identity

- **Agent builds:** detection of the current mount, the player's own horses and the saddle swap; the fingerprint (model, sex, coat); temporary and permanent assignment.
- **Test:** mount an NPC horse (temporary), reload (gone), saddle it (permanent), reload (same horse, same id). Two coats of one breed get different ids. Your starting horse gets rolled once.
- **Done when:** the overlay shows a stable id for every owned horse across 3 reloads.

### Phase 3: Personality roller

- **Agent builds, in Core only:** load `breeds.json`; roll good or bad, traits with breed leanings, 1 to 2 quirks, natural stats with tier caps, and breaking difficulty for wild horses.
- **Test (unit):** 10,000 rolls per breed; the good share stays within 2 points of `GoodHorseChance`; average traits follow the breed's leanings; no stat ever passes its tier cap; unbreakable stays under `UnbreakableChance`; the same seed gives the same roll.
- **Done when:** all unit tests pass and the overlay shows rolled values in game.

### Phase 4: Save file

- **Agent builds:** read and write `horses.json`, written only on saddle, change or game save, never every frame; a backup copy; recovery from a broken file; `ResetAllHorses`.
- **Test:** a unit round-trip test; in game, quit and reload and get the same personality; delete or corrupt the file and the mod recovers from the backup without crashing.
- **Done when:** 5 owned horses keep their personalities across 3 sessions.

### Phase 5: Stats and the bond reveal

- **Agent builds:** natural stat differences applied in steps per bond level (health and stamina), tier caps, and re-applying after load or a stable visit if the game resets them.
- **Test:** a bond 1 horse shows vanilla bars; each level moves them; nothing passes its tier; check what happens to stats if the mod is removed and note it in the README.
- **Done when:** two horses of one breed show different bars by bond level 3.

### Phase 6: Core behaviors

- **Agent builds:** threat spooks from Courage (predators, gunfire, explosions nearby); random spooks from Nerves (weighted by night, forest, gallop and weak bond); bucks, refusals and kicks from Temper; the bond XP multiplier from Bond affinity; Hard and Unbreakable breaking for wild horses; the mission guard and cooldowns.
- **Test:** with Rampage, spawn wolves beside a low-courage and a high-courage horse; ride a high-nerves horse through a forest at night; push a high-temper horse until it bucks; try to break an Unbreakable wild horse 5 times; start a mission and confirm nothing fires.
- **Done when:** you can tell a brave horse from a nervous one without the overlay.

### Phase 7: Quirks, reactions and demeanor

- **Agent builds:** the quirks that passed Phase 1, care reactions (pat, brush, feed, calm), and breed demeanor.
- **Test:** each quirk shows at least once in 15 minutes with its horse; cooldowns hold; each reaction matches the table; none fire in missions.
- **Done when:** you can name a horse's quirk by watching it.

### Phase 8: Skills and experience

- **Agent builds:** exposure tracking (gunfire near the horse, predator encounters survived, time and distance ridden, sprints) turned into skill growth at the class learning rate; skills reduce panic in their own situation and raise speed, acceleration and handling toward the ceiling. Handling growth is experimental.
- **Test (unit):** growth curves and caps. In game: about 10 gunfights make a timid horse visibly calmer under fire but not around bears.
- **Done when:** a trained horse behaves differently from the same horse untrained.

### Phase 9: Polish and release

- **Agent builds:** performance pass, settings validation, log levels, overlay off by default, README, changelog and the release zip.
- **You:** play a full session with a few popular horse mods installed, including the Unruliness mod, and run the end-to-end test.
- **Done when:** the acceptance run passes and the zip installs cleanly on a fresh game folder.

## Testing

Logic is tested outside the game on every commit; behavior is tested in game at the end of each phase; the whole mod passes one end-to-end run before release.

### Unit tests (no game needed)

Run from Visual Studio's Test Explorer against `HorsePersonalities.Core`:

- **Roller:** good share, trait averages per breed, quirk counts, breaking difficulty odds over 10,000 rolls.
- **Caps:** natural stats and skills never pass the breed tier, whatever the roll.
- **Learning speed:** single and multi-class rates match the sheet (Turkoman 1.17 / 1.35, Mustang 0.81 / 1.35).
- **Save file:** round trip, broken file, missing file, schema upgrade.
- **Settings:** missing or bad INI values fall back to defaults with a log line.

### Test tools in game

- The debug overlay, to see hidden values while you play.
- DevTools hotkeys: re-roll this horse, force good or bad, set one trait, force a quirk, force Unbreakable, reset all horses.
- Rampage Trainer: spawn any breed, spawn wolves or bears, set the time to night, teleport to a forest.

### End-to-end acceptance run

1. Fresh game folder, hooks and the release zip: the mod loads and the log lists all 19 breeds.
2. Load a save: your current horse is rolled once; the overlay is off by default.
3. Buy a Turkoman: bond 1, vanilla bars, fidgets when kept standing too long.
4. Mount an NPC's horse, then reload: its temporary personality is gone.
5. Break 3 wild Mustangs: breaking difficulty differs; a forced Unbreakable never breaks.
6. Bond two horses of one breed to level 2: their bars and bonding speed differ.
7. Spawn wolves beside a low-courage and a high-courage horse: one panics, one holds.
8. Ride a high-nerves horse through a forest at night: random spooks, cooldowns respected.
9. Pat, brush and feed: reactions match each horse's temperament.
10. About 10 gunfights: the gunfire skill rises; the horse is calmer under fire but not around bears.
11. Start a story mission: no spooks, bucks or quirks.
12. Save, quit and reload: every personality is identical.
13. Set `ResetAllHorses`: a new playthrough re-rolls everything.
14. Play 60 minutes: no crash, no noticeable FPS drop, the log stays small.
15. Uninstall: the game runs and the save loads.

### After a game or hook update

Re-run the Phase 1 trigger keys, then acceptance steps 1 to 3, 7 and 12. Anything that breaks is fixed in the game adapter first.

## Risks and open questions

The biggest unknown is which behaviors a script can trigger on one horse; Phase 1 answers that before anything depends on it.

| Risk | Why it matters | Fallback |
| --- | --- | --- |
| C# hook broken on the current build | Nothing loads | Switch to C++ with ScriptHookRDR2 V2; same plan |
| A trigger doesn't exist (sounds, look-at, kick, graze, agitation per horse) | Its quirk or tell can't ship | Phase 1 gate: substitute a working behavior or cut it |
| No permanent horse ID | Personalities attach to the wrong horse | Fingerprint of model, sex and coat; add parts if the game exposes more; document rare collisions |
| Can't tell playthroughs apart | Old personalities carry into a new game | `ResetAllHorses` for now; look for a save-slot signal later |
| The game resets stats on load or at stables | Natural stats vanish | Re-apply on mount and after load |
| Stat changes get written into the game save | Removing the mod may leave changed horses | Test in Phase 5, warn in the README, back up saves |
| Handling can't change per horse | Handling skill does nothing | Keep it experimental; drop it if Phase 1 says so |
| The breaking minigame can't be controlled | Hard and Unbreakable can't work as planned | Unbreakable forces a buck-off at the end; Hard adds extra buck attempts |
| A kick in town counts as a crime | Players get a wanted level from a quirk | Test it; turn Kicker off in towns or near lawmen |
| Other horse mods change stats or temperament | Odd combinations | Apply differences on top of whatever the game reports; list known conflicts |
| A game update renames or breaks natives | The mod stops working | All native calls in one adapter; the update regression check |

### Open questions

- [ ] Prized horses: which models are special, rare or community favorites? Navod fills the sheet's Prized tab.
- [ ] Prized horses: how much slower they bond (proposed *Prized bond speed* 0.6, so 40% slower) and how much harder to break (proposed *Prized break score bonus* +1.0).

- [ ] Training: is it only experience, or also deliberate training with its own actions?
- [ ] What counts as "pushed" for a Temper buck: spurring at low stamina, a long sprint, or both?
- [ ] Should story mission horses that use a normal breed model be treated as NPC horses? (Gang and other story-only models are decided: vanilla in v1.)
- [x] Stat tier: decided, the breed's Handling class is its tier (see Hard rules).
- [x] Online-only breeds: decided, vanilla in v1 (see Who gets a personality).

## Rules for the coding agent

Work one phase at a time, keep logic testable outside the game, and ask Navod to test in game instead of guessing how the game behaves.

1. Do the phases in order. Start the next only when its "Done when" passes and Navod confirms it in game.
   Exception (Nov 24, 2026, Navod's call): Phases 2, 3 and 4 are built while Phase 1 waits for game time. Their in-game checks still wait until Phase 1 passes, and if Phase 1 shows a detector doesn't work (saddle swap, wild horse mount), the game wiring changes, not the Core rules.
2. The Phase 1 results in the sheet's Quirks tab are the source of truth. Never build on a trigger marked Doesn't work.
3. Put all logic in `HorsePersonalities.Core`, with no game references, and unit test it. The game script is a thin layer that reads the game and calls Core.
4. Route every native call through one `IGameApi` adapter. It is the one place to fix after a game update, and it can be faked in tests.
5. Look up every native in alloc8or's DB before using it, and write its name in a comment. If its behavior is unclear, add a DevTools hotkey and ask Navod to run it.
6. Never hardcode breed data or tuning numbers. Breeds come from `breeds.json`, settings from the INI.
7. Keep the per-frame tick cheap: no file reads or writes, slow checks every 250 to 1000 ms, and the save written only on events.
8. Never crash the game. Catch errors per feature, log them, and turn off only the feature that failed.
9. Log every behavior event with its cause: the trait, the roll and the cooldown.
10. Respect the hard rules in Locked design decisions: tier caps, no behaviors in missions, cooldowns, no UI.
11. Commit per task with a clear message, and tag each finished phase (`v0.1`, `v0.2` and so on).
12. When a decision changes, update Locked design decisions in this doc first, then the code.
13. End each phase with a short report: what was built, unit test results, known issues, and the exact in-game steps for Navod to test.
