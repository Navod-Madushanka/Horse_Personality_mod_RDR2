# Phase 7b report: coat personalities and signature behaviors

Written Nov 27, 2026. Built and unit tested (505 tests pass); not run in game yet.

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

## Turkoman coats (added Nov 27)

| Coat | Archetype | Leans (C/N/T/B) | Quirks | Signatures |
| --- | --- | --- | --- | --- |
| Black, Chestnut, Grey, Perlino | The Elite Adrenaline Junkie (baseline) | +2 / -1 / +1 / +1 | Head-tosser | Restless, Trail snob |
| Dark Bay | The Aristocratic Assassin | +2 / -2 / +1 / 0 | Kicker | War horse, Snob, Silent stealth, Mud tantrum, Restless; loves sugar cubes, ignores haycubes |
| Gold (prized: community favorite) | The Hollywood Action Star | +1 / +1 / +1 / +1 | Head-tosser, Chatterbox | Showboat, Must lead, Vain, Restless, Trail snob |
| Silver | The Stoic Ghost | +2 / -2 / +1 / 0 | Kicker | Town disdain, Stoic, Restless |

| Signature | How it plays |
| --- | --- |
| Restless | Standing still 10 s with you on it or near: agitated, 1.5/min |
| Trail snob | After 30 s of walking: an insulted snort, 0.5/min |
| War horse | Never spooks while someone is fighting you; off it, goes for an enemy within 5 m |
| Snob | Glares at a cheaper horse within 8 m (Standard tier or lower, not a Turkoman), 0.5/min |
| Showboat | Stop from a gallop with 3+ people within 15 m: 60% rear |
| Must lead | Behind a moving horse within 12 m: agitated, 1/min |
| Vain | Every brushing: a happy nicker and call |

The Turkoman is Standard in this mod's stat tiers, so Snob's "lower tier" means its own tier or lower, other than Turkomans. Cut: charging ambushes and sure footing (its Courage instead), the muddy pelt (Mud tantrum), jumping wagons and splashing rivers (no steering), the Silver's sidewalk bite (Town disdain).

## American Standardbred coats (added Nov 27)

| Coat | Archetype | Leans (C/N/T/B) | Quirk | Signatures |
| --- | --- | --- | --- | --- |
| Light Buckskin | The Thin-Skinned Sports Car (baseline) | -1 / +2 / 0 / +1 | Head-tosser | Snake drama, Hill sigh |
| Black (prized: Rare) | The Midnight Legend | 0 / +1 / +1 / 0 | Head-tosser | Trail snob, Restless, Night owl, Snake drama |
| Silver Tail Buckskin | The Golden Boy | 0 / +1 / 0 / +1 | Chatterbox | Racer, Spoiled, Showboat, Clumsy, Scatterbrain, Snake drama; loves sugar cubes |
| Palomino Dapple | The Teacher's Pet | -2 / +2 / -2 / +2 | Shadow | Separation anxiety, Vain, Snake drama |
| Buckskin | The Track Rookie | 0 / +1 / 0 / +1 | Head-tosser | No brakes, Scatterbrain, Clumsy, Snake drama |

| Signature | How it plays |
| --- | --- |
| Racer | Another moving horse within 15 m while you ride at pace: a challenging call, 1/min |
| Spoiled | Stopping after 20 s+ of galloping: 70% it calls for a treat |
| Separation anxiety | Left standing with you 3 m+ away (any distance): whinnies, 3/min |
| No brakes | Gallop to under walking pace within 1.5 s: 50% a sideways skid |

Cut: swamps (no swamp check yet), the Black's bear-pelt tantrum (no pelt moment), the binoculars (Restless covers standing still), fences and rivers (no steering), trees and squirrels (Scatterbrain).

## Nokota coats (added Nov 27)

All three Nokota coats have a design, so the baseline's Grudge is on each of them.

| Coat | Archetype | Leans (C/N/T/B) | Quirk | Signatures |
| --- | --- | --- | --- | --- |
| Reverse Dapple Roan (prized: community favorite) | The Stunt Driver | +1 / +1 / +1 / +1 | Head-tosser | Showboat, Downhill snob, Grudge |
| Blue Roan | The Scarred Survivor | +1 / +1 / +1 / -1 | Nosy | Stranger danger, Vain, Grudge; loves oatcakes |
| White Roan | The Wild Child | 0 / +1 / 0 / +1 | Wanderer | Wanderlust, Selective hearing, Restless, Saddle hater, Grudge |

| Signature | How it plays | Needs in game |
| --- | --- | --- |
| Grudge | Hurt while you ride it: snorts and stamps, then 2 minutes of nothing back for pats and brushing; a treat forgives it | Trigger 60 (does the damage event fire on a crash?) |
| Downhill snob | Held to a walk going steeply downhill: an offended snort, 1/min | - |
| Stranger danger | Someone hurrying toward you within 6 m: spins on them, agitated and glaring. Never a real kick | - |

Cut: low health (breed stats), slippery when spooked (its Nerves), cornering and wagon jumping (no steering), joining a deer herd (Wanderlust wanders off).

## Thoroughbred coats (added Nov 27)

| Coat | Archetype | Leans (C/N/T/B) | Quirk | Signatures |
| --- | --- | --- | --- | --- |
| Black Chestnut | The High-Strung Drama Queen (baseline) | -1 / +2 / +1 / +1 | Head-tosser | Needs soothing, Clumsy |
| Brindle | The Exotic Supermodel | -1 / +2 / +1 / 0 | Chatterbox | Snake drama, Mud tantrum, Vain, Needs soothing, Clumsy |
| Reverse Dapple Black (prized: Special) | The Brooding Outlaw Prince | +1 / 0 / +1 / 0 | Head-tosser | Snob, Guard dog, Town disdain, Night owl |
| Blood Bay | The Untamed Rebel | +2 / +1 / +2 / +1 | Head-tosser | No brakes, Needs soothing, Clumsy |
| Dapple Grey | The Loyal Veteran | +1 / -1 / -1 / +2 | Shadow | Hill crawler, Hill sigh, Water sigh, Apple beggar; loves apples |

| Signature | How it plays | Needs in game |
| --- | --- | --- |
| Needs soothing | After any danger, frets (call and stamp) 2/min for up to 2 minutes, until you pat or calm it | - |
| Hill crawler | Climbing steeply: capped at a walk, even when spurred | Trigger 45 |
| Water sigh | Moving through water: a heavy sigh, 1/min | Trigger 61 |
| Apple beggar | You on foot next to it for 10 s: nickers and looks at you, 1/min | - |

Rachel, John's Dapple Grey in the epilogue, is a story model (`a_c_horse_gang_john`) and stays vanilla, as decided for gang horses. Cut: height (the game's), swamps (no check yet), the carcass tantrum (no carcass moment), overshooting turns and hurdling logs (no steering or jumping), the nudge (no animation).

## Andalusian coats (added Nov 28)

All three Andalusian coats have a design, so the baseline's War horse is on each of them.

| Coat | Archetype | Leans (C/N/T/B) | Quirk | Signatures |
| --- | --- | --- | --- | --- |
| Perlino (prized: Rare) | The Waterfall Spirit | +2 / -2 / +1 / +2 | Shadow | War horse, Guard dog, Jealous |
| Dark Bay | The Heavy Enforcer | +2 / -2 / +1 / 0 | Nosy | War horse, Hill crawler, Hill sigh |
| Rose Gray | The Pit-Fighter | +2 / -2 / +2 / -1 | Kicker | War horse, Short king, Hates brushing |

New: **Hates brushing** (every brushing gets a stamp and a snort). Cut: slowness, getting winded and tank handling (stats and the game's handling), jumps (no control), refusing to flee (no hook into the command; Shadow keeps it close), wagons (Short king covers horses).

## Ardennes coats (added Nov 28)

All three Ardennes coats have a design, so the baseline's War horse is on each of them.

| Coat | Archetype | Leans (C/N/T/B) | Quirk | Signatures |
| --- | --- | --- | --- | --- |
| Strawberry Roan | The Heavyweight Brawler | +2 / -2 / +2 / 0 | Kicker | War horse, Wolf brawler, Short king, Restless |
| Bay Roan | The Gentle Giant | +2 / -2 / -2 / +2 | Shadow | War horse, Unhurried |
| Iron Grey Roan | The Silent Juggernaut | +2 / -2 / +1 / 0 | Nosy | War horse, Balker, Stoic |

New: **Balker** (steep climb or water: plants its hooves, a refusal, 2/min, however hard you spur; water needs trigger 61). Cut: plowing through trees and riders, battleship turning, the long stop and running off cliffs (no movement control), head shoves (no nudge animation), bullying horses off the hay (Short king squares up), wagons, doorways and fences.

## Testing it

- **Test mode** has a new last preset, **Coat design**: a coat model gets exactly its written design, with no dice. The Arabian, Fox Trotter and Mustang coat models are assigned to it, so in test mode every one you meet is its design. Other presets on an Arabian carry no signatures (F11 cycles as before).
- **DevTools triggers 43-55** check each native the signatures rely on. See the home test doc, Stage 12.

## Open

- Treat item codes (`data/treats.csv`): feed each treat and copy the code from the log.
- Hill sigh's sound and climb threshold after triggers 51-55.
- Navod writes the other breeds' coats. Decided Nov 27: the White Arabian, the Tiger Striped Bay, the Black Standardbred and the Perlino Andalusian are prized (Rare) and the Silver Dapple Pinto, Gold Turkoman and Reverse Dapple Roan Nokota prized (community favorites); the Reverse Dapple Black Thoroughbred prized (Special).
