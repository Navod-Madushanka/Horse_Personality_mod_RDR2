# Phase 7b report: coat personalities and signature behaviors

Written Nov 27, 2026. Built and unit tested (478 tests pass); not run in game yet.

## What changed

- **Coats tab → breeds.json.** `data/sheet-export/Coats.csv` (the sheet's Coats tab) and `data/treats.csv` go into `breeds.json` (schema 7). The converter and the mod both check every row: the model must belong to the named breed, quirks and signatures must exist (cut ones can't be used), treats must be in `treats.csv`, leanings -2..+2, demeanor 0..2, at most 2 signature quirks.
- **Rolls around the coat.** For a model with a coat, the coat's leanings replace the breed's; one horse strays from them by *Coat spread* (0.3, versus 0.6 for breeds) and half the usual good/bad shift. So two Ice Queens are both brave and slow to win over, but not identical. The coat's signature quirks always show (a single one may get a rolled companion), and the horse carries the coat's archetype and signatures. The overlay shows `Coat: The Ice Queen - Town disdain, Mud tantrum, Snake drama`.
- **Other horses don't change.** A model with no coat rolls exactly as in Phase 7 (same seed, same horse).
- **Saved.** `horses.json` keeps `archetype` and `signatures`. Horses rolled before this phase keep their old personality, with no signatures.
- **Coat demeanor and treats** replace the breed's for horses rolled with a coat. A loved treat gets a happy call; an ignored one a dismissive snort (the Ice Queen and wild carrots). This needs the treat's item code in `treats.csv` (all blank for now; the log shows the code of whatever you feed).

## The signatures

All share one cooldown (*Signature cooldown*, 60 s) and the 20 s gap, and are held in missions and cutscenes.

| Signature | How it plays | Needs in game |
| --- | --- | --- |
| Snake drama | Snakes count as threats from 3x the predator range | Every snake model is now a threat for all horses (5 added) |
| Moody refusal | Quick stop at a walk, 0.15/min (1.5x at bond 1) | - |
| Mud tantrum | Agitated when dirt rank ≥ Fussy dirt rank | Dirt scale unverified (Phase 7) |
| Town disdain | In town, snort and stare at someone within 6 m, 1/min | Trigger 43 |
| City lover | Random spooks x0.5 in towns | Trigger 43 |
| Night owl | Random spooks x0.5 at night instead of x night multiplier | - |
| Dramatic rear | Rears (50%) when you start aiming, standing still, from 18:00 | Trigger 47 |
| Escape artist | Left standing 60 s with you 20 m+ away: unhitch, wander | Trigger 44 |
| Prankster | Sitting still near people fighting you, before you fire: throws you, 0.5/min | Trigger 48 |
| Lazy pace | Capped at a walk unless you spur (every frame) | Trigger 45 |
| Roadside grazer | Sit still on it 10 s: it grazes 12 s, once per stop | Trigger 46 |
| Hill sigh | Walking uphill faster than 0.8 m/s up: a sigh, 1/min | Triggers 51-54 (sound), 55 (threshold); a snort for now |
| Brush excitement | 90%: agitated, then happy, as the brushing starts | - |
| Scatterbrain | Skid at a gallop, 0.3/min, no fear | - |
| Wolf brawler | Off it: attacks a predator for 4 s instead of spooking; ridden: eyes it, unafraid | Trigger 49 |
| Soul stare | 6 s stare at a stranger within 4 m, 1/min | - |
| Camp creeper | You near, off it: follows someone within 10 m for 6 s, 0.3/min | Trigger 50 |

## Testing it

- **Test mode** has a new last preset, **Coat design**: a coat model gets exactly its written design, with no dice. The seven Arabian coat models are assigned to it, so in test mode every Arabian you meet is its design. Other presets on an Arabian carry no signatures (F11 cycles as before).
- **DevTools triggers 43-55** check each native the signatures rely on. See the home test doc, Stage 12.

## Open

- Treat item codes (`data/treats.csv`): feed each treat and copy the code from the log.
- Hill sigh's sound and climb threshold after triggers 51-55.
- Navod writes the other breeds' coats; the White Arabian on the Prized tab as Rare is still his call.
