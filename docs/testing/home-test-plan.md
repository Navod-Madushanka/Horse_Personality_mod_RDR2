<!-- Exported from the shared home test doc (claude.ai artifact LHNYRDvz4rwgNyL2MNEVbF) on Nov 27, 2026.
     The doc is the working copy (Result dropdowns, notes); this file is the snapshot kept with the code. -->

# Home test session: Phases 0 to 4

Nov 24, 2026 · @Navod

One play session tests everything built so far: the Phase 1 trigger menu and detectors, and the in-game checks for Phases 2, 3 and 4. Go through the stages in order, since each sets up the next, and mark each row as you go. Plan on two to three hours.

## Before you start

About 15 minutes, outside the game.

- [x] `git pull`, then build the solution in Visual Studio (Release). It needs `ScriptHookRDRNetAPI.dll` in `lib/`.
- [x] Copy into the game's `scripts` folder: `HorsePersonalities.dll`, `HorsePersonalities.DevTools.dll`, and the `HorsePersonalities` folder with `breeds.json`.
- [x] Back up the whole save folder: `Documents\Rockstar Games\Red Dead Redemption 2`. Stage 2 changes horse stats and Stage 8 resets saved horses.
- [x] Delete `scripts\HorsePersonalities\log.txt`, `phase1-results.csv` and any `horses*.json`, so this session starts clean.
- [x] Pick a save in free roam (not mid-mission) with: your own horse, $100 or more, a horse brush and two kinds of horse food (for example a carrot and hay), outside a town, within a short ride of a stable, a road with people on it, and a wild horse area (the plains around Heartland Overflow or the Great Plains have plenty).
- [x] Optional: Rampage Trainer, for spawning wolves (Stage 4) and NPCs (Stage 3). The steps work without it, just more slowly.
- [x] Have this doc open on a phone or second screen. Mark each row's Result as you go; write in Notes only when something is odd.

## Screens and keys

Two panels, both off until you open them. The overlay sits on the right and the test menu on the left, so both can be open at once.

| Key | What it does |
| --- | --- |
| F9 | Debug overlay (right side): the horse you're riding, its fingerprint, temporary or permanent, its rolled personality, and the live tick time |
| F10 | Test menu (left side): the 42 numbered tests, plus the *Detectors* list at the bottom |
| Page Down / Page Up | Select the next / previous test |
| End | Run the selected test on your horse |
| Home | Record *Works* for the selected test |
| Delete | Record *Doesn't work* for the selected test |
| Insert | The hook reloads all scripts (counts as a reload in Stage 8) |
| F8 | The hook's console, with script errors |

- **"Menu test" rows** (M01 to M42): select the test with Page Down, press End, then Home or Delete. The game writes that to `phase1-results.csv` for you; the Result column here is for anything else you notice.
- **The menu's *Last* line** shows what a test just did. **The *Detectors* list** shows the last 6 things the mod noticed (pats, saddle changes, missions...).
- **Your horse for menu tests** is the one you ride, otherwise your last horse, otherwise your saddle horse. *No horse found* means get on it for a second.
- If a test won't stop, run **M34** (*Clear the horse's tasks*).

## Stage 1: load the save

Checks Phase 0 again after the new code, and that your own horse becomes permanent on its own.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 1.1 | Start the game and load your save | Bottom of the screen when the loading ends | *Horse Personalities loaded (19 breeds)* appears |  |  |
| 1.2 | Press F8 | The hook's console | Both scripts listed, no red errors |  |  |
| 1.3 | Press F9, then mount your own horse | Subtitle, and the overlay on the right | Subtitle shows the right breed, model, sex and bond level. Overlay shows the breed with its tier (for example *Standard tier*) |  |  |
| 1.4 | Same horse | Overlay's *Fingerprint* line | It says *permanent, first seen* with today's date |  |  |
| 1.5 | Same horse | Overlay's personality lines | A *good horse* or *bad horse* line, four traits (Courage, Nerves, Temper, Bond), 1 or 2 quirks, four natural stats, and *Breaking: Normal (stable horse)* |  |  |
| 1.6 | Alt-tab to the game folder | `scripts\HorsePersonalities\` | `horses.json` now exists. Open it: your horse's model is in it |  |  |
| 1.7 | Ride for 2 minutes with the overlay open | Overlay's *Tick* line | Stays under 1 ms |  |  |

Write down your horse's Courage and its first quirk here; Stage 8 checks they come back the same: ...

## Stage 2: your own horse, out in the open

Away from towns and lawmen, on flat ground, horse not hitched. Open the menu (F10). After each test press Home or Delete. Only M10 and M21 wait for Stage 3.

**On foot, standing next to the horse**

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| M01 to M08 | Run each *Sound* test | Listen; the *Detectors* list prints whether a sound was playing | The horse makes a sound. Note which of the 8 names worked and what each sounded like (whinny, snort...) |  |  |
| M09 | Stand a few steps to the side, run *Look at you* | The horse's head | Its head turns toward you for about 5 seconds |  |  |
| M20 | Run *Bolt away* | The horse | It runs away, then stops after about 4 seconds |  |  |
| M23 to M27 | Run each *Scenario* test, waiting for the last to finish | The horse | M23-M24 graze, M25 rests, M26 drinks (stand near water), M27 rolls. Note which ones did anything |  |  |
| M28 | Run *Follow you*, then walk away | The horse | It follows you for about 8 seconds |  |  |

**Wanderer retest (added Nov 25)**: on foot, horse not hitched, on open grass away from water. Stand still after each one and give it the full time.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| M36 | Run *Graze, animation named* | The horse's head | It lowers its head and eats grass for about 20 s |  |  |
| M37 | Run *Graze, domestic, animation named* | The horse's head | Same as M36. Note whether it looks different |  |  |
| M38 | Run *Rest, animation named* | The horse | It settles and rests (head low, weight shifted) |  |  |
| M39 | Run *Wander nearby* | Where the horse goes | It walks off on its own, stays within about 15 m, and stops after 20 s |  |  |
| M40 | Run *Wander freely*, then whistle it back if it goes far | Where the horse goes | It walks off on its own and stops after 20 s. Note roughly how far it went |  |  |
| M41 | Run *Wander, then graze* | The horse, for the full 30 s | It drifts off for about 12 s, then stops and grazes. **If this passes, the Wanderer quirk ships** |  |  |

If M36 to M38 do nothing, note it: the drink scenario played without water before, so a scenario that does nothing at all is a different problem.

**Stats and readouts**: open the horse's stat card first (stand next to it and focus on it)

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| M29 | Note the speed bar. Run *Speed rank +2*, check the bar, run it again | Speed bar, and the *Last* line (rank before and after) | The bar goes up after the first press and back after the second |  |  |
| M30 | Same as M29 for health | Health bar | Up, then back |  |  |
| M31 | Same as M29 for stamina | Stamina bar | Up, then back |  |  |
| M32 | Run *Read bond* | *Last* line | Its *Bond rank* equals the bond level the game shows |  |  |
| M33 | Run *Read all horse stats* | *Last* line | A *Cores:* line appears (the full list goes to the log) |  |  |
| M35 | Run *Read horse identity* | *Last* line | A line of numbers appears. Copy it into Notes |  |  |

**Riding the horse**

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| M11, M12 | Standing still, run each *Rear up* | The horse | It rears on its back legs |  |  |
| M15 | Run *Agitate, keep rider* | The horse | It gets restless (stamps, tosses its head) and you stay on |  |  |
| M13, M14, M16 | Run each *Throw rider* and *Agitate, throw rider*; get back on after each | You | You're thrown off |  |  |
| M17 | Run *Force dismount* | You | You get off normally |  |  |
| M18 | At a canter, run *Quick stop* | The horse | It stops suddenly |  |  |
| M19 | At a full gallop, run *Skid* | The horse | It skids |  |  |
| M22 | Standing still, run *Horse action explorer* about 21 times, slowly | The horse, and the id on the *Last* line | Write down every id that did something, and what it did. Look hardest for a kick, a piaffe (prancing on the spot) and head tossing |  |  |

## Stage 3: with people around

Find a road with travelers, or spawn a person with Rampage. Stay out of town: a kick may count as a crime.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| M10 | With a person within 15 m of your horse, run *Look at nearest NPC* | The horse's head; *Last* says *No NPC found* if nobody was close enough | Its head turns toward the person |  |  |
| M21 | Get the person to stand right behind your horse (or stand there yourself), run *Kick nearest NPC* | The horse, and whether you get a wanted level | It kicks. Note any wanted level or witness |  |  |
| 3.1 | Get on a stranger's horse (one ridden or left by an NPC, not yours) with the overlay open | Overlay | Breed and tier right; *Fingerprint* line says *temporary (gone on reload...)* |  |  |
| 3.2 | Same horse | `horses.json` | This horse is **not** in it |  |  |
| 3.3 | Get off and on the same horse again | Overlay personality lines | Same values as before (same session, same horse) |  |  |

Write down this horse's Courage and first quirk; Stage 5 uses it: ...

Keep this horse nearby: Stage 5 takes it to a stable.

## Stage 4: caring for your horse

Back on your own horse. Keep the menu open and watch its *Detectors* list after each action. Nothing to press for these: just mark the Result.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| D36 | Pat your horse | *Detectors* | An *Interaction:* line. Copy the word or hex code after it into Notes |  |  |
| D37 | Brush your horse | *Detectors* | *Interaction: Interaction\_Brush*, or a hex code (copy it) |  |  |
| D38 | Feed it one food, then a different one | *Detectors* | One *Interaction:* line per feed. Copy both |  |  |
| D44 | After a pat or brush, wait a second | *Detectors* | A *Bond:* line with the XP number going up |  |  |
| D39 | Get your horse scared (spawn wolves nearby, or fire your gun), then calm it with the game's prompt | *Detectors* | A *Calm:* line, ideally ending *fully calmed True* |  |  |
| 4.1 | While it's scared | *Detectors* | Note any *agitated by animal* line, and the *Taming state* lines |  |  |
| 4.2 | Back to normal | Panel and overlay | The game didn't stutter during the scare; *Tick* stayed under 1 ms |  |  |

## Stage 5: at a stable

Ride the stranger's horse from Stage 3 to a stable. This stage checks the most important rule: your saddle makes a personality permanent, with the same values.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| D40 | Put your saddle on the stranger's horse (stable it or keep it at the stable, or use the saddle prompt) | *Detectors* | A *Saddle horse changed* line |  |  |
| 5.1 | Mount it with the overlay open | Overlay | *Fingerprint* now says *permanent*, and Courage and first quirk match what you wrote in Stage 3 |  |  |
| 5.2 | Check the file | `horses.json` | It now holds two horses: yours and this one |  |  |
| 5.3 | In the stable, switch back to your original horse | *Detectors*, overlay | *Saddle horse changed* again; overlay shows your horse's same values as Stage 1 |  |  |
| 5.4 | Look at two horses of the same breed but different coats (for example two Morgans at the stable, or buy a cheap second horse) | Overlay *Fingerprint* lines | The two fingerprints differ |  |  |
| 5.5 | If you can find two horses with the same breed **and** coat, run M35 on each | *Last* line | Copy both lines into Notes, even if they match |  |  |
| 5.6 | Buy a horse | Overlay | It's *permanent* right away, origin stable (*Breaking: Normal (stable horse)*) |  |  |

If you bought a horse for 5.6, switch back to your main horse before Stage 6.

## Stage 6: a wild horse

Find a wild horse (they show on the minimap only when close; the plains have many). Keep the menu and overlay open.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 6.1 | Approach and calm it as the game shows | *Detectors* *Taming state* lines | Note which states appear (for example *TARGET\_DETECTED*, *BEING\_PATTED*) |  |  |
| D43 | Mount it | *Detectors* | *Mounted wild horse*, then *Horse started breaking* |  |  |
| 6.2 | While it bucks | Overlay | Fingerprint *temporary*. The *Breaking:* line shows Normal, Hard or Unbreakable with no *(stable horse)* after it, because it's wild |  |  |
| 6.3 | Finish breaking it | *Detectors* | *Horse broken* |  |  |
| 6.4 | Put your saddle on it | *Detectors*, overlay | *Saddle horse changed*; overlay *permanent*; *Breaking* line unchanged (it stays wild) |  |  |
| 6.5 | Check the file | `horses.json` | This horse is in it with `"origin": "Wild"` |  |  |

Then switch back to your main horse at a stable, or keep riding the wild one; either works for Stage 7.

## Stage 7: a story mission and a cutscene

This decides how Phase 6 will keep horses calm during missions. The game may autosave during a mission; that's fine, because the stat tests are already undone.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| D41 | Start a story mission (a yellow letter on the map) | *Detectors* | *Mission flag (GET\_MISSION\_FLAG): True* when it starts |  |  |
| 7.1 | Play until the mission ends, or fail or quit it | *Detectors* | *Mission flag: False* when it's over |  |  |
| D42 | During a cutscene (most missions open with one) | *Detectors* | Note which lines change: *Mission flag*, *Screen faded out*, *Player control on*. Ideally at least one turns on at the start and off at the end |  |  |
| 7.2 | During the mission, ride a horse | Overlay | Still shows the horse normally; no errors in F8 |  |  |

## Stage 8: reloads and the save file

The Phase 2 and 4 checks. 8.5 and 8.6 change files on purpose, so do them last and in order.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 8.1 | Get on a stranger's horse you haven't ridden before. Note its Courage. Press Insert (reloads scripts), then mount it again | Overlay | Still *temporary*, but with different values: a temporary personality doesn't survive a reload |  |  |
| 8.2 | Press Insert again, mount your main horse | Overlay | *Permanent*, and Courage and first quirk match what you wrote in Stage 1 |  |  |
| 8.3 | Quit to the main menu and load your save. Check every horse you saddled today (main, Stage 5, Stage 6) | Overlay | Same fingerprint and same values for each |  |  |
| 8.4 | Do 8.3 two more times (three reloads in all) | Overlay | Same every time. This passes Phase 2 and Phase 4 |  |  |
| 8.5 | Quit the game completely. Open `horses.json` in Notepad, delete a few characters from the middle, save. Start the game and load | Game, F8 console, then `scripts\HorsePersonalities\` | No crash. A `horses.broken-...json` file appeared. Your horses come back from the backup (the horse saddled last may roll new values, since the backup is one save behind) |  |  |
| 8.6 | Quit. In `scripts\HorsePersonalities.ini` set `ResetAllHorses = true`. Start and load | Overlay, the folder, the INI | Your horses have new values; a `horses.reset-...json` file appeared; the INI line is back to `false` |  |  |
| 8.7 | Optional, to get your horses back: quit, delete `horses.json`, rename the `horses.reset-...json` file to `horses.json` | Overlay after loading | The Stage 8.3 values are back |  |  |
| 8.8 | After quitting | Size of `log.txt` | Under 1 MB |  |  |

## Test mode for Stages 9 to 11 (added Nov 26)

No guessing: give each horse a known personality. Set `TestPersonalities = true` in `HorsePersonalities.ini` before loading. Every horse then starts as the **Average** preset, and **F11** gives the horse you ride (or your last horse within 15 m) the next preset. The subtitle names it and says what it tests; the overlay (F9) shows *Test preset: name*. Test mode saves to `horses.test.json`, so your real horses are untouched; set it back to `false` at the end.

| Preset | Use it for |
| --- | --- |
| Average | Baseline (every trait 0) |
| Coward | 10.2, 10.4, 10.5 (Courage -2); Nosy |
| Brave | 10.3 (Courage +2); Shadow |
| Nervous | 10.6 (Nerves +2); Chatterbox |
| Steady | Almost never spooks; Wanderer |
| Hothead | 10.7, 10.8 (Temper +2); Kicker |
| Gentle | Affectionate reactions, fast bonding (10.10); Picky eater |
| Grumpy | Grumpy reactions; Head-tosser |
| Aloof | Aloof reactions, slow bonding; Wanderer, Chatterbox |
| Wild Hard | 10.9 on a wild horse: 2 extra throws; Kicker |
| Wild Unbreakable | 10.9 on a wild horse: thrown every attempt; Head-tosser |
| Prized stats | Stage 9 (health +1, stamina -2), *(prized)*; Shadow, Picky eater |

## Stage 9: Phase 5 stat bars and prized horses (added Nov 26)

Stages 1 to 8 passed on Nov 26. This stage is new. Back up the save first: it writes stats into the game. Before loading, delete the `MaxStatVariance` and `RevealPerBondLevel` lines from `HorsePersonalities.ini` (or delete the file). Optional: set `ResetAllHorses = true` once, so your horses re-roll under the prized rules.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 9.1 | Load, F9, mount your saddle horse | `log.txt` | Lines *original health base rank is ...* and *original stamina base rank is ...* |  |  |
| 9.2 | Same horse | Overlay | First line says *good horse (prized)*; natural stats show health and stamina in ranks |  |  |
| 9.3 | At bond 1, open its stat card | Health and stamina bars | Same as a vanilla horse of that coat |  |  |
| M42 | Run *Bond level: next* until bond 2, wait a second | Health and stamina bars, `log.txt` | A +1 stat gains a segment, a -1 stat loses one; a log line for each change |  |  |
| 9.4 | M42 again, to bond 3 | Bars | Only a -2 stat drops a second segment. At bond 4 nothing more changes |  |  |
| 9.5 | Change saddles | Bars | The yellow bonus segments change; the mod's change to the white base stays |  |  |
| 9.6 | Save the game, quit, reload, mount | Bars, `log.txt` | Same bars as before within a second. Note whether the log shows a new change (the game reset it) or none (the game kept it) |  |  |
| 9.7 | Stable the horse, take it out again | Bars, `log.txt` | Same bars; note the same as 9.6 |  |  |
| 9.8 | Buy a horse | Overlay | *good horse (prized)* |  |  |
| 9.9 | Set `RestoreVanillaStats = true`, reload, mount each changed horse | Bars, `log.txt` | Bars back to vanilla, with a *\[RestoreVanillaStats\]* log line. Set it back to false afterwards |  |  |

If M42 does nothing, the bond-level native doesn't work: test 9.3 to 9.4 by bonding normally (brushing, feeding, riding) instead.

## Stage 10: Phase 6 behaviors (added Nov 26)

The first stage where personalities change how horses act. Every behavior writes a line to `log.txt` with its cause, for example *Rear (scared: Predator, Courage -1.40 ...)*. Set `LogLevel = Debug` in the INI for bonding lines. Use Rampage to spawn wolves and find wild horses. Check each horse's traits on the overlay (F9) first.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 10.1 | Start a story mission, play a bit, end it | `log.txt` | *Behaviors held: story mission* at the start, *Behaviors back on.* after; nothing spooks or bucks during it |  |  |
| 10.2 | Ride a horse with Courage -1 or less; spawn wolves 10 m away | Horse, log | It rears (or throws you if its Temper is +1 or more); each wolf counts once |  |  |
| 10.3 | Same with a Courage +1 or higher horse | Horse, log | Mostly calm: agitated at most |  |  |
| 10.4 | Get off, stand 5 m from a timid horse, spawn a wolf | Horse | It bolts for about 4 s. A brave one watches the wolf |  |  |
| 10.5 | Fire your gun near a timid horse; then let an NPC fire near it | Log | Lines name *Gunfire* or *RiderGunfire*. Note whether faraway shots count too |  |  |
| 10.6 | Ride a Nerves +1.5 or higher horse at night for 15 min | Horse, log | One or two *spooked at nothing* (skid at a gallop, rear or agitated otherwise) |  |  |
| 10.7 | On a Temper +1 or higher horse: sprint 30+ s, spur at low stamina, stop hard from a gallop | Log | An occasional throw, logged *pushed (...)*. An average horse only refuses, rarely |  |  |
| 10.8 | Away from town, stand right behind a Temper +1.5 horse for 3 s, step away, repeat | Horse | Within a few tries it kicks you |  |  |
| 10.9 | Find a wild horse; the overlay shows its breaking (Normal, Hard, Unbreakable). Try to break it | Horse, log | Hard: thrown about 2 extra times, then it breaks. Unbreakable: thrown every attempt (try 5) |  |  |
| 10.10 | Ride a permanent horse with Bond affinity far from 0 for 10 min | `log.txt` (Debug) | *bond XP ... scaled* lines; the bond bar moves faster or slower than usual |  |  |
| 10.11 | Ride your own horse 30 min as usual | You | It feels alive, not annoying. Note anything that fired too often |  |  |

## Stage 11: Phase 7 quirks, care reactions and demeanor (added Nov 26)

In test mode. Give each quirk about 5 minutes in its situation, and note if it shows too often. Every quirk event writes a log line naming the quirk.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 11.1 | **Nervous** preset (Chatterbox): ride near other horses, then alone | Listen, log | It calls or snorts now and then, more near other horses; never more than once every 2 min |  |  |
| 11.2 | **Hothead** preset (Kicker): stand it still with an NPC right behind it, first away from town, then in town with people and a lawman around | Horse, log | It kicks the NPC both times. In town, note whether the law comes after you (it may, by design). Try a dog behind it too: it kicks that as well |  |  |
| 11.3 | **Grumpy** preset (Head-tosser): leave it standing next to you | Horse | Restless head tossing now and then |  |  |
| 11.4 | **Coward** preset (Nosy): stand by it on a road with people passing | Horse's head | It turns to watch people and horses going by |  |  |
| 11.5 | **Brave** preset (Shadow): get off and walk away | Horse | It follows you a few steps (about 6 s) |  |  |
| 11.6 | **Steady** preset (Wanderer): get off, don't hitch it, walk 10 m away and wait 30 s | Horse | It wanders off, then grazes. Try once hitched too and note what happens |  |  |
| 11.7 | **Gentle** preset (Picky eater): feed two kinds of treat, each twice | Horse, log | The same treat gets the same reaction both times (loved: a call and a nicker; ignored: a snort). Copy the item codes from the log |  |  |
| 11.8 | **Gentle**: pat and brush | Horse | Soft nicker after each |  |  |
| 11.9 | **Grumpy**: pat, brush and feed | Horse | Head toss or stamping after each |  |  |
| 11.10 | **Aloof**: pat, brush and feed | Horse | Nothing back after pat and feed; fidgets after brushing |  |  |
| 11.11 | **Grumpy**: let a wolf scare it, then calm it | Horse, log | It fights the calming (agitated again) |  |  |
| 11.12 | Any preset on a **Turkoman** or **Thoroughbred**: leave it standing a minute | Horse, log | It fidgets (*demeanor Impatient*) |  |  |
| 11.13 | Any preset on an **Arabian** or **Mustang**: stand by it with people around | Horse, log | It looks around at them (*demeanor Alert*) |  |  |
| 11.14 | A dirty **Turkoman** (roll it in mud) | Log | *demeanor Fussy* snorts. If never, copy the dirtiness value from DevTools test 33 into Notes |  |  |
| 11.15 | Start a mission with the **Nervous** preset (Chatterbox) | Horse, log | No calls, no quirks during it |  |  |
| 11.16 | Set `TestPersonalities = false`, load | Overlay | Your real horses are back with their own personalities |  |  |

## Stage 12: Phase 7b coat personalities (added Nov 27)

Do this before 11.16, still in test mode. Part A checks the game can do each habit (DevTools, F10, triggers 43 to 55). Part B rides the Arabian coats: in test mode each Arabian coat gets its exact written design (preset **Coat design**, shown in the overlay as *Coat: name*). Use whichever coats you can get; the Black is sold in Saint Denis, the Rose Gray Bay in Blackwater. Every habit writes a log line naming it.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.1 | Trigger 43 in Valentine, then out on the trail | Panel | True in town, False outside |  |  |
| 12.2 | Hitch your horse, walk 5 m away, trigger 44 | Horse | It frees itself and drifts off |  |  |
| 12.3 | Ride, trigger 45, push forward without spurring, then spur | Horse | Stuck at a walk; spurring still gallops |  |  |
| 12.4 | Sit still on the horse, trigger 46 | Horse | It grazes with you on it |  |  |
| 12.5 | Trigger 47, then aim your gun on horseback | Log | Flips to True while aiming |  |  |
| 12.6 | Trigger 48 in a fight or standoff, and once when nobody is hostile | Panel | Counts the hostiles; 0 when calm |  |  |
| 12.7 | Off the horse near a wild animal (wolf or coyote ideal), trigger 49 | Horse | It goes for the animal, stops after 4 s |  |  |
| 12.8 | Off the horse in camp near someone, trigger 50 | Horse | It walks up behind them, then stops |  |  |
| 12.9 | Triggers 51 to 54 (sigh candidates) | Listen | Write in Notes which sounds like a heavy sigh (silence = no such sound) |  |  |
| 12.10 | Trigger 55, walk up a steep hill | Log | Write the upward speed on a steep climb in Notes |  |  |
| 12.11 | **Any Arabian coat**: get on and open the overlay (F9) | Overlay | *Coat: archetype - signatures* matches the Coats tab |  |  |
| 12.12 | **Any Arabian**: ride near a snake | Horse, log | It reacts from much further than to a wolf (*Snake drama*) |  |  |
| 12.13 | **Grey, Black or Rose Gray Bay**: ride at a walk for a few minutes | Horse, log | An occasional refusal (*Moody refusal*), rarer at higher bond |  |  |
| 12.14 | **White** (or Grey): ride through Saint Denis with people close | Horse, log | Snorts and stares at people (*Town disdain*) |  |  |
| 12.15 | **Black**: stand still at dusk, draw your gun | Horse | It rears (*Dramatic rear*), about half the time |  |  |
| 12.16 | **Black**: hitch it, walk 20 m away, wait a minute | Horse | It unhitches and wanders (*Escape artist*) |  |  |
| 12.17 | **Black**: sit still on it while hostiles square up, don't fire | Horse, log | Sometimes throws you (*Prankster*); never once you've fired |  |  |
| 12.18 | **Rose Gray Bay**: ride without spurring, then sit still 10 s, then walk up a hill | Horse | Walks slowly (*Lazy pace*), grazes (*Roadside grazer*), sighs (*Hill sigh*, a snort for now) |  |  |
| 12.19 | **Red Chestnut**: start brushing; gallop a while | Horse, log | Excited at the brush; an odd skid at a gallop without fear (*Scatterbrain*) |  |  |
| 12.20 | **Warped Brindle**: off it near a wolf; then in camp near people | Horse, log | Attacks the wolf (*Wolf brawler*); stares at strangers close by (*Soul stare*); sneaks up on someone (*Camp creeper*) |  |  |
| 12.21 | Feed a coat horse each treat it loves or ignores (Coats tab) | Log | Copy each treat's item code from the log into Notes, for `treats.csv` |  |  |
| 12.22 | Any coat habit during a mission | Horse, log | Nothing: all held, no grazing mid-mission |  |  |

### Stage 12, Missouri Fox Trotter coats

The Silver Dapple Pinto is at Blackwater's stable; the Amber Champagne runs wild in Scarlett Meadows.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.23 | **Silver Dapple** or any other Fox Trotter except the Amber: pat it a few times | Horse, log | Nothing back (*Stoic*) |  |  |
| 12.24 | **Silver Dapple**: get off, don't hitch it, walk 10 m away and wait | Horse | After about 8 s it strolls off down the road (*Wanderlust*); whistle it back |  |  |
| 12.25 | **Silver Dapple**: hitch it, walk 25 m away, wait a minute | Horse | It unhitches and wanders (*Escape artist*, now watched from far away) |  |  |
| 12.26 | **Silver Dapple**: sit still on it and aim your rifle for 10 s, a few times | Horse, log | Now and then it throws you (bond 1-2) or fidgets (bond 3-4) (*Bored buck*) |  |  |
| 12.27 | **Amber Champagne**: trot a few minutes, then walk, then sit still 10 s, then brush it | Horse, log | Trips now and then (*Clumsy*), stops to graze at a walk (*Snack magnet*), grazes when idle, nickers at the brush |  |  |

### Stage 12, Mustang coats

The Tiger Striped Bay runs wild near Rio Bravo (New Austin); the Grullo Dun and Wild Bay run wild in the Heartlands. Run triggers 56 to 59 first.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.28 | Trigger 56, then whistle on foot and on horseback | Log | One line per whistle, button True |  |  |
| 12.29 | Off the horse, 30 m away: trigger 57, then whistle; then whistle again | Horse | It starts to come, stops and noses the ground; comes on the second whistle |  |  |
| 12.30 | Hogtie someone, stand by them, trigger 58; again with nobody tied | Panel | 1, then 0 |  |  |
| 12.31 | Trigger 59, sneak a few seconds, stand up | Log | True while sneaking |  |  |
| 12.32 | **Any Mustang** except the Wild Bay: stand it near another horse a few minutes | Horse, log | It squares up now and then (*Short king*), never a real fight |  |  |
| 12.33 | **Baseline, Tiger or Wild Bay**: get off, walk 30 m away, whistle a few times | Horse, log | Now and then it ignores you for a few seconds (*Selective hearing*) |  |  |
| 12.34 | **Tiger Striped Bay**: get on a few times; hogtie a bounty next to it; let a wolf or bear come close while you're off it | Horse, log | Nips as you get on (*Saddle hater*), stamps at the bounty without hurting it (*Bounty stomper*), goes for the predator (*Wolf brawler*) |  |  |
| 12.35 | **Grullo Dun**: leave it outside a saloon in town, go in a minute | Log | It stomps at people who pass close (*Guard dog*) |  |  |
| 12.36 | **Grullo Dun**: sprint down a quiet road; then sprint during a fight | Horse, log | Snorts and trots on the quiet road (*Unhurried*); full gallop in the fight |  |  |
| 12.37 | **Grullo Dun**: sneak next to it near a camp | Listen | No sounds from the mod (*Silent stealth*) |  |  |
| 12.38 | **Wild Bay** as your saddle horse: pat or ride another horse next to it | Horse, log | It calls and stamps (*Jealous*) |  |  |

### Stage 12, Turkoman coats

The Dark Bay is sold at Blackwater, the Gold at Saint Denis, the Silver at Tumbleweed.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.39 | **Any Turkoman**: sit still on it for 30 s | Horse, log | It starts fidgeting after about 10 s (*Restless*) |  |  |
| 12.40 | **Baseline or Gold**: walk it a couple of minutes | Horse, log | An insulted snort now and then (*Trail snob*) |  |  |
| 12.41 | **Dark Bay**: ride into a gunfight; then get off with an enemy close | Horse, log | Never spooks in the fight; goes for the enemy when you're off (*War horse*) |  |  |
| 12.42 | **Dark Bay**: stand it next to a Morgan or a draft horse; then next to another Turkoman | Horse, log | Glares at the Morgan or draft (*Snob*), not at the Turkoman |  |  |
| 12.43 | **Dark Bay**: feed a sugar cube, then a haycube | Horse, log | Happy call for the sugar cube; a snort for the haycube (needs the item codes in `treats.csv`) |  |  |
| 12.44 | **Gold**: gallop into Saint Denis and stop by a crowd | Horse | It rears, more often than not (*Showboat*) |  |  |
| 12.45 | **Gold**: ride behind another rider; then brush it | Horse, log | Agitated stuck behind (*Must lead*); happy after every brushing (*Vain*) |  |  |
| 12.46 | **Silver**: ride through a busy town; pat it | Horse, log | Snorts and glares at people close (*Town disdain*); nothing back for the pat (*Stoic*) |  |  |

### Stage 12, American Standardbred coats

The Black runs wild in Big Valley, the Buckskin wild across New Hanover and West Elizabeth; the Silver Tail Buckskin is sold at Tumbleweed, the Palomino Dapple at Valentine.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.47 | **Any Standardbred**: ride near a snake | Horse, log | It reacts from far away (*Snake drama*) |  |  |
| 12.48 | **Black**: walk it a minute, then sit still on it | Horse, log | Insulted snorts at a walk (*Trail snob*), fidgets when kept still (*Restless*) |  |  |
| 12.49 | **Silver Tail**: ride alongside another rider; gallop 30 s and stop | Horse, log | Calls at the other rider (*Racer*); calls for a treat after the stop (*Spoiled*) |  |  |
| 12.50 | **Palomino Dapple**: hitch it outside a saloon, walk 5 m away, then go inside a minute | Listen, log | It whinnies while you're away (*Separation anxiety*) |  |  |
| 12.51 | **Palomino Dapple**: brush it | Horse | Happy every time (*Vain*) |  |  |
| 12.52 | **Buckskin**: gallop, then stop as hard as you can, a few times | Horse, log | A sideways skid about half the time (*No brakes*) |  |  |

### Stage 12, Nokota coats

The Reverse Dapple Roan is sold at Saint Denis; the Blue Roan and White Roan run wild in West Elizabeth and New Hanover. Run trigger 60 first.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.53 | Trigger 60, then ride into a rock or tree, or take a small fall | Log | One line per hit on your horse |  |  |
| 12.54 | **Any Nokota**: crash it, then pat it, then feed it, then pat again | Horse, log | Snorts after the crash (*Grudge*); nothing for the first pat; the treat forgives it; the second pat gets its usual reaction |  |  |
| 12.55 | **Reverse Dapple**: walk it down a steep slope | Horse, log | Offended snorts (*Downhill snob*) |  |  |
| 12.56 | **Blue Roan**: in town, let someone run toward you (or run at a friend NPC) | Horse, log | It spins on them, agitated and glaring, never kicks (*Stranger danger*) |  |  |
| 12.57 | **White Roan**: get off, don't hitch it, walk 10 m away; then whistle | Horse | It strolls off (*Wanderlust*); sometimes ignores the whistle (*Selective hearing*) |  |  |

### Stage 12, Thoroughbred coats

The Brindle is sold at Strawberry; the Blood Bay and Dapple Grey run wild in the Heartlands and Big Valley; the Reverse Dapple Black comes with the Ultimate Edition. Run trigger 61 first.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.58 | Trigger 61, then ride into a river and out | Log | True in the water, False out of it |  |  |
| 12.59 | **Baseline, Brindle or Blood Bay**: get into a gunfight, then ride away and wait; again, but pat it after | Horse, log | It frets for a while after the fight (*Needs soothing*); a pat stops it |  |  |
| 12.60 | **Brindle**: roll it in mud; ride near a snake | Horse, log | Agitated when dirty (*Mud tantrum*); reacts to the snake from far off (*Snake drama*) |  |  |
| 12.61 | **Reverse Dapple Black**: leave it in camp near other horses and people | Horse, log | Glares at cheaper horses (*Snob*), stomps at strangers who come close (*Guard dog*) |  |  |
| 12.62 | **Blood Bay**: gallop and stop hard, a few times | Horse, log | Sideways skids (*No brakes*) |  |  |
| 12.63 | **Dapple Grey**: spur it up a steep hill; ride through a river; stand next to it on foot | Horse, log | Walks the hill even when spurred (*Hill crawler*), sighs in the water (*Water sigh*), asks you for an apple (*Apple beggar*) |  |  |

### Stage 12, Andalusian coats

The Perlino stands wild by the waterfall at Brandywine Drop (Roanoke Ridge); the Dark Bay is sold at Strawberry, the Rose Gray at Van Horn.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.64 | **Any Andalusian**: ride into a gunfight | Horse, log | It never spooks while the fight is on (*War horse*) |  |  |
| 12.65 | **Perlino**: get off in town with people passing close; then, as your saddle horse, ride another horse next to it | Horse, log | Stomps at people who come close (*Guard dog*); calls and stamps at the other horse (*Jealous*) |  |  |
| 12.66 | **Dark Bay**: spur it up a steep trail | Horse, log | Drops to a walk with a snort (*Hill crawler*, *Hill sigh*) |  |  |
| 12.67 | **Rose Gray**: brush it a few times; stand it near another horse | Horse, log | Stamps and snorts at every brushing (*Hates brushing*); squares up to the other horse (*Short king*) |  |  |

### Stage 12, Ardennes coats

The Strawberry Roan is sold at Van Horn, the Bay Roan at Scarlett Meadows; the Iron Grey Roan is a pre-order bonus.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.68 | **Any Ardennes**: ride into a gunfight and empty a repeater from the saddle | Horse, log | It never spooks while the fight is on (*War horse*) |  |  |
| 12.69 | **Strawberry Roan**: off it near a wolf; then stand it near a smaller horse; then sit still on it | Horse, log | Goes for the wolf (*Wolf brawler*), squares up to the horse (*Short king*), fidgets when kept still (*Restless*) |  |  |
| 12.70 | **Bay Roan**: spur it down a quiet road; then in a fight | Horse, log | Trots on the quiet road (*Unhurried*), full gallop in the fight |  |  |
| 12.71 | **Iron Grey Roan**: spur it up a steep trail; then into a river; then pat it | Horse, log | Refuses on the climb and in the water (*Balker*); nothing back for the pat (*Stoic*) |  |  |

### Stage 12, Hungarian Halfbred coats

The Dapple Dark Grey is sold at Valentine (or rescued from Wrobel's barn); the Flaxen Chestnut runs wild in the Heartlands and Big Valley; the Piebald Tobiano rides with bounty hunters.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.72 | **Any Hungarian Halfbred** except the Flaxen Chestnut: walk it a couple of minutes; then ride into a gunfight | Horse, log | Insulted snorts at a walk (*Trail snob*); never spooks in the fight (*War horse*) |  |  |
| 12.73 | **Piebald Tobiano**: ride through a busy town; stand it next to a Morgan | Horse, log | Snorts and glares at people (*Town disdain*), glares at the Morgan (*Snob*) |  |  |
| 12.74 | **Dapple Dark Grey**: ride it into a tree, then spur it straight away | Horse, log | A judgmental snort, then only a trot for about 30 s (*Strict*) |  |  |
| 12.75 | **Flaxen Chestnut**: stop and get off to pick herbs; gallop and stop hard; ride beside another rider | Horse, log | Stomps while you wait (*Restless*), skids on the hard stop (*No brakes*), calls at the other rider (*Racer*) |  |  |

### Stage 12, American Paint coats

The Splashed White turns up at random campsites; the Overo and Tobiano run wild across New Hanover, Ambarino and West Elizabeth; the Grey Overo is sold at Tumbleweed. Run trigger 62 first.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.76 | Trigger 62, standing beside the horse | Horse | It stares at an empty spot off to its left for about 6 s |  |  |
| 12.77 | **Any Paint**: walk it past wild carrots or grass a few minutes | Horse, log | It stops now and then to graze (*Snack magnet*) |  |  |
| 12.78 | **Splashed White**: ride toward wolves far off; stand by it at night; hitch it and walk away | Horse, log | Snorts and looks their way before they're close (*Sixth sense*); stares at nothing at night (*Night stare*); unhitches itself (*Escape artist*) |  |  |
| 12.79 | **Overo**: get off with a wolf or bear 20-30 m away; stand next to it on foot | Horse, log | Walks a few steps toward the animal (*Curious*); asks you for a treat (*Apple beggar*) |  |  |
| 12.80 | **Tobiano**: ride through Saint Denis | Horse, log | Stops dead and snorts now and then (*Country bumpkin*) |  |  |
| 12.81 | **Grey Overo**: ride into a cactus; feed a sugar cube, then a haycube | Horse, log | A flat stare at you (*Deadpan*); a snort for the sugar cube, a happy call for the haycube (needs the item codes) |  |  |

### Stage 12, Appaloosa coats

The Leopard is sold at Van Horn (and comes from No, No and Thrice, No); the Few Spotted turns up at random campsites; the Brown Leopard is sold at Tumbleweed; the Blanket and Leopard Blanket run wild in Big Valley and New Hanover.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.83 | **Any Appaloosa** except the Few Spotted: walk it on a flat road a couple of minutes | Horse, log | Bored, insulted snorts (*Trail snob*) |  |  |
| 12.84 | **Leopard**: brush it; gallop into town and stop by a crowd; roll it in mud | Horse, log | Happy every brushing (*Vain*), rears for the crowd (*Showboat*), agitated when dirty (*Mud tantrum*) |  |  |
| 12.85 | **Few Spotted**: hitch it in Saint Denis and stand back; feed it with someone close by; walk away | Horse, log | Side-steps from people (*Crowd shy*), snorts at the treat instead of eating happily, strolls off (*Wanderlust*) |  |  |
| 12.86 | **Brown Leopard**: spur it up a steep slope; feed it an oatcake; spur it up again | Horse, log | Walks the first climb (*Hill crawler*); sprints the second (*Bribable*; needs the oatcake code) |  |  |
| 12.87 | **Blanket or Leopard Blanket**: gallop a while; stand next to it on foot; sit still on it | Horse, log | An odd skid at a gallop (*Scatterbrain*), asks you for an apple (*Apple beggar*), fidgets (*Restless*) |  |  |

### Stage 12, story dogs

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 12.82 | In camp, stand next to Cain and run DevTools trigger 63 (read only) | Panel | Shows a model code; if it isn't 0xC5C5D255 (`a_c_doghobo_01`), copy it into Notes so Cain can be kept off the dog list |  |  |

## Stage 13: bond XP that lasts (added Nov 28)

Phase 6's bond pace changes the bond XP the game gives, but in Stage 9 the game put the XP back within about three minutes. These two triggers check whether adding XP (instead of setting it) is kept. Use a test save. For each: stand by your horse, press the trigger, then do nothing to it for 5 minutes (no patting, brushing, feeding or hard riding). The log writes a line every 30 s ending in *kept*, *partly kept* or *gone*.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 13.1 | DevTools trigger 64 (*Bond XP +50, added*), then wait 5 minutes | Log | Every line says *kept* | | Copy the last line into Notes |
| 13.2 | DevTools trigger 65 (*Bond XP +50, set*, the control), then wait 5 minutes | Log | Expected to end in *gone*, as in Stage 9 | | Copy the last line into Notes |
| 13.3 | If 13.1 kept it: save, quit, reload | Overlay or trigger 32 | The bond XP is still the higher number | | |

## Stage 14: Phase 8 skills and long-use growth (added Nov 28)

To see growth without riding for hours, set `ExperienceSpeed = 20` in `HorsePersonalities.ini` (back to 1 afterwards). Test mode off or on both work; use a horse you don't mind changing. Every counted gunfight and predator writes a log line.

| # | Do this | Look at | Pass if | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 14.1 | Ride a permanent horse with the overlay (F9) open | Overlay | *Experience*, *Skills* and *Long-use growth* lines; minutes ridden go up as you ride |  |  |
| 14.2 | Gallop a few minutes; press sprint from a walk several times | Overlay | Gallop minutes and sprints go up; speed and acceleration skills rise |  |  |
| 14.3 | A horse with natural speed +1 (overlay): gallop until its speed skill passes 0.5 | Speed bar, log | One more segment, with a *speed base rank* log line |  |  |
| 14.4 | A timid horse (Coward preset): get into gunfights until its gunfire skill passes 0.6, then fire near it again; then meet a wolf | Horse, log | Clearly fewer spooks under fire than at the start (the log shows *+ skill*); still scared of the wolf |  |  |
| 14.5 | Reach bond 3 (trigger 42) with *Long use* hours met (ExperienceSpeed high, or ride) | Speed and acceleration bars | +1 segment each, unless already at the tier cap |  |  |
| 14.6 | A community favorite (Silver Dapple Pinto, Gold Turkoman or Reverse Dapple Roan) at bond 4 and 30 hours | Bars | Speed and acceleration up to +2 over the breed |  |  |
| 14.7 | Handling skill past 0.5 (about 2 hours ridden at ExperienceSpeed 1) | Log, riding feel | An *agility base rank* line. Note whether turning feels any different |  |  |
| 14.8 | Save, quit, reload; then set `RestoreVanillaStats = true` and reload | Bars, overlay | Experience and growth are kept; with Restore on, speed, acceleration and agility go back to the originals |  |  |
| 14.9 | Start a mission and ride a while | Overlay | No experience counted during it |  |  |

## If something goes wrong, and what to send back

- **A test does nothing:** press Delete and write *nothing happened*. That's a result, not a failure of the session.
- **The game crashes or freezes:** restart, load the same save, note the row you were on, and carry on from the next row.
- **The menu or overlay doesn't open:** press F8 and copy any red error; check both DLLs are in `scripts`.
- **A horse won't calm down or a task won't end:** run M34.
- **Short on time:** Stages 1, 2, 5 and 8 matter most. Stage 7 can be a later session.

When you're done, send Claude these from `Red Dead Redemption 2\scripts\HorsePersonalities\`:

- [ ] `log.txt` (and `log.old.txt` if there is one)
- [ ] `phase1-results.csv`
- [ ] `horses.json`
- [ ] This doc, filled in

Claude then marks the Quirks tab in the breed sheet, proposes a fix or a cut for each failure, and writes up which phases passed.
