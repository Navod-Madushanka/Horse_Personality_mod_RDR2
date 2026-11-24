using System.Collections.Generic;
using HorsePersonalities.Core.Hashing;

namespace HorsePersonalities.Core.GameApi
{
    /// <summary>An AI event the mod reads, with the number of data fields GET_EVENT_DATA needs for it.</summary>
    public sealed class GameEventType
    {
        public string Name { get; }
        public uint Hash { get; }
        /// <summary>Must match the game exactly, or GET_EVENT_DATA returns nothing (femga rdr3_discoveries, AI/EVENTS).</summary>
        public int DataSize { get; }
        public string Fields { get; }

        public GameEventType(string name, int dataSize, string fields)
        {
            Name = name;
            Hash = Joaat.Hash(name);
            DataSize = dataSize;
            Fields = fields;
        }
    }

    /// <summary>
    /// Events in event group 0 (SCRIPT_EVENT_QUEUE_AI) that matter for horses. Sizes and fields come from
    /// femga's rdr3_discoveries event table. Event hashes are joaat of the names (checked in tests).
    /// </summary>
    public static class GameEvents
    {
        public static readonly GameEventType PedAnimalInteraction = new GameEventType("EVENT_PED_ANIMAL_INTERACTION", 3, "ped, animal, interaction type hash");
        public static readonly GameEventType CalmPed = new GameEventType("EVENT_CALM_PED", 4, "calmer ped, mount, calm type, fully calmed");
        public static readonly GameEventType HorseBroken = new GameEventType("EVENT_HORSE_BROKEN", 3, "rider, horse, broken event type");
        public static readonly GameEventType HorseStartedBreaking = new GameEventType("EVENT_HORSE_STARTED_BREAKING", 0, "");
        public static readonly GameEventType PlayerMountWildHorse = new GameEventType("EVENT_PLAYER_MOUNT_WILD_HORSE", 1, "wild horse");
        public static readonly GameEventType PlayerHorseAgitatedByAnimal = new GameEventType("EVENT_PLAYER_HORSE_AGITATED_BY_ANIMAL", 4, "horse, animal, ?, ?");
        public static readonly GameEventType ShotFiredWhizzedBy = new GameEventType("EVENT_SHOT_FIRED_WHIZZED_BY", 1, "entity shot at");
        public static readonly GameEventType ShotFired = new GameEventType("EVENT_SHOT_FIRED", 0, "");
        public static readonly GameEventType EntityDamaged = new GameEventType("EVENT_ENTITY_DAMAGED", 9, "damaged entity, damager, weapon hash, ...");
        public static readonly GameEventType LootComplete = new GameEventType("EVENT_LOOT_COMPLETE", 3, "looter, looted entity, success");

        public static readonly IReadOnlyList<GameEventType> All = new[]
        {
            PedAnimalInteraction, CalmPed, HorseBroken, HorseStartedBreaking, PlayerMountWildHorse,
            PlayerHorseAgitatedByAnimal, ShotFiredWhizzedBy, ShotFired, EntityDamaged, LootComplete,
        };

        private static readonly Dictionary<uint, GameEventType> ByHash = BuildIndex();

        private static Dictionary<uint, GameEventType> BuildIndex()
        {
            var d = new Dictionary<uint, GameEventType>();
            foreach (var e in All) d[e.Hash] = e;
            return d;
        }

        public static bool TryGet(uint hash, out GameEventType type) => ByHash.TryGetValue(hash, out type);

        /// <summary>
        /// Interaction type hashes seen in EVENT_PED_ANIMAL_INTERACTION. Names from femga's TASK_ANIMAL_INTERACTION list.
        /// Phase 1 checks which ones a pat, brush and feed actually send; unknown hashes are logged in hex.
        /// </summary>
        public static readonly IReadOnlyList<string> InteractionNames = new[]
        {
            "Interaction_Brush", "Interaction_Food", "Interaction_Idle", "Interaction_Injection_Quick", "Interaction_Ointment",
            "Interaction_LootSaddleBags", "Interaction_Dog_Patting", "Interaction_Dog_Idle",
            // Confirmed in game (Phase 1): every interaction sends Begin first, then its own type, then Complete and Cancel together.
            "Interaction_Begin", "Interaction_Complete", "Interaction_Cancel",
            // Guesses for the horse pat; none of them matched (see PatHash).
            "Interaction_Patting", "Interaction_Horse_Patting", "Interaction_Pat", "Interaction_Calm",
        };

        /// <summary>
        /// The horse pat's type hash. Its name is not known: seen in game between Interaction_Begin and Interaction_Complete,
        /// always with the horse's calm events, never for brush or feed.
        /// </summary>
        public const uint PatHash = 0x175897C0;

        public static string DescribeInteraction(uint hash)
        {
            foreach (string n in InteractionNames)
                if (Joaat.Hash(n) == hash) return n;
            if (hash == PatHash) return $"0x{hash:X8} (horse pat)";
            return $"0x{hash:X8}";
        }
    }
}
