using System;
using System.Collections.Generic;
using System.Text;
using HorsePersonalities.Core.GameApi;

namespace HorsePersonalities.DevTools
{
    /// <summary>What a trigger needs from the running script.</summary>
    public interface ITriggerContext
    {
        IGameApi Game { get; }
        /// <summary>The horse triggers act on: the one you ride, else your last mount, else your saddle horse.</summary>
        int Horse { get; }
        int Now { get; }
        /// <summary>Runs an action later, e.g. to clear a task after a few seconds.</summary>
        void After(int ms, Action action);
        void Log(string message);
    }

    /// <summary>One row of the trigger spike: a single game behavior to try on your horse.</summary>
    public sealed class Trigger
    {
        public string Id { get; }
        public string Name { get; }
        /// <summary>Which quirk or behavior in the build plan depends on this, for the sheet's Quirks tab.</summary>
        public string UsedFor { get; }
        /// <summary>What to do before pressing fire, and what to look for.</summary>
        public string HowToTest { get; }
        public bool NeedsHorse { get; }
        private readonly Func<ITriggerContext, string> _run;

        public Trigger(string id, string name, string usedFor, string howToTest, Func<ITriggerContext, string> run, bool needsHorse = true)
        {
            Id = id;
            Name = name;
            UsedFor = usedFor;
            HowToTest = howToTest;
            _run = run;
            NeedsHorse = needsHorse;
        }

        /// <summary>Fires the trigger and returns a status line for the panel and log.</summary>
        public string Run(ITriggerContext ctx) => _run(ctx);
    }

    /// <summary>
    /// The Phase 1 trigger list (Build plan: Phase 1). Each entry calls one native through IGameApi. The natives come
    /// from the native DB; arguments the DB does not document are guesses, which is why this phase exists.
    /// </summary>
    public static class TriggerList
    {
        private static int _explorerAction = -1;
        private static readonly Dictionary<PedAttribute, int> BonusBefore = new Dictionary<PedAttribute, int>();

        public static List<Trigger> Build(DevToolsConfig config)
        {
            var t = new List<Trigger>();

            // ---- Sounds (Chatterbox, snorts in Nerves and reactions) ----
            foreach (string name in config.Vocalizations)
            {
                string n = name;
                t.Add(new Trigger("sound." + n.ToLowerInvariant(), $"Sound: {n}", "Chatterbox; snorts for Nerves; care reactions",
                    "Stand next to the horse. Listen for a whinny or snort.",
                    ctx =>
                    {
                        ctx.Game.PlayVocalization(ctx.Horse, n);
                        ctx.After(300, () => ctx.Log($"Sound {n}: IS_ANIMAL_VOCALIZATION_PLAYING = {ctx.Game.IsVocalizationPlaying(ctx.Horse)}"));
                        return $"PLAY_ANIMAL_VOCALIZATION \"{n}\"";
                    }));
            }

            // ---- Look-at (Nosy, Courage tells, Aloof reaction) ----
            t.Add(new Trigger("lookat.player", "Look at you (5 s)", "Nosy; ears/head toward threats; Aloof looks away",
                "Get off and stand a few steps to the side of the horse. Its head should turn to you.",
                ctx =>
                {
                    ctx.Game.LookAtEntity(ctx.Horse, ctx.Game.PlayerPed, 5000);
                    return "TASK_LOOK_AT_ENTITY player 5000 ms";
                }));
            t.Add(new Trigger("lookat.npc", "Look at nearest NPC (5 s)", "Nosy",
                "Stand near the horse with an NPC within 15 m.",
                ctx =>
                {
                    int npc = ctx.Game.GetClosestHumanPed(ctx.Horse, 15f, ctx.Game.PlayerPed);
                    if (npc == 0) return "No NPC found within 15 m (GET_CLOSEST_PED)";
                    ctx.Game.LookAtEntity(ctx.Horse, npc, 5000);
                    return $"TASK_LOOK_AT_ENTITY npc {npc}";
                }));

            // ---- Rear, buck, dismount (Courage spooks, Temper bucks, Unbreakable) ----
            t.Add(Action("rear.1", "Rear up (action 1)", 1, "Courage spook; Temper refusal", "Ride the horse, standing or walking."));
            t.Add(Action("rear.5", "Rear up (action 5)", 5, "Courage spook; Temper refusal", "Ride the horse, standing or walking."));
            t.Add(Action("buck.2", "Throw rider (action 2)", 2, "Temper buck; Unbreakable buck-off", "Ride the horse. You should be thrown."));
            t.Add(Action("buck.10", "Throw rider (action 10)", 10, "Temper buck", "Ride the horse. You should be thrown."));
            t.Add(new Trigger("agitate", "Agitate, keep rider", "Head-tosser; restless state; raise agitation",
                "Ride or stand by the horse. Look for restless stamping or head tossing.",
                ctx => { ctx.Game.HorseAgitate(ctx.Horse, false); return "_HORSE_AGITATE kickOffRider=false"; }));
            t.Add(new Trigger("agitate.throw", "Agitate, throw rider", "Temper buck; Unbreakable buck-off",
                "Ride the horse.",
                ctx => { ctx.Game.HorseAgitate(ctx.Horse, true); return "_HORSE_AGITATE kickOffRider=true"; }));
            t.Add(new Trigger("dismount", "Force dismount", "Temper refusal (fallback for a buck)",
                "Ride the horse. You should get off.",
                ctx => { ctx.Game.DismountPlayer(); return "TASK_DISMOUNT_ANIMAL"; }, needsHorse: false));
            t.Add(Action("stop.3", "Quick stop (action 3)", 3, "Refusal; Courage spook at a gallop", "Ride at a canter or gallop."));
            t.Add(Action("skid.8", "Skid (action 8)", 8, "Refusal; spook at a gallop", "Ride at a gallop."));
            t.Add(new Trigger("flee", "Bolt away from you (4 s)", "Courage bolt; Nerves spook",
                "Get off and stand next to the horse.",
                ctx =>
                {
                    ctx.Game.FleeFrom(ctx.Horse, ctx.Game.PlayerPed, 30f, 4000);
                    ctx.After(4500, () => ctx.Game.ClearTasks(ctx.Horse));
                    return "TASK_SMART_FLEE_PED from player 4000 ms";
                }));

            // ---- Kick (Kicker, Grumpy brush reaction, Temper) ----
            t.Add(new Trigger("kick.combat", "Kick nearest NPC (combat task, 4 s)", "Kicker; kicks if you linger behind",
                "Get off. Put an NPC (or yourself) right behind the horse. Watch for a kick. Do this away from towns: it may count as a crime.",
                ctx =>
                {
                    int npc = ctx.Game.GetClosestHumanPed(ctx.Horse, 6f, ctx.Game.PlayerPed);
                    if (npc == 0) return "No NPC within 6 m";
                    ctx.Game.Combat(ctx.Horse, npc);
                    ctx.After(4000, () => ctx.Game.ClearTasks(ctx.Horse));
                    return $"TASK_COMBAT_PED npc {npc}, cleared after 4 s";
                }));
            t.Add(new Trigger("explore.action", "Horse action explorer (next id)", "Kick, piaffe or anything not found yet",
                "Each press tries the next TASK_HORSE_ACTION id (0-20). Note any id that kicks, piaffes or tosses its head.",
                ctx =>
                {
                    _explorerAction = (_explorerAction + 1) % 21;
                    ctx.Game.HorseAction(ctx.Horse, _explorerAction);
                    return $"TASK_HORSE_ACTION id {_explorerAction}";
                }));

            // ---- Grazing and following (Wanderer, Shadow) ----
            foreach (string scenario in config.Scenarios)
            {
                string s = scenario;
                t.Add(new Trigger("scenario." + s.ToLowerInvariant(), $"Scenario: {s} (20 s)", "Wanderer (grazing); idle demeanor",
                    "Get off and leave the horse unhitched.",
                    ctx =>
                    {
                        ctx.Game.StartScenario(ctx.Horse, s, 20000);
                        return $"TASK_START_SCENARIO_IN_PLACE_HASH {s}";
                    }));
            }
            t.Add(new Trigger("follow", "Follow you (8 s)", "Shadow",
                "Get off and walk away from the horse.",
                ctx =>
                {
                    ctx.Game.FollowEntity(ctx.Horse, ctx.Game.PlayerPed, 2.0f, 8000);
                    ctx.After(8000, () => ctx.Game.ClearTasks(ctx.Horse));
                    return "TASK_FOLLOW_TO_OFFSET_OF_ENTITY 2 m behind, cleared after 8 s";
                }));

            // ---- Stats and bond (Phase 5 reveal, Bond affinity) ----
            t.Add(BonusToggle(PedAttribute.Speed, "Speed and acceleration skills; natural stats"));
            t.Add(BonusToggle(PedAttribute.Health, "Health and stamina reveal by bond level"));
            t.Add(BonusToggle(PedAttribute.Stamina, "Health and stamina reveal by bond level"));
            t.Add(new Trigger("read.bond", "Read bond level and bond XP", "Bond affinity; bond-level reveal",
                "Check the rank against the bond level the game shows, then pat or brush and read again.",
                ctx =>
                {
                    AttributeReading b = ctx.Game.ReadAttribute(ctx.Horse, PedAttribute.Bonding);
                    return $"Bond rank {b.Rank} (base {b.BaseRank}, max {b.MaxRank}), XP {b.Points}/{b.MaxPoints}";
                }));
            t.Add(new Trigger("read.stats", "Read all horse stats (to log)", "Natural stats; tier caps",
                "Compare with the horse's stat bars in the stable or horse menu.",
                ctx =>
                {
                    var sb = new StringBuilder();
                    foreach (PedAttribute a in new[] { PedAttribute.Health, PedAttribute.Stamina, PedAttribute.Agility, PedAttribute.Speed, PedAttribute.Acceleration, PedAttribute.Courage, PedAttribute.Bonding, PedAttribute.Unruliness })
                        ctx.Log(ctx.Game.ReadAttribute(ctx.Horse, a).ToString());
                    sb.Append($"Cores: health {ctx.Game.GetCoreValue(ctx.Horse, AttributeCore.Health)}, stamina {ctx.Game.GetCoreValue(ctx.Horse, AttributeCore.Stamina)}. Full list in log.txt");
                    return sb.ToString();
                }));

            // ---- Utility (no result needed) ----
            t.Add(new Trigger("clear", "Clear the horse's tasks", "(utility)",
                "Stops whatever a trigger started.",
                ctx => { ctx.Game.ClearTasks(ctx.Horse); ctx.Game.ClearLookAt(ctx.Horse); return "CLEAR_PED_TASKS + TASK_CLEAR_LOOK_AT"; }));
            // Added after the Phase 1 checklist was written: kept last so numbers 01-34 stay the same.
            t.Add(new Trigger("read.identity", "Read horse identity (Phase 2)", "Fingerprint: which parts the game exposes",
                "Ride or stand by the horse. Try two horses of the same breed and coat: do any numbers differ?",
                ctx =>
                {
                    IGameApi g = ctx.Game;
                    int h = ctx.Horse;
                    // The mod's sex readout (IS_PED_MALE) reads "male" on a known female horse, so show the hook's own reading beside it.
                    string hookGender;
                    try { hookGender = (RDR2.Entity.FromHandle(h) as RDR2.Ped)?.Gender.ToString() ?? "n/a"; }
                    catch (System.Exception ex) { hookGender = "threw " + ex.GetType().Name; }
                    string line = $"model 0x{g.GetModel(h):X8}, IS_PED_MALE says {(g.IsMale(h) ? "male" : "female")}, hook Gender {hookGender}, outfits {g.GetOutfitCount(h)}, meta ped type {g.GetMetaPedType(h)}, " +
                        $"taming {Detectors.TamingName(g.GetTamingState(h))}; saddle {g.GetSaddleHorse()}, active {g.GetActiveHorse()}, temp {g.GetTempHorse()}, this {h}";
                    ctx.Log("Identity: " + line);
                    return line;
                }));
            // Phase 1 retest for Wanderer (Nov 25, 2026): grazing did nothing without its conditional animation, and the drink
            // scenario played on the spot. Numbers 36-41, after the existing ones so the checklist numbers don't move.
            t.Add(new Trigger("graze.anim", "Graze, animation named (20 s)", "Wanderer (grazing)",
                "Get off; don't hitch the horse. Does it lower its head and eat?",
                ctx => { ctx.Game.StartScenario(ctx.Horse, "WORLD_ANIMAL_HORSE_GRAZING", 20000, "WORLD_ANIMAL_HORSE_GRAZING"); return "Scenario WORLD_ANIMAL_HORSE_GRAZING + conditional anim WORLD_ANIMAL_HORSE_GRAZING"; }));
            t.Add(new Trigger("graze.domestic.anim", "Graze, domestic, animation named (20 s)", "Wanderer (grazing)",
                "Get off; don't hitch the horse. Does it lower its head and eat?",
                ctx => { ctx.Game.StartScenario(ctx.Horse, "WORLD_ANIMAL_HORSE_GRAZING_DOMESTIC", 20000, "WORLD_ANIMAL_HORSE_GRAZING"); return "Scenario WORLD_ANIMAL_HORSE_GRAZING_DOMESTIC + conditional anim WORLD_ANIMAL_HORSE_GRAZING"; }));
            t.Add(new Trigger("rest.anim", "Rest, animation named (20 s)", "Idle demeanor",
                "Get off. Does it settle and rest?",
                ctx => { ctx.Game.StartScenario(ctx.Horse, "WORLD_ANIMAL_HORSE_RESTING_DOMESTIC", 20000, "WORLD_ANIMAL_HORSE_RESTING"); return "Scenario WORLD_ANIMAL_HORSE_RESTING_DOMESTIC + conditional anim WORLD_ANIMAL_HORSE_RESTING"; }));
            t.Add(new Trigger("wander.nearby", "Wander nearby (20 s)", "Wanderer (drifting off)",
                "Get off and stand still. Does it walk around within about 15 m, then stop when cleared?",
                ctx =>
                {
                    ctx.Game.WanderNearby(ctx.Horse, 15f);
                    ctx.After(20000, () => ctx.Game.ClearTasks(ctx.Horse));
                    return "TASK_WANDER_IN_AREA radius 15 m, cleared after 20 s";
                }));
            t.Add(new Trigger("wander.freely", "Wander freely (20 s)", "Wanderer (drifting off)",
                "Get off and stand still. Does it walk off on its own? Whistle it back afterwards.",
                ctx =>
                {
                    ctx.Game.WanderFreely(ctx.Horse);
                    ctx.After(20000, () => ctx.Game.ClearTasks(ctx.Horse));
                    return "TASK_WANDER_STANDARD, cleared after 20 s";
                }));
            t.Add(new Trigger("wander.graze", "Wander, then graze (30 s)", "Wanderer (the whole quirk)",
                "Get off and stand still. It should drift off for about 12 s, then stop and graze.",
                ctx =>
                {
                    int h = ctx.Horse;
                    ctx.Game.WanderNearby(h, 15f);
                    ctx.After(12000, () => { ctx.Game.ClearTasks(h); ctx.Game.StartScenario(h, "WORLD_ANIMAL_HORSE_GRAZING", 18000, "WORLD_ANIMAL_HORSE_GRAZING"); });
                    return "TASK_WANDER_IN_AREA 12 s, then grazing with its animation named";
                }));
            // Phase 5 testing (Nov 26, 2026), number 42: step the bond level to see the stat reveal without hours of bonding.
            t.Add(new Trigger("bond.next", "Bond level: next (1-4, wraps)", "Phase 5 bond reveal",
                "Use a test save: the game may keep the new level. Ride or stand by the horse, press, wait 1-2 s, check its health and stamina bars.",
                ctx =>
                {
                    int before = ctx.Game.ReadAttribute(ctx.Horse, PedAttribute.Bonding).Rank;
                    int next = before >= 4 ? 1 : before + 1;
                    ctx.Game.SetBondLevel(ctx.Horse, next);
                    ctx.After(500, () => ctx.Log($"Bond after _SET_MOUNT_BONDING_LEVEL {next}: rank {ctx.Game.ReadAttribute(ctx.Horse, PedAttribute.Bonding).Rank}"));
                    return $"_SET_MOUNT_BONDING_LEVEL {before} -> {next}";
                }));

            // ---- Phase 7b: coat signatures (Nov 27, 2026). Numbers 43-55, appended so earlier numbers don't move. ----
            t.Add(new Trigger("town.check", "Am I in a town?", "Town disdain, City lover",
                "Press in Valentine or Saint Denis, then out on the trail. True in town, False outside?",
                ctx => $"_GET_MAP_ZONE_AT_COORDS type TOWN: in town = {ctx.Game.IsInTown(ctx.Horse)}"));
            t.Add(new Trigger("unhitch", "Unhitch, then wander (15 s)", "Escape artist",
                "Hitch the horse at a post, walk 5 m away, press. Does it free itself and drift off?",
                ctx =>
                {
                    int h = ctx.Horse;
                    ctx.Game.Unhitch(h);
                    ctx.After(1000, () => ctx.Game.WanderNearby(h, 15f));
                    ctx.After(16000, () => ctx.Game.ClearTasks(h));
                    return "_UNHITCH_HORSE, then TASK_WANDER_IN_AREA 15 m for 15 s";
                }));
            t.Add(new Trigger("lazy.cap", "Lazy pace: walk cap (20 s)", "Lazy pace",
                "Ride, press, push forward without spurring. Stuck at a walk? Does spurring still get a gallop?",
                ctx =>
                {
                    int h = ctx.Horse, until = ctx.Now + 20000;
                    void Cap()
                    {
                        if (ctx.Now >= until) { ctx.Log("Lazy pace cap ended."); return; }
                        if (!ctx.Game.IsSprintPressed) ctx.Game.SetMaxMoveBlendRatio(h, 1.0f);
                        ctx.After(0, Cap);
                    }
                    Cap();
                    return "SET_PED_MAX_MOVE_BLEND_RATIO 1.0 every frame while not spurring, 20 s";
                }));
            t.Add(new Trigger("graze.ridden", "Graze with you on it (12 s)", "Roadside grazer",
                "Sit still on the horse and press. Does it lower its head and eat with you on its back?",
                ctx => { ctx.Game.StartScenario(ctx.Horse, "WORLD_ANIMAL_HORSE_GRAZING", 12000, "WORLD_ANIMAL_HORSE_GRAZING"); return "Grazing scenario while ridden, 12 s"; }));
            t.Add(new Trigger("aim.check", "Am I aiming? (10 s readout)", "Dramatic rear",
                "Press, then draw and aim your gun on horseback for a few seconds. The log should flip to True while aiming.",
                ctx =>
                {
                    for (int i = 1; i <= 10; i++) { int n = i; ctx.After(i * 1000, () => ctx.Log($"Aiming check {n}/10: {ctx.Game.IsPlayerAiming}")); }
                    return "IS_PLAYER_FREE_AIMING once a second for 10 s (see the log)";
                }, needsHorse: false));
            t.Add(new Trigger("combat.check", "Enemies in combat with me (30 m)", "Prankster",
                "Press during a fight or standoff (hostile NPCs). How many does it count? 0 when nobody is hostile?",
                ctx =>
                {
                    int player = ctx.Game.PlayerPed, n = 0, near = 0;
                    foreach (int ped in ctx.Game.PedsNear(player, 30f))
                    {
                        if (ped == player || !ctx.Game.IsHuman(ped)) continue;
                        near++;
                        if (ctx.Game.IsPedInCombatWith(ped, player)) n++;
                    }
                    return $"IS_PED_IN_COMBAT: {n} of {near} people within 30 m are fighting you";
                }, needsHorse: false));
            t.Add(new Trigger("brawl.animal", "Attack the nearest animal (4 s)", "Wolf brawler",
                "Get off near a wolf or coyote (or any wild animal). Does your horse go for it and stop after 4 s?",
                ctx =>
                {
                    int h = ctx.Horse, target = 0;
                    float best = 40f;
                    foreach (int ped in ctx.Game.PedsNear(h, 40f))
                    {
                        if (ped == h || ped == ctx.Game.PlayerPed || ctx.Game.IsHuman(ped) || ctx.Game.IsDead(ped)) continue;
                        float d = ctx.Game.DistanceBetween(h, ped);
                        if (d < best) { best = d; target = ped; }
                    }
                    if (target == 0) return "No animal within 40 m.";
                    ctx.Game.Combat(h, target);
                    ctx.After(4000, () => ctx.Game.ClearTasks(h));
                    return $"TASK_COMBAT_PED on animal {target} (model 0x{ctx.Game.GetModel(target):X8}, {best:0} m), cleared after 4 s";
                }));
            t.Add(new Trigger("creep.npc", "Sneak up on the nearest person (6 s)", "Camp creeper",
                "Get off in camp near someone. Does the horse walk up behind them quietly, then stop?",
                ctx =>
                {
                    int h = ctx.Horse, target = ctx.Game.GetClosestHumanPed(h, 15f, ctx.Game.PlayerPed);
                    if (target == 0) return "Nobody within 15 m.";
                    ctx.Game.FollowEntity(h, target, 1.5f, 6000);
                    ctx.After(6000, () => ctx.Game.ClearTasks(h));
                    return $"TASK_FOLLOW_TO_OFFSET_OF_ENTITY on {target}, 1.5 m behind, 6 s";
                }));
            foreach (string sigh in new[] { "TIRED", "SIGH", "EXHAUSTED", "PANT" })
            {
                string name = sigh;
                t.Add(new Trigger("sigh." + name.ToLowerInvariant(), $"Sigh candidate: {name}", "Hill sigh",
                    "Press and listen. Does it sound like a heavy, put-upon sigh? (Silence = the sound doesn't exist.)",
                    ctx => { ctx.Game.PlayVocalization(ctx.Horse, name); return $"Vocalization {name}"; }));
            }
            t.Add(new Trigger("climb.readout", "Climb speed readout (10 s)", "Hill sigh",
                "Press, then walk the horse up a steep hill. The log shows upward speed (m/s) each second; note it on a steep climb.",
                ctx =>
                {
                    int h = ctx.Horse;
                    for (int i = 1; i <= 10; i++) { int n = i; ctx.After(i * 1000, () => ctx.Log($"Climb {n}/10: up {ctx.Game.GetVelocity(h).Z:0.00} m/s, speed {ctx.Game.GetSpeed(h):0.0} m/s")); }
                    return "GET_ENTITY_VELOCITY once a second for 10 s (see the log)";
                }));

            // ---- Mustang coats (Nov 27, 2026): 56-59 ----
            t.Add(new Trigger("whistle.watch", "Whistle detection (20 s)", "Selective hearing",
                "Press, then whistle a few times (on foot and on horseback). Each whistle should log one line with True for the button.",
                ctx =>
                {
                    int until = ctx.Now + 20000;
                    void Watch()
                    {
                        if (ctx.Now >= until) { ctx.Log("Whistle watch ended."); return; }
                        foreach (GameEvent e in ctx.Game.ReadAiEvents())
                            if (e.Hash == GameEvents.PedWhistle.Hash) ctx.Log($"EVENT_PED_WHISTLE; whistle button held: {ctx.Game.IsWhistlePressed}");
                        ctx.After(0, Watch);
                    }
                    Watch();
                    return "Watching for EVENT_PED_WHISTLE for 20 s (see the log)";
                }, needsHorse: false));
            t.Add(new Trigger("whistle.ignore", "Ignore the next whistle (20 s)", "Selective hearing",
                "Get off, walk 30 m away, press, then whistle. Does the horse start to come, then stop and nose the ground for 6 s? Whistle again: does it come?",
                ctx =>
                {
                    int h = ctx.Horse, until = ctx.Now + 20000;
                    void Watch()
                    {
                        if (ctx.Now >= until) { ctx.Log("No whistle heard in 20 s."); return; }
                        foreach (GameEvent e in ctx.Game.ReadAiEvents())
                            if (e.Hash == GameEvents.PedWhistle.Hash)
                            {
                                ctx.After(1500, () => { ctx.Game.ClearTasks(h); ctx.Game.StartScenario(h, "WORLD_ANIMAL_HORSE_GRAZING", 6000, "WORLD_ANIMAL_HORSE_GRAZING"); ctx.Log("Whistle ignored: tasks cleared, grazing 6 s."); });
                                return;
                            }
                        ctx.After(0, Watch);
                    }
                    Watch();
                    return "Waiting for your whistle (20 s)";
                }));
            t.Add(new Trigger("hogtied.check", "Hogtied people near me (10 m)", "Bounty stomper",
                "Hogtie someone, stand next to them, press. Should count 1; 0 when nobody is tied up.",
                ctx =>
                {
                    int player = ctx.Game.PlayerPed, n = 0, near = 0;
                    foreach (int ped in ctx.Game.PedsNear(player, 10f))
                    {
                        if (ped == player || !ctx.Game.IsHuman(ped)) continue;
                        near++;
                        if (ctx.Game.IsHogtied(ped)) n++;
                    }
                    return $"IS_PED_HOGTIED: {n} of {near} people within 10 m are tied up";
                }, needsHorse: false));
            t.Add(new Trigger("sneak.check", "Am I sneaking? (10 s readout)", "Silent stealth",
                "Press, then crouch-walk (sneak) for a few seconds and stand up again. The log should flip to True while sneaking.",
                ctx =>
                {
                    for (int i = 1; i <= 10; i++) { int n = i; ctx.After(i * 1000, () => ctx.Log($"Sneaking {n}/10: {ctx.Game.IsPlayerSneaking}")); }
                    return "GET_PED_STEALTH_MOVEMENT once a second for 10 s (see the log)";
                }, needsHorse: false));

            // ---- Nokota coats (Nov 27, 2026): 60 ----
            t.Add(new Trigger("damage.watch", "Horse damage watch (30 s)", "Grudge",
                "Press, then ride the horse into a rock or tree, or take a small fall. Each hit should log one line.",
                ctx =>
                {
                    int h = ctx.Horse, until = ctx.Now + 30000;
                    void Watch()
                    {
                        if (ctx.Now >= until) { ctx.Log("Damage watch ended."); return; }
                        foreach (GameEvent e in ctx.Game.ReadAiEvents())
                            if (e.Hash == GameEvents.EntityDamaged.Hash && e.Data.Length >= 1 && (int)e.Data[0] == h)
                                ctx.Log($"EVENT_ENTITY_DAMAGED on your horse (damager {(e.Data.Length > 1 ? e.Data[1] : 0)}, weapon 0x{(e.Data.Length > 2 ? e.Data[2] : 0):X8})");
                        ctx.After(0, Watch);
                    }
                    Watch();
                    return "Watching EVENT_ENTITY_DAMAGED on your horse for 30 s (see the log)";
                }));

            // ---- Thoroughbred coats (Nov 27, 2026): 61 ----
            t.Add(new Trigger("water.check", "Is my horse in water? (15 s readout)", "Water sigh",
                "Press, then ride into a river or lake and out again. The log should flip to True in the water.",
                ctx =>
                {
                    int h = ctx.Horse;
                    for (int i = 1; i <= 15; i++) { int n = i; ctx.After(i * 1000, () => ctx.Log($"In water {n}/15: {ctx.Game.IsInWater(h)}")); }
                    return "IS_ENTITY_IN_WATER once a second for 15 s (see the log)";
                }));
            return t;
        }

        private static Trigger Action(string id, string name, int actionId, string usedFor, string how) =>
            new Trigger(id, name, usedFor, how, ctx =>
            {
                ctx.Game.HorseAction(ctx.Horse, actionId);
                return $"TASK_HORSE_ACTION id {actionId}";
            });

        private static Trigger BonusToggle(PedAttribute attribute, string usedFor) =>
            new Trigger("stat." + attribute.ToString().ToLowerInvariant(), $"{attribute} rank +2 / restore", usedFor,
                "Press once and check the stat bar; press again to put it back. Reload a save to see if it sticks.",
                ctx =>
                {
                    AttributeReading before = ctx.Game.ReadAttribute(ctx.Horse, attribute);
                    if (BonusBefore.TryGetValue(attribute, out int original))
                    {
                        ctx.Game.SetAttributeBonusRank(ctx.Horse, attribute, original);
                        BonusBefore.Remove(attribute);
                    }
                    else
                    {
                        BonusBefore[attribute] = before.BonusRank;
                        ctx.Game.SetAttributeBonusRank(ctx.Horse, attribute, before.BonusRank + 2);
                    }
                    AttributeReading after = ctx.Game.ReadAttribute(ctx.Horse, attribute);
                    return $"{attribute}: rank {before.Rank} -> {after.Rank} (bonus {before.BonusRank} -> {after.BonusRank}, max {after.MaxRank})";
                });
    }
}
