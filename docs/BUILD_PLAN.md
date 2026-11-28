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

### Coat personalities

Decided Nov 27, 2026. Every coat gets a hand-written personality that fits the horse in the game and is fun to ride, starting with Navod's Arabians. Navod writes the coats; the mod turns each into data in the breed sheet's **Coats** tab.

- **Layers become breed, then coat, then individual.** A coat's row gives its archetype and vibe; its own trait leanings, which replace the breed's; its signature quirks and signature behaviors; the treats it loves and ignores; and its demeanor.
- **Same character, small differences.** Every White Arabian is the Ice Queen: her signature quirks and behaviors always show, and her traits stay close to the design. The dice only nudge the details (Roller tab: *Coat spread*, smaller than the breed spread; how jumpy, which extra quirk), so no two feel exactly alike.
- A coat with no row yet uses its breed as before.
- **Signature behaviors** are new habits built from what the game allows (see Phase 7b), each in the sheet's **Signatures** tab with how it works and whether it has passed a trigger test. Ideas the game can't do are listed there as cut, so the written personality and the mod stay honest with each other.
- The hard rules still hold: no behavior during story missions and cutscenes (a Rose Gray grazes on ordinary rides only), cooldowns, no UI.
- Prized coats (special, rare, community favorites) still always roll good; their flaws come from their quirks and signature behaviors.

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
- **The cost, slower bonding:** a prized horse takes much longer to reach bond level 4 (see Bonding and breaking).
- **The cost, harder to break:** a prized wild horse's breaking score is raised (Roller tab: *Prized break score bonus*), so it breaks Hard more often. It is never Unbreakable: an unusable favorite would be the very disappointment this rule prevents.
- Breed leanings still apply. A prized Arabian is still more alert than most breeds, just better than an average Arabian.

### How the player discovers it

- There is no menu. A new horse looks exactly like a vanilla horse of its breed.
- Behavior traits show from day one: you learn a horse is nervous when it spooks.
- Health and stamina differences appear as the bond grows, on top of vanilla bonding: the first rank of difference at bond level 2, a second rank (only a -2 horse has one) at bond level 3 (decided Nov 26, 2026). By bond 3 everything shows, before the slow climb to bond 4.
- Speed, acceleration and handling grow with training toward the horse's natural ceiling, and with long use past it (see Long-use growth).
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
- **The climb to bond 4 is earned** (decided Nov 25, 2026). Levels 1 to 3 come at the normal pace, set by Bond affinity as before, so a new horse still feels rewarding. The climb from level 3 to 4 is slower for better horses:
  - a bad horse: normal pace;
  - a good horse: slower (Roller tab: *Good bond 4 speed*, proposed 0.75, so a third longer);
  - a prized horse (stable-bought, special, rare, community favorite; always good): slowest (Roller tab: *Prized bond 4 speed*, proposed 0.5, so twice as long).
- Prized wild horses also break Hard more often, but are never Unbreakable.

### Skills and learning speed

- Class rates from the sheet: Superior 1.5 / 0.8, Race 1.3 / 0.7, Riding 1.0 / 1.0, Work 0.9 / 1.1, War 0.8 / 1.5, Draft 0.6 / 1.2 (physical / combat).
- Multi-class breeds take their best class, minus 10%. Turkoman: 1.17 physical, 1.35 combat.
- A skill only makes up for personality in its own situation. A timid horse can learn to handle gunfire and stay timid about everything else.
- Training design is open; v1 only grows skills from experience.

### Long-use growth

Decided Nov 25, 2026. A horse ridden from the start of the story to the end should be clearly better than anything the player could just pick up.

- **Any horse** that is bonded and ridden for a long time grows its speed and acceleration 1 rank past its natural ceiling. It still stops at the top of its tier, so it gets better but stays in its class.
- **Community favorites** grow to 2 ranks above their breed's usual value. They are the one exception to the tier rule: a long-kept favorite can become the fastest horse in the game.
- No stat ever passes the game's own maximum rank (9).
- **Earned two ways:** part unlocks with bond levels, the rest with time ridden and sprints (the Phase 8 experience tracking). A horse only gets the full growth if it is both well bonded and ridden a lot.
- Only speed and acceleration grow this way; health and stamina keep their natural values (revealed by bond, Phase 5).
- How many hours and which bond levels unlock each step are tuning numbers in the sheet, set in Phase 8.

### Hard rules

- Stats and skills never push a horse past its breed tier. A trained draft never handles like an Arabian.
- The one exception: community favorites' speed and acceleration can grow 2 ranks above the breed's usual value with long use, past the tier (see Long-use growth).
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
| RestoreVanillaStats | false | Puts each horse's stats back to the game's own values as you ride it, and stops applying natural stats. Turn on before uninstalling |
| SpookFrequency | 1.0 | Multiplier on random spook checks |
| QuirkCooldownSeconds | 120 | Shortest gap between two quirk events on one horse |
| DisableDuringMissions | true | Turns behaviors off in missions and cutscenes |
| DebugOverlay | false | Shows hidden values on screen |
| DebugKey | F9 | Toggles the overlay |
| TestPersonalities | false | Test mode: no random rolls; every horse gets a preset from `HorsePersonalities/test-personalities.json`, kept in `horses.test.json` so the real save is untouched |
| TestPresetKey | F11 | In test mode, gives the horse you ride (or your last horse nearby) the next preset |
| ResetAllHorses | false | Wipes saved personalities on next load, for a new playthrough |
| LogLevel | Info | Lowest level written to log.txt: Debug, Info, Warn or Error |

## What the player sees

Behavior is the mod's only interface: no menus, no popups, no new animations. Everything below reuses what the game already does, and only behaviors marked Works in the sheet's Quirks tab (after Phase 1) ship.

### How each trait shows

| Trait | Low | High |
| --- | --- | --- |
| Courage | Ears and head snap toward threats early, snorts, jostles, rears or bolts near predators and gunfire | Stays loose and holds position under fire |
| Nerves | Calm idle, ears up, ignores small things | Restless idle, random snorts and side-steps, spooks at nothing at night or at a gallop |
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

### Test mode (development only)

Decided Nov 26, 2026, so each behavior can be tested on a horse whose personality is known, without guessing.

- `TestPersonalities = true` turns the randomizer off. A new horse gets the preset named for its model in `test-personalities.json` (its `assign` list), or the *Average* preset.
- `TestPresetKey` (F11) gives the horse you ride the next preset. Its old personality is replaced on the spot, and the overlay shows the preset's name and what it is for.
- Presets cover every trait extreme, every quirk, Hard and Unbreakable wild horses, a prized horse and a Phase 5 stat test. They are data, edited without rebuilding.
- Test mode saves to `horses.test.json` and never touches `horses.json`.

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
  - The mod changes the stat's **base rank**, never its bonus rank: the bonus (the yellow segments) is the game's own, from tack, and changes when the saddle does.
  - The first time the mod sees a permanent horse it stores the horse's original base rank for each stat in `horses.json`. From then on it keeps base = original + the revealed difference, inside 0 and the game's max rank.
  - Applied to the horse you ride and to your saddle horse, checked every second. Temporary personalities get no stat changes.
  - `RestoreVanillaStats` puts every horse it sees back to its stored original, for a clean uninstall.
  - Speed and acceleration differences wait for Phase 8, where they grow with use.
- **Test:** a bond 1 horse shows vanilla bars; each level moves them; nothing passes its tier; check what happens to stats if the mod is removed and note it in the README.
- **Done when:** two horses of one breed show different bars by bond level 3.

### Phase 6: Core behaviors

- **Agent builds:** threat spooks from Courage (predators, gunfire, explosions nearby); random spooks from Nerves (weighted by night, gallop and weak bond); bucks, refusals and kicks from Temper; the bond XP multiplier from Bond affinity; Hard and Unbreakable breaking for wild horses; the mission guard and cooldowns.
  Decided Nov 26, 2026:
  - **Forests are dropped for v1:** no native says "in a forest". Night, gallop and bond weight random spooks.
  - **"Pushed" means any of:** spurring at low stamina, a long sprint, a hard stop or sharp turn at speed, and being scared (a high-Temper horse may buck instead of rearing).
  - **Strength, noticeable but rare:** a very nervous horse spooks at nothing about once every 10-15 minutes of riding at night; a cowardly one reacts to most nearby gunfire and predators; an average horse rarely does either.
  - Every rate, chance and threshold is in the breed sheet's Behavior tab, multiplied by the player's `SpookFrequency` for random spooks.
  - **Reactions use only behaviors that passed Phase 1:** rear, throw rider, agitate, quick stop, skid, bolt, look at the threat, snort, and kick (combat task, cleared after a moment).
  - **Bond speed:** the mod scales each bond XP gain the game gives a permanent horse (Bond affinity, the slower climb to bond 4 for good and prized horses, and slower for Hard wild horses) by adding or removing bond points.
  - **Breaking:** a Hard wild horse throws you a few extra times before it can break; an Unbreakable one always throws you before the break finishes.
  - **The mission guard** holds every behavior while `GET_MISSION_FLAG` is on or a cutscene shows (letterbox bars, anim scene camera, gameplay camera off, or player control off), when `DisableDuringMissions` is true.
- **Test:** with Rampage, spawn wolves beside a low-courage and a high-courage horse; ride a high-nerves horse at night; push a high-temper horse until it bucks; try to break an Unbreakable wild horse 5 times; start a mission and confirm nothing fires.
- **Done when:** you can tell a brave horse from a nervous one without the overlay.

### Phase 7: Quirks, reactions and demeanor

- **Agent builds:** the quirks that passed Phase 1, care reactions (pat, brush, feed, calm), and breed demeanor.
  Decided Nov 26, 2026:
  - **Quirks, built from what Phase 1 proved:**
    - Chatterbox: calls out and snorts now and then, more when other horses are near.
    - Kicker: kicks an NPC standing right behind it, in town or out, witnesses or not. Changed Nov 28, 2026 (Navod): a kick seen by the law brings the law, and that is part of the fun; you choose whether to take a Kicker into town. The Kicker witness range setting is retired. It kicks dogs standing behind it too (Navod, Nov 28; the dog models are in `data/dog-models.csv`).
    - Head-tosser: restless head tossing while standing still.
    - Nosy: turns to watch people and animals passing within 12 m.
    - Shadow: follows you a few steps when you walk away.
    - Wanderer: wanders off, then grazes (Phase 1 trigger 41), when left standing with you a few metres off. The game has no "is hitched" check, so it relies on the game keeping a hitched horse tied.
    - Picky eater: loves some treats and ignores others. The game names the item fed (`EVENT_INVENTORY_ITEM_REMOVED`), and each horse's taste for each item comes from its seed, so it is the same treat every time.
    - Each quirk waits `QuirkCooldownSeconds` (default 120) between two showings, and shares the 20 s gap with Phase 6 behaviors.
  - **Care reactions** play just after the vanilla animation (`Interaction_Complete`). The horse's reaction style comes from its traits:
    - Affectionate: Bond affinity +0.75 or more, Temper under +0.75.
    - Grumpy: Temper +0.75 or more.
    - Aloof: Bond affinity -0.75 or less.
    - Anything else: vanilla only.
    Each reaction is less likely as the bond grows.
  - **Breed demeanor** is four numbers per breed in the sheet's new Demeanor tab, from each breed's demeanor text:
    - Impatient: fidgets when kept standing.
    - Alert: watches its surroundings.
    - Affectionate: leans toward affectionate care reactions.
    - Fussy: unhappy when dirty, if the game's dirtiness attribute can be read.
  - All rates are in the Behavior tab. Quirks and demeanor are held in missions and cutscenes like every behavior.
- **Test:** each quirk shows at least once in 15 minutes with its horse; cooldowns hold; each reaction matches the table; none fire in missions.
- **Done when:** you can name a horse's quirk by watching it.

### Phase 7b: Coat personalities and signature behaviors

- **Agent builds:**
  - the Coats tab in `breeds.json`, and rolls around a coat's design;
  - each signature behavior in the Signatures tab: first a DevTools trigger to prove the game can do it, then the behavior itself;
  - treat names in the sheet, mapped to the item codes the game reports.
- **You:** write the coats (Arabians first), run the new triggers, mark Works or Doesn't work.
- **Test:** each coat's signature behaviors show with that coat, and two horses of one coat feel like the same character but not identical.
- **Done when:** you can tell which Arabian coat you're riding with your eyes closed (almost).
- **Built Nov 27, 2026** (code only, not yet tested in game; report: `docs/phase-reports/phase-7b.md`):
  - A coat's leanings replace its breed's, with a tighter spread (Roller tab *Coat spread* 0.3) and half the quality shift; its signature quirks always show; its demeanor and loved/ignored treats replace the breed's. Horses whose model has no coat roll exactly as before. Roller version 4, breeds.json schema 7.
  - All 17 kept signatures are built. Signatures share one cooldown (Behavior tab *Signature cooldown* 60 s) and are held in missions and cutscenes like every behavior, so Roadside grazer never grazes mid-mission.
  - Every snake model is a threat now (five added to `predator-models.csv`), not only for Snake drama.
  - Test mode: a new **Coat design** preset gives a coat model exactly its written design (no dice); the seven Arabian coat models are assigned to it. Other presets on those horses carry no signatures, so each preset still tests one thing.
  - DevTools triggers 43-55 check the natives the signatures need (town zone, unhitch, walk cap, graze ridden, aiming, enemies in combat, attack an animal, follow a person, four sigh sounds, climb speed).
- **Missouri Fox Trotter coats** (Navod, Nov 27, 2026): a baseline *Blue-Collar Athlete* for the five coats without their own design, *The Elusive Celebrity* (Silver Dapple Pinto) and *The Golden Retriever* (Amber Champagne). Five new signatures, built from tested pieces, so no new DevTools triggers:
  - **Stoic** (baseline, Silver Dapple): a pat gets nothing back, whatever its traits ("the emotional range of a brick wall").
  - **Wanderlust** (Silver Dapple): left untied with you 8 m+ away for 8 s, it walks off down the road freely (trigger 40's wander) for 40 s; whistle it back.
  - **Bored buck** (Silver Dapple): you aim from its back, standing still, for 8 s: 30% it throws you (bond 1-2) or fidgets and spoils your aim (bond 3-4). Once per aim.
  - **Clumsy** (Amber Champagne): a trip (skid) now and then at a trot or canter, 0.3/min.
  - **Snack magnet** (Amber Champagne): at a walk it stops dead and grazes a few seconds, 0.3/min (needs trigger 46).
  - Cut: cornering like a wagon and knocking over fences (no script control over handling or jumping), nudging you while you loot (no nudge animation; its Shadow quirk keeps it close instead). Endurance is the breed's own stamina.
  - Prized (Navod, Nov 27): Silver Dapple Pinto (community favorite), White Arabian (Rare).
- **Mustang coats** (Navod, Nov 27, 2026): a baseline *Short King* for the five coats without a design, *The Scarred Mercenary* (Tiger Striped Bay), *The Stoic Bodyguard* (Grullo Dun) and *The Scrappy Underdog* (Wild Bay). Eight new signatures; whistle, hogtied and sneaking detection need DevTools triggers 56-59:
  - **Short king** (baseline, Tiger, Grullo): squares up to another horse close by (agitated, glaring at it), 0.5/min. No real fight: a script-started fight could hurt an NPC's horse and turn its rider hostile.
  - **Selective hearing** (baseline, Tiger, Wild Bay): you whistle while it is off on its own: 30% it ignores you for 6 s, nosing at the ground, before it comes. Needs trigger 56-57 (whistle detection, cancelling the come-to-you).
  - **Saddle hater** (Tiger): 40% it nips and tosses its head as you get on.
  - **Bounty stomper** (Tiger): stamps and glares at a hogtied person within 3 m, 1/min. It never actually tramples: that would kill your bounties. Trigger 58.
  - **Guard dog** (Grullo): left alone, stomps at strangers within 3 m (your saddlebags), 2/min, watched at any distance.
  - **Silent stealth** (Grullo): while you sneak, none of the mod's sounds play from it. Trigger 59.
  - **Unhurried** (Grullo): spurring with no danger in the last 60 s (no threat, gunfire or fight) caps it at a trot, with a snort the first time.
  - **Jealous** (Wild Bay): you pat, brush or feed another horse, or ride one, within 20 m of it: it calls and stamps.
  - Reused: Wolf brawler now covers bears too (it always did: every predator); Clumsy for the Wild Bay's zero spatial awareness.
  - Cut: refusing your path for its own shortcut, charging through brush and taking cliffs like stairs (no script control over steering or terrain), getting stuck between trees and walking into campfires (no control; Clumsy stands in), nipping as you tighten the girth (no girth moment; Saddle hater plays on mounting).
  - Prized (Navod, Nov 27): Tiger Striped Bay (Rare).
- **Turkoman coats** (Navod, Nov 27, 2026): a baseline *Elite Adrenaline Junkie* for the four coats without a design, *The Aristocratic Assassin* (Dark Bay), *The Hollywood Action Star* (Gold) and *The Stoic Ghost* (Silver). Seven new signatures from tested pieces, so no new DevTools triggers:
  - **Restless** (all): standing still 10 s with you on it or near: stomps, snorts, tosses its head, 1.5/min (sooner and more often than the breed's Impatient demeanor).
  - **Trail snob** (baseline, Gold): 30 s of walking: an insulted snort, 0.5/min.
  - **War horse** (Dark Bay): in a fight it never spooks; off it, it goes for an enemy within 5 m for 4 s.
  - **Snob** (Dark Bay): ears back (snort and glare) at a cheaper horse within 8 m: any breed of its own stat tier or lower (the Turkoman is Standard), other than Turkomans, 0.5/min.
  - **Showboat** (Gold): you stop from a gallop with 3+ people within 15 m: 60% it rears.
  - **Must lead** (Gold): riding behind another moving horse within 12 m: agitated, 1/min.
  - **Vain** (Gold): every brushing gets a happy nicker and call, no dice.
  - Reused: Silent stealth and Mud tantrum (Dark Bay), Town disdain and Stoic (Silver). The Dark Bay loves sugar cubes and ignores haycubes.
  - Cut: charging into ambushes and never losing its footing (no control over speed or footing), disgust at a muddy pelt (no pelt-stowing moment; Mud tantrum covers the vanity), veering off to jump wagons and splash through rivers (no steering), the Silver's bite (a real bite on a sidewalk is a crime; Town disdain snaps instead).
  - Prized (Navod, Nov 27): Gold Turkoman (community favorite).
- **American Standardbred coats** (Navod, Nov 27, 2026): a baseline *Thin-Skinned Sports Car* for the Light Buckskin, *The Midnight Legend* (Black), *The Golden Boy* (Silver Tail Buckskin), *The Teacher's Pet* (Palomino Dapple) and *The Track Rookie* (Buckskin). Four new signatures from tested pieces, so no new DevTools triggers:
  - **Racer** (Silver Tail): another horse moving within 15 m while you ride at pace: a challenging call, 1/min.
  - **Spoiled** (Silver Tail): stopping after 20 s+ of galloping: 70% it calls for its sugar cube.
  - **Separation anxiety** (Palomino Dapple): left standing with you 3 m+ away, at any distance: whinnies, 3/min.
  - **No brakes** (Buckskin): stopping hard from a gallop (under walking pace within 1.5 s): 50% a sideways skid.
  - Reused: Snake drama (all), Hill sigh (baseline), Trail snob, Restless and Night owl (Black), Clumsy, Scatterbrain and Showboat (Silver Tail, Buckskin), Vain (Palomino Dapple). The Silver Tail loves sugar cubes.
  - Cut: swamps as an insult (no swamp check yet), the Black's bear-pelt tantrum (no pelt moment), the binoculars (Restless covers standing still), tripping over fences and splashing into rivers (no steering), trees and squirrels (Scatterbrain covers it).
  - Prized (Navod, Nov 27): Black Standardbred (Rare).
- **Nokota coats** (Navod, Nov 27, 2026): *The Stunt Driver* (Reverse Dapple Roan), *The Scarred Survivor* (Blue Roan) and *The Wild Child* (White Roan); the baseline's habits are on all three, which are the only Nokota coats. Three new signatures; DevTools trigger 60 checks the damage event:
  - **Grudge** (all): hurt while you ride it (a crash into a rock, a fall, a bullet): snorts and stamps, and for 2 minutes your pats and brushing get nothing back. A treat forgives it.
  - **Downhill snob** (Reverse Dapple): held to a walk going steeply downhill: an offended snort, 1/min.
  - **Stranger danger** (Blue Roan): someone hurrying toward you within 6 m: it spins on them, agitated and glaring, never a real kick (kicking a townsperson is a crime).
  - Reused: Showboat (Reverse Dapple), Vain (Blue Roan; it loves oatcakes), Wanderlust, Selective hearing, Restless and Saddle hater (White Roan).
  - Cut: low health (that is the breed's stats), being slippery when spooked (its Nerves), cornering too fast and jumping wagons (no steering), joining a herd of deer (Wanderlust wanders; no herd to join).
  - Prized (Navod, Nov 27): Reverse Dapple Roan Nokota (community favorite).
- **Thoroughbred coats** (Navod, Nov 27, 2026): a baseline *High-Strung Drama Queen* for the Black Chestnut, *The Exotic Supermodel* (Brindle), *The Brooding Outlaw Prince* (Reverse Dapple Black), *The Untamed Rebel* (Blood Bay) and *The Loyal Veteran* (Dapple Grey). Five new signatures; DevTools trigger 61 checks the in-water native:
  - **Needs soothing** (baseline, Brindle, Blood Bay): after any danger (a threat, gunfire, a fight) it frets, calling and stamping 2/min for up to 2 minutes, until you pat or calm it.
  - **Hill crawler** (Dapple Grey): climbing steeply, it slows to a walk even when you spur it (trigger 45's walk cap).
  - **Water sigh** (Dapple Grey): moving through water: a heavy sigh, 1/min (trigger 61).
  - **Apple beggar** (Dapple Grey): you stand on foot next to it for 10 s: it nickers and looks at you for an apple, 1/min. It loves apples.
  - Reused: Clumsy (baseline: trips on rocks with its long legs), Snake drama, Mud tantrum and Vain (Brindle), Snob, Guard dog, Town disdain and Night owl (Reverse Dapple Black), No brakes (Blood Bay), Hill sigh (Dapple Grey).
  - Rachel, John's Dapple Grey in the epilogue, is a story model (a_c_horse_gang_john) and stays vanilla, as decided for gang horses; a Dapple Grey you tame gets this design.
  - Cut: towering over townsfolk (its size is the game's), swamps (no swamp check yet), the carcass tantrum (no carcass moment), overshooting turns and hurdling logs (no steering or jumping control), nudging you (no nudge animation; Apple beggar asks instead).
  - Prized (Navod, Nov 27): Reverse Dapple Black Thoroughbred (Special).
- **Andalusian coats** (Navod, Nov 28, 2026): *The Waterfall Spirit* (Perlino), *The Heavy Enforcer* (Dark Bay) and *The Pit-Fighter* (Rose Gray); the baseline's War horse is on all three, which are the only Andalusian coats. One new signature, code only:
  - **Hates brushing** (Rose Gray): every brushing gets a stamp and a snort, no dice.
  - Reused: War horse (all: never spooks in a fight, goes for enemies when you're off), Guard dog and Jealous (Perlino), Hill crawler and Hill sigh (Dark Bay), Short king (Rose Gray).
  - Prized (Navod, Nov 28): Perlino (Rare).
  - Cut: slow on straightaways, getting winded, handling like a tank (its stats and the game's handling), refusing jumps (no jumping control), refusing to flee when told (no hook into the flee command; its Shadow quirk keeps it close), pinning its ears at wagons (wagons aren't horses; Short king covers other horses).
- **Ardennes coats** (Navod, Nov 28, 2026): *The Heavyweight Brawler* (Strawberry Roan), *The Gentle Giant* (Bay Roan) and *The Silent Juggernaut* (Iron Grey Roan); the baseline's War horse is on all three, which are the only Ardennes coats. One new signature:
  - **Balker** (Iron Grey): on a steep climb or in water it plants its hooves (a refusal), 2/min, however hard you spur. Water needs trigger 61.
  - Reused: War horse (all), Unhurried (Bay Roan, and the baseline's "sprinting is a punishment"), Wolf brawler, Short king and Restless (Strawberry Roan), Stoic (Iron Grey).
  - Prized (Navod, Nov 28): Iron Grey Roan (Special).
  - Cut: plowing through small trees and barrelling over riders, turning like a battleship, the half-mile stopping distance and running off cliffs (no control over movement), shoving you with its head (no nudge animation; Restless stands in), bullying horses off the hay (Short king squares up instead), stepping on wagons, blocking doorways and crushing fences (no control).
- **Hungarian Halfbred coats** (Navod, Nov 28, 2026): a baseline *Military Pedigree* for the Liver Chestnut, *The Outlaw Mercenary* (Piebald Tobiano), *The Cavalry Officer* (Dapple Dark Grey) and *The Fiery Sergeant* (Flaxen Chestnut). One new signature:
  - **Strict** (Dapple Dark Grey): hurt by sloppy riding (into a tree, down a ledge: the damage event, trigger 60): a judgmental snort, then no sprinting for 30 s (capped at a trot when you spur).
  - Reused: War horse and Trail snob (baseline: composed in a fight, routine rides beneath it), Wolf brawler, Town disdain and Snob (Piebald), Restless, No brakes and Racer (Flaxen Chestnut).
  - Prized (Navod, Nov 28): Piebald Tobiano (Rare).
  - Cut: plowing through roadblocks, slow off the line and turning like a stagecoach (no movement control), kicking passing dogs (dogs aren't watched; its Kicker quirk kicks people behind it, witnesses or not), refusing to be hitched by smaller breeds (no hitching control; Snob glares at them).
- **American Paint coats** (Navod, Nov 28, 2026): *The Mystic Wanderer* (Splashed White), *The Trailblazer* (Overo), *The Ranch Hand* (Tobiano) and *The Desert Scout* (Grey Overo); the baseline's Snack magnet (it wanders off for wild carrots) is on all four, which are the only Paint coats. Five new signatures; DevTools trigger 62 checks the look-at-a-spot native:
  - **Sixth sense** (Splashed White): a predator within 60 m, before it's close enough to scare a horse: a warning snort and a look its way, once per predator.
  - **Night stare** (Splashed White): at night, standing near you, it stares at an empty spot for 6 s, 0.5/min (trigger 62).
  - **Curious** (Overo): off it, a predator within 30 m: it walks a few steps toward it instead of backing away, 1/min.
  - **Country bumpkin** (Tobiano): ridden in a town: it stops dead and snorts, 1/min.
  - **Deadpan** (Grey Overo): hurt while you ride it (a cactus, a crash; the damage event, trigger 60): a flat, unblinking stare at you, no sound.
  - Reused: Escape artist (Splashed White unties itself), Apple beggar (Overo noses your satchel), Snack magnet (all). The Grey Overo loves haycubes and ignores sugar cubes; the Paints love wild carrots.
  - Cut: navigating fog, the wooden-bridge refusal and standing in a stream (no bridge, fog or stream check), nudging you off balance (no animation), bison (not watched; Curious is for predators), refusing a warm stable (no stable control).
  - Fixed with this: the behavior runner stopped looking at a horse once you were 15 m away, so Escape artist (20 m) could never fire. A horse with Escape artist or Wanderlust is now watched at any distance, for those two habits only.

### Phase 8: Skills and experience

- **Agent builds:** exposure tracking (gunfire near the horse, predator encounters survived, time and distance ridden, sprints) turned into skill growth at the class learning rate; skills reduce panic in their own situation and raise speed, acceleration and handling toward the ceiling. Long-use growth on top: +1 rank past the natural ceiling for any horse (within its tier), up to +2 over the breed's usual value for community favorites, unlocked by bond level and riding time. Handling growth is experimental.
- **Test (unit):** growth curves and caps. In game: about 10 gunfights make a timid horse visibly calmer under fire but not around bears.
- **Done when:** a trained horse behaves differently from the same horse untrained, and a long-kept community favorite out-runs a fresh horse of a higher tier.

### Phase 9: Polish and release

- **Agent builds:** performance pass, settings validation, log levels, overlay off by default, README, changelog and the release zip.
- **You:** play a full session with a few popular horse mods installed, including the Unruliness mod, and run the end-to-end test.
- **Done when:** the acceptance run passes and the zip installs cleanly on a fresh game folder.

## Testing

Logic is tested outside the game on every commit; behavior is tested in game at the end of each phase; the whole mod passes one end-to-end run before release.

The in-game test plan, stage by stage (Stages 1 to 12, Phases 0 to 7b), is `docs/testing/home-test-plan.md`: a snapshot of the shared home test doc, which stays the working copy for marking results.

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
| Stat changes get written into the game save | Removing the mod may leave changed horses | Test in Phase 5; `RestoreVanillaStats` puts them back before uninstalling; warn in the README; back up saves |
| Handling can't change per horse | Handling skill does nothing | Keep it experimental; drop it if Phase 1 says so |
| The breaking minigame can't be controlled | Hard and Unbreakable can't work as planned | Unbreakable forces a buck-off at the end; Hard adds extra buck attempts |
| A kick in town counts as a crime | Players get a wanted level from a quirk | Decided Nov 28, 2026: that is the point. A Kicker kicks in town too, and the law may come; the player chooses where to take it |
| Other horse mods change stats or temperament | Odd combinations | Apply differences on top of whatever the game reports; list known conflicts |
| A game update renames or breaks natives | The mod stops working | All native calls in one adapter; the update regression check |

### Open questions

- [ ] Prized horses: which models are special, rare or community favorites? Navod fills the sheet's Prized tab. Community favorites also get the bigger long-use growth.
- [ ] Long-use growth: the hours ridden and bond levels that unlock each rank (Phase 8).
- [ ] Bonding: how much slower the climb to bond 4 is (proposed 0.75 for good horses, 0.5 for prized), and how much harder prized wild horses are to break (proposed *Prized break score bonus* +1.0). Tune in Phase 6.

- [ ] Training: is it only experience, or also deliberate training with its own actions?
- [x] What counts as "pushed" for a Temper buck: decided Nov 26, all of spurring at low stamina, a long sprint, hard stops and turns, and being scared.
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
