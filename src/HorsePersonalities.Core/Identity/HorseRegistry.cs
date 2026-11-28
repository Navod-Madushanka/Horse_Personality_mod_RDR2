using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Identity
{
    public enum PersonalityStatus { Temporary, Permanent }

    /// <summary>A horse the mod knows: its fingerprint, personality and whether it is kept.</summary>
    public sealed class HorseRecord
    {
        public HorseFingerprint Id { get; }
        public Personality Personality { get; }
        public PersonalityStatus Status { get; }
        public string FirstSeen { get; }
        public string LastSeen { get; internal set; }

        /// <summary>
        /// The game's own base rank for each stat the mod changes ("health", "stamina"), stored the first time the mod saw
        /// the horse (Phase 5). Lets the mod re-apply after a reset and restore the original for an uninstall.
        /// </summary>
        public Dictionary<string, int> VanillaBase { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Test mode: the preset this personality came from; null for a rolled one.</summary>
        public string Preset { get; internal set; }

        /// <summary>Phase 8: what the horse has lived through. Kept for permanent horses; skills are worked out from it.</summary>
        public Skills.Experience Experience { get; internal set; } = new Skills.Experience();

        /// <summary>
        /// The mod's own bond (Build plan: Trust), in the game's bond XP points. Null until the mod first reads the horse's
        /// bond as a permanent horse; it then starts at the game's bond XP.
        /// </summary>
        public double? Trust { get; internal set; }

        public HorseRecord(HorseFingerprint id, Personality personality, PersonalityStatus status, string firstSeen, string lastSeen)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Personality = personality ?? throw new ArgumentNullException(nameof(personality));
            Status = status;
            FirstSeen = firstSeen;
            LastSeen = lastSeen;
        }

        internal HorseRecord AsPermanent(string today)
        {
            var r = new HorseRecord(Id, Personality, PersonalityStatus.Permanent, FirstSeen, today) { Preset = Preset, Experience = Experience.Copy(), Trust = Trust };
            foreach (var kv in VanillaBase) r.VanillaBase[kv.Key] = kv.Value;
            return r;
        }
    }

    /// <summary>
    /// Who gets a personality (Build plan: Who gets a personality):
    /// - A horse with no breed: none, ever (vanilla in v1).
    /// - Mounting someone else's horse: a temporary personality for this session, kept by the horse's handle.
    /// - The player's saddle on a horse: that same personality becomes permanent, kept by fingerprint.
    /// - A saddle horse the mod has never seen (the starting horse, a new purchase): rolled once, permanent, origin Stable.
    /// - Prized (Build plan: Prized horses): a stable horse, or a model on the sheet's Prized tab however it was found.
    /// Permanent records come back from horses.json through <see cref="Load"/>; temporary ones are never saved.
    /// </summary>
    public sealed class HorseRegistry
    {
        private const string LogCategory = "Registry";

        private readonly PersonalityRoller _roller;
        private readonly ILog _log;
        private readonly Func<ulong> _newSeed;
        private readonly Func<DateTime> _clock;
        private readonly Func<HorseFingerprint, bool> _isPrizedModel;
        private Func<HorseFingerprint, Breed, HorseOrigin, ulong, (Personality, string)> _override;

        /// <summary>
        /// Test mode: every new personality comes from this instead of the roller (Build plan: Test mode). It returns the
        /// personality and the preset's name.
        /// </summary>
        public void UseInsteadOfRoller(Func<HorseFingerprint, Breed, HorseOrigin, ulong, (Personality, string)> source) => _override = source;
        private readonly Dictionary<string, HorseRecord> _permanent = new Dictionary<string, HorseRecord>(StringComparer.Ordinal);
        private readonly Dictionary<int, HorseRecord> _temporary = new Dictionary<int, HorseRecord>();
        private readonly Dictionary<string, int> _liveHandle = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> _collisionWarned = new HashSet<string>(StringComparer.Ordinal);

        /// <param name="isPrizedModel">True for models on the sheet's Prized tab. Null: none.</param>
        public HorseRegistry(PersonalityRoller roller, ILog log, Func<ulong> newSeed = null, Func<DateTime> clock = null,
            Func<HorseFingerprint, bool> isPrizedModel = null)
        {
            _isPrizedModel = isPrizedModel ?? (_ => false);
            _roller = roller ?? throw new ArgumentNullException(nameof(roller));
            _log = log ?? NullLog.Instance;
            _newSeed = newSeed ?? SeededRandom.NewSeed;
            _clock = clock ?? (() => DateTime.Now);
        }

        /// <summary>True when permanent data changed since the last <see cref="MarkSaved"/>.</summary>
        public bool Dirty { get; private set; }

        public IReadOnlyCollection<HorseRecord> Permanent => _permanent.Values;
        public int TemporaryCount => _temporary.Count;

        public void MarkSaved() => Dirty = false;

        /// <summary>Stores a stat's original base rank on a permanent horse (Phase 5) and marks the save as changed.</summary>
        /// <summary>Phase 8: experience changed enough to be worth writing (a gunfight, a predator, a few minutes of riding).</summary>
        public void ExperienceChanged(HorseRecord record)
        {
            if (record != null && record.Status == PersonalityStatus.Permanent) Dirty = true;
        }

        /// <summary>Trust grew enough to be worth writing (Build plan: Trust).</summary>
        public void TrustChanged(HorseRecord record)
        {
            if (record != null && record.Status == PersonalityStatus.Permanent) Dirty = true;
        }

        public void RememberVanillaBase(HorseRecord record, string stat, int baseRank)
        {
            if (record.Status != PersonalityStatus.Permanent) return;
            if (record.VanillaBase.TryGetValue(stat, out int old) && old == baseRank) return;
            record.VanillaBase[stat] = baseRank;
            Dirty = true;
        }

        /// <summary>Takes permanent records read from horses.json. Does not mark the registry dirty.</summary>
        public void Load(IEnumerable<HorseRecord> records)
        {
            foreach (HorseRecord r in records)
            {
                if (r.Status != PersonalityStatus.Permanent) continue;
                _permanent[r.Id.Key] = r;
            }
            _log.Info(LogCategory, $"{_permanent.Count} saved horses loaded.");
        }

        /// <summary>The personality for a horse the player is on or next to, if it has one. Rolls nothing.</summary>
        public HorseRecord Find(int handle, HorseFingerprint id)
        {
            if (_permanent.TryGetValue(id.Key, out HorseRecord p)) return p;
            return _temporary.TryGetValue(handle, out HorseRecord t) && t.Id.Equals(id) ? t : null;
        }

        /// <summary>The player mounted a horse. Returns its personality, rolling a temporary one if needed; null for no breed.</summary>
        public HorseRecord OnMount(int handle, HorseFingerprint id, Breed breed, HorseOrigin origin, bool isSaddleHorse)
        {
            if (breed == null) return null;
            if (isSaddleHorse) return OnSaddle(handle, id, breed);
            if (_permanent.TryGetValue(id.Key, out HorseRecord kept)) return Touch(handle, kept);

            if (_temporary.TryGetValue(handle, out HorseRecord temp) && temp.Id.Equals(id)) return temp;
            string today = Today();
            temp = New(id, breed, origin, PersonalityStatus.Temporary, today);
            _temporary[handle] = temp;
            _log.Info(LogCategory, $"Temporary personality for horse {handle} ({id}, {origin}): {temp.Personality}");
            return temp;
        }

        /// <summary>
        /// The player's saddle went on this horse (or the mod first sees the saddle horse). Its temporary personality
        /// becomes permanent; a horse with none is rolled now as a stable horse. Null for no breed.
        /// </summary>
        public HorseRecord OnSaddle(int handle, HorseFingerprint id, Breed breed)
        {
            if (breed == null) return null;
            if (_permanent.TryGetValue(id.Key, out HorseRecord kept)) return Touch(handle, kept);

            string today = Today();
            HorseRecord record;
            if (_temporary.TryGetValue(handle, out HorseRecord temp) && temp.Id.Equals(id))
            {
                record = temp.AsPermanent(today);
                _temporary.Remove(handle);
                _log.Info(LogCategory, $"Saddled horse {handle} ({id}): temporary personality is now permanent.");
            }
            else
            {
                record = New(id, breed, HorseOrigin.Stable, PersonalityStatus.Permanent, today);
                _log.Info(LogCategory, $"First sight of saddle horse {handle} ({id}): rolled {record.Personality}");
            }
            _permanent[id.Key] = record;
            _liveHandle[id.Key] = handle;
            Dirty = true;
            return record;
        }

        private bool Prized(HorseFingerprint id, HorseOrigin origin) => origin == HorseOrigin.Stable || _isPrizedModel(id);

        private HorseRecord New(HorseFingerprint id, Breed breed, HorseOrigin origin, PersonalityStatus status, string today)
        {
            ulong seed = _newSeed();
            if (_override != null)
            {
                var (p, preset) = _override(id, breed, origin, seed);
                return new HorseRecord(id, p, status, today, today) { Preset = preset };
            }
            return new HorseRecord(id, _roller.Roll(breed, origin, seed, Prized(id, origin), id.Model), status, today, today);
        }

        /// <summary>
        /// Test mode: replaces a horse's personality on the spot (TestPresetKey). Keeps its status, dates and stored
        /// original stat ranks. Returns the new record, or null if the mod doesn't know the horse yet.
        /// </summary>
        public HorseRecord Replace(int handle, HorseFingerprint id, Personality personality, string preset)
        {
            HorseRecord old = Find(handle, id);
            if (old == null) return null;
            var r = new HorseRecord(id, personality, old.Status, old.FirstSeen, old.LastSeen) { Preset = preset, Experience = old.Experience.Copy(), Trust = old.Trust };
            foreach (var kv in old.VanillaBase) r.VanillaBase[kv.Key] = kv.Value;
            if (old.Status == PersonalityStatus.Permanent)
            {
                _permanent[id.Key] = r;
                Dirty = true;
            }
            else _temporary[handle] = r;
            _log.Info(LogCategory, $"Test mode: horse {handle} ({id}) is now preset {preset}: {personality}");
            return r;
        }

        private HorseRecord Touch(int handle, HorseRecord kept)
        {
            string key = kept.Id.Key;
            if (_liveHandle.TryGetValue(key, out int other) && other != handle && _collisionWarned.Add(key))
                _log.Warn(LogCategory, $"Horses {other} and {handle} share fingerprint {key}, so they share one personality. The game shows no way yet to tell them apart.");
            _liveHandle[key] = handle;
            string today = Today();
            if (kept.LastSeen != today)
            {
                kept.LastSeen = today;
                Dirty = true;
            }
            return kept;
        }

        private string Today() => _clock().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }
}
