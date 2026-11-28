using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Json;

namespace HorsePersonalities.Core.Skills
{
    /// <summary>
    /// Phase 8 tuning, from the breed sheet's Skills tab (rule 6: no tuning numbers in code).
    /// Steps are skill gained per unit of experience at learning rate 1; a skill runs from 0 to 1.
    /// </summary>
    public sealed class SkillTuning
    {
        private readonly Dictionary<string, double> _v;

        private SkillTuning(Dictionary<string, double> v) { _v = v; }

        public double GunfightStep => _v["gunfightStep"];
        public double PredatorStep => _v["predatorStep"];
        public double GunfightGapSeconds => _v["gunfightGapSeconds"];
        public double SkillCourageBonus => _v["skillCourageBonus"];
        public double SpeedPerGallopMinute => _v["speedPerGallopMinute"];
        public double AccelerationPerSprint => _v["accelerationPerSprint"];
        public double SprintGapSeconds => _v["sprintGapSeconds"];
        public double HandlingPerRiddenMinute => _v["handlingPerRiddenMinute"];
        public double SkillRankAt => _v["skillRankAt"];
        public int LongUseRank1Bond => (int)_v["longUseRank1Bond"];
        public double LongUseRank1Hours => _v["longUseRank1Hours"];
        public int LongUseRank2Bond => (int)_v["longUseRank2Bond"];
        public double LongUseRank2Hours => _v["longUseRank2Hours"];
        public int FavoriteMaxRanks => (int)_v["favoriteMaxRanks"];
        public bool HandlingGrowth => _v["handlingGrowth"] >= 0.5;
        public int HandlingAgilityRanks => (int)_v["handlingAgilityRanks"];
        public double SaveEveryRiddenMinutes => _v["saveEveryRiddenMinutes"];

        /// <summary>JSON key, Skills tab label, smallest and largest allowed value, whole numbers only.</summary>
        public static readonly IReadOnlyList<(string Key, string Label, double Min, double Max, bool Whole)> Fields = new[]
        {
            ("gunfightStep", "Gunfight step", 0.0, 1.0, false),
            ("predatorStep", "Predator step", 0.0, 1.0, false),
            ("gunfightGapSeconds", "Gunfight gap seconds", 1.0, 3600.0, false),
            ("skillCourageBonus", "Skill courage bonus", 0.0, 4.0, false),
            ("speedPerGallopMinute", "Speed per gallop minute", 0.0, 1.0, false),
            ("accelerationPerSprint", "Acceleration per sprint", 0.0, 1.0, false),
            ("sprintGapSeconds", "Sprint gap seconds", 0.0, 600.0, false),
            ("handlingPerRiddenMinute", "Handling per ridden minute", 0.0, 1.0, false),
            ("skillRankAt", "Skill rank at", 0.01, 1.0, false),
            ("longUseRank1Bond", "Long use rank 1 bond", 1.0, 4.0, true),
            ("longUseRank1Hours", "Long use rank 1 hours", 0.0, 1000.0, false),
            ("longUseRank2Bond", "Long use rank 2 bond", 1.0, 4.0, true),
            ("longUseRank2Hours", "Long use rank 2 hours", 0.0, 1000.0, false),
            ("favoriteMaxRanks", "Favorite max ranks", 0.0, 9.0, true),
            ("handlingGrowth", "Handling growth", 0.0, 1.0, true),
            ("handlingAgilityRanks", "Handling agility ranks", 0.0, 3.0, true),
            ("saveEveryRiddenMinutes", "Save every ridden minutes", 1.0, 600.0, false),
        };

        /// <summary>Every value given; for tests.</summary>
        public static SkillTuning Of(IDictionary<string, double> values)
        {
            var d = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var f in Fields) d[f.Key] = values.TryGetValue(f.Key, out double v) ? v : throw new ArgumentException("Missing " + f.Key);
            return new SkillTuning(d);
        }

        internal static SkillTuning Read(JsonValue j, List<string> problems)
        {
            if (j.Kind != JsonKind.Object)
            {
                problems.Add("\"skills\" must be an object with the Skills tab's settings.");
                return null;
            }
            var v = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var f in Fields)
            {
                double x = j.GetNumber(f.Key, double.NaN);
                if (double.IsNaN(x)) problems.Add($"skills.{f.Key} ({f.Label}) is missing.");
                else if (x < f.Min || x > f.Max) problems.Add($"skills.{f.Key} ({f.Label}) = {x} is outside {f.Min} to {f.Max}.");
                else if (f.Whole && x != Math.Floor(x)) problems.Add($"skills.{f.Key} ({f.Label}) = {x} must be a whole number.");
                v[f.Key] = x;
            }
            if (v.TryGetValue("longUseRank1Bond", out double b1) && v.TryGetValue("longUseRank2Bond", out double b2) && b2 < b1)
                problems.Add("skills.longUseRank2Bond (Long use rank 2 bond) must not be below Long use rank 1 bond.");
            if (v.TryGetValue("longUseRank1Hours", out double h1) && v.TryGetValue("longUseRank2Hours", out double h2) && h2 < h1)
                problems.Add("skills.longUseRank2Hours (Long use rank 2 hours) must not be below Long use rank 1 hours.");
            return new SkillTuning(v);
        }
    }
}
