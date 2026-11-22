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
