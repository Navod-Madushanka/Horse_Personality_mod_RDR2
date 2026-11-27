using System;
using System.Globalization;
using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Hashing;
using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Behavior
{
    public enum CareKind { Pat, Brush, Feed, Calm }

    public enum ReactionStyle { Neutral, Affectionate, Aloof, Grumpy }

    public enum Taste { Ordinary, Loves, Ignores }

    /// <summary>
    /// Care reactions (Build plan: Reactions to care), played just after the vanilla animation. The style comes from
    /// Bond affinity (plus the breed's Affectionate demeanor) and Temper, and reactions get less likely as the bond grows:
    ///
    ///         | Affectionate          | Aloof           | Grumpy
    ///   Pat   | soft nicker (Content) | looks away (-)  | head toss (Agitate)
    ///   Brush | stands still, nicker  | fidgets         | stamps (Agitate)
    ///   Feed  | happy, bigger if fav  | eats, nothing   | head toss after
    ///   Calm  | settles (-)           | settles slowly  | fights it (Agitate)
    ///
    /// Picky eaters react to the treat itself: one they love gets a happy call, one they ignore a dismissive snort.
    /// "Looks away" has no scriptable animation, so aloof horses simply give nothing back.
    /// </summary>
    public sealed class CareReactor
    {
        private readonly BehaviorTuning _t;
        private readonly Func<double> _random;

        public CareReactor(BehaviorTuning tuning, Func<double> random)
        {
            _t = tuning;
            _random = random;
        }

        public ReactionStyle Style(Personality p, DemeanorTraits demeanor)
        {
            double affinity = p.Traits.BondAffinity + _t.AffectionateDemeanorBonus * (demeanor?.Affectionate ?? 0);
            if (p.Traits.Temper >= _t.ReactionTraitThreshold) return ReactionStyle.Grumpy;
            if (affinity >= _t.ReactionTraitThreshold) return ReactionStyle.Affectionate;
            if (affinity <= -_t.ReactionTraitThreshold) return ReactionStyle.Aloof;
            return ReactionStyle.Neutral;
        }

        /// <summary>The horse's taste for one item, fixed by its seed: the same treat always gets the same answer.</summary>
        public Taste TasteFor(Personality p, uint itemHash)
        {
            if (itemHash == 0) return Taste.Ordinary;
            uint h = Joaat.Hash(p.Seed.ToString("X16", CultureInfo.InvariantCulture) + ":" + itemHash.ToString("X8", CultureInfo.InvariantCulture));
            double u = h / 4294967296.0;
            if (u < _t.PickyLoveShare) return Taste.Loves;
            if (u < _t.PickyLoveShare + _t.PickyDislikeShare) return Taste.Ignores;
            return Taste.Ordinary;
        }

        /// <summary>What the horse does after a care action. Up to two actions (a picky eater's love is a call and a nicker).</summary>
        /// <summary>Brush excitement (coat signature): as soon as you start brushing. None when it doesn't happen.</summary>
        public (BehaviorAction First, BehaviorAction Second, string Cause) BrushStarted(Personality p)
        {
            if (p == null || !p.Has(Signature.BrushExcitement)) return (BehaviorAction.None, BehaviorAction.None, "");
            double roll = _random();
            if (roll >= _t.BrushExcitementChance) return (BehaviorAction.None, BehaviorAction.None, $"Brush excitement: roll {roll:0.000} vs {_t.BrushExcitementChance:0.000} (calm this time)");
            return (BehaviorAction.Agitate, BehaviorAction.Content, $"Brush excitement: roll {roll:0.000} vs {_t.BrushExcitementChance:0.000}");
        }

        /// <param name="coat">The horse's coat design, if it has one: its loved and ignored treats come first.</param>
        /// <param name="treatName">The fed treat's name (data/treats.csv), when the item code is known.</param>
        public (BehaviorAction First, BehaviorAction Second, string Cause) React(Personality p, DemeanorTraits demeanor, CareKind kind, int bondLevel, uint itemHash,
            Coat coat = null, string treatName = null)
        {
            if (kind == CareKind.Feed && coat != null && treatName != null)
            {
                if (coat.LovesTreats.Any(x => string.Equals(x, treatName, StringComparison.OrdinalIgnoreCase)))
                    return (BehaviorAction.Call, BehaviorAction.Content, $"{coat.Archetype}: loves {treatName}");
                if (coat.IgnoresTreats.Any(x => string.Equals(x, treatName, StringComparison.OrdinalIgnoreCase)))
                    return (BehaviorAction.Snub, BehaviorAction.None, $"{coat.Archetype}: judges you for {treatName}");
            }
            if (kind == CareKind.Feed && p.Quirks.Contains(Quirk.PickyEater))
            {
                Taste taste = TasteFor(p, itemHash);
                if (taste == Taste.Loves) return (BehaviorAction.Call, BehaviorAction.Content, $"Picky eater: loves this treat (0x{itemHash:X8})");
                if (taste == Taste.Ignores) return (BehaviorAction.Snort, BehaviorAction.None, $"Picky eater: doesn't care for this treat (0x{itemHash:X8})");
            }

            ReactionStyle style = Style(p, demeanor);
            if (style == ReactionStyle.Neutral) return (BehaviorAction.None, BehaviorAction.None, "neutral: vanilla only");
            double chance = Math.Max(0, _t.ReactionChance * (1 - _t.ReactionBondSoften * Math.Max(0, bondLevel - 1)));
            double roll = _random();
            string why = $"{style} {kind}, bond {bondLevel}: roll {roll:0.000} vs {chance:0.000}";
            if (roll >= chance) return (BehaviorAction.None, BehaviorAction.None, why + " (no reaction)");

            switch (style)
            {
                case ReactionStyle.Affectionate:
                    if (kind == CareKind.Calm) return (BehaviorAction.None, BehaviorAction.None, why + ": settles");
                    return (BehaviorAction.Content, BehaviorAction.None, why);
                case ReactionStyle.Aloof:
                    return kind == CareKind.Brush ? (BehaviorAction.Agitate, BehaviorAction.None, why + ": fidgets")
                                                  : (BehaviorAction.None, BehaviorAction.None, why + ": gives nothing back");
                default: // Grumpy
                    return (BehaviorAction.Agitate, kind == CareKind.Brush || kind == CareKind.Calm ? BehaviorAction.Snort : BehaviorAction.None, why);
            }
        }
    }
}
