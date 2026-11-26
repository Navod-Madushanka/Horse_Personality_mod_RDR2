# Phase 7 report: quirks, care reactions, demeanor, and test mode

Written Nov 26, 2026. Built and unit tested; not run in game yet (it waits, with Phases 5 and 6, for game time).

## Test mode (use this to test Phases 5 to 7)

Set `TestPersonalities = true` in `HorsePersonalities.ini`. Then:

- **No random rolls.** Every horse the mod sees gets the preset its model is given in `HorsePersonalities/test-personalities.json` (the `assign` list, empty by default), or **Average**.
- **F11** (`TestPresetKey`) gives the horse you ride (or your last horse within 15 m) the **next preset**. A subtitle names it and says what it tests, and the overlay (F9) shows `Test preset: <name>`.
- Test mode saves to `horses.test.json`. Your real `horses.json` is never touched. Turn test mode off and your real horses are back as they were.
- Presets are data: edit or add them in the JSON file, no rebuild needed (restart the game or press Insert).

| Preset | What it tests |
| --- | --- |
| Average | Baseline: every trait 0 |
| Coward | Courage -2: rears or bolts at threats; Nosy |
| Brave | Courage +2: holds its nerve; Shadow |
| Nervous | Nerves +2, Courage -1: spooks at nothing about every 13 min at night; Chatterbox |
| Steady | Nerves -2: almost never spooks at nothing; Wanderer |
| Hothead | Temper +2, Courage -1: throws you when scared or pushed, kicks you if you linger behind; Kicker |
| Gentle | Temper -2, Bond affinity +2: affectionate care reactions, bonds fast; Picky eater |
| Grumpy | Temper +1.5, Bond -1: grumpy care reactions, fights calming; Head-tosser |
| Aloof | Bond affinity -2: aloof care reactions, bonds slowly; Wanderer, Chatterbox |
| Wild Hard | Wild, breaks Hard: 2 extra throws while breaking; Kicker |
| Wild Unbreakable | Wild, never breaks: thrown every attempt; Head-tosser |
| Prized stats | Prized: health +1, stamina -2 (Phase 5 reveal), slowest climb to bond 4; Shadow, Picky eater |

Every quirk is on at least one preset. A Wild preset makes breaking testable on any horse you can get on while it is still being broken (a wild one); on a tame horse the breaking lines simply never fire.

## What was built

**Quirks** (one cooldown per horse, `QuirkCooldownSeconds`, default 120 s, plus the shared 20 s gap):

| Quirk | When | What it does |
| --- | --- | --- |
| Chatterbox | Any time, 0.6 per minute, twice that with another horse within 12 m | Calls or snorts |
| Kicker | Standing still, an NPC right behind it, nobody else within 25 m | Kicks the NPC (combat task, cleared after 1.5 s) |
| Head-tosser | Standing still, 0.8 per minute | Restless head toss |
| Nosy | Someone or an animal within 12 m, 1 per minute | Turns to look at them |
| Shadow | You walk more than 4 m away | Follows you for 6 s |
| Wanderer | Standing 25 s with you 6 m or more away | Wanders for 12 s, then grazes for 18 s (Phase 1 trigger 41) |
| Picky eater | You feed it | Loves some treats (call and nicker), ignores others (dismissive snort). The treat is named by `EVENT_INVENTORY_ITEM_REMOVED`; each horse's taste for each item comes from its seed, so the same treat always gets the same answer. |

**Care reactions** play 0.6 s after the vanilla animation ends (`Interaction_Complete`). The style is decided first:
- **Grumpy:** Temper +0.75 or more.
- **Affectionate:** Bond affinity, plus 0.4 per point of the breed's Affectionate demeanor, +0.75 or more.
- **Aloof:** that same value -0.75 or less.
- **Anything else:** vanilla only.

|  | Affectionate | Aloof | Grumpy |
| --- | --- | --- | --- |
| Pat | Soft nicker | Nothing back | Head toss and snort |
| Brush | Nicker | Fidgets | Stamps, then snorts |
| Feed | Nicker (picky eaters: by treat) | Nothing | Head toss |
| Calm (within 30 s of one of its spooks) | Settles | Nothing | Fights it: agitated again |

The chance is 90% at bond 1, falling to 36% at bond 4.

**Demeanor**, per breed from the sheet's new Demeanor tab (0-2 each), using the tell cooldown:
- **Impatient** (Turkoman 2, Thoroughbred 2, Standardbred 1): fidgets after standing 45 s.
- **Alert** (Arabian 2, Mustang 2, others 1): looks at passers-by.
- **Affectionate** (drafts, Arabian, Kentucky Saddler and others): leans care reactions affectionate.
- **Fussy** (Turkoman 2): snorts when dirty. The dirtiness scale is a guess; see below.

All of it is held during missions and cutscenes, and logged with its cause, for example `Wander (Wanderer: left standing 26 s with you 9 m away)`.

## Unit test results

`dotnet test tests/HorsePersonalities.Tests`: **453 passed, 0 failed** (45 new).

- **Every quirk's trigger,** including Kicker's witness rule and Chatterbox's rate (about 100 calls in 166 simulated minutes, twice that with horses near).
- **Cooldowns, and demeanor:** impatience, alertness, fussiness.
- **Care reactions:** every cell of the reaction table, the style rules and the breed bonus, softening with the bond, picky tastes (fixed per treat, about a third loved, a third ignored, different per horse).
- **Test mode:** the shipped presets cover every quirk and case; F11 cycles through all and wraps; `assign`; bad files refused with clear messages; replacing keeps status and stored stat originals; the test save is its own file.

## Known issues and things only the game can answer

1. **Dirtiness:** `SA_DIRTINESS` read as a rank; Fussy triggers at rank 2 or more. If the scale is different (0-100), the *Fussy dirt rank* setting needs changing.
2. **Wanderer and hitching:** there is no "is hitched" native. A hitched horse may try to wander and be held by the game, or may break loose. Note what happens.
3. **Kicker** uses the combat task on an NPC. With no one else within 25 m nobody should see it, but the NPC itself may react, flee or report it.
4. **Picky eater** relies on `EVENT_INVENTORY_ITEM_REMOVED` arriving within 5 s of the feed. If it doesn't, every feed is "ordinary" and the log line says `0x00000000`.
5. **Calm** reactions only count a calm within 30 s of one of the mod's own spooks, since calm events also fire during every pat (Phase 1).
6. **Nosy and Alert** look at the first person or horse the scan finds, which may not be the one passing.

## In-game test steps

The home test doc's Stage 11 has these as rows. Use test mode throughout.

1. Set `TestPersonalities = true`, load, press F9. Mount a horse: the overlay says `Test preset: Average`.
2. Press F11 until the preset you want shows. Each subtitle says what it tests.
3. For each quirk, use its preset (see the table above) and give it 5 minutes in the situation from the Quirks table. Note whether it shows, and whether it shows too often.
4. **Care:** pat, brush and feed the Gentle, Grumpy and Aloof presets; check each against the care table.
5. **Picky eater:** feed Gentle two kinds of treat, twice each. The same treat should get the same reaction both times. Copy the item codes from the log.
6. **Demeanor:** leave a Turkoman (any preset) standing for a minute; ride an Arabian past people.
7. **Missions:** start a mission with the Chatterbox preset: no calls during it.
8. Turn test mode off and load: your real horses are as they were.

**Done when** you can name a horse's quirk by watching it.
