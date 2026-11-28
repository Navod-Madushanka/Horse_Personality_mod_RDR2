using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Identity;

namespace HorsePersonalities.Core.Tracking
{
    /// <summary>The debug overlay's text (Build plan: Debug overlay). Built once per mount, drawn every frame; the overlay adds the live tick time.</summary>
    public static class OverlayText
    {
        /// <param name="skills">Phase 8: with the Skills tab, a permanent horse also shows its experience, skills and long-use ranks.</param>
        /// <param name="favorite">The horse is a community favorite (Prized tab), for its long-use ranks.</param>
        public static IReadOnlyList<string> Build(HorseDescription horse, HorseRecord record, Skills.SkillTuning skills = null, bool favorite = false)
        {
            var lines = new List<string>
            {
                "HORSE PERSONALITIES - debug overlay",
                // Short lines: the overlay shares the screen with the DevTools menu, so the model name gets its own line.
                $"{horse.BreedLabel}{(horse.Breed != null ? " (" + horse.Breed.Tier + " tier)" : "")} - {(horse.IsMale ? "male" : "female")} - bond {horse.BondRank}",
                horse.ModelName,
            };
            if (record == null)
            {
                lines.Add("No personality: horses with no breed row stay vanilla in v1.");
                return lines;
            }
            lines.Add("Fingerprint " + record.Id.Key);
            lines.Add(record.Status == PersonalityStatus.Permanent
                ? $"Permanent, first seen {record.FirstSeen}"
                : "Temporary: gone on reload, saddle it to keep it");
            if (record.Preset != null) lines.Add("Test preset: " + record.Preset);
            lines.AddRange(record.Personality.Describe());
            if (record.Status == PersonalityStatus.Permanent)
                lines.Add(record.Trust.HasValue
                    ? $"Trust {(horse.TrustLevel > 0 ? horse.TrustLevel.ToString() : "?")} ({record.Trust.Value:0} points; game bond XP {horse.BondPoints})"
                    : "Trust: starts at the game's bond XP within a second");
            if (skills != null && horse.Breed != null && record.Status == PersonalityStatus.Permanent)
            {
                var set = Skills.SkillSet.From(record.Experience, horse.Breed.Learning, skills);
                int longUse = Skills.GrowthPlanner.LongUseRanks(horse.FeatureBond, record.Experience.RiddenHours, favorite, skills);
                lines.Add("Experience: " + record.Experience);
                lines.Add("Skills: " + set);
                lines.Add($"Long-use growth: {longUse} rank{(longUse == 1 ? "" : "s")}{(favorite ? " (community favorite: up to 2)" : "")}");
            }
            return lines;
        }
    }
}
