using System;
using System.Globalization;
using HorsePersonalities.Core.Breeds;

namespace HorsePersonalities.Core.Skills
{
    /// <summary>
    /// What a permanent horse has lived through (Build plan: Phase 8). Saved in horses.json as counts; skills are worked
    /// out from them (<see cref="SkillSet.From"/>), so a retuned step or class rate applies to every horse at once.
    /// </summary>
    public sealed class Experience
    {
        public double Gunfights { get; set; }
        public double Predators { get; set; }
        public double RiddenMinutes { get; set; }
        public double GallopMinutes { get; set; }
        public double Sprints { get; set; }

        public double RiddenHours => RiddenMinutes / 60.0;

        public bool IsEmpty => Gunfights == 0 && Predators == 0 && RiddenMinutes == 0 && GallopMinutes == 0 && Sprints == 0;

        public Experience Copy() => new Experience
        {
            Gunfights = Gunfights, Predators = Predators, RiddenMinutes = RiddenMinutes, GallopMinutes = GallopMinutes, Sprints = Sprints,
        };

        public override string ToString() => string.Format(CultureInfo.InvariantCulture,
            "{0:0} gunfights, {1:0} predators, {2:0.0} h ridden, {3:0} min galloping, {4:0} sprints",
            Gunfights, Predators, RiddenHours, GallopMinutes, Sprints);
    }

    /// <summary>A horse's five skills, 0 (untrained) to 1 (at its ceiling).</summary>
    public struct SkillSet
    {
        public double Gunfire, Predators, Speed, Acceleration, Handling;

        /// <summary>Experience x step x the breed's learning rate (combat for gunfire and predators, physical for the rest), capped at 1.</summary>
        public static SkillSet From(Experience e, LearningRate rate, SkillTuning t)
        {
            if (e == null || t == null) return default;
            double C(double v) => Math.Max(0, Math.Min(1, v));
            return new SkillSet
            {
                Gunfire = C(e.Gunfights * t.GunfightStep * rate.Combat),
                Predators = C(e.Predators * t.PredatorStep * rate.Combat),
                Speed = C(e.GallopMinutes * t.SpeedPerGallopMinute * rate.Physical),
                Acceleration = C(e.Sprints * t.AccelerationPerSprint * rate.Physical),
                Handling = C(e.RiddenMinutes * t.HandlingPerRiddenMinute * rate.Physical),
            };
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture,
            "gunfire {0:0.00}, predators {1:0.00}, speed {2:0.00}, accel {3:0.00}, handling {4:0.00}",
            Gunfire, Predators, Speed, Acceleration, Handling);
    }
}
