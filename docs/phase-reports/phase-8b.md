# Phase 8b report: admired horses

Written Nov 28, 2026, at Navod's request. Built and unit tested (568 tests pass); not run in game yet (no game access). Home test Stage 16.

## What was asked

In the game, people compliment a fine horse (an Arabian, a Turkoman, a Missouri Fox Trotter) as you ride past. Navod wants that for every horse that is clean, fit and at bond level 4.

Decisions (Navod, Nov 28): bond 4 only; "fit" means both cores (the inner rings) are well fed; compliments come in towns and on roads, with a cooldown.

## What was built

- **When a horse is admired:** bond 4, health and stamina cores both 70 or more, dirtiness rank 1 or less. Only horses with a personality (every breed in the sheet).
- **Who says it:** while you ride it slower than 5 m/s or stand within 4 m of it on foot, the nearest person within 8 m who has the line, isn't in combat with you, isn't hogtied and isn't already talking. They glance at the horse (3 s) and, 0.6 s later, say the game's own **NICE_HORSE** line in their own voice. Gang members say **PLAYER_HORSE_IN_GOOD_CONDITION** when their voice has it.
- **How often:** 2 per minute while someone is in range, but at most one per 180 s, and the same person at most once per 10 minutes. Held in missions and cutscenes like every behavior.
- **Off switch:** `NpcCompliments = false` in the INI.
- **Logs:** *complimented by person ...* at Info; *admired* / *not admired (why)* at Debug when it changes.
- **The sheet:** nine rows in the Behavior tab (*Admire bond*, *Admire min core*, *Admire max dirt rank*, *Admire max speed*, *Admire beside distance*, *Admire range*, *Admire rate*, *Admire cooldown*, *Admire same person minutes*).
- **Code:** `Core/Behavior/AdmirationBrain.cs` decides; `BehaviorRunner.Admire` reads the game once a second (the people scan only runs when the horse qualifies and the cooldown is over). Three new `IGameApi` calls: `HasSpeechLine` (`DOES_CONTEXT_EXIST_FOR_THIS_PED`), `IsSpeaking` (`IS_ANY_SPEECH_PLAYING`) and `Say` (`PLAY_PED_AMBIENT_SPEECH_NATIVE` with the 64-byte speech-params struct).

## Where the lines come from

femga's audio-bank dump (rdr3_discoveries, `audio/audio_banks`) lists `NICE_HORSE_01` in 364 voice banks: ordinary townsfolk across the map. `PLAYER_HORSE_IN_GOOD_CONDITION` is in 23 banks (camp members). The speech name drops the number (`NICE_HORSE`); variation 0 picks a random take.

## Guesses the game has to confirm (Stage 16)

- **The speech call:** no voice name (so the person's own voice), `speech_params_force_normal`, the player as listener. Trigger 67 tries this; trigger 68 tries `speech_params_force` with no listener. If only 68 works, the mod switches to it.
- **The dirtiness scale:** the stat readout now logs Dirtiness; 16.4 notes the rank clean and dirty, to set *Admire max dirt rank*.
- **Doubling up:** the game's own compliments for its fine breeds still happen. An Arabian may hear both; the 3 minute cooldown only covers the mod's.

## DevTools triggers

- 66: the nearest person's horse lines (read only).
- 67: the nearest person says "Nice horse" (force normal, to you).
- 68: the same with the other speech settings.
