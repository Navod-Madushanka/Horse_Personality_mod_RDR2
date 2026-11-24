using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Identity;

namespace HorsePersonalities.Core.Tracking
{
    /// <summary>The debug overlay's text (Build plan: Debug overlay). Built once per mount, drawn every frame; the overlay adds the live tick time.</summary>
    public static class OverlayText
    {
        public static IReadOnlyList<string> Build(HorseDescription horse, HorseRecord record)
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
            lines.AddRange(record.Personality.Describe());
            return lines;
        }
    }
}
