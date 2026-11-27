# Phase 7b report: coat personalities and signature behaviors

Written Nov 27, 2026. Built and unit tested (490 tests pass); not run in game yet.

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

## Missouri Fox Trotter coats (added Nov 27)

| Coat | Archetype | Leans (C/N/T/B) | Quirk | Signatures |
| --- | --- | --- | --- | --- |
| Black Tovero, Blue Roan, Buckskin Brindle, Dapple Grey, Sable Champagne | The Blue-Collar Athlete (baseline) | +1 / -2 / -1 / 0 | Shadow | Stoic |
| Silver Dapple Pinto (prized: community favorite) | The Elusive Celebrity | +2 / -1 / 0 / -1 | Nosy | Wanderlust, Escape artist, Bored buck, Stoic |
| Amber Champagne | The Golden Retriever | +1 / 0 / -1 / +2 | Shadow | Clumsy, Snack magnet, Roadside grazer |

| Signature | How it plays |
| --- | --- |
| Stoic | A pat gets nothing back, whatever its traits (brushing and feeding still can) |
| Wanderlust | Untied, you 8 m+ away for 8 s: wanders freely for 40 s, once per stop. Whistle it back |
| Bored buck | You aim from its back, standing still, 8 s: 30% it throws you (bond 1-2) or fidgets (bond 3-4), once per aim |
| Clumsy | A skid at a trot or canter, 0.3/min |
| Snack magnet | At a walk: a quick stop, then 5 s of grazing, 0.3/min |

Cut: cornering like a wagon, knocking over fences (no script control), nudging you while you loot (Shadow keeps it close instead). Mud, rain and hills not bothering it is its Nerves -2 and no Mud tantrum; endurance is the breed's own stamina.

**Fix found while building:** the runner stopped looking at a horse more than 15 m from you, so Escape artist (20 m) could never have fired. Horses with Escape artist or Wanderlust are now watched at any distance, for those two habits only.

## Mustang coats (added Nov 27)

| Coat | Archetype | Leans (C/N/T/B) | Quirks | Signatures |
| --- | --- | --- | --- | --- |
| Black Overo, Buckskin, Chestnut Tovero, Golden Dun, Red Dun Overo | The Short King (baseline) | +2 / -2 / +1 / 0 | Nosy | Short king, Selective hearing |
| Tiger Striped Bay (prized: Rare) | The Scarred Mercenary | +2 / -2 / +2 / -1 | Kicker | Wolf brawler, Saddle hater, Bounty stomper, Short king, Selective hearing |
| Grullo Dun | The Stoic Bodyguard | +1 / -2 / 0 / +1 | Nosy | Guard dog, Silent stealth, Unhurried, Short king |
| Wild Bay | The Scrappy Underdog | +1 / 0 / -1 / +2 | Shadow, Chatterbox | Clumsy, Jealous, Selective hearing |

| Signature | How it plays | Needs in game |
| --- | --- | --- |
| Short king | Squares up to a horse within 8 m (agitated, glaring), 0.5/min. Never a real fight | - |
| Selective hearing | Your whistle, with it off on its own: 30% it cancels its come-to-you and noses the ground 6 s | Triggers 56-57 |
| Saddle hater | 40% nip and head toss as you get on | - |
| Bounty stomper | Stamps and glares at a hogtied person within 3 m, 1/min. Never tramples (your bounty would die) | Trigger 58 |
| Guard dog | Left alone, at any distance: stomps at a stranger within 3 m, 2/min | - |
| Silent stealth | No sounds from the mod while you sneak | Trigger 59 |
| Unhurried | Spurring with no danger in 60 s: capped at a trot, one snort | Trigger 45 |
| Jealous | You pat, brush, feed or ride another horse within 20 m of it: call and stamp, 80% | - |

Cut: shortcuts, charging brush, cliffs like stairs (no steering or terrain control); stuck between trees and walking into campfires (Clumsy stands in); the girth nip (Saddle hater plays on mounting).

## Testing it

- **Test mode** has a new last preset, **Coat design**: a coat model gets exactly its written design, with no dice. The Arabian, Fox Trotter and Mustang coat models are assigned to it, so in test mode every one you meet is its design. Other presets on an Arabian carry no signatures (F11 cycles as before).
- **DevTools triggers 43-55** check each native the signatures rely on. See the home test doc, Stage 12.

## Open

- Treat item codes (`data/treats.csv`): feed each treat and copy the code from the log.
- Hill sigh's sound and climb threshold after triggers 51-55.
- Navod writes the other breeds' coats. Decided Nov 27: the White Arabian and the Tiger Striped Bay are prized (Rare) and the Silver Dapple Pinto prized (community favorite).
