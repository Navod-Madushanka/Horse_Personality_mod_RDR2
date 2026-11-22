using System.Collections.Generic;

namespace HorsePersonalities.Core.GameApi
{
    /// <summary>
    /// ePedAttribute from the native DB (see SET_ATTRIBUTE_BASE_RANK). Indexes are the game's; do not reorder.
    /// </summary>
    public enum PedAttribute
    {
        Health = 0,
        Stamina = 1,
        SpecialAbility = 2,
        Courage = 3,
        Agility = 4,
        Speed = 5,
        Acceleration = 6,
        Bonding = 7,
        Unruliness = 21,
    }

    /// <summary>eAttributeCore: the inner rings shown on the horse's stat bars.</summary>
    public enum AttributeCore
    {
        Health = 0,
        Stamina = 1,
        DeadEye = 2,
    }

    /// <summary>One attribute as the game reports it.</summary>
    public struct AttributeReading
    {
        public PedAttribute Attribute;
        public int Rank;
        public int BaseRank;
        public int BonusRank;
        public int MaxRank;
        public int Points;
        public int MaxPoints;

        public override string ToString() =>
            $"{Attribute}: rank {Rank} (base {BaseRank} + bonus {BonusRank}, max {MaxRank}), points {Points}/{MaxPoints}";
    }

    /// <summary>A game event read from the AI event queue in the current frame.</summary>
    public struct GameEvent
    {
        public uint Hash;
        /// <summary>The event's data fields, when the adapter knows how many to read; otherwise empty.</summary>
        public long[] Data;
    }

    /// <summary>
    /// The only door to the game (Rules for the coding agent, rule 4). Every native call goes through an
    /// implementation of this interface, so a game update is fixed in one place and Core can be tested with a fake.
    /// Handles are the game's script handles; 0 means none.
    /// </summary>
    public interface IGameApi
    {
        // ---- Time and session ----
        /// <summary>GET_GAME_TIMER: milliseconds since the game started. Pauses with the game.</summary>
        int GameTimeMs { get; }
        bool IsLoadingScreenActive { get; }
        bool IsPlayerPlaying { get; }
        bool IsMissionActive { get; }
        bool IsScreenFadedOut { get; }
        bool IsPlayerControlOn { get; }

        // ---- Player and horses ----
        int PlayerPed { get; }
        int GetMount(int ped);
        int GetLastMount(int ped);
        /// <summary>The horse wearing the player's saddle.</summary>
        int GetSaddleHorse();
        int GetActiveHorse();
        int GetTempHorse();
        bool Exists(int entity);
        bool IsDead(int entity);
        uint GetModel(int entity);
        bool IsMale(int ped);
        /// <summary>eTamingState from _GET_HORSE_TAMING_STATE (0 invalid .. fleeing).</summary>
        int GetTamingState(int horse);
        float DistanceBetween(int a, int b);
        int GetClosestHumanPed(int aroundEntity, float radius, int excludePed);

        // ---- Attributes (stats and bonding) ----
        AttributeReading ReadAttribute(int ped, PedAttribute attribute);
        void SetAttributeBonusRank(int ped, PedAttribute attribute, int rank);
        int GetCoreValue(int ped, AttributeCore core);

        // ---- Behaviors (Phase 1 trigger candidates) ----
        void PlayVocalization(int ped, string vocalizationName);
        bool IsVocalizationPlaying(int ped);
        void LookAtEntity(int ped, int target, int durationMs);
        void ClearLookAt(int ped);
        /// <summary>TASK_HORSE_ACTION. Known ids: 1 rear, 2 throw rider, 3 quick stop, 5 rear, 8 skid, 9/10 throw passenger/rider.</summary>
        void HorseAction(int horse, int actionId);
        void HorseAgitate(int horse, bool kickOffRider);
        void DismountPlayer();
        void StartScenario(int ped, string scenarioName, int durationMs);
        void FollowEntity(int ped, int target, float offsetBehind, int timeoutMs);
        void FleeFrom(int ped, int threat, float distance, int timeMs);
        void Combat(int ped, int target);
        void ClearTasks(int ped);

        // ---- Events ----
        /// <summary>Events in the AI queue this frame (event group 0). Must be read every frame: the queue is cleared.</summary>
        IReadOnlyList<GameEvent> ReadAiEvents();

        // ---- Output ----
        void ShowSubtitle(string text);
        void DrawText(string text, float x, float y, float scale);
    }
}
