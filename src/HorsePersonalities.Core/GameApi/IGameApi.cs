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
        /// <summary>SA_DIRTINESS: how dirty the horse is (Phase 7 Fussy demeanor; scale unverified).</summary>
        Dirtiness = 16,
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
        /// <summary>
        /// DLC::GET_IS_LOADING_SCREEN_ACTIVE. Do not use as a gate: in game (hook 2.2.0) it reads true for the whole
        /// session when read as a bool, so anything waiting on it never runs (found in Phase 0). Use IsPlayerPlaying.
        /// </summary>
        bool IsLoadingScreenActive { get; }
        bool IsPlayerPlaying { get; }
        bool IsMissionActive { get; }
        bool IsScreenFadedOut { get; }
        bool IsPlayerControlOn { get; }
        // Cutscene candidates (Phase 1 checks which of these flip in a cutscene or mission). Hashes taken from the hook's own wrappers.
        bool HasLetterBox { get; }
        bool IsGameplayCamRendering { get; }
        bool IsCinematicCamRendering { get; }
        bool IsAnimSceneCamActive { get; }

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
        /// <summary>GET_NUM_META_PED_OUTFITS: how many outfits (coats) the horse's model has. Phase 2 checks if it helps tell coats apart.</summary>
        int GetOutfitCount(int ped);
        /// <summary>_GET_META_PED_TYPE. Phase 2 readout only.</summary>
        int GetMetaPedType(int ped);
        int GetClosestHumanPed(int aroundEntity, float radius, int excludePed);

        // ---- Phase 6: what the horse is doing and what is around it ----
        /// <summary>GET_ENTITY_SPEED, m/s.</summary>
        float GetSpeed(int entity);
        /// <summary>GET_CLOCK_HOURS: the game's hour, 0-23.</summary>
        int GameHour { get; }
        /// <summary>IS_CONTROL_PRESSED(0, INPUT_HORSE_SPRINT 0x5AA007D7): the player is spurring.</summary>
        bool IsSprintPressed { get; }
        bool IsPedShooting(int ped);
        Vec3 GetPosition(int entity);
        /// <summary>GET_ENTITY_FORWARD_VECTOR: the way the entity faces.</summary>
        Vec3 GetForward(int entity);
        /// <summary>Peds within <paramref name="radius"/> of an entity, from the hook's ped pool. May come back empty if the pool read fails.</summary>
        IReadOnlyList<int> PedsNear(int around, float radius);
        /// <summary>PED::IS_PED_HUMAN.</summary>
        bool IsHuman(int ped);

        // ---- Phase 7b: coat signatures (all need a DevTools check, triggers 43-54) ----
        /// <summary>_GET_MAP_ZONE_AT_COORDS(x, y, z, 1 = TOWN) is not 0: the entity stands in a town.</summary>
        bool IsInTown(int entity);
        /// <summary>IS_PLAYER_FREE_AIMING.</summary>
        bool IsPlayerAiming { get; }
        /// <summary>IS_PED_IN_COMBAT(ped, target).</summary>
        bool IsPedInCombatWith(int ped, int target);
        /// <summary>SET_PED_MAX_MOVE_BLEND_RATIO (1 walk, 2 trot, 3 run). Lasts one frame: call it every frame.</summary>
        void SetMaxMoveBlendRatio(int ped, float ratio);
        /// <summary>_UNHITCH_HORSE.</summary>
        void Unhitch(int horse);
        /// <summary>GET_ENTITY_VELOCITY(entity, 0).</summary>
        Vec3 GetVelocity(int entity);
        /// <summary>IS_PED_HOGTIED (Bounty stomper, trigger 58).</summary>
        bool IsHogtied(int ped);
        /// <summary>GET_PED_STEALTH_MOVEMENT on the player: sneaking (Silent stealth, trigger 59).</summary>
        bool IsPlayerSneaking { get; }
        /// <summary>IS_CONTROL_PRESSED for INPUT_WHISTLE or INPUT_WHISTLE_HORSEBACK (Selective hearing, trigger 56).</summary>
        bool IsWhistlePressed { get; }
        /// <summary>IS_ENTITY_IN_WATER (Water sigh, trigger 61).</summary>
        bool IsInWater(int entity);

        // ---- Phase 8b: people admiring the horse (triggers 66-68) ----
        /// <summary>DOES_CONTEXT_EXIST_FOR_THIS_PED: the person's voice has this speech line (e.g. NICE_HORSE).</summary>
        bool HasSpeechLine(int ped, string line);
        /// <summary>IS_ANY_SPEECH_PLAYING.</summary>
        bool IsSpeaking(int ped);
        /// <summary>
        /// PLAY_PED_AMBIENT_SPEECH_NATIVE with the speech-params struct: the line in the person's own voice (no voice name),
        /// a random variation, the given speech params, addressed to <paramref name="listener"/>. Returns what the game returns.
        /// </summary>
        bool Say(int ped, string line, string speechParams, int listener);

        // ---- Attributes (stats and bonding) ----
        AttributeReading ReadAttribute(int ped, PedAttribute attribute);
        void SetAttributeBonusRank(int ped, PedAttribute attribute, int rank);
        /// <summary>SET_ATTRIBUTE_BASE_RANK. Phase 5 changes only the base rank; the bonus is the game's (tack).</summary>
        void SetAttributeBaseRank(int ped, PedAttribute attribute, int rank);
        /// <summary>SET_ATTRIBUTE_POINTS. Phase 6 uses it on bonding points to speed up or slow down bonding; unverified in game.</summary>
        void SetAttributePoints(int ped, PedAttribute attribute, int points);
        /// <summary>ADD_ATTRIBUTE_POINTS: adds to the attribute's XP. Trigger 64 checks whether the game keeps bond XP added this way (SET doesn't last).</summary>
        void AddAttributePoints(int ped, PedAttribute attribute, int points);
        /// <summary>_SET_MOUNT_BONDING_LEVEL (1-4). DevTools only, for testing the bond reveal; the game may save it.</summary>
        void SetBondLevel(int horse, int level);
        /// <summary>
        /// GET_DEFAULT_ATTRIBUTE_POINTS_NEEDED_FOR_RANK for bonding on this horse's model: the bond XP a level starts at.
        /// _SET_MOUNT_BONDING_LEVEL alone did not change the bond rank in game (Nov 27, 2026); setting the points does what a pat does.
        /// </summary>
        int BondPointsForRank(int horse, int rank);
        int GetCoreValue(int ped, AttributeCore core);

        // ---- Behaviors (Phase 1 trigger candidates) ----
        void PlayVocalization(int ped, string vocalizationName);
        bool IsVocalizationPlaying(int ped);
        void LookAtEntity(int ped, int target, int durationMs);
        void ClearLookAt(int ped);
        /// <summary>TASK_LOOK_AT_COORD (Night stare, trigger 62). Arguments past the coordinates are guesses.</summary>
        void LookAtPoint(int ped, Vec3 point, int durationMs);
        /// <summary>TASK_HORSE_ACTION. Known ids: 1 rear, 2 throw rider, 3 quick stop, 5 rear, 8 skid, 9/10 throw passenger/rider.</summary>
        void HorseAction(int horse, int actionId);
        void HorseAgitate(int horse, bool kickOffRider);
        void DismountPlayer();
        /// <param name="conditionalAnim">The scenario's conditional animation (femga: scenario_types_with_conditional_anims). Without one the game may ignore the scenario; Phase 1 grazing did nothing with none.</param>
        void StartScenario(int ped, string scenarioName, int durationMs, string conditionalAnim = null);
        /// <summary>TASK_WANDER_IN_AREA around the ped's own position. Candidate for the Wanderer quirk.</summary>
        void WanderNearby(int ped, float radius);
        /// <summary>TASK_WANDER_STANDARD. Candidate for the Wanderer quirk.</summary>
        void WanderFreely(int ped);
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
