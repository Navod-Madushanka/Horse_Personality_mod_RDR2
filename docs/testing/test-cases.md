<!-- Exported from the shared test cases doc (claude.ai artifact DbfpyLBvLjbuL9NkK8Zfyf) on Nov 28, 2026.
     The doc is the working copy (Result dropdowns, notes); this file is the snapshot kept with the code. -->

# Horse Personalities: Test Cases Left To Do

Nov 28, 2026 · @Navod

Every test left before version 1.0, stage by stage. Each row is one test: its name, why it matters, how to do it, what to look at, and what should happen. Mark the Result as you go and write in Notes only when something is odd.

## Before you start

About 15 minutes, outside the game.

- [ ] **Back up your save folder:** `Documents\Rockstar Games\Red Dead Redemption 2`. Several tests change horse stats and bond levels.
- [ ] **Get the latest code:** `git pull`, then build the solution in Visual Studio (Release).
- [ ] **Copy into the game's `scripts` folder:** `HorsePersonalities.dll`, `HorsePersonalities.DevTools.dll`, and the `HorsePersonalities` folder with `breeds.json`.
- [ ] **Clean start:** delete `scripts\HorsePersonalities\log.txt` so the log only has this session.
- [ ] **INI for testing:** in `HorsePersonalities.ini` set `LogLevel = Debug` (more log lines). Leave everything else at its default unless a test says otherwise.
- [ ] **A good save:** free roam, your own horse, money, a brush, a few kinds of horse food, near a town and a stable.
- [ ] **Optional:** Rampage Trainer, to spawn wolves and people faster.

## Keys

| Key | What it does |
| --- | --- |
| F9 | Overlay: the horse's hidden personality, coat, skills and bond |
| F10 | DevTools test menu (the numbered triggers) |
| Page Down / Page Up | Pick the next or previous trigger |
| End | Run the picked trigger on your horse |
| F11 | Test mode only: give the horse you ride the next preset |
| Insert | Reload all scripts (use after changing the INI) |

**"Trigger 42"** means trigger number 42 in the F10 menu. **"Preset"** means a fixed test personality, used when `TestPersonalities = true`. Every behavior writes a line in `log.txt` naming what it did and why.

**Order:** Stage 9 leftovers, then 10 to 14 and 16, and Stage 15 (the release check) last.

## Stage 9 leftovers: stat bars

Most of Stage 9 passed on Nov 27. These three were not tried.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 9.4 Bond 4 changes nothing new | The stat reveal should stop at bond 3 | Use trigger 42 to raise your horse from bond 3 to bond 4 | Health and stamina bars, `log.txt` | No bar changes and no new stat log line |  |
| 9.5 Saddle swap keeps the mod's change | Saddles add their own bonus; the mod must not fight it | Change the saddle on a horse whose bars the mod changed | Health and stamina bars | The yellow bonus part changes with the saddle; the white base part stays as the mod set it |  |
| 9.9 Restore vanilla stats | Players need a safe way back before uninstalling | Ride a horse the mod changed. Set `RestoreVanillaStats = true`, press Insert, stay on the horse a few seconds | Bars, `log.txt` | Bars go back to vanilla and the log has a *\[RestoreVanillaStats\]* line. Set it back to false after |  |

## Stage 10: core behaviors (Phase 6)

The first behaviors driven by personality. Check each horse's traits on the overlay (F9) before you start a test. Rampage Trainer makes spawning wolves easier.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 10.1 Missions stay vanilla | Behaviors must never break a story mission | Start a story mission, play a bit, finish it | `log.txt`, the horse | *Behaviors held: story mission* at the start, *Behaviors back on.* at the end. No spooks or bucks during it |  |
| 10.2 Timid horse fears wolves | Courage is the main trait players will notice | Ride a horse with Courage -1 or lower. Spawn wolves about 10 m away | The horse, `log.txt` | It rears (or throws you if its Temper is +1 or more). Each wolf counts only once |  |
| 10.3 Brave horse stays calm | Brave horses must feel different from timid ones | Same as 10.2 with a horse of Courage +1 or higher | The horse | Mostly calm; at most a little agitated |  |
| 10.4 Loose horse bolts | A scared horse without you should run, a brave one should watch | Get off, stand 5 m from a timid horse, spawn a wolf. Then try a brave horse | The horse | Timid: runs off for about 4 s. Brave: turns to watch the wolf |  |
| 10.5 Gunfire scares | Gunfights are where timid horses matter most | Fire your gun near a timid horse. Then let a stranger fire near it | `log.txt` | Lines name *Gunfire* (stranger) or *RiderGunfire* (you). Note if far-off shots count too |  |
| 10.6 Nervous horse spooks at nothing | Nerves should show without any threat | Ride a horse with Nerves +1.5 or higher at night for 15 minutes | The horse, `log.txt` | One or two *spooked at nothing* (a skid at a gallop, otherwise a rear or agitation) |  |
| 10.7 Hot-tempered horse fights pushing | Temper should punish riding it too hard | On a Temper +1 or higher horse: sprint 30+ s, spur when stamina is low, stop hard from a gallop | `log.txt` | Now and then it throws you, logged *pushed (...)*. An average horse only refuses, rarely |  |
| 10.8 Kick from behind | Standing behind a bad-tempered horse should be risky | Away from town, stand right behind a Temper +1.5 horse for 3 s, step away, repeat | The horse | Within a few tries it kicks you |  |
| 10.9 Hard and Unbreakable wild horses | Breaking difficulty is rolled per wild horse | Find a wild horse; the overlay shows Normal, Hard or Unbreakable. Try to break it | The horse, `log.txt` | Hard: throws you about 2 extra times, then breaks. Unbreakable: throws you every time (try 5) |  |
| 10.10 Bond speed by affinity | Bond affinity should change how fast bond grows | Ride a horse with Bond affinity far from 0 for 10 minutes | `log.txt` | *bond XP ... scaled* lines. Note: Stage 13 checks whether the game keeps the change |  |
| 10.11 Feels right overall | Behaviors must feel alive, not annoying | Ride your own horse 30 minutes as you normally play | You | It feels alive. Note anything that happened too often |  |

## Stage 11: quirks, care and demeanor (Phase 7)

Turn on test mode first: `TestPersonalities = true` in the INI, then press Insert. Press F11 on a horse to switch it to the next preset; the overlay shows which one it has. Give each quirk about 5 minutes. Do Stage 12 before 11.16.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 11.1 Chatterbox | Checks the calling quirk and its limit | **Nervous** preset: ride near other horses, then alone | Listen, `log.txt` | Calls or snorts now and then, more near other horses; never more than once every 2 minutes |  |
| 11.2 Kicker | A Kicker kicks anyone behind it, even in town | **Hothead** preset: stand it still with a person right behind it, first outside town, then in town near a lawman. Then a dog behind it | The horse, `log.txt` | It kicks the person both times, and the dog. In town the law may come (that's by design) |  |
| 11.3 Head-tosser | Checks the idle fidget quirk | **Grumpy** preset: leave it standing next to you | The horse | Restless head tosses now and then |  |
| 11.4 Nosy | Checks the horse watching people | **Coward** preset: stand by it on a road with people passing | Its head | It turns to watch people and horses going by |  |
| 11.5 Shadow | Checks the follow-you quirk | **Brave** preset: get off and walk away | The horse | It follows you a few steps, about 6 s |  |
| 11.6 Wanderer | Checks the drift-off quirk | **Steady** preset: get off, don't hitch it, walk 10 m away, wait 30 s. Then try once hitched | The horse | Unhitched: it wanders off, then grazes. Note what happens when hitched |  |
| 11.7 Picky eater | Each horse should love some treats and ignore others | **Gentle** preset: feed two kinds of treat, each twice | The horse, `log.txt` | Same treat, same reaction both times (loved: call and nicker; ignored: snort). Copy the item codes from the log into Notes |  |
| 11.8 Affectionate care | Kind horses should thank you | **Gentle**: pat it, then brush it | The horse | A soft nicker after each |  |
| 11.9 Grumpy care | Grumpy horses should complain | **Grumpy**: pat, brush and feed it | The horse | Head toss or stamping after each |  |
| 11.10 Aloof care | Aloof horses should give little back | **Aloof**: pat, brush and feed it | The horse | Nothing after pat and feed; fidgets after brushing |  |
| 11.11 Grumpy calming | A grumpy horse resists being calmed | **Grumpy**: let a wolf scare it, then calm it | The horse, `log.txt` | It fights the calming (agitated again) |  |
| 11.12 Impatient breeds | Breed demeanor: hot breeds hate waiting | Any preset on a **Turkoman** or **Thoroughbred**: leave it standing a minute | The horse, `log.txt` | It fidgets (*demeanor Impatient*) |  |
| 11.13 Alert breeds | Breed demeanor: watchful breeds look around | Any preset on an **Arabian** or **Mustang**: stand by it with people around | The horse, `log.txt` | It looks around at them (*demeanor Alert*) |  |
| 11.14 Fussy breeds | Breed demeanor: fussy breeds hate dirt | Roll a **Turkoman** in mud | `log.txt` | *demeanor Fussy* snorts. If never, run *Read all horse stats* and copy the Dirtiness line into Notes |  |
| 11.15 Quirks off in missions | Quirks must not break missions either | Start a mission with the **Nervous** preset | The horse, `log.txt` | No calls and no quirks during it |  |
| 11.16 Back to real horses | Test mode must not touch your real save | After Stage 12: set `TestPersonalities = false`, press Insert | Overlay | Your real horses have their own personalities again |  |

## Stage 12: coat personalities (Phase 7b)

Still in test mode, so each coat horse gets exactly its written design (preset **Coat design**; the overlay shows *Coat: name*). Part A checks the game can do each habit with a DevTools trigger. Then ride each coat you can get. Every habit writes a log line with its name, shown here in italics.

### Part A: can the game do it? (triggers 43 to 55)

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.1 Town check | Town habits need to know you're in town | Run trigger 43 in Valentine, then on a trail | Panel | True in town, False outside |  |
| 12.2 Unhitch | Escape artist frees itself | Hitch your horse, walk 5 m away, run trigger 44 | The horse | It frees itself and drifts off |  |
| 12.3 Walk cap | Lazy and slow horses hold a walk | Ride, run trigger 45, push forward without spurring, then spur | The horse | Stuck at a walk; spurring still gallops |  |
| 12.4 Graze with rider | Roadside grazer eats with you on it | Sit still on the horse, run trigger 46 | The horse | It grazes with you on its back |  |
| 12.5 Aiming | Some habits react to you aiming | Run trigger 47, then aim your gun on horseback | `log.txt` | Turns True while aiming |  |
| 12.6 Hostiles count | Fight habits need to know who's hostile | Run trigger 48 in a standoff, then when nobody is hostile | Panel | Counts the hostiles; 0 when calm |  |
| 12.7 Attack animal | Wolf brawler fights predators | Off the horse near a wolf or coyote, run trigger 49 | The horse | It goes for the animal and stops after 4 s |  |
| 12.8 Follow a person | Camp creeper sneaks up on people | Off the horse in camp near someone, run trigger 50 | The horse | It walks up behind them, then stops |  |
| 12.9 Sigh sound | Several habits need a sigh | Run triggers 51 to 54 one by one | Listen | Write in Notes which one sounds like a heavy sigh (silence means no such sound) |  |
| 12.10 Climb speed | Hill habits need the climbing speed | Run trigger 55, walk up a steep hill | `log.txt` | Write the upward speed on a steep climb in Notes |  |

### Part B: Arabian coats

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.11 Coat shows | The right design must load for each coat | Get on any Arabian, open the overlay (F9) | Overlay | *Coat: name - habits* matches the Coats tab |  |
| 12.12 Snake drama | Arabians fear snakes from far off | Any Arabian: ride near a snake | The horse, `log.txt` | It reacts from much further away than to a wolf |  |
| 12.13 Moody refusal | Proud coats sometimes refuse | Grey, Black or Rose Gray Bay: ride at a walk a few minutes | The horse, `log.txt` | An occasional refusal, rarer at higher bond |  |
| 12.14 Town disdain | The White Arabian looks down on townsfolk | White (or Grey): ride through Saint Denis with people close | The horse, `log.txt` | Snorts and stares at people |  |
| 12.15 Dramatic rear | The Black loves drama at night | Black: stand still at dusk, draw your gun | The horse | It rears about half the time |  |
| 12.16 Escape artist | The Black won't stay tied | Black: hitch it, walk 20 m away, wait a minute | The horse | It unhitches and wanders |  |
| 12.17 Prankster | The Black throws you when trouble starts | Black: sit still on it while hostiles square up; don't fire | The horse, `log.txt` | Sometimes throws you; never after you've fired |  |
| 12.18 Lazy, grazer, hill sigh | The Rose Gray Bay is a slacker | Rose Gray Bay: ride without spurring, sit still 10 s, walk up a hill | The horse | Walks slowly (*Lazy pace*), grazes (*Roadside grazer*), sighs (*Hill sigh*) |  |
| 12.19 Brush excitement, scatterbrain | The Red Chestnut is excitable | Red Chestnut: start brushing; gallop a while | The horse, `log.txt` | Excited at the brush; an odd skid at a gallop without fear |  |
| 12.20 Brawler, stare, creeper | The Warped Brindle is the odd one | Warped Brindle: off it near a wolf; then in camp near people | The horse, `log.txt` | Attacks the wolf (*Wolf brawler*), stares at strangers (*Soul stare*), sneaks up on someone (*Camp creeper*) |  |
| 12.21 Treat codes | Loved and ignored treats need the game's item codes | Feed a coat horse each treat it loves or ignores (Coats tab) | `log.txt` | Copy each treat's item code into Notes, for `treats.csv` |  |
| 12.22 Habits off in missions | Coat habits must not break missions | Start a mission on any coat horse | The horse, `log.txt` | Nothing happens; no grazing mid-mission |  |

### Missouri Fox Trotter coats

The Silver Dapple Pinto is at Blackwater's stable; the Amber Champagne runs wild in Scarlett Meadows.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.23 Stoic | Fox Trotters stay cool about affection | Silver Dapple (or any Fox Trotter except the Amber): pat it a few times | The horse, `log.txt` | Nothing back |  |
| 12.24 Wanderlust | The Silver Dapple roams if left loose | Silver Dapple: get off, don't hitch it, walk 10 m away and wait | The horse | After about 8 s it strolls off down the road; whistle it back |  |
| 12.25 Escape artist from far | Checks it is watched even when you're far away | Silver Dapple: hitch it, walk 25 m away, wait a minute | The horse | It unhitches and wanders |  |
| 12.26 Bored buck | It gets bored while you aim | Silver Dapple: sit still on it and aim your rifle 10 s, a few times | The horse, `log.txt` | Now and then it throws you (bond 1-2) or fidgets (bond 3-4) |  |
| 12.27 Clumsy, snack magnet | The Amber is a goofy foodie | Amber Champagne: trot a few minutes, walk, sit still 10 s, then brush it | The horse, `log.txt` | Trips now and then, stops to graze at a walk, grazes when idle, nickers at the brush |  |

### Mustang coats

The Tiger Striped Bay runs wild near Rio Bravo; the Grullo Dun and Wild Bay run wild in the Heartlands. Run triggers 56 to 59 first.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.28 Whistle check | Selective hearing needs to see your whistle | Run trigger 56, then whistle on foot and on horseback | `log.txt` | One line per whistle, button True |  |
| 12.29 Ignore whistle | Checks the horse can pretend not to hear | Off the horse, 30 m away: run trigger 57, whistle; then whistle again | The horse | It starts to come, stops and noses the ground; comes on the second whistle |  |
| 12.30 Hogtied check | Bounty stomper needs to see a tied person | Hogtie someone, stand by them, run trigger 58; again with nobody tied | Panel | 1, then 0 |  |
| 12.31 Sneak check | Silent stealth needs to see you sneaking | Run trigger 59, sneak a few seconds, stand up | `log.txt` | True while sneaking |  |
| 12.32 Short king | Small Mustangs act tough | Any Mustang except the Wild Bay: stand it near another horse a few minutes | The horse, `log.txt` | It squares up now and then, never a real fight |  |
| 12.33 Selective hearing | Mustangs come when they feel like it | Baseline, Tiger or Wild Bay: get off, walk 30 m away, whistle a few times | The horse, `log.txt` | Now and then it ignores you for a few seconds |  |
| 12.34 Saddle hater, stomper, brawler | The Tiger is a mercenary | Tiger Striped Bay: get on a few times; hogtie a bounty next to it; let a wolf come close while you're off it | The horse, `log.txt` | Nips as you get on, stamps at the bounty without hurting it, goes for the wolf |  |
| 12.35 Guard dog | The Grullo guards your saddlebags | Grullo Dun: leave it outside a saloon, go in a minute | `log.txt` | It stomps at people who pass close |  |
| 12.36 Unhurried | It only runs when it matters | Grullo Dun: sprint down a quiet road; then sprint during a fight | The horse, `log.txt` | Snorts and trots on the quiet road; full gallop in the fight |  |
| 12.37 Silent stealth | It keeps quiet when you sneak | Grullo Dun: sneak next to it near a camp | Listen | No sounds from the mod |  |
| 12.38 Jealous | The Wild Bay hates you riding others | Wild Bay as your saddle horse: pat or ride another horse next to it | The horse, `log.txt` | It calls and stamps |  |

### Turkoman coats

The Dark Bay is sold at Blackwater, the Gold at Saint Denis, the Silver at Tumbleweed.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.39 Restless | Turkomans hate standing still | Any Turkoman: sit still on it for 30 s | The horse, `log.txt` | It starts fidgeting after about 10 s |  |
| 12.40 Trail snob | A walk is beneath them | Baseline or Gold: walk it a couple of minutes | The horse, `log.txt` | An insulted snort now and then |  |
| 12.41 War horse | The Dark Bay is built for battle | Dark Bay: ride into a gunfight; then get off with an enemy close | The horse, `log.txt` | Never spooks in the fight; goes for the enemy when you're off |  |
| 12.42 Snob | It looks down on cheaper horses only | Dark Bay: stand it next to a Morgan or a draft; then next to another Turkoman | The horse, `log.txt` | Glares at the Morgan or draft, not at the Turkoman |  |
| 12.43 Coat treats | Each coat loves and ignores its own treats | Dark Bay: feed a sugar cube, then a haycube | The horse, `log.txt` | Happy call for the sugar cube, a snort for the haycube (needs the item codes in `treats.csv`) |  |
| 12.44 Showboat | The Gold shows off for crowds | Gold: gallop into Saint Denis and stop by a crowd | The horse | It rears, more often than not |  |
| 12.45 Must lead, vain | The Gold must be first and pampered | Gold: ride behind another rider; then brush it | The horse, `log.txt` | Agitated stuck behind; happy after every brushing |  |
| 12.46 Town disdain, stoic | The Silver is cold with everyone | Silver: ride through a busy town; pat it | The horse, `log.txt` | Snorts and glares at people close; nothing back for the pat |  |

### American Standardbred coats

The Black runs wild in Big Valley, the Buckskin across New Hanover and West Elizabeth; the Silver Tail Buckskin is sold at Tumbleweed, the Palomino Dapple at Valentine.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.47 Snake drama | Standardbreds panic at snakes | Any Standardbred: ride near a snake | The horse, `log.txt` | It reacts from far away |  |
| 12.48 Trail snob, restless | The Black wants to race, not walk | Black: walk it a minute, then sit still on it | The horse, `log.txt` | Insulted snorts at a walk, fidgets when kept still |  |
| 12.49 Racer, spoiled | The Silver Tail loves racing and rewards | Silver Tail: ride beside another rider; gallop 30 s and stop | The horse, `log.txt` | Calls at the other rider; calls for a treat after the stop |  |
| 12.50 Separation anxiety | The Palomino Dapple hates being left | Palomino Dapple: hitch it outside a saloon, walk 5 m away, then go in a minute | Listen, `log.txt` | It whinnies while you're away |  |
| 12.51 Vain | It loves being groomed | Palomino Dapple: brush it | The horse | Happy every time |  |
| 12.52 No brakes | The Buckskin can't stop cleanly | Buckskin: gallop, then stop as hard as you can, a few times | The horse, `log.txt` | A sideways skid about half the time |  |

### Nokota coats

The Reverse Dapple Roan is sold at Saint Denis; the Blue Roan and White Roan run wild in West Elizabeth and New Hanover. Run trigger 60 first.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.53 Damage check | Grudge needs to know when the horse gets hurt | Run trigger 60, then ride into a rock or tree, or take a small fall | `log.txt` | One line per hit on your horse |  |
| 12.54 Grudge | Nokotas sulk after you crash them | Any Nokota: crash it, pat it, feed it, pat again | The horse, `log.txt` | Snorts after the crash; nothing for the first pat; the treat forgives it; the second pat gets its usual reaction |  |
| 12.55 Downhill snob | The Reverse Dapple hates slow descents | Reverse Dapple: walk it down a steep slope | The horse, `log.txt` | Offended snorts |  |
| 12.56 Stranger danger | The Blue Roan protects you from rushers | Blue Roan: in town, let someone run toward you | The horse, `log.txt` | It spins on them, agitated and glaring; never kicks |  |
| 12.57 Wanderlust, selective hearing | The White Roan is a wild child | White Roan: get off, don't hitch it, walk 10 m away; then whistle | The horse | It strolls off; sometimes ignores the whistle |  |

### Thoroughbred coats

The Brindle is sold at Strawberry; the Blood Bay and Dapple Grey run wild in the Heartlands and Big Valley; the Reverse Dapple Black comes with the Ultimate Edition. Run trigger 61 first.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.58 Water check | Water sigh and balking need to know about water | Run trigger 61, ride into a river and out | `log.txt` | True in the water, False out of it |  |
| 12.59 Needs soothing | Thoroughbreds fret after danger | Baseline, Brindle or Blood Bay: get into a gunfight, ride away and wait; again, but pat it after | The horse, `log.txt` | It frets for a while after the fight; a pat stops it |  |
| 12.60 Mud tantrum, snake drama | The Brindle is a diva | Brindle: roll it in mud; ride near a snake | The horse, `log.txt` | Agitated when dirty; reacts to the snake from far off |  |
| 12.61 Snob, guard dog | The Reverse Dapple Black is a proud guard | Reverse Dapple Black: leave it in camp near other horses and people | The horse, `log.txt` | Glares at cheaper horses, stomps at strangers who come close |  |
| 12.62 No brakes | The Blood Bay can't stop cleanly | Blood Bay: gallop and stop hard, a few times | The horse, `log.txt` | Sideways skids |  |
| 12.63 Hill crawler, water sigh, apple beggar | The Dapple Grey is an old veteran | Dapple Grey: spur it up a steep hill; ride through a river; stand next to it on foot | The horse, `log.txt` | Walks the hill even when spurred, sighs in the water, asks you for an apple |  |

### Andalusian coats

The Perlino stands wild by the waterfall at Brandywine Drop; the Dark Bay is sold at Strawberry, the Rose Gray at Van Horn.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.64 War horse | Andalusians stay steady in battle | Any Andalusian: ride into a gunfight | The horse, `log.txt` | It never spooks while the fight is on |  |
| 12.65 Guard dog, jealous | The Perlino guards you and wants you to itself | Perlino: get off in town with people passing close; then, as your saddle horse, ride another horse next to it | The horse, `log.txt` | Stomps at people who come close; calls and stamps at the other horse |  |
| 12.66 Hill crawler, hill sigh | The heavy Dark Bay hates climbs | Dark Bay: spur it up a steep trail | The horse, `log.txt` | Drops to a walk with a snort |  |
| 12.67 Hates brushing, short king | The Rose Gray is a fighter | Rose Gray: brush it a few times; stand it near another horse | The horse, `log.txt` | Stamps and snorts at every brushing; squares up to the other horse |  |

### Ardennes coats

The Strawberry Roan is sold at Van Horn, the Bay Roan at Scarlett Meadows; the Iron Grey Roan is a pre-order bonus.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.68 War horse | Ardennes don't flinch at gunfire | Any Ardennes: ride into a gunfight and empty a repeater from the saddle | The horse, `log.txt` | It never spooks while the fight is on |  |
| 12.69 Brawler, short king, restless | The Strawberry Roan is a brawler | Strawberry Roan: off it near a wolf; stand it near a smaller horse; sit still on it | The horse, `log.txt` | Goes for the wolf, squares up to the horse, fidgets when kept still |  |
| 12.70 Unhurried | The Bay Roan is a gentle giant | Bay Roan: spur it down a quiet road; then in a fight | The horse, `log.txt` | Trots on the quiet road; full gallop in the fight |  |
| 12.71 Balker, stoic | The Iron Grey is an immovable juggernaut | Iron Grey Roan: spur it up a steep trail; then into a river; then pat it | The horse, `log.txt` | Refuses on the climb and in the water; nothing back for the pat |  |

### Hungarian Halfbred coats

The Dapple Dark Grey is sold at Valentine (or rescued from Wrobel's barn); the Flaxen Chestnut runs wild in the Heartlands and Big Valley; the Piebald Tobiano rides with bounty hunters.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.72 Trail snob, war horse | Military horses want action | Any Halfbred except the Flaxen Chestnut: walk it a couple of minutes; then ride into a gunfight | The horse, `log.txt` | Insulted snorts at a walk; never spooks in the fight |  |
| 12.73 Town disdain, snob | The Piebald is an outlaw mercenary | Piebald Tobiano: ride through a busy town; stand it next to a Morgan | The horse, `log.txt` | Snorts and glares at people; glares at the Morgan |  |
| 12.74 Strict | The Dapple Dark Grey punishes sloppy riding | Dapple Dark Grey: ride it into a tree, then spur it straight away | The horse, `log.txt` | A judging snort, then only a trot for about 30 s |  |
| 12.75 Restless, no brakes, racer | The Flaxen Chestnut is a fiery sergeant | Flaxen Chestnut: get off to pick herbs; gallop and stop hard; ride beside another rider | The horse, `log.txt` | Stomps while you wait, skids on the hard stop, calls at the other rider |  |

### American Paint coats

The Splashed White turns up at random campsites; the Overo and Tobiano run wild across New Hanover, Ambarino and West Elizabeth; the Grey Overo is sold at Tumbleweed. Run trigger 62 first.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.76 Stare at a spot | Night stare needs the horse to look at nothing | Stand beside the horse, run trigger 62 | The horse | It stares at an empty spot to its left for about 6 s |  |
| 12.77 Snack magnet | Paints stop for snacks | Any Paint: walk it past wild carrots or grass a few minutes | The horse, `log.txt` | It stops now and then to graze |  |
| 12.78 Sixth sense, night stare, escape | The Splashed White is a mystic wanderer | Splashed White: ride toward wolves far off; stand by it at night; hitch it and walk away | The horse, `log.txt` | Snorts and looks their way before they're close; stares at nothing at night; unhitches itself |  |
| 12.79 Curious, apple beggar | The Overo is a trailblazer | Overo: get off with a wolf 20-30 m away; stand next to it on foot | The horse, `log.txt` | Walks a few steps toward the animal; asks you for a treat |  |
| 12.80 Country bumpkin | The Tobiano is a ranch hand, lost in the city | Tobiano: ride through Saint Denis | The horse, `log.txt` | Stops dead and snorts now and then |  |
| 12.81 Deadpan, odd treats | The Grey Overo is a dry desert scout | Grey Overo: ride into a cactus; feed a sugar cube, then a haycube | The horse, `log.txt` | A flat stare at you; a snort for the sugar cube, a happy call for the haycube (needs the item codes) |  |

### Appaloosa coats

The Leopard is sold at Van Horn; the Few Spotted turns up at random campsites; the Brown Leopard is sold at Tumbleweed; the Blanket and Leopard Blanket run wild in Big Valley and New Hanover.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.83 Trail snob | Appaloosas are bored by flat roads | Any Appaloosa except the Few Spotted: walk it on a flat road a couple of minutes | The horse, `log.txt` | Bored, insulted snorts |  |
| 12.84 Vain, showboat, mud tantrum | The Leopard is a spotted diva | Leopard: brush it; gallop into town and stop by a crowd; roll it in mud | The horse, `log.txt` | Happy every brushing, rears for the crowd, agitated when dirty |  |
| 12.85 Crowd shy, wanderlust | The Few Spotted is a ghostly loner | Few Spotted: hitch it in Saint Denis and stand back; feed it with someone close; walk away | The horse, `log.txt` | Side-steps from people, snorts at the treat instead of eating happily, strolls off |  |
| 12.86 Hill crawler, bribable | The Brown Leopard works for food | Brown Leopard: spur it up a steep slope; feed it an oatcake; spur it up again | The horse, `log.txt` | Walks the first climb; sprints the second (needs the oatcake code) |  |
| 12.87 Scatterbrain, beggar, restless | The Blankets are wild adventurers | Blanket or Leopard Blanket: gallop a while; stand next to it; sit still on it | The horse, `log.txt` | An odd skid at a gallop, asks you for an apple, fidgets |  |

### Dutch Warmblood coats

The Chocolate Roan is sold at Valentine; the Sooty Buckskin at Scarlett Meadows; the Seal Brown pulls wagons and carries lawmen.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.88 Spur snob | Warmbloods hate spur spamming | Any Warmblood: tap sprint 4 or 5 times quickly | The horse, `log.txt` | An offended snort |  |
| 12.89 Mud tantrum, snob, bribable | The Chocolate Roan is a polished professional | Chocolate Roan: ride through mud; stand it near a Mustang; spur it uphill, feed a sugar cube, spur again | The horse, `log.txt` | Agitated when dirty, glares at the Mustang, walks the hill until fed (needs the sugar cube code) |  |
| 12.90 Wanderlust, hearing, snacks | The Sooty Buckskin is a gritty prospector | Sooty Buckskin: leave it untied and walk away; whistle; walk it past grass | The horse, `log.txt` | Strolls off, sometimes ignores the whistle, stops to graze |  |
| 12.91 Moody refusal | The Seal Brown is a quiet professional with moods | Seal Brown: ride at a walk through town a few minutes | The horse, `log.txt` | Stops dead now and then; calm with the crowds |  |

### Belgian Draft coats

Both coats pull wagons and carts, and turn up at stables that sell drafts.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.94 Unhurried | Galloping is only a suggestion for a Belgian | Either Belgian: spur it down a quiet road; then during a fight | The horse, `log.txt` | Holds a trot on the quiet road; gallops in the fight |  |
| 12.95 Apple beggar, clumsy | The Blond Chestnut is a gentle giant | Blond Chestnut: stand next to it on foot; trot a few minutes | The horse, `log.txt` | Asks you for an apple; trips now and then |  |
| 12.96 Lazy, bribable, sigh, hearing | The Mealy Chestnut is a union foreman | Mealy Chestnut: ride without spurring; feed a haycube and ride again; walk up a hill; get off 30 m away and whistle | The horse, `log.txt` | Shuffles at a walk until fed (needs the haycube code), sighs on the hill, sometimes ignores the whistle |  |

### Shire coats

The Raven Black is Hosea's horse (and sold at some stables); the Dark Bay and Light Grey pull wagons and coaches.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.97 War horse | Shires are rock-steady under fire | Any Shire: ride into a gunfight | The horse, `log.txt` | Never spooks while the fight is on |  |
| 12.98 Brawler, short king, treat tantrum | The Raven Black is a chaotic titan | Raven Black: off it near a wolf; stand it near another horse; ride an hour without feeding it, then feed it | The horse, `log.txt` | Goes for the wolf, squares up to the horse, tantrums after the hour and stops once fed |  |
| 12.99 Spur snob, unhurried, snacks, hills | The Dark Bay is an immovable mountain | Dark Bay: tap sprint quickly on a quiet road; walk past grass; spur it up a hill | The horse, `log.txt` | Snorts and holds a trot, stops to graze, walks the hill |  |
| 12.100 Vain, beggar, mud tantrum | The Light Grey is a sleepy cloud who likes to look nice | Light Grey: brush it; stand next to it on foot; ride through mud | The horse, `log.txt` | Happy every brushing, asks for a treat, agitated when muddy |  |

### Suffolk Punch coats

Both coats pull wagons, farm carts and coaches, and turn up at stables that sell drafts.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.101 Unhurried, bribable, vain, beggar | The Sorrel is a round-bellied optimist | Sorrel: spur it on a quiet road, feed an oatcake, spur again; brush it; stand by it on foot | The horse, `log.txt` | A jog until fed, then a gallop (needs the oatcake code); happy every brushing; asks for an apple |  |
| 12.102 Saddle hater, town dawdler, turn grumble | The Red Chestnut is a grumpy old-timer | Red Chestnut: get on a few times; ride and spur through Valentine; turn it round sharply on a road | The horse, `log.txt` | Ears back as you get on; walks through town however you spur; snorts at the sharp turn |  |

### Kentucky Saddler coats

The Buttermilk Buckskin is the PC bonus coat; the Silver Bay and Chestnut Pinto run wild across New Hanover and West Elizabeth; the Black and Grey are common town mounts.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.103 Needs soothing | Kentuckys have no appetite for danger | Any Kentucky: get into a gunfight, ride away and wait | The horse, `log.txt` | It frets for a while after; a pat settles it |  |
| 12.104 Vain, mud tantrum, hill sigh | The Buttermilk is a polished showboat | Buttermilk Buckskin: brush it; ride through mud; walk up a hill | The horse, `log.txt` | Happy every brushing, agitated when muddy, sighs on the hill |  |
| 12.105 Freeze, clumsy | The Silver Bay freezes when scared | Silver Bay: ride toward a wolf; trot a few minutes | The horse, `log.txt` | Stops dead when scared, no rearing; trips now and then |  |
| 12.106 Wanderlust, snacks, restless | The Chestnut Pinto is a free spirit | Chestnut Pinto: leave it untied and walk away; ride past bushes; sit still on it | The horse, `log.txt` | Strolls off, stops to graze, fidgets when kept still |  |
| 12.107 Bail out, city lover | The Black and Grey are town commuters | Black or Grey: ride into an ambush or near gunfire; then through a busy town | The horse, `log.txt` | Throws you and trots away from the trouble; calm in town |  |

### Morgan coats

The Palomino and Flaxen Chestnut run wild across New Hanover and West Elizabeth; the Liver Chestnut is the PC bonus coat; the Bay and Bay Roan are common ranch and town mounts.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.108 Snake drama | Morgans think snakes are the end of the world | Any Morgan: ride near a snake | The horse, `log.txt` | Reacts from far off |  |
| 12.109 Winded, racer, vain | The Palomino is a pocket-sized prince | Palomino: gallop a minute; ride beside another rider; brush it | The horse, `log.txt` | Wheezes after about 30 s, calls at the other rider, happy every brushing |  |
| 12.110 Short king, guard dog, no brakes | The Liver Chestnut is a feisty little brawler | Liver Chestnut: stand it near a Shire or Ardennes; leave it in town with people passing; gallop and stop hard | The horse, `log.txt` | Squares up to the big horse, stomps at people close, skids on the stop |  |
| 12.111 Clumsy, needs soothing | The Flaxen Chestnut is a gentle sidekick | Flaxen Chestnut: trot a while; get into a gunfight, then ride away | The horse, `log.txt` | Trips now and then; frets after the fight until patted |  |
| 12.112 Bail out, city lover | The Bay and Bay Roan are honest workers | Bay or Bay Roan: ride near a bear or into gunfire; then through a busy town | The horse, `log.txt` | Puts you down and trots off; calm in town |  |

### Tennessee Walker coats

The Mahogany Bay is Arthur's first horse (Adler Ranch); the Flaxen Roan is sold at Tumbleweed; the Dapple Bay runs wild across New Hanover and West Elizabeth; the Black Rabicano, Chestnut and Red Roan are common mounts; the Gold Palomino is the PC bonus coat.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.113 Jealous, vain, needs soothing | The Mahogany Bay is your humble first partner | Mahogany Bay as your saddle horse: ride another horse next to it; brush it; near an explosion | The horse, `log.txt` | Calls and stares at you, happy every brushing, frets after the explosion until patted |  |
| 12.114 Unhurried, snacks | The Flaxen Roan is a desert nomad | Flaxen Roan: spur it on a quiet road; walk past grass and cacti | The horse, `log.txt` | Holds its pace; stops to graze |  |
| 12.115 Snacks, hill crawler, hill sigh | The Dapple Bay is a country explorer | Dapple Bay: walk past grass; spur it up a steep ridge | The horse, `log.txt` | Stops to graze; walks the climb with a sigh |  |
| 12.116 Bail out, city lover | The common coats are honest workers | Black Rabicano, Chestnut or Red Roan: ride into an ambush; then through town | The horse, `log.txt` | Puts you down and trots off; calm in town |  |

### Buell, Rachel and Cain

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 12.92 Buell's habits | Buell counts as a Dutch Warmblood with his own design | Buell (after Hamish's missions): gallop down a steep slope; stand by him on foot; tap sprint quickly | The horse, `log.txt` | Agitated galloping downhill (*Cliff nerves*), asks for an apple, snorts at the spur spam |  |
| 12.93 Rachel's model | Rachel's game model is a guess | In the epilogue, ride Rachel with the overlay (F9) open | Overlay | The model reads `a_c_horse_john_endlesssummer` and she shows *Coat: The Loyal Veteran*. If not, copy the model into Notes |  |
| 12.82 Cain's model | Cain must stay safe from Kickers | In camp, stand next to Cain and run trigger 63 (read only) | Panel | Shows a model code. If it isn't 0xC5C5D255, copy it into Notes |  |

## Stage 13: bond XP that lasts

The game put back bond XP the mod changed within about 3 minutes (Stage 9). These tests check if adding XP instead of setting it is kept. Use a test save. After pressing the trigger, leave the horse alone for 5 minutes: no patting, brushing, feeding or hard riding.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 13.1 Added XP stays | If yes, bond speed per horse can work | Stand by your horse, run trigger 64, wait 5 minutes | `log.txt` (a line every 30 s) | Every line ends in *kept*. Copy the last line into Notes |  |
| 13.2 Set XP (control) | Confirms the old way still fails | Same with trigger 65 | `log.txt` | Expected to end in *gone*. Copy the last line into Notes |  |
| 13.3 Kept after reload | The change must survive saving | Only if 13.1 kept it: save, quit, reload | Overlay or trigger 32 | The bond XP is still the higher number |  |

## Stage 14: skills and growth (Phase 8)

To see growth without riding for hours, set `ExperienceSpeed = 20` in the INI and press Insert. Set it back to 1 when you're done. Use a horse you don't mind changing.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 14.1 Experience counts | Everything in Phase 8 grows from it | Ride a permanent horse with the overlay (F9) open | Overlay | *Experience*, *Skills* and *Long-use growth* lines; minutes ridden go up |  |
| 14.2 Speed skills grow | Galloping should train speed | Gallop a few minutes; press sprint from a walk several times | Overlay | Gallop minutes and sprints go up; speed and acceleration skills rise |  |
| 14.3 Natural speed shows | A fast horse grows into its speed | On a horse with natural speed +1 (overlay), gallop until its speed skill passes 0.5 | Speed bar, `log.txt` | One more bar segment and a *speed base rank* log line |  |
| 14.4 Gunfights calm a timid horse | Experience should beat fear, only where it was earned | **Coward** preset: get into gunfights until the gunfire skill passes 0.6, fire near it again, then meet a wolf | The horse, `log.txt` | Clearly fewer spooks under fire than at first (log shows *+ skill*); still scared of the wolf |  |
| 14.5 Long use, rank 1 | A long-kept horse should get better | Reach bond 3 (trigger 42) with enough hours ridden (high ExperienceSpeed) | Speed and acceleration bars | +1 segment each, unless already at the class limit |  |
| 14.6 Community favorite, rank 2 | Favorites can become the best horse in the game | A Silver Dapple Pinto, Gold Turkoman or Reverse Dapple Roan at bond 4 and 30 hours | Bars | Speed and acceleration up to +2 over the breed |  |
| 14.7 Handling | Unknown if agility changes how a horse handles | Ride until handling skill passes 0.5 (about 2 hours at ExperienceSpeed 1) | `log.txt`, the feel | An *agility base rank* line. Note whether turning feels different |  |
| 14.8 Kept and restorable | Growth must survive saves and be undoable | Save, quit, reload. Then set `RestoreVanillaStats = true` and reload | Bars, overlay | Experience and growth kept; with Restore on, speed, acceleration and agility go back. Set it back to false |  |
| 14.9 Nothing counts in missions | Missions shouldn't train horses | Start a mission and ride a while | Overlay | No experience added during it |  |

## Stage 16: admired horses (Phase 8b)

People should compliment any horse at bond 4 that's clean and well fed. Use a breed the game doesn't already praise (a Shire, Morgan or Tennessee Walker), so you know the compliment is the mod's. Keep `LogLevel = Debug` to see *admired* or *not admired (why)* lines; every compliment writes *complimented by person ...*.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 16.1 People have the lines | The mod uses the game's own voice lines | Next to a townsperson, run trigger 66; then next to a gang member in camp | Panel | Townsperson: NICE\_HORSE True. Gang member: PLAYER\_HORSE\_IN\_GOOD\_CONDITION True. Copy both lines into Notes |  |
| 16.2 Speech works (way 1) | The way to make someone speak is a guess | On your horse within 10 m of a townsperson, run trigger 67 | Listen, panel | They look at the horse and compliment it in their own voice |  |
| 16.3 Speech works (way 2) | The backup if way 1 is silent | Same with trigger 68 | Listen, panel | They compliment it. Note which of 67 and 68 worked |  |
| 16.4 Dirt scale | "Clean" needs the right number | Run *Read all horse stats* on a freshly brushed horse, then after riding through mud | `log.txt` | Copy the Dirtiness rank clean and dirty into Notes |  |
| 16.5 Compliments in town | The main feature | Bond 4 (trigger 42), brushed, fed until both cores are full. Ride at a walk through Valentine or Saint Denis for 5 minutes | Listen, `log.txt` | A compliment every few minutes from different people; never two within 3 minutes |  |
| 16.6 Standing beside it | Compliments also come when you're on foot | Same horse: get off and stand next to it near people | Listen, `log.txt` | Compliments still come |  |
| 16.7 Not at a gallop | Nobody can admire a blur | Gallop through town | `log.txt` | No compliments while galloping |  |
| 16.8 Not when dirty | It must be clean | Ride through mud until it's dirty, then through town | `log.txt` | *not admired (dirt rank ...)*; no compliments until you brush it |  |
| 16.9 Not when hungry | It must be well fed | Let the cores drop below 70 with a long ride without feeding | `log.txt` | *not admired (cores ...)*; feeding brings compliments back |  |
| 16.10 Not below bond 4 | Compliments must be earned | A bond 3 horse, clean and fed | `log.txt` | *not admired (bond 3 < 4)*; no compliments from the mod |  |
| 16.11 Gang in camp | The gang has its own line | Ride into camp on the admired horse | Listen | Gang members say their *good condition* line |  |
| 16.12 Off switch | Players can turn it off | Set `NpcCompliments = false`, press Insert, ride through town | `log.txt` | No compliments from the mod. Set it back to true |  |
| 16.13 Not in missions | Missions stay vanilla | Ride past people during a mission | `log.txt` | No compliments from the mod |  |

## Stage 15: release check (do this last)

Only after every other stage passes. Build the zip on your PC: `powershell -ExecutionPolicy Bypass -File tools\make-release.ps1` writes `dist\HorsePersonalities-0.9.0.zip`.

| Test | Why test it | How to do it | Look at | Should happen | Result |
| --- | --- | --- | --- | --- | --- |
| 15.1 Clean install | Players install from the zip, not your dev folder | On a clean game folder (hooks only, no DevTools, no old mod files), copy the zip's `scripts` folder in and load a save | Banner, `log.txt` | *Horse Personalities loaded (19 breeds)* and no errors in the log |  |
| 15.2 Typo warning | Players will misspell settings | Misspell a setting in the INI (for example `SpookFrequncy = 2`), press Insert | `log.txt` | A warning naming the typo and suggesting SpookFrequency |  |
| 15.3 Test mode warning | Nobody should play a real save in test mode by accident | Set `TestPersonalities = true` and `ExperienceSpeed = 20`, press Insert | Banner, `log.txt` | Banner says TEST MODE and ExperienceSpeed x20; the log warns about both. Set them back |  |
| 15.4 Full play session | The mod must hold up over real play | Play an hour or more: missions, towns, a hunt, a fight | `log.txt`, the feel | Nothing breaks; behaviors feel alive, not spammy; the log's tick-time lines stay under 1 ms |  |
| 15.5 With other horse mods | Many players run several horse mods | The same with popular horse mods installed, including the Unruliness mod | The game, `log.txt` | No crashes or clashes between mods. Note anything odd |  |
| 15.6 Uninstall | Removing the mod must be safe | Delete the mod's files, reload | Stat bars | Your horses are back to vanilla |  |

When all six pass, the version becomes 1.0.0.

## What to send back

After each session, send these so failures can be fixed and numbers tuned:

- [ ] `scripts\HorsePersonalities\log.txt`
- [ ] `phase1-results.csv` (DevTools results)
- [ ] `horses.json`
- [ ] This doc with Results and Notes filled in

The values to copy into Notes along the way: treat item codes (11.7, 12.21), the sigh sound (12.9), the climb speed (12.10), Rachel's and Cain's models (12.93, 12.82), which speech trigger worked (16.2, 16.3), the dirt ranks (16.4), and the bond XP lines (13.1, 13.2).
