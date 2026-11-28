# Phase 8 report: skills, experience and long-use growth

Written Nov 28, 2026. Built and unit tested (539 tests pass); not run in game yet, by decision (no game access). Home test Stage 14.

## What was built

- **Experience, per permanent horse,** saved in `horses.json` as `experience`: gunfights, predator encounters, minutes ridden, minutes at a gallop, sprints. Older files load with none.
  - A gunfight is gunfire or an explosion near the horse while you ride it or stand by it, at most one per *Gunfight gap* (60 s).
  - A predator encounter is a predator first seen within Predator range.
  - Ridden and gallop minutes count while you ride. A sprint is a sprint press from below a gallop, at most one per *Sprint gap* (10 s).
  - Nothing counts in missions or cutscenes. The file is written after each gunfight or predator, and every 5 minutes of riding.
  - `ExperienceSpeed` in the INI (default 1, up to 100) multiplies everything, to test growth without riding for hours.
- **Skills (0 to 1) are worked out from experience,** never stored: count x the Skills tab's step x the breed's learning rate (combat for gunfire and predators, physical for speed, acceleration and handling). Retuning applies to every horse at once.
- **Skills ease fear in their own situation only.** A full gunfire skill adds 2 Courage (*Skill courage bonus*) against gunfire and explosions; a full predator skill the same against predators. With the proposed numbers, 10 gunfights take a Courage -2 Morgan from spooking at 90% of gunshots to 61%; it's still as scared of a bear.
- **Speed and acceleration on the stat bars.** The natural speed and acceleration rolled since Phase 3 finally show: a positive one once that skill reaches 0.5 (it grows into it), a zero or negative one from the start.
- **Long-use growth:** +1 speed and acceleration at bond 3 and 10 hours ridden. Community favorites get a second rank at bond 4 and 30 hours. Any horse stops at its tier's up cap above its breed's usual value; favorites stop at +2, past the tier. Nothing passes rank 9.
- **Handling (experimental):** a handling skill of 0.5 raises the Agility base rank by 1, within the tier cap. *Handling growth* 0 turns it off. Whether Agility changes how the horse handles is a test row.
- As with health and stamina, only base ranks change; each original is stored on first sight and re-applied every session. `RestoreVanillaStats` puts speed, acceleration and agility back too.
- **Overlay (F9)** shows the horse's experience, skills and long-use ranks.
- **New sheet tab, Skills,** with all 17 numbers; `breeds.json` gets a `skills` section.

## Tuning to look at after testing

- 10 gunfights giving 0.90 -> 0.61 may be too gentle for "visibly calmer". Raising *Skill courage bonus* (2) or *Gunfight step* (0.08) makes it stronger.
- *Long use rank 1 hours* (10) and *rank 2 hours* (30) are guesses at "a horse kept from early in the story".

## Open

- Bond pace (Phase 6) still waits on home test Stage 13: long-use growth uses the game's own bond level, so it works either way.
