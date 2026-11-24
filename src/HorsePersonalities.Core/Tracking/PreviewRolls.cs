using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Tracking
{
    /// <summary>
    /// Phase 3 preview: rolls each mounted horse once per session so the overlay can show a personality in game.
    /// Not saved and not tied to the horse's identity yet: Phase 2 adds identity (temporary and permanent) and
    /// Phase 4 saves it. Rolled as a wild horse, so the overlay can also show the breaking difficulty it would have.
    /// </summary>
    public sealed class PreviewRolls
    {
        private readonly PersonalityRoller _roller;
        private readonly Func<ulong> _newSeed;
        private readonly Dictionary<int, Personality> _byHandle = new Dictionary<int, Personality>();

        public PreviewRolls(PersonalityRoller roller, Func<ulong> newSeed = null)
        {
            _roller = roller ?? throw new ArgumentNullException(nameof(roller));
            _newSeed = newSeed ?? SeededRandom.NewSeed;
        }

        /// <summary>The horse's preview roll, rolling it the first time. Null for a horse with no breed (vanilla in v1).</summary>
        public Personality For(int handle, Breed breed, out bool rolledNow)
        {
            rolledNow = false;
            if (breed == null) return null;
            if (_byHandle.TryGetValue(handle, out Personality p) && p.Breed == breed.Name) return p;
            p = _roller.Roll(breed, HorseOrigin.Wild, _newSeed());
            _byHandle[handle] = p;
            rolledNow = true;
            return p;
        }

        public int Count => _byHandle.Count;
    }

    /// <summary>The debug overlay's text (Build plan: Debug overlay). Built once per mount, drawn every frame; the overlay adds the live tick time.</summary>
    public static class OverlayText
    {
        public static IReadOnlyList<string> Build(HorseDescription horse, Personality personality)
        {
            var lines = new List<string>
            {
                "HORSE PERSONALITIES - debug overlay",
                $"{horse.BreedLabel}{(horse.Breed != null ? " (" + horse.Breed.Tier + " tier)" : "")} - {horse.ModelName} - {(horse.IsMale ? "male" : "female")} - bond {horse.BondRank}",
            };
            if (personality == null)
            {
                lines.Add("No personality: horses with no breed row stay vanilla in v1.");
            }
            else
            {
                lines.Add("Preview roll (Phase 3): not saved, re-rolls on reload.");
                foreach (string l in personality.Describe())
                    lines.Add(l.StartsWith("Breaking:", StringComparison.Ordinal) ? l.Replace("Breaking:", "Breaking if wild:") : l);
            }
            return lines;
        }
    }
}
