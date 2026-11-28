using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Json;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>
    /// Phase 6 tuning, from the breed sheet's Behavior tab (rule 6: no tuning numbers in code). Rates are per minute,
    /// times are seconds, distances metres, speeds m/s.
    /// </summary>
    public sealed class BehaviorTuning
    {
        private readonly Dictionary<string, double> _v;

        private BehaviorTuning(Dictionary<string, double> v) { _v = v; }

        public double this[string key] => _v[key];

        public double SpookBaseRate => _v["spookBaseRate"];
        public double NervesSlope => _v["nervesSlope"];
        public double NightSpookMultiplier => _v["nightSpookMultiplier"];
        public int NightStartsHour => (int)_v["nightStartsHour"];
        public int NightEndsHour => (int)_v["nightEndsHour"];
        public double GallopSpookMultiplier => _v["gallopSpookMultiplier"];
        public double GallopSpeed => _v["gallopSpeed"];
        public double WeakBondSpookMultiplier => _v["weakBondSpookMultiplier"];
        public double FullBondSpookMultiplier => _v["fullBondSpookMultiplier"];
        public double ThreatSpookChance => _v["threatSpookChance"];
        public double CourageSlope => _v["courageSlope"];
        public double PredatorRange => _v["predatorRange"];
        public double RiderGunfireScale => _v["riderGunfireScale"];
        public double ExplosionCouragePenalty => _v["explosionCouragePenalty"];
        public double PushBuckChance => _v["pushBuckChance"];
        public double TemperSlope => _v["temperSlope"];
        public double MaxPushBuckChance => _v["maxPushBuckChance"];
        public double LowStamina => _v["lowStamina"];
        public double LongSprintSeconds => _v["longSprintSeconds"];
        public double HardStopSlowdown => _v["hardStopSlowdown"];
        public double ScaredBuckTemper => _v["scaredBuckTemper"];
        public double RearCourage => _v["rearCourage"];
        public double BoltCourage => _v["boltCourage"];
        public double KickChance => _v["kickChance"];
        public double KickLingerSeconds => _v["kickLingerSeconds"];
        public double KickBehindDistance => _v["kickBehindDistance"];
        public double BondAffinitySlope => _v["bondAffinitySlope"];
        public double HardBondSpeed => _v["hardBondSpeed"];
        public int HardExtraThrows => (int)_v["hardExtraThrows"];
        public double HardThrowAfterSeconds => _v["hardThrowAfterSeconds"];
        public double UnbreakableThrowAfterSeconds => _v["unbreakableThrowAfterSeconds"];
        public double SpookCooldown => _v["spookCooldown"];
        public double BuckCooldown => _v["buckCooldown"];
        public double KickCooldown => _v["kickCooldown"];
        public double TellCooldown => _v["tellCooldown"];
        public double BehaviorGap => _v["behaviorGap"];
        public double ThreatQuietSeconds => _v["threatQuietSeconds"];
        // Phase 7: quirks, care reactions, demeanor.
        public double StandingStillSpeed => _v["standingStillSpeed"];
        public double ChatterboxRate => _v["chatterboxRate"];
        public double ChatterboxHorseNearMultiplier => _v["chatterboxHorseNearMultiplier"];
        public double HeadTosserRate => _v["headTosserRate"];
        public double NosyRate => _v["nosyRate"];
        public double NosyRange => _v["nosyRange"];
        public double ShadowTriggerDistance => _v["shadowTriggerDistance"];
        public double ShadowFollowSeconds => _v["shadowFollowSeconds"];
        public double WandererDelaySeconds => _v["wandererDelaySeconds"];
        public double WandererPlayerDistance => _v["wandererPlayerDistance"];
        public double WanderSeconds => _v["wanderSeconds"];
        public double GrazeSeconds => _v["grazeSeconds"];
        public double KickerChance => _v["kickerChance"];
        public double ReactionDelaySeconds => _v["reactionDelaySeconds"];
        public double ReactionTraitThreshold => _v["reactionTraitThreshold"];
        public double ReactionChance => _v["reactionChance"];
        public double ReactionBondSoften => _v["reactionBondSoften"];
        public double PickyLoveShare => _v["pickyLoveShare"];
        public double PickyDislikeShare => _v["pickyDislikeShare"];
        public double ImpatientAfterSeconds => _v["impatientAfterSeconds"];
        public double ImpatientRate => _v["impatientRate"];
        public double AlertRate => _v["alertRate"];
        public double FussyDirtRank => _v["fussyDirtRank"];
        public double FussyRate => _v["fussyRate"];
        public double AffectionateDemeanorBonus => _v["affectionateDemeanorBonus"];
        // Phase 7b: signature behaviors.
        public double SnakeRangeMultiplier => _v["snakeRangeMultiplier"];
        public double MoodyRefusalRate => _v["moodyRefusalRate"];
        public double WalkSpeed => _v["walkSpeed"];
        public double NightOwlMultiplier => _v["nightOwlMultiplier"];
        public double CityLoverMultiplier => _v["cityLoverMultiplier"];
        public double TownDisdainRate => _v["townDisdainRate"];
        public double TownDisdainRange => _v["townDisdainRange"];
        public double DramaticRearChance => _v["dramaticRearChance"];
        public double DuskStartsHour => _v["duskStartsHour"];
        public double EscapeAfterSeconds => _v["escapeAfterSeconds"];
        public double EscapePlayerDistance => _v["escapePlayerDistance"];
        public double PranksterRate => _v["pranksterRate"];
        public double PranksterEnemyRange => _v["pranksterEnemyRange"];
        public double LazyWalkRatio => _v["lazyWalkRatio"];
        public double GrazerIdleSeconds => _v["grazerIdleSeconds"];
        public double GrazerSeconds => _v["grazerSeconds"];
        public double HillClimbSpeed => _v["hillClimbSpeed"];
        public double HillSighRate => _v["hillSighRate"];
        public double ScatterbrainRate => _v["scatterbrainRate"];
        public double SoulStareRange => _v["soulStareRange"];
        public double SoulStareSeconds => _v["soulStareSeconds"];
        public double SoulStareRate => _v["soulStareRate"];
        public double BrawlSeconds => _v["brawlSeconds"];
        public double CreeperRange => _v["creeperRange"];
        public double CreeperRate => _v["creeperRate"];
        public double CreeperSeconds => _v["creeperSeconds"];
        public double BrushExcitementChance => _v["brushExcitementChance"];
        public double SignatureCooldown => _v["signatureCooldown"];
        public double WanderlustDelaySeconds => _v["wanderlustDelaySeconds"];
        public double WanderlustPlayerDistance => _v["wanderlustPlayerDistance"];
        public double WanderlustSeconds => _v["wanderlustSeconds"];
        public double BoredAimSeconds => _v["boredAimSeconds"];
        public double BoredBuckChance => _v["boredBuckChance"];
        public double ClumsyRate => _v["clumsyRate"];
        public double SnackMagnetRate => _v["snackMagnetRate"];
        public double SnackSeconds => _v["snackSeconds"];
        public double ShortKingRange => _v["shortKingRange"];
        public double ShortKingRate => _v["shortKingRate"];
        public double SelectiveHearingChance => _v["selectiveHearingChance"];
        public double SelectiveHearingSeconds => _v["selectiveHearingSeconds"];
        public double SaddleHaterChance => _v["saddleHaterChance"];
        public double BountyStomperRange => _v["bountyStomperRange"];
        public double BountyStomperRate => _v["bountyStomperRate"];
        public double GuardRange => _v["guardRange"];
        public double GuardRate => _v["guardRate"];
        public double UnhurriedRatio => _v["unhurriedRatio"];
        public double DangerMemorySeconds => _v["dangerMemorySeconds"];
        public double JealousRange => _v["jealousRange"];
        public double JealousChance => _v["jealousChance"];
        public double RestlessSeconds => _v["restlessSeconds"];
        public double RestlessRate => _v["restlessRate"];
        public double TrailSnobSeconds => _v["trailSnobSeconds"];
        public double TrailSnobRate => _v["trailSnobRate"];
        public double WarHorseRange => _v["warHorseRange"];
        public double SnobRate => _v["snobRate"];
        public double ShowboatRange => _v["showboatRange"];
        public double ShowboatCrowd => _v["showboatCrowd"];
        public double ShowboatChance => _v["showboatChance"];
        public double MustLeadRange => _v["mustLeadRange"];
        public double MustLeadRate => _v["mustLeadRate"];
        public double RacerRange => _v["racerRange"];
        public double RacerRate => _v["racerRate"];
        public double SpoiledGallopSeconds => _v["spoiledGallopSeconds"];
        public double SpoiledChance => _v["spoiledChance"];
        public double SeparationDistance => _v["separationDistance"];
        public double SeparationRate => _v["separationRate"];
        public double NoBrakesChance => _v["noBrakesChance"];
        public double NoBrakesSeconds => _v["noBrakesSeconds"];
        public double GrudgeSeconds => _v["grudgeSeconds"];
        public double DownhillSpeed => _v["downhillSpeed"];
        public double DownhillSnobRate => _v["downhillSnobRate"];
        public double StrangerDangerRange => _v["strangerDangerRange"];
        public double StrangerDangerSpeed => _v["strangerDangerSpeed"];
        public double StrangerDangerRate => _v["strangerDangerRate"];
        public double NeedsSoothingDelay => _v["needsSoothingDelay"];
        public double NeedsSoothingSeconds => _v["needsSoothingSeconds"];
        public double NeedsSoothingRate => _v["needsSoothingRate"];
        public double HillCrawlRatio => _v["hillCrawlRatio"];
        public double WaterSighRate => _v["waterSighRate"];
        public double BeggarRange => _v["beggarRange"];
        public double BeggarSeconds => _v["beggarSeconds"];
        public double BeggarRate => _v["beggarRate"];
        public double BalkRate => _v["balkRate"];
        public double StrictSeconds => _v["strictSeconds"];
        public double StrictRatio => _v["strictRatio"];

        public static readonly IReadOnlyList<(string Key, string Label, double Min, double Max, bool Whole)> Fields = new[]
        {
            ("spookBaseRate", "Spook base rate", 0.0, 5.0, false),
            ("nervesSlope", "Nerves slope", 0.0, 3.0, false),
            ("nightSpookMultiplier", "Night spook multiplier", 0.0, 10.0, false),
            ("nightStartsHour", "Night starts hour", 0.0, 23.0, true),
            ("nightEndsHour", "Night ends hour", 0.0, 23.0, true),
            ("gallopSpookMultiplier", "Gallop spook multiplier", 0.0, 10.0, false),
            ("gallopSpeed", "Gallop speed", 1.0, 30.0, false),
            ("weakBondSpookMultiplier", "Weak bond spook multiplier", 0.0, 10.0, false),
            ("fullBondSpookMultiplier", "Full bond spook multiplier", 0.0, 10.0, false),
            ("threatSpookChance", "Threat spook chance", 0.001, 0.999, false),
            ("courageSlope", "Courage slope", 0.0, 5.0, false),
            ("predatorRange", "Predator range", 1.0, 200.0, false),
            ("riderGunfireScale", "Rider gunfire scale", 0.0, 1.0, false),
            ("explosionCouragePenalty", "Explosion courage penalty", 0.0, 4.0, false),
            ("pushBuckChance", "Push buck chance", 0.0, 1.0, false),
            ("temperSlope", "Temper slope", 0.0, 3.0, false),
            ("maxPushBuckChance", "Max push buck chance", 0.0, 1.0, false),
            ("lowStamina", "Low stamina", 0.0, 100.0, false),
            ("longSprintSeconds", "Long sprint seconds", 1.0, 600.0, false),
            ("hardStopSlowdown", "Hard stop slowdown", 0.5, 50.0, false),
            ("scaredBuckTemper", "Scared buck temper", -2.0, 3.0, false),
            ("rearCourage", "Rear courage", -3.0, 3.0, false),
            ("boltCourage", "Bolt courage", -3.0, 3.0, false),
            ("kickChance", "Kick chance", 0.0, 1.0, false),
            ("kickLingerSeconds", "Kick linger seconds", 0.5, 60.0, false),
            ("kickBehindDistance", "Kick behind distance", 0.5, 10.0, false),
            ("bondAffinitySlope", "Bond affinity slope", 0.0, 2.0, false),
            ("hardBondSpeed", "Hard bond speed", 0.05, 2.0, false),
            ("hardExtraThrows", "Hard extra throws", 0.0, 10.0, true),
            ("hardThrowAfterSeconds", "Hard throw after seconds", 0.5, 120.0, false),
            ("unbreakableThrowAfterSeconds", "Unbreakable throw after seconds", 0.5, 120.0, false),
            ("spookCooldown", "Spook cooldown", 0.0, 3600.0, false),
            ("buckCooldown", "Buck cooldown", 0.0, 3600.0, false),
            ("kickCooldown", "Kick cooldown", 0.0, 3600.0, false),
            ("tellCooldown", "Tell cooldown", 0.0, 3600.0, false),
            ("behaviorGap", "Behavior gap", 0.0, 3600.0, false),
            ("threatQuietSeconds", "Threat quiet seconds", 1.0, 600.0, false),
            ("standingStillSpeed", "Standing still speed", 0.0, 5.0, false),
            ("chatterboxRate", "Chatterbox rate", 0.0, 20.0, false),
            ("chatterboxHorseNearMultiplier", "Chatterbox horse near multiplier", 0.0, 10.0, false),
            ("headTosserRate", "Head-tosser rate", 0.0, 20.0, false),
            ("nosyRate", "Nosy rate", 0.0, 20.0, false),
            ("nosyRange", "Nosy range", 1.0, 100.0, false),
            ("shadowTriggerDistance", "Shadow trigger distance", 0.5, 50.0, false),
            ("shadowFollowSeconds", "Shadow follow seconds", 0.5, 60.0, false),
            ("wandererDelaySeconds", "Wanderer delay seconds", 1.0, 600.0, false),
            ("wandererPlayerDistance", "Wanderer player distance", 0.0, 100.0, false),
            ("wanderSeconds", "Wander seconds", 1.0, 120.0, false),
            ("grazeSeconds", "Graze seconds", 1.0, 120.0, false),
            ("kickerChance", "Kicker chance", 0.0, 1.0, false),
            ("reactionDelaySeconds", "Reaction delay seconds", 0.0, 10.0, false),
            ("reactionTraitThreshold", "Reaction trait threshold", 0.0, 2.0, false),
            ("reactionChance", "Reaction chance", 0.0, 1.0, false),
            ("reactionBondSoften", "Reaction bond soften", 0.0, 0.33, false),
            ("pickyLoveShare", "Picky love share", 0.0, 1.0, false),
            ("pickyDislikeShare", "Picky dislike share", 0.0, 1.0, false),
            ("impatientAfterSeconds", "Impatient after seconds", 1.0, 600.0, false),
            ("impatientRate", "Impatient rate", 0.0, 20.0, false),
            ("alertRate", "Alert rate", 0.0, 20.0, false),
            ("fussyDirtRank", "Fussy dirt rank", 0.0, 100.0, false),
            ("fussyRate", "Fussy rate", 0.0, 20.0, false),
            ("affectionateDemeanorBonus", "Affectionate demeanor bonus", 0.0, 2.0, false),
            ("snakeRangeMultiplier", "Snake range multiplier", 1.0, 10.0, false),
            ("moodyRefusalRate", "Moody refusal rate", 0.0, 20.0, false),
            ("walkSpeed", "Walk speed", 0.5, 10.0, false),
            ("nightOwlMultiplier", "Night owl multiplier", 0.0, 10.0, false),
            ("cityLoverMultiplier", "City lover multiplier", 0.0, 10.0, false),
            ("townDisdainRate", "Town disdain rate", 0.0, 20.0, false),
            ("townDisdainRange", "Town disdain range", 1.0, 50.0, false),
            ("dramaticRearChance", "Dramatic rear chance", 0.0, 1.0, false),
            ("duskStartsHour", "Dusk starts hour", 0.0, 23.0, false),
            ("escapeAfterSeconds", "Escape after seconds", 1.0, 3600.0, false),
            ("escapePlayerDistance", "Escape player distance", 0.0, 200.0, false),
            ("pranksterRate", "Prankster rate", 0.0, 20.0, false),
            ("pranksterEnemyRange", "Prankster enemy range", 1.0, 200.0, false),
            ("lazyWalkRatio", "Lazy walk ratio", 0.1, 3.0, false),
            ("grazerIdleSeconds", "Grazer idle seconds", 1.0, 600.0, false),
            ("grazerSeconds", "Grazer seconds", 1.0, 120.0, false),
            ("hillClimbSpeed", "Hill climb speed", 0.1, 10.0, false),
            ("hillSighRate", "Hill sigh rate", 0.0, 20.0, false),
            ("scatterbrainRate", "Scatterbrain rate", 0.0, 20.0, false),
            ("soulStareRange", "Soul stare range", 0.5, 50.0, false),
            ("soulStareSeconds", "Soul stare seconds", 0.5, 60.0, false),
            ("soulStareRate", "Soul stare rate", 0.0, 20.0, false),
            ("brawlSeconds", "Brawl seconds", 0.5, 60.0, false),
            ("creeperRange", "Creeper range", 1.0, 100.0, false),
            ("creeperRate", "Creeper rate", 0.0, 20.0, false),
            ("creeperSeconds", "Creeper seconds", 0.5, 60.0, false),
            ("brushExcitementChance", "Brush excitement chance", 0.0, 1.0, false),
            ("signatureCooldown", "Signature cooldown", 0.0, 3600.0, false),
            ("wanderlustDelaySeconds", "Wanderlust delay seconds", 1.0, 600.0, false),
            ("wanderlustPlayerDistance", "Wanderlust player distance", 0.0, 200.0, false),
            ("wanderlustSeconds", "Wanderlust seconds", 1.0, 600.0, false),
            ("boredAimSeconds", "Bored aim seconds", 1.0, 120.0, false),
            ("boredBuckChance", "Bored buck chance", 0.0, 1.0, false),
            ("clumsyRate", "Clumsy rate", 0.0, 20.0, false),
            ("snackMagnetRate", "Snack magnet rate", 0.0, 20.0, false),
            ("snackSeconds", "Snack seconds", 1.0, 60.0, false),
            ("shortKingRange", "Short king range", 0.5, 100.0, false),
            ("shortKingRate", "Short king rate", 0.0, 20.0, false),
            ("selectiveHearingChance", "Selective hearing chance", 0.0, 1.0, false),
            ("selectiveHearingSeconds", "Selective hearing seconds", 0.5, 60.0, false),
            ("saddleHaterChance", "Saddle hater chance", 0.0, 1.0, false),
            ("bountyStomperRange", "Bounty stomper range", 0.5, 50.0, false),
            ("bountyStomperRate", "Bounty stomper rate", 0.0, 20.0, false),
            ("guardRange", "Guard range", 0.5, 50.0, false),
            ("guardRate", "Guard rate", 0.0, 20.0, false),
            ("unhurriedRatio", "Unhurried ratio", 0.1, 3.0, false),
            ("dangerMemorySeconds", "Danger memory seconds", 0.0, 3600.0, false),
            ("jealousRange", "Jealous range", 1.0, 200.0, false),
            ("jealousChance", "Jealous chance", 0.0, 1.0, false),
            ("restlessSeconds", "Restless seconds", 1.0, 600.0, false),
            ("restlessRate", "Restless rate", 0.0, 20.0, false),
            ("trailSnobSeconds", "Trail snob seconds", 1.0, 600.0, false),
            ("trailSnobRate", "Trail snob rate", 0.0, 20.0, false),
            ("warHorseRange", "War horse range", 0.5, 50.0, false),
            ("snobRate", "Snob rate", 0.0, 20.0, false),
            ("showboatRange", "Showboat range", 1.0, 100.0, false),
            ("showboatCrowd", "Showboat crowd", 1.0, 50.0, false),
            ("showboatChance", "Showboat chance", 0.0, 1.0, false),
            ("mustLeadRange", "Must lead range", 1.0, 100.0, false),
            ("mustLeadRate", "Must lead rate", 0.0, 20.0, false),
            ("racerRange", "Racer range", 1.0, 100.0, false),
            ("racerRate", "Racer rate", 0.0, 20.0, false),
            ("spoiledGallopSeconds", "Spoiled gallop seconds", 1.0, 600.0, false),
            ("spoiledChance", "Spoiled chance", 0.0, 1.0, false),
            ("separationDistance", "Separation distance", 0.5, 100.0, false),
            ("separationRate", "Separation rate", 0.0, 20.0, false),
            ("noBrakesChance", "No brakes chance", 0.0, 1.0, false),
            ("noBrakesSeconds", "No brakes seconds", 0.1, 10.0, false),
            ("grudgeSeconds", "Grudge seconds", 1.0, 3600.0, false),
            ("downhillSpeed", "Downhill speed", 0.1, 10.0, false),
            ("downhillSnobRate", "Downhill snob rate", 0.0, 20.0, false),
            ("strangerDangerRange", "Stranger danger range", 0.5, 50.0, false),
            ("strangerDangerSpeed", "Stranger danger speed", 0.1, 20.0, false),
            ("strangerDangerRate", "Stranger danger rate", 0.0, 60.0, false),
            ("needsSoothingDelay", "Needs soothing delay", 0.0, 600.0, false),
            ("needsSoothingSeconds", "Needs soothing seconds", 1.0, 3600.0, false),
            ("needsSoothingRate", "Needs soothing rate", 0.0, 20.0, false),
            ("hillCrawlRatio", "Hill crawl ratio", 0.1, 3.0, false),
            ("waterSighRate", "Water sigh rate", 0.0, 20.0, false),
            ("beggarRange", "Beggar range", 0.5, 20.0, false),
            ("beggarSeconds", "Beggar seconds", 1.0, 600.0, false),
            ("beggarRate", "Beggar rate", 0.0, 20.0, false),
            ("balkRate", "Balk rate", 0.0, 20.0, false),
            ("strictSeconds", "Strict seconds", 1.0, 600.0, false),
            ("strictRatio", "Strict ratio", 0.1, 3.0, false),
        };

        /// <summary>Every value at its Behavior tab default, with overrides; for tests.</summary>
        public static BehaviorTuning Of(IDictionary<string, double> values)
        {
            var d = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var f in Fields) d[f.Key] = values.TryGetValue(f.Key, out double v) ? v : throw new ArgumentException("Missing " + f.Key);
            return new BehaviorTuning(d);
        }

        internal static BehaviorTuning Read(JsonValue j, List<string> problems)
        {
            if (j.Kind != JsonKind.Object)
            {
                problems.Add("\"behavior\" must be an object with the Behavior tab's settings.");
                return null;
            }
            var v = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var f in Fields)
            {
                double x = j.GetNumber(f.Key, double.NaN);
                if (double.IsNaN(x)) problems.Add($"behavior.{f.Key} ({f.Label}) is missing.");
                else if (x < f.Min || x > f.Max) problems.Add($"behavior.{f.Key} ({f.Label}) = {x} is outside {f.Min} to {f.Max}.");
                else if (f.Whole && x != Math.Floor(x)) problems.Add($"behavior.{f.Key} ({f.Label}) = {x} must be a whole number.");
                v[f.Key] = x;
            }
            return new BehaviorTuning(v);
        }
    }
}
