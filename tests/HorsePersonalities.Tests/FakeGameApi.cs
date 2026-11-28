using System.Collections.Generic;
using HorsePersonalities.Core.GameApi;

namespace HorsePersonalities.Tests
{
    /// <summary>A scriptable stand-in for the game, for testing Core logic without RDR2.</summary>
    internal sealed class FakeGameApi : IGameApi
    {
        public int GameTimeMs { get; set; }
        public bool IsLoadingScreenActive { get; set; }
        public bool IsPlayerPlaying { get; set; } = true;
        public bool IsMissionActive { get; set; }
        public bool IsScreenFadedOut { get; set; }
        public bool IsPlayerControlOn { get; set; } = true;
        public bool HasLetterBox { get; set; }
        public bool IsGameplayCamRendering { get; set; } = true;
        public bool IsCinematicCamRendering { get; set; }
        public bool IsAnimSceneCamActive { get; set; }
        public int PlayerPed { get; set; } = 1;
        public int Mount { get; set; }
        public int SaddleHorse { get; set; }

        public readonly Dictionary<int, uint> Models = new Dictionary<int, uint>();
        public readonly HashSet<int> Existing = new HashSet<int>();
        public readonly HashSet<int> Males = new HashSet<int>();
        public readonly Dictionary<(int, PedAttribute), AttributeReading> Attributes = new Dictionary<(int, PedAttribute), AttributeReading>();
        public readonly List<string> Subtitles = new List<string>();
        public readonly List<GameEvent> Events = new List<GameEvent>();

        public int AddHorse(int handle, uint model, bool male = true, int bondRank = 1)
        {
            Existing.Add(handle);
            Models[handle] = model;
            if (male) Males.Add(handle);
            Attributes[(handle, PedAttribute.Bonding)] = new AttributeReading { Attribute = PedAttribute.Bonding, Rank = bondRank };
            return handle;
        }

        public int GetMount(int ped) => ped == PlayerPed ? Mount : 0;
        public int GetLastMount(int ped) => Mount;
        public int GetSaddleHorse() => SaddleHorse;
        public int GetActiveHorse() => SaddleHorse;
        public int GetTempHorse() => 0;
        public bool Exists(int entity) => Existing.Contains(entity);
        public bool IsDead(int entity) => false;
        public uint GetModel(int entity) => Models.TryGetValue(entity, out uint m) ? m : 0;
        public bool IsMale(int ped) => Males.Contains(ped);
        public int GetTamingState(int horse) => 0;
        public float DistanceBetween(int a, int b) => 0;
        public int GetOutfitCount(int ped) => 1;
        public readonly Dictionary<int, float> Speeds = new Dictionary<int, float>();
        public readonly Dictionary<int, Vec3> Positions = new Dictionary<int, Vec3>();
        public float GetSpeed(int entity) => Speeds.TryGetValue(entity, out float v) ? v : 0;
        public int GameHour { get; set; } = 12;
        public bool IsSprintPressed { get; set; }
        public bool IsPedShooting(int ped) => false;
        public Vec3 GetPosition(int entity) => Positions.TryGetValue(entity, out Vec3 v) ? v : default;
        public Vec3 GetForward(int entity) => new Vec3(0, 1, 0);
        public IReadOnlyList<int> PedsNear(int around, float radius) => new List<int>();
        public bool IsHuman(int ped) => false;
        public bool InTown;
        public bool IsInTown(int entity) => InTown;
        public bool IsPlayerAiming { get; set; }
        public bool IsPedInCombatWith(int ped, int target) => false;
        public float? MaxBlendRatio;
        public void SetMaxMoveBlendRatio(int ped, float ratio) => MaxBlendRatio = ratio;
        public readonly List<int> Unhitched = new List<int>();
        public void Unhitch(int horse) => Unhitched.Add(horse);
        public Vec3 GetVelocity(int entity) => new Vec3(0, 0, 0);
        public bool IsHogtied(int ped) => false;
        public bool IsInWater(int entity) => false;
        public void LookAtPoint(int ped, Vec3 point, int durationMs) { }
        public bool IsPlayerSneaking { get; set; }
        public bool IsWhistlePressed { get; set; }
        public void SetAttributePoints(int ped, PedAttribute attribute, int points)
        {
            var r = ReadAttribute(ped, attribute);
            r.Points = points;
            Attributes[(ped, attribute)] = r;
        }
        public void AddAttributePoints(int ped, PedAttribute attribute, int points) => SetAttributePoints(ped, attribute, ReadAttribute(ped, attribute).Points + points);
        public int GetMetaPedType(int ped) => 0;
        public int GetClosestHumanPed(int aroundEntity, float radius, int excludePed) => 0;

        public AttributeReading ReadAttribute(int ped, PedAttribute attribute) =>
            Attributes.TryGetValue((ped, attribute), out var r) ? r : new AttributeReading { Attribute = attribute };
        public void SetAttributeBonusRank(int ped, PedAttribute attribute, int rank)
        {
            var r = ReadAttribute(ped, attribute);
            r.BonusRank = rank;
            r.Rank = r.BaseRank + rank;
            Attributes[(ped, attribute)] = r;
        }
        public int GetCoreValue(int ped, AttributeCore core) => 100;
        public void SetAttributeBaseRank(int ped, PedAttribute attribute, int rank)
        {
            var r = ReadAttribute(ped, attribute);
            r.BaseRank = rank;
            r.Rank = rank + r.BonusRank;
            Attributes[(ped, attribute)] = r;
        }
        public int BondPointsForRank(int horse, int rank) => rank <= 1 ? 0 : (rank - 1) * 500;

        public void SetBondLevel(int horse, int level)
        {
            var r = ReadAttribute(horse, PedAttribute.Bonding);
            r.Rank = r.BaseRank = level;
            Attributes[(horse, PedAttribute.Bonding)] = r;
        }

        public void PlayVocalization(int ped, string vocalizationName) { }
        public bool IsVocalizationPlaying(int ped) => false;
        public void LookAtEntity(int ped, int target, int durationMs) { }
        public void ClearLookAt(int ped) { }
        public void HorseAction(int horse, int actionId) { }
        public void HorseAgitate(int horse, bool kickOffRider) { }
        public void DismountPlayer() { Mount = 0; }
        public void StartScenario(int ped, string scenarioName, int durationMs, string conditionalAnim = null) { }
        public void WanderNearby(int ped, float radius) { }
        public void WanderFreely(int ped) { }
        public void FollowEntity(int ped, int target, float offsetBehind, int timeoutMs) { }
        public void FleeFrom(int ped, int threat, float distance, int timeMs) { }
        public void Combat(int ped, int target) { }
        public void ClearTasks(int ped) { }

        public IReadOnlyList<GameEvent> ReadAiEvents() => Events;

        public void ShowSubtitle(string text) => Subtitles.Add(text);
        public void DrawText(string text, float x, float y, float scale) { }
    }
}
