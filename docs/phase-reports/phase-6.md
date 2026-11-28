# Phase 6 report: core behaviors

Written Nov 26, 2026. Built and unit tested. Bonding was redesigned as **trust** on Nov 28, 2026 and tested in game the same day; see *Trust: bond pace redesigned* below. The other behaviors still wait for game time.

## What was built

The first phase where a personality changes how a horse acts. All decisions are in Core (`src/HorsePersonalities.Core/Behavior/`); `BehaviorRunner` in the game layer only reads the game and plays what Core decides. Every behavior is logged with its cause, for example `Rear (scared: Predator, Courage -1.40: roll 0.212 vs 0.820)`.

**Who:** the horse you ride, or your last horse when you're within 15 m of it. Temporary personalities act too (that's how you learn a borrowed horse); bonding changes only apply to permanent ones.

**Threats (Courage).** Predators within 20 m (wolves, bears, cougars, panthers, alligators, boars, the lion, rattlesnakes: `data/predator-models.csv`), gunfire, explosions.
- Chance to spook: about 90% at Courage -2, 50% at 0, 10% at +2. Explosions count as 1 Courage worse; your own shots count 40% of a stranger's.
- Each threat is judged **once**: each predator when it comes in range, gunfire once per gunfight (over after 20 quiet seconds). Without that, a wolf standing nearby would scare even a brave horse in the end.
- Ridden: Temper +1 or more throws you instead of rearing; Courage below -0.5 rears; otherwise it only gets agitated.
- Not ridden: Courage below 0 bolts away from the threat for 4 s; otherwise it watches it.
- A timid horse that holds its nerve still gives a tell: it looks at the threat and calls out.

**Random spooks (Nerves).** Rate per minute = 0.01 × e^(0.55 × Nerves) × night (2.5) × gallop (1.5) × weak bond (1.5) or full bond (0.6) × `SpookFrequency`.
- A Nerves +2 horse at night: about once every 13 minutes; at a gallop, about every 9.
- An average horse: about once every 40 minutes at night, every 100 in daylight.
- Forests are not used (no native says "in a forest").
- Ridden at a gallop: skid. Slower: rear if timid, else agitated. Not ridden: agitated.

**Pushed (Temper).** Spurring at low stamina (under 25), a sprint over 30 s, or a hard stop or turn (losing 8 m/s per second from a gallop).
- Chance: 5% at Temper 0, about 37% at +2, never above 50%.
- Temper +1 or more throws you; below that it refuses (quick stop).

**Kick (Temper).** Stand within 2.5 m behind the horse for 3 s: 5% at Temper 0, about 37% at +2. Once per time you stand there. Uses the combat task Phase 1 found, cleared after 1.5 s.

**Bonding (Bond affinity and quality).** *Replaced Nov 28, 2026 by trust; see below.* As first built, each second the bond XP the game just gave a permanent horse was scaled by adding or removing points:
- Bond affinity: about 1.5 times at +2, 0.67 at -2.
- From bond 3 to 4: × 0.75 for a good horse, × 0.5 for a prized one.
- A wild horse that broke Hard: × 0.8.
- Fractions carried over. A gain that raised the bond level was left alone.

The same multipliers now scale trust instead.

**Breaking wild horses.**
- **Hard:** throws you after 8 s on each of its first 2 attempts, then can break normally.
- **Unbreakable:** throws you after 5 s on every attempt, before the break can finish.

**Cooldowns:** spook 60 s, buck 90 s, kick 60 s, tells 30 s, and at least 20 s between any two behaviors of one horse.

**Mission guard:** with `DisableDuringMissions = true` (the default), everything is held while the mission flag is on, letterbox bars show, the anim scene camera is active, the gameplay camera is off, or player control is off. The log says when behaviors are held and when they come back. Breaking is not held: no mission breaks horses.

**Tuning:** every number above is in the breed sheet's new **Behavior** tab (37 settings).

## Unit test results

`dotnet test tests/HorsePersonalities.Tests`: **408 passed, 0 failed** (46 new).

- **Strength:** a Nerves +2 horse at night spooks every 10-15 minutes; an average horse at most twice an hour at night; a simulated hour of night riding with a very nervous horse gives 2-9 spooks.
- **Factors and chances:** night, gallop, bond and `SpookFrequency` each move the rate the right way; threat chances of about 90% / 50% / 10%.
- **Reactions:** every rule above, explosions and your own gunfire, the cause text, tells.
- **Cooldowns and gap; pushes:** each push reported once; buck chance by Temper and its cap.
- **Kicks:** linger time and position.
- **Threat memory:** a wolf judged once; a new gunfight after a quiet spell.
- **Every mission-guard signal.**
- **Bond pace and breaking:** every bond multiplier, carried fractions; Hard (exactly 2 extra throws), Unbreakable (5 attempts, 5 throws), Normal (left to the game).

## Known issues and things only the game can answer

1. **Gunfire detection.** Uses `EVENT_SHOT_FIRED` and `EVENT_SHOT_FIRED_WHIZZED_BY` from the event queue; Phase 1 never tested them. If they fire for shots far away, or not at all, gunfire spooks need another signal.
2. **Your own shots.** "Rider gunfire" relies on `IS_PED_SHOOTING` being true in the same window as the shot event.
3. **The sprint key.** `IS_CONTROL_PRESSED(0, INPUT_HORSE_SPRINT)` with keyboard and controller.
4. **Bond points.** Answered (Stage 13, Nov 28): the game keeps bond XP a script sets only until it restores its own saved value (after a reload, and sometimes seconds later in the same session). Bond pace moved to trust.
5. **Predators** come from the hook's ped pool, which can fail at random (the hook returns nothing). A missed scan just tries again 0.5 s later.
6. **Kick** attacks the player with the combat task. It may do damage, and it should never fire in town (Phase 7 adds a witness and lawman check for the Kicker quirk; this Temper kick only fires when you stand behind your own horse).
7. **Throwing you at a gallop** can hurt. That is vanilla's behavior too, but if it feels unfair, push bucks could be made refusals above some speed.
8. **Breaking timing.** 5 and 8 seconds are guesses for how long a break takes; tune them in the Behavior tab.

## In-game test steps

Use a test save in free roam, with Rampage Trainer for spawning. Set `LogLevel = Debug` in the INI to see bond adjustments.

1. **Mission guard:** start a story mission and watch `log.txt`: `Behaviors held: story mission`, then `Behaviors back on.` after it. Nothing should spook, buck or kick during it.
2. **Predators:** with the overlay (F9), find or spawn wolves near a horse with low Courage (-1 or less) and near one with high Courage (+1 or more), riding each. The timid one rears or throws you (if Temper +1 or more); the brave one mostly only gets agitated or does nothing. Each wolf only once.
3. **Not riding:** stand 5 m from a timid horse and spawn a wolf: it bolts. A brave one looks at the wolf.
4. **Gunfire:** fire near a timid horse, and have an NPC fire near it. Note whether the log names `Gunfire` or `RiderGunfire`, and whether shots far away trigger it.
5. **Nerves:** ride a Nerves +1.5 or higher horse at night for 15 minutes. Expect one or two random spooks (skid at a gallop, rear or agitated otherwise), each logged as `spooked at nothing`.
6. **Pushed:** on a Temper +1 or higher horse, sprint for over 30 s, spur with low stamina, and stop hard from a gallop. Expect an occasional throw, logged as `pushed (...)`. An average horse only refuses, rarely.
7. **Kick:** stand right behind a Temper +1.5 horse for 3 s, step away, repeat. Within a few tries it kicks. Never in town.
8. **Breaking:** use Rampage to find a wild Mustang. Its overlay line `Breaking if wild` (or `Breaking:` once ridden) says Normal, Hard or Unbreakable. A Hard one throws you twice more than usual; an Unbreakable one throws you every time (try 5).
9. **Trust:** done Nov 28 (see below).
10. **Overall:** ride your own horse for 30 minutes as usual. It should feel alive, not annoying.

**Done when** you can tell a brave horse from a nervous one without the overlay.

## Trust: bond pace redesigned (Nov 28, 2026)

### Why

Home test Stage 13 showed that bond XP the mod sets does not last:
- `ADD_ATTRIBUTE_POINTS` (trigger 64), `SET_ATTRIBUTE_POINTS` (trigger 65) and trigger 42's level crossings all held for 5 to 7.5 minutes within a session;
- after save and reload, all were gone: the game restored its own saved bond XP (86).

Re-adding the difference after every reset was rejected. It fights the game, its level-ups repeat, and slowing a horse means taking XP away below levels the game already unlocked. Navod chose a mod-side value instead (Build plan: Trust).

### What was built

- The mod never writes bond XP. The game's bond is 100% vanilla.
- Each permanent horse has `trust` in `horses.json`, in bond XP points. Levels 1-4 use the game's own thresholds for the model (`GET_DEFAULT_ATTRIBUTE_POINTS_NEEDED_FOR_RANK`).
- Each second, for the ridden horse and the saddle horse (so pats on foot count), each rise in the game's bond XP is scaled by the multipliers above and added to trust.
- Drops (the game restoring its value) are ignored. Jumps over 100 XP (trainers, DevTools trigger 42) are added unscaled.
- Trust starts at the horse's bond XP the first time the mod reads it, so owned horses keep their bond. No schema change.
- Saved on a level change, an outside jump, and every 50 points.
- The stat reveal (Phase 5), spook weighting, care reactions, signatures, long-use growth (Phase 8) and admired horses (Phase 8b) follow the trust level. Temporary personalities use the game's bond.
- The overlay shows a Trust line. Trigger 42's description now explains that it raises trust.

Unit tests: **576 passed, 0 failed**, including 7 new trust tests (levels, start, scaling, the game's restore, a new session, outside jumps, level-up saves, temporary horses) and a save round trip.

### In-game results (Nov 28, 2026, test mode, Dutch Warmblood chocolate roan)

| Check | Result |
| --- | --- |
| Thresholds | The game gave level 2 at 350, 3 at 950, 4 at 1950, matching trigger 42 |
| Trust starts | At the game's bond XP (86, level 1) as soon as the saddle horse was seen, before mounting |
| Natural gains | +1 and +2 while riding, +15 for a pat, each added to trust |
| Trigger 42 | +216, +136 and +600 counted as outside jumps, unscaled; trust 1 → 2 → 3 |
| The game restores its XP | Within the session: 350 → 136 (about a minute after the trigger), 950 → 216 (8 s after), 350 → 260 (3 s after). Trust kept its value each time and went on counting from the game's new value |
| Reload | Trust came back at 400 (level 2) while the game's bond was 1 (206 XP) |
| Pace | Average and Bad horse x1.00; Aloof (Bond affinity -2) x0.67; Prized stats at trust 2 (affinity +0.5) x1.11; at trust 3 x0.55 (the prized 0.5 for the climb to 4) |
| Stat reveal | Prized stats at trust 2: Health 4 → 5, Stamina 5 → 4; at trust 3: Stamina 4 → 3. The bars stayed revealed while the game's bond fell back to 1 |
| Saved | `horses.test.json` has the trust value; the other horses have `null` until the mod reads them |

**Passed.** Trust survives reloads and the game's own restores, and the reveal follows it.

Notes:
- A trigger 42 jump of +92 (from 258 to 350) was under the 100 limit, so it was scaled like a natural gain (x0.55). Only DevTools and trainer jumps of 36 to 100 XP are affected; natural gains seen are at most +35. Left at 100.
- Not yet seen: a reload after trust 3 (the trust 2 reload passed), and a Hard wild horse's x0.8.
- Gains under 50 points since the last write are lost if the game closes (the hook never says when it exits); at most about 2 minutes of riding.

