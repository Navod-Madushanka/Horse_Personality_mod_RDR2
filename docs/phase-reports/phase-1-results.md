# Phase 1 results: trigger spike, tested in game

Tested Nov 24, 2026 on hook 2.2.0 (ScriptHookRDR2DotNet V2), one Thoroughbred, in open country. Source: `phase1-results.csv` and `log.txt` from the game's `scripts/HorsePersonalities` folder, plus Navod's notes where the CSV was ambiguous. All 35 triggers have a result. Mission and cutscene detectors are not tested yet.

## Findings that change the code

1. **Yes/no natives read true.** `Function.Call<bool>` returned true for every yes/no native (loading screen, screen faded, `IS_PED_MALE`, `IS_ENTITY_DEAD`). Reading the result as an integer and testing the lowest byte fixes all of them. The hook's own wrappers (`Game.IsLoading`, `Ped.Gender`) are wrong too; don't use them.
2. **Horse sex:** `IS_PED_MALE` is correct once read that way (female Thoroughbred, male Standardbred).
3. **Ranks run 0 to 9.** The stat bar in the game shows rank + 1 segments (10 segments for max 9); the yellow segment is the bonus rank. Base and bonus read separately and match the bars.
4. **Interactions:** every pat, brush or feed sends `Interaction_Begin` (0x3528CE82), then its own type, then `Interaction_Complete` (0x018311EE) and `Interaction_Cancel` (0x25FC13DC) together.
   - Pat: 0x175897C0 (no known name), always with calm events.
   - Brush: `Interaction_Brush`. Feed: `Interaction_Food`.
   - Taming a wild horse sends extra types: 0xD295DE33, 0x20CFA0C8, 0x8EE87964.
5. **Wild horses:** `EVENT_PLAYER_MOUNT_WILD_HORSE` fires, and the taming state is 6 (BREAKING_ACTIVE) while mounted. After breaking, the state goes to INVALID and bond XP starts.
6. **Saddle swaps:** the saddle horse reads `0` for about 9 seconds during a swap, then the new horse. The 250 ms poll handles it.

## Trigger results

| Trigger | Result | Notes |
| --- | --- | --- |
| Read bond level and XP (32) | Works | Matches the game's bond level; base 1, max 4 |
| Read all stats (33) | Works | Matches the stat bars |
| Sound SNORT | Works | Game confirms vocalization playing |
| Sound AGITATED | Works | Game confirms vocalization playing |
| Sound CONTENT | Works | Heard; the game's "playing" check read False, so don't rely on that check for short sounds |
| Sound CALL | Works | Navod confirmed after an accidental second entry in the CSV |
| Sound WHINNY, NEIGH, HORSE_SNORT, HORSE_WHINNY | Doesn't work | No sound; names are wrong. Cut. |
| Look at you | Works | |
| Look at nearest NPC | Works | Navod confirmed near an NPC. The log's "No NPC found within 15 m" lines were from empty country, not a failure. |
| Rear up, action 1 and action 5 | Works | |
| Throw rider, action 2 and action 10 | Works | |
| Agitate, keep rider | Works | |
| Agitate, throw rider | Works | |
| Force dismount | Works | |
| Quick stop (action 3), skid (action 8) | Works | |
| Bolt away from you | Works | |
| Kick (combat task on nearest NPC) | Works | The only kick method found. Test away from towns: it can look like a crime. |
| Horse action explorer | Done, nothing new | Ids 0 to 20 all tried. Id 0 lifts the front legs; the rest only agitate the horse (Navod). No kick, piaffe or head toss turned up. |
| Scenario DRINK_GROUND_DOMESTIC | Works | |
| Scenario WALLOW | Works | |
| Scenario GRAZING, GRAZING_DOMESTIC, RESTING_DOMESTIC | Doesn't work | Nothing happened. Need other grazing names for the Wanderer quirk. |
| Follow you | Works | For the Shadow quirk |
| Speed, Health, Stamina rank +2 / restore | Works | Speed 7 to 9 and back, Stamina 2 to 4 and back. Health went 2 to 4 and was restored afterwards (log 21:15: rank 4 to 2, bonus 2 to 0). |
| Clear the horse's tasks | Works | |
| Read horse identity | Works | Nothing here tells two horses of one model and sex apart |

## Detectors

| Detector | Result |
| --- | --- |
| Pat, brush, feed | Works, all three told apart (see above) |
| Calm | Works; calm types 0 to 5 seen. `fully calmed True` came 4 times in this log, always with calm type 1. Calm events also fire during every pat, so Phase 7 must not treat a pat as a calm. |
| Saddle swap | Works |
| Wild horse mounted, breaking, taming state | Works |
| Bond XP and rank changes | Works; passive gain is about 2 XP per 5 seconds while riding |
| Screen fade, player control | Fired once at 20:28:53 (a quick fade and back); cause not identified |
| Mission flag, cutscenes | **Not tested** |

## Proposed replacements for failures

- Sounds: keep SNORT, AGITATED, CONTENT, CALL. Cut the four that make no sound.
- Grazing: use DRINK_GROUND_DOMESTIC or WALLOW for idle behavior until a grazing name is found. Add more candidate names to `Scenarios` in the DevTools INI and try them.
- Kick: use the combat-task method, behind a check that no lawman or civilian is close.
- Piaffe (Show-off) and a direct kick action: not in `TASK_HORSE_ACTION` ids 0 to 20. Kick uses the combat task. Show-off needs another route (a scenario or `TASK_PLAY_ANIM`) or gets cut from v1.

## The breed sheet's Quirks tab

Marked from these results on Nov 24, 2026 (Claude), with a notes column saying which method worked:

| Quirk | Result | How |
| --- | --- | --- |
| Chatterbox | Works | SNORT, AGITATED, CONTENT, CALL |
| Kicker | Works | Combat task on the NPC behind, cleared after 4 s; needs a lawman and witness check |
| Head-tosser | Works | `_HORSE_AGITATE`, keeping the rider |
| Nosy | Works | Look-at |
| Shadow | Works | Follow, 2 m behind |
| Wanderer | Doesn't work | No grazing scenario; drinking and wallowing work instead |
| Picky eater | Works | `Interaction_Food` event, then a sound |
| Show-off | Doesn't work | No piaffe found |

## What the same log shows for Phases 2, 3 and 4

The main script ran beside DevTools all session, with no errors or warnings in 762 log lines.

- **Temporary personalities reset on reload:** the same black Standardbred rolled seed `35CD...` at 19:06 and `E6E2...` after the 19:21 reload.
- **Saddling keeps the values:** the Arabian's temporary roll at 20:26 (seed `65ADE...`) is the one written to `horses.json` at 20:27.
- **Wild horses:** `EVENT_PLAYER_MOUNT_WILD_HORSE` marked the Kentucky Saddler wild (taming state 6). It kept Wild origin when saddled, and `horses.json` says `"origin": "Wild"`.
- **Two coats, two ids:** Thoroughbred reversed dapple black and blood bay got different fingerprints.
- **Saved horses survive reloads:** the Thoroughbred loaded with the same values in 5 sessions in a row (19:46 to 20:03).
- **ResetAllHorses:** at 19:33 it moved the save to `horses.reset-20261124-193346.json`, started empty and set itself back to false.
- **Sex in fingerprints:** before the yes/no fix, every horse was saved as male. The reset file has the same Thoroughbred twice (male and female) for that reason. The current `horses.json` is clean.

Not checked yet: the Arabian and Kentucky Saddler across reloads (both were saddled after the last reload), a broken `horses.json` in game, and the tick time (only logged at the Debug level).

## Still to do before Phase 1 is closed

1. Test the mission flag and cutscene detectors (start a story mission; watch "Mission flag", "Screen faded", "Player control", and the four camera lines added after this session).
2. Decide what replaces Wanderer's grazing and Show-off's piaffe, or cut them from v1.

And for Phases 2 to 4: reload 3 times and check all three saved horses, break `horses.json` once (home test doc, row 8.5), and watch the overlay's tick time for a few minutes.

Done since the first draft: Health bonus restored; explorer ids 0 to 20 tried.
