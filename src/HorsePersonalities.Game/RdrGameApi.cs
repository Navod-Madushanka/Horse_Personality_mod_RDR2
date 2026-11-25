using System;
using System.Collections.Generic;
using System.Drawing;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Hashing;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;

namespace HorsePersonalities.Game
{
    /// <summary>
    /// The one place the mod talks to RDR2 (rule 4). Every call goes through a hash in <see cref="Natives"/>,
    /// not through the hook's typed wrappers, so a hook update that renames a wrapper cannot break the mod.
    /// After a game update, fix things here first.
    ///
    /// Arguments marked "unverified" are best guesses from the native DB and RedM scripts; Phase 1 checks them in game.
    /// </summary>
    public sealed class RdrGameApi : IGameApi
    {
        private const int AiEventGroup = 0; // SCRIPT_EVENT_QUEUE_AI
        private const int MaxEventData = 16;
        private readonly List<GameEvent> _events = new List<GameEvent>();

        // ---- Time and session ----

        /// <summary>
        /// Reads a native's yes/no result. In game every Function.Call&lt;bool&gt; read as true (loading screen, faded out, IS_PED_MALE on
        /// a female horse, IS_ENTITY_DEAD on a live horse), so the result is read as a number and only its lowest byte is tested.
        /// </summary>
        private static bool CallBool(ulong hash, params InputArgument[] args) => (Function.Call<int>(hash, args) & 0xFF) != 0;

        /// <summary>The raw 32-bit result of a native, for diagnostics.</summary>
        public int RawResult(ulong hash, params InputArgument[] args) => Function.Call<int>(hash, args);


        public int GameTimeMs => Function.Call<int>(Natives.GET_GAME_TIMER);
        public bool IsLoadingScreenActive => CallBool(Natives.GET_IS_LOADING_SCREEN_ACTIVE);
        public bool IsPlayerPlaying => CallBool(Natives.IS_PLAYER_PLAYING, PlayerId);
        /// <summary>Cross-check using the hook's own wrappers, for when a native hash is wrong. Used only by Phase 0 diagnostics.</summary>
        public string StartupDiagnostics()
        {
            string native, hook;
            try { native = $"native loading={IsLoadingScreenActive} (raw 0x{RawResult(Natives.GET_IS_LOADING_SCREEN_ACTIVE):X}) playing={IsPlayerPlaying} fadedOut={IsScreenFadedOut} (raw 0x{RawResult(Natives.IS_SCREEN_FADED_OUT):X})"; } catch (Exception ex) { native = "native threw " + ex.GetType().Name; }
            try { hook = $"hook loading={RDR2.Game.IsLoading} playing={RDR2.Game.Player.IsPlaying}"; } catch (Exception ex) { hook = "hook threw " + ex.GetType().Name; }
            return native + "; " + hook;
        }

        /// <summary>MISC::GET_MISSION_FLAG. Phase 1 checks it is true for the whole of a story mission.</summary>
        public bool IsMissionActive => CallBool(Natives.GET_MISSION_FLAG);
        public bool IsScreenFadedOut => CallBool(Natives.IS_SCREEN_FADED_OUT);
        public bool IsPlayerControlOn => CallBool(Natives.IS_PLAYER_CONTROL_ON, PlayerId);
        public bool HasLetterBox => CallBool(Natives.HAS_LETTER_BOX);
        public bool IsGameplayCamRendering => CallBool(Natives.IS_GAMEPLAY_CAM_RENDERING);
        public bool IsCinematicCamRendering => CallBool(Natives.IS_CINEMATIC_CAM_RENDERING);
        public bool IsAnimSceneCamActive => CallBool(Natives.IS_ANIM_SCENE_CAM_ACTIVE);

        // ---- Player and horses ----

        private static int PlayerId => Function.Call<int>(Natives.PLAYER_ID);
        public int PlayerPed => Function.Call<int>(Natives.PLAYER_PED_ID);
        public int GetMount(int ped) => Function.Call<int>(Natives.GET_MOUNT, ped);
        public int GetLastMount(int ped) => Function.Call<int>(Natives.GET_LAST_MOUNT, ped);
        public int GetSaddleHorse() => Function.Call<int>(Natives.GET_SADDLE_HORSE_FOR_PLAYER, PlayerId);
        public int GetActiveHorse() => Function.Call<int>(Natives.GET_ACTIVE_HORSE_FOR_PLAYER, PlayerId);
        public int GetTempHorse() => Function.Call<int>(Natives.GET_TEMP_PLAYER_HORSE, PlayerId);
        public bool Exists(int entity) => entity != 0 && CallBool(Natives.DOES_ENTITY_EXIST, entity);
        public bool IsDead(int entity) => CallBool(Natives.IS_ENTITY_DEAD, entity);
        public uint GetModel(int entity) => Function.Call<uint>(Natives.GET_ENTITY_MODEL, entity);
        public bool IsMale(int ped) => CallBool(Natives.IS_PED_MALE, ped);
        public int GetTamingState(int horse) => Function.Call<int>(Natives.GET_HORSE_TAMING_STATE, horse);

        public float DistanceBetween(int a, int b)
        {
            Vector3 pa = Coords(a), pb = Coords(b);
            float dx = pa.X - pb.X, dy = pa.Y - pb.Y, dz = pa.Z - pb.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public int GetOutfitCount(int ped) => Function.Call<int>(Natives.GET_NUM_META_PED_OUTFITS, ped);
        public int GetMetaPedType(int ped) => Function.Call<int>(Natives.GET_META_PED_TYPE, ped);

        private static Vector3 Coords(int entity) => Function.Call<Vector3>(Natives.GET_ENTITY_COORDS, entity, true, true);

        public unsafe int GetClosestHumanPed(int aroundEntity, float radius, int excludePed)
        {
            // PED::GET_CLOSEST_PED: p4/p5/p7-p9 unverified; pedType -1 = any.
            Vector3 p = Coords(aroundEntity);
            int found = 0;
            bool ok = CallBool(Natives.GET_CLOSEST_PED, p.X, p.Y, p.Z, radius, true, true, &found, false, false, false, -1);
            if (!ok || found == 0 || found == excludePed || found == aroundEntity) return 0;
            return CallBool(Natives.IS_PED_HUMAN, found) ? found : 0;
        }

        // ---- Attributes ----

        public AttributeReading ReadAttribute(int ped, PedAttribute attribute)
        {
            int a = (int)attribute;
            return new AttributeReading
            {
                Attribute = attribute,
                Rank = Function.Call<int>(Natives.GET_ATTRIBUTE_RANK, ped, a),
                BaseRank = Function.Call<int>(Natives.GET_ATTRIBUTE_BASE_RANK, ped, a),
                BonusRank = Function.Call<int>(Natives.GET_ATTRIBUTE_BONUS_RANK, ped, a),
                MaxRank = Function.Call<int>(Natives.GET_MAX_ATTRIBUTE_RANK, ped, a),
                Points = Function.Call<int>(Natives.GET_ATTRIBUTE_POINTS, ped, a),
                MaxPoints = Function.Call<int>(Natives.GET_MAX_ATTRIBUTE_POINTS, ped, a),
            };
        }

        public void SetAttributeBonusRank(int ped, PedAttribute attribute, int rank) =>
            Function.Call(Natives.SET_ATTRIBUTE_BONUS_RANK, ped, (int)attribute, rank);

        public int GetCoreValue(int ped, AttributeCore core) => Function.Call<int>(Natives.GET_ATTRIBUTE_CORE_VALUE, ped, (int)core);

        // ---- Behaviors ----

        public void PlayVocalization(int ped, string vocalizationName) =>
            Function.Call(Natives.PLAY_ANIMAL_VOCALIZATION, ped, vocalizationName, true); // p2 unverified

        public bool IsVocalizationPlaying(int ped) => CallBool(Natives.IS_ANIMAL_VOCALIZATION_PLAYING, ped);

        public void LookAtEntity(int ped, int target, int durationMs) =>
            Function.Call(Natives.TASK_LOOK_AT_ENTITY, ped, target, durationMs, 2048, 3, 0); // flags/priority as in RedM scripts; unverified

        public void ClearLookAt(int ped) => Function.Call(Natives.TASK_CLEAR_LOOK_AT, ped);

        public void HorseAction(int horse, int actionId) =>
            Function.Call(Natives.TASK_HORSE_ACTION, horse, actionId, 0, 0); // p2, p3 are 0 in R* scripts

        public void HorseAgitate(int horse, bool kickOffRider) => Function.Call(Natives.HORSE_AGITATE, horse, kickOffRider);

        public void DismountPlayer() => Function.Call(Natives.TASK_DISMOUNT_ANIMAL, PlayerPed, 0, 0, 0, 0, 0);

        public void StartScenario(int ped, string scenarioName, int durationMs, string conditionalAnim = null) =>
            // heading -1 is meant to keep the current heading; unverified. conditionalAnim 0 lets the game pick one.
            Function.Call(Natives.TASK_START_SCENARIO_IN_PLACE_HASH, ped, Joaat.Hash(scenarioName), durationMs, true,
                conditionalAnim == null ? 0u : Joaat.Hash(conditionalAnim), -1.0f, false);

        public void WanderNearby(int ped, float radius)
        {
            Vector3 p = Coords(ped);
            // p5/p6 (minimum walk length, pause between walks) and p7 are unverified; values as in RedM scripts.
            Function.Call(Natives.TASK_WANDER_IN_AREA, ped, p.X, p.Y, p.Z, radius, 2.0f, 5.0f, 0);
        }

        public void WanderFreely(int ped) => Function.Call(Natives.TASK_WANDER_STANDARD, ped, 10.0f, 10);

        public void FollowEntity(int ped, int target, float offsetBehind, int timeoutMs) =>
            Function.Call(Natives.TASK_FOLLOW_TO_OFFSET_OF_ENTITY, ped, target, 0.0f, -offsetBehind, 0.0f,
                1.0f, timeoutMs, 1.5f, true, false, true, false, false, false); // walk speed; p9/p11-p13 unverified

        public void FleeFrom(int ped, int threat, float distance, int timeMs) =>
            Function.Call(Natives.TASK_SMART_FLEE_PED, ped, threat, distance, timeMs, 0, 3.0f, 0);

        public void Combat(int ped, int target) => Function.Call(Natives.TASK_COMBAT_PED, ped, target, 0, 16);

        public void ClearTasks(int ped) => Function.Call(Natives.CLEAR_PED_TASKS, ped, false, false);

        // ---- Events ----

        public unsafe IReadOnlyList<GameEvent> ReadAiEvents()
        {
            _events.Clear();
            int count = Function.Call<int>(Natives.GET_NUMBER_OF_EVENTS, AiEventGroup);
            ulong* buffer = stackalloc ulong[MaxEventData];
            for (int i = 0; i < count; i++)
            {
                uint hash = Function.Call<uint>(Natives.GET_EVENT_AT_INDEX, AiEventGroup, i);
                long[] data = Array.Empty<long>();
                if (GameEvents.TryGet(hash, out GameEventType type) && type.DataSize > 0 && type.DataSize <= MaxEventData)
                {
                    for (int k = 0; k < type.DataSize; k++) buffer[k] = 0;
                    if (CallBool(Natives.GET_EVENT_DATA, AiEventGroup, i, buffer, type.DataSize))
                    {
                        data = new long[type.DataSize];
                        // Each field sits in an 8-byte slot; handles and hashes use the low 32 bits.
                        for (int k = 0; k < type.DataSize; k++) data[k] = unchecked((int)(uint)buffer[k]);
                    }
                }
                _events.Add(new GameEvent { Hash = hash, Data = data });
            }
            return _events;
        }

        // ---- Output ----

        public void ShowSubtitle(string text) => RDR2.UI.Screen.PrintSubtitle(text);

        /// <summary>x and y are in the hook's 1280x720 screen space.</summary>
        public void DrawText(string text, float x, float y, float scale) =>
            new TextElement(text, new PointF(x, y), scale, Color.White, Alignment.Left, true, true).Draw();
    }
}
