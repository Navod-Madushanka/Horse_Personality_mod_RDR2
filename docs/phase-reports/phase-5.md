# Phase 5 report: stats and the bond reveal, plus prized horses

Written Nov 26, 2026. Built and unit tested; checked in game on Nov 27 (see In-game results).

## What was built

**Prized horses** (plan: Prized horses, decided Nov 25)

- A horse is prized if it was rolled as a stable horse (bought, or already owned when the mod first saw it), or if its model is on the breed sheet's new **Prized** tab (special, rare or community favorite) however it was found. The tab has the six PC bonus coats so far.
- A prized horse always rolls good, and **no** trait sits on the bad side of its breed (a normal good horse may keep one flaw). Its flaws come only from its quirks.
- A prized wild horse adds *Prized break score bonus* (+1) to its breaking score, so it breaks Hard more often, and it is never Unbreakable.
- The overlay and the log show "(prized)" after good/bad, and `horses.json` keeps `"prized": true`.
- *Good bond 4 speed* (0.75) and *Prized bond 4 speed* (0.5) are in the Roller tab and loaded, ready for Phase 6. Nothing slows bonding yet.
- Horses saved before this keep their rolls; they show as not prized. Your bad stable-bought Thoroughbred stays bad until you reset (`ResetAllHorses = true`).

**Phase 5: health and stamina on the stat bars**

- Every second, the horse you ride and your saddle horse get their **health and stamina base ranks** set to original + the difference revealed so far:
  - **bond 1:** vanilla bars;
  - **bond 2:** the first rank of difference (+1 or -1);
  - **bond 3:** a second rank, for the rare -2 horse.
- The **bonus rank** (yellow segments, from the saddle) is never touched.
- The first time the mod sees a permanent horse it stores the game's own base ranks in `horses.json` (`"vanillaBase"`), before changing anything. A game reset (reload, stable) is put right within a second.
- Temporary personalities and horses with no breed get no stat changes. Ranks stay between 0 and the game's max for the horse.
- **`RestoreVanillaStats = true`** in the INI puts each horse you ride or saddle back to its stored original, and stops applying differences. Use it before uninstalling.
- Speed and acceleration differences wait for Phase 8 (long-use growth).
- Each change goes in the log: `... at bond 2: Health base rank 4 -> 5 (original 4, natural +1, showing +1)`.
- The reveal levels (2 and 3) are the Roller tab's *Reveal first rank bond* and *Reveal second rank bond*.
- `RevealPerBondLevel` is gone from the INI; an old line only logs a warning.

**DevTools trigger 42, *Bond level: next*:** steps the horse's bond level 1 → 2 → 3 → 4 → 1 with `_SET_MOUNT_BONDING_LEVEL`, so the reveal can be tested without hours of bonding. The game may keep the level it sets: use a test save.

## Unit test results

`dotnet test tests/HorsePersonalities.Tests`: **362 passed, 0 failed** (42 new).

- **Prized horses:** always good with no bad-side trait over 10,000 rolls for 4 breeds; still get quirks; wild ones never Unbreakable and Hard exactly when the boosted score passes the line; stable horses and Prized-tab models are prized, a saddled stolen horse is not; saved and loaded.
- **Phase 5:** the reveal at each bond level; originals learned at bond 1 with vanilla bars; one rank at bond 2, the second at bond 3; a game reset put back; the bonus rank ignored; ranks inside 0 and the game's max; restore; temporary horses untouched; originals saved.
- **Converter:** the Prized tab, with every bad entry rejected (a gang horse, an unknown reason, a duplicate).

## In-game results (Nov 27, 2026, home test Stage 9)

Tested on the Thoroughbred (reversed dapple black, original health and stamina base rank 2) with test mode's *Prized stats* preset (health +1, stamina -2), then with test mode off.

| Row | Result |
| --- | --- |
| 9.1 Originals stored | Pass: `original health base rank is 2`, `original stamina base rank is 2` |
| 9.2 Overlay | Pass: *good horse (prized)*, health +1 and stamina -2 |
| 9.3, M42, 9.4 Reveal by bond level | Pass. Bond 2 came from a real pat (XP 348 to 353) and the mod applied it within 0.1 s: health 2 to 3, stamina 2 to 1. Bond 3 (trigger 42): stamina 1 to 0, health unchanged. Bond 4 and the step back to bond 1 were not tried. |
| 9.5 Saddle change | Not tried |
| 9.6 Save, quit, reload | **The game resets the base ranks to vanilla on load.** The mod re-applied them 0.2 s after loading. |
| 9.7 Stable the horse, take it out | **The game keeps the mod's base ranks**; nothing needed re-applying. Stable cutscenes held behaviors as designed. |
| 9.8 Buy a horse | Pass: a stable-bought Dutch Warmblood rolled *good horse (prized)* |
| 9.9 RestoreVanillaStats | Bars vanilla, but only because the game had already reset them on load; no `[RestoreVanillaStats]` line, since that line is written only for a change |

### What this means

- **Uninstalling is safe.** The game does not keep a changed base rank across a reload, so removing the mod and reloading gives vanilla stats. `RestoreVanillaStats` stays as a safety net. The README should say this.
- **The mod must re-apply every session**, which it does within a second of loading.

### Two problems found and fixed

1. **`_SET_MOUNT_BONDING_LEVEL` does nothing.** The hash matches the hook's own wrapper, but the bond rank stayed the same after three presses. DevTools trigger 42 now also sets the bond XP to the amount the next level starts at (`GET_DEFAULT_ATTRIBUTE_POINTS_NEEDED_FOR_RANK`, hash taken from the hook), the way a pat crossing the threshold does. Tested: bond 2 to 3, XP 361 to 950.
2. **Phase 6's bond pace shrank trigger 42's jump.** It scales every bond XP gain each second. Gains over 300 XP in one step (bonding never gives that much; the largest seen was +35) are now left alone and logged at the Debug level. This also covers a trainer's "max bond".

### Open problem for Phase 6: the game overrides bond XP the mod sets

About three minutes after trigger 42 set bond XP to 950, with nothing pressed, the game put it back: `Bond: rank 3 -> 2, XP 950 -> 363` (21:25:22). The mod followed and re-applied the bond 2 stats. Phase 6's bond-pace scaling seems to be undone the same way: a pat gave +17, the mod scaled it to +18 (x1.11), and the next reading was 380, which is +17 on 363.

So the game keeps the real bond XP somewhere the mod doesn't write, and copies it back. Setting `PA_BONDING` points therefore only lasts a few minutes:

- Trigger 42 is still useful for testing, but check the bars right after pressing it.
- **Bond pace (Bond affinity, slower bond 4 for good and prized horses, Hard wild horses) probably has no lasting effect in game.** Check it in home test row 10.10 before relying on it, and look for another way to change bonding speed.

## Known issues and things only the game can answer

1. **Does the game keep a changed base rank?** If it saves it, removing the mod leaves changed horses; `RestoreVanillaStats` is the fix, and the README must say so. If the game resets it on load or at a stable, the mod re-applies within a second.
2. **Does setting the base rank show on the bars at all**, or only the total? Phase 1 changed the bonus rank, not the base.
3. **`_SET_MOUNT_BONDING_LEVEL`** (DevTools 42) is unverified. If it does nothing, test the reveal by bonding normally: brushing, feeding and riding.
4. **Max rank:** `GET_MAX_ATTRIBUTE_RANK` read 9 for the Thoroughbred; a horse whose base is already 9 can't go up.

## In-game test steps

Back up your save first: this phase writes stats into the game.

1. Build, copy both DLLs and `HorsePersonalities/breeds.json` into `scripts`. In `HorsePersonalities.ini`, delete the `MaxStatVariance` and `RevealPerBondLevel` lines, or delete the file so a fresh one is written.
2. Optional, for prized rolls on your current horses: set `ResetAllHorses = true` once.
3. Load, press F9, and mount your saddle horse. The log should say `original health base rank is ...` and `original stamina base rank is ...`.
4. Open the horse's stat card and note its health and stamina bars. Check its natural stats on the overlay (`health +1`, `stamina -1` and so on).
5. **Bond 1:** the bars match the vanilla horse.
6. Use DevTools trigger **42** to step to bond 2. Within a second, a horse with health +1 gains a health segment, one with -1 loses one. Check the log line for each change.
7. Step to bond 3: only a -2 horse drops a second segment. Step to bond 4: nothing more changes.
8. Change saddles: the yellow bonus segments change, but the mod's change to the base stays.
9. Save the game, quit, reload. Within a second of mounting, the bars are the same as before the reload. Note whether the log shows a new change (the game reset it) or none (the game kept it).
10. Visit a stable, take the horse out again: same check as step 9.
11. Set `RestoreVanillaStats = true`, reload, mount the horse: the bars go back to vanilla, and the log says so.
12. Buy a horse (or reset your horses) and check the overlay says `good horse (prized)`.

**Done when** two horses of one breed show different bars by bond level 3.
