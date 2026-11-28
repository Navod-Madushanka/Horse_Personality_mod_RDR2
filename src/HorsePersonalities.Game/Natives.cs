// Generated from the native list in ScriptHookRDR2DotNet V2 (RDR2.Native/NativeDecl.cs), which follows
// alloc8or's RDR3 native DB: https://alloc8or.github.io/rdr3-native-db/
// Rule 5: every native the mod calls is listed here with its namespace and signature. Look one up in the DB
// before adding it. Hashes are the game's and do not change between hook releases.
namespace HorsePersonalities.Game
{
    internal static class Natives
    {
        /// <summary>ENTITY::GET_ENTITY_SPEED(int entity) -> float</summary>
        public const ulong GET_ENTITY_SPEED = 0xFB6BA510A533DF81;
        /// <summary>CLOCK::GET_CLOCK_HOURS() -> int</summary>
        public const ulong GET_CLOCK_HOURS = 0xC82CF208C2B19199;
        /// <summary>PAD::IS_CONTROL_PRESSED(int control, uint action) -> bool</summary>
        public const ulong IS_CONTROL_PRESSED = 0xF3A21BCD95725A4A;
        /// <summary>INPUT_HORSE_SPRINT, from the hook's eInputType.HorseSprint.</summary>
        public const uint INPUT_HORSE_SPRINT = 0x5AA007D7;
        /// <summary>PED::IS_PED_SHOOTING(int ped) -> bool</summary>
        public const ulong IS_PED_SHOOTING = 0x34616828CD07F1A1;
        /// <summary>ENTITY::GET_ENTITY_FORWARD_VECTOR(int entity) -> Vector3</summary>
        public const ulong GET_ENTITY_FORWARD_VECTOR = 0x2412D9C05BB09B97;
        /// <summary>ATTRIBUTE::ADD_ATTRIBUTE_POINTS(int ped, int attributeIndex, int p2)</summary>
        public const ulong ADD_ATTRIBUTE_POINTS = 0x75415EE0CB583760;
        /// <summary>ATTRIBUTE::SET_ATTRIBUTE_POINTS(int ped, int attributeIndex, int p2)</summary>
        public const ulong SET_ATTRIBUTE_POINTS = 0x09A59688C26D88DF;
        /// <summary>ATTRIBUTE::SET_ATTRIBUTE_BASE_RANK(int ped, int attributeIndex, int newValue)</summary>
        public const ulong SET_ATTRIBUTE_BASE_RANK = 0x5DA12E025D47D4E5;
        /// <summary>PED::_SET_MOUNT_BONDING_LEVEL(int ped, int bondingLevel)</summary>
        public const ulong SET_MOUNT_BONDING_LEVEL = 0xA69899995997A63B;
        /// <summary>TASK::TASK_WANDER_IN_AREA(int ped, float x, float y, float z, float radius, float p5, float p6, int p7)</summary>
        public const ulong TASK_WANDER_IN_AREA = 0xE054346CA3A0F315;
        /// <summary>TASK::TASK_WANDER_STANDARD(int ped, float p1, int p2): the DB says 10.0 and 10 wander with no time limit.</summary>
        public const ulong TASK_WANDER_STANDARD = 0xBB9CE077274F6A1B;
        /// <summary>PED::GET_NUM_META_PED_OUTFITS(int ped) -> int</summary>
        public const ulong GET_NUM_META_PED_OUTFITS = 0x10C70A515BC03707;
        /// <summary>PED::_GET_META_PED_TYPE(int ped) -> int</summary>
        public const ulong GET_META_PED_TYPE = 0xEC9A1261BF0CE510;
        /// <summary>MISC::GET_GAME_TIMER() -> int</summary>
        public const ulong GET_GAME_TIMER = 0x4F67E8ECA7D3F667;
        /// <summary>DLC::GET_IS_LOADING_SCREEN_ACTIVE() -> bool</summary>
        public const ulong GET_IS_LOADING_SCREEN_ACTIVE = 0x71D4BF5890659B0C;
        /// <summary>PLAYER::IS_PLAYER_PLAYING(int player) -> bool</summary>
        public const ulong IS_PLAYER_PLAYING = 0xBFFB35986CAAE58C;
        /// <summary>MISC::GET_MISSION_FLAG() -> bool</summary>
        public const ulong GET_MISSION_FLAG = 0xB15CD1CF58771DE1;
        /// <summary>CAM::IS_SCREEN_FADED_OUT() -> bool</summary>
        public const ulong IS_SCREEN_FADED_OUT = 0xF5472C80DF2FF847;
        /// <summary>PLAYER::IS_PLAYER_CONTROL_ON(int player) -> bool</summary>
        public const ulong IS_PLAYER_CONTROL_ON = 0x7964097FCE4C244B;
        /// <summary>CAM::HAS_LETTER_BOX() -> bool (cinematic black bars; read from the hook's own wrapper)</summary>
        public const ulong HAS_LETTER_BOX = 0x81DCFD13CF39920E;
        /// <summary>CAM::IS_GAMEPLAY_CAM_RENDERING() -> bool</summary>
        public const ulong IS_GAMEPLAY_CAM_RENDERING = 0x8660EA714834E412;
        /// <summary>CAM::IS_CINEMATIC_CAM_RENDERING() -> bool</summary>
        public const ulong IS_CINEMATIC_CAM_RENDERING = 0xBF7C780731AADBF8;
        /// <summary>CAM::_IS_ANIM_SCENE_CAM_ACTIVE() -> bool</summary>
        public const ulong IS_ANIM_SCENE_CAM_ACTIVE = 0x20389408F0E93B9A;
        /// <summary>PLAYER::PLAYER_ID() -> int</summary>
        public const ulong PLAYER_ID = 0x217E9DC48139933D;
        /// <summary>PLAYER::PLAYER_PED_ID() -> int</summary>
        public const ulong PLAYER_PED_ID = 0x096275889B8E0EE0;
        /// <summary>PED::GET_MOUNT(int ped) -> int</summary>
        public const ulong GET_MOUNT = 0xE7E11B8DCBED1058;
        /// <summary>PED::_GET_LAST_MOUNT(int ped) -> int</summary>
        public const ulong GET_LAST_MOUNT = 0x4C8B59171957BCF7;
        /// <summary>PLAYER::_GET_SADDLE_HORSE_FOR_PLAYER(int player) -> int</summary>
        public const ulong GET_SADDLE_HORSE_FOR_PLAYER = 0xB48050D326E9A2F3;
        /// <summary>PLAYER::_GET_ACTIVE_HORSE_FOR_PLAYER(int player) -> int</summary>
        public const ulong GET_ACTIVE_HORSE_FOR_PLAYER = 0x46FA0AE18F4C7FA9;
        /// <summary>PLAYER::_GET_TEMP_PLAYER_HORSE(int player) -> int</summary>
        public const ulong GET_TEMP_PLAYER_HORSE = 0xD3F7445CEA2E5035;
        /// <summary>ENTITY::DOES_ENTITY_EXIST(int entity) -> bool</summary>
        public const ulong DOES_ENTITY_EXIST = 0xD42BD6EB2E0F1677;
        /// <summary>ENTITY::IS_ENTITY_DEAD(int entity) -> bool</summary>
        public const ulong IS_ENTITY_DEAD = 0x7D5B1F88E7504BBA;
        /// <summary>ENTITY::GET_ENTITY_MODEL(int entity) -> uint</summary>
        public const ulong GET_ENTITY_MODEL = 0xDA76A9F39210D365;
        /// <summary>PED::IS_PED_MALE(int ped) -> bool</summary>
        public const ulong IS_PED_MALE = 0x6D9F5FAA7488BA46;
        /// <summary>PED::_GET_HORSE_TAMING_STATE(int horse) -> int</summary>
        public const ulong GET_HORSE_TAMING_STATE = 0x454AD4DA6C41B5BD;
        /// <summary>ENTITY::GET_ENTITY_COORDS(int entity, bool alive, bool realCoords) -> Vector3</summary>
        public const ulong GET_ENTITY_COORDS = 0xA86D5F069399F44D;
        /// <summary>PED::GET_CLOSEST_PED(float x, float y, float z, float radius, bool p4, bool p5, int* outPed, bool p7, bool p8, bool p9, int pedType) -> bool</summary>
        public const ulong GET_CLOSEST_PED = 0xC33AB876A77F8164;
        /// <summary>PED::IS_PED_HUMAN(int ped) -> bool</summary>
        public const ulong IS_PED_HUMAN = 0xB980061DA992779D;
        /// <summary>ATTRIBUTE::GET_ATTRIBUTE_RANK(int ped, int attributeIndex) -> int</summary>
        public const ulong GET_ATTRIBUTE_RANK = 0xA4C8E23E29040DE0;
        /// <summary>ATTRIBUTE::GET_ATTRIBUTE_BASE_RANK(int ped, int attributeIndex) -> int</summary>
        public const ulong GET_ATTRIBUTE_BASE_RANK = 0x147149F2E909323C;
        /// <summary>ATTRIBUTE::GET_ATTRIBUTE_BONUS_RANK(int ped, int coreIndex) -> int</summary>
        public const ulong GET_ATTRIBUTE_BONUS_RANK = 0x0EFA71F4B4330E04;
        /// <summary>ATTRIBUTE::GET_MAX_ATTRIBUTE_RANK(int ped, int attributeIndex) -> int</summary>
        public const ulong GET_MAX_ATTRIBUTE_RANK = 0x704674A0535A471D;
        /// <summary>ATTRIBUTE::GET_ATTRIBUTE_POINTS(int ped, int attributeIndex) -> int</summary>
        public const ulong GET_ATTRIBUTE_POINTS = 0x219DA04BAA9CB065;
        /// <summary>ATTRIBUTE::GET_MAX_ATTRIBUTE_POINTS(int ped, int attributeIndex) -> int</summary>
        public const ulong GET_MAX_ATTRIBUTE_POINTS = 0x223BF310F854871C;
        /// <summary>ATTRIBUTE::GET_DEFAULT_ATTRIBUTE_POINTS_NEEDED_FOR_RANK(Hash modelHash, int attributeIndex, int rank) -> int (hash read from the hook's own wrapper)</summary>
        public const ulong GET_DEFAULT_ATTRIBUTE_POINTS_NEEDED_FOR_RANK = 0x94A7F191DB49A44D;
        /// <summary>ATTRIBUTE::SET_ATTRIBUTE_BONUS_RANK(int ped, int attributeIndex, int newValue) -> void</summary>
        public const ulong SET_ATTRIBUTE_BONUS_RANK = 0x920F9488BD115EFB;
        /// <summary>ATTRIBUTE::_GET_ATTRIBUTE_CORE_VALUE(int ped, int coreIndex) -> int</summary>
        public const ulong GET_ATTRIBUTE_CORE_VALUE = 0x36731AC041289BB1;
        /// <summary>AUDIO::PLAY_ANIMAL_VOCALIZATION(int ped, string vocalizationName, bool p2) -> void</summary>
        public const ulong PLAY_ANIMAL_VOCALIZATION = 0xEE066C7006C49C0A;
        /// <summary>AUDIO::IS_ANIMAL_VOCALIZATION_PLAYING(int pedHandle) -> bool</summary>
        public const ulong IS_ANIMAL_VOCALIZATION_PLAYING = 0xC265DF9FB44A9FBD;
        /// <summary>TASK::TASK_LOOK_AT_ENTITY(int ped, int lookAtTarget, int duration, int p3, int p4, int p5) -> void</summary>
        public const ulong TASK_LOOK_AT_ENTITY = 0x69F4BE8C8CC4796C;
        /// <summary>TASK::TASK_LOOK_AT_COORD(int ped, float x, float y, float z, int duration, int flags, int priority, int p7) -> void</summary>
        public const ulong TASK_LOOK_AT_COORD = 0x6FA46612594F7973;
        /// <summary>TASK::TASK_CLEAR_LOOK_AT(int ped) -> void</summary>
        public const ulong TASK_CLEAR_LOOK_AT = 0x0F804F1DB19B9689;
        /// <summary>TASK::TASK_HORSE_ACTION(int ped, int action, int targetPed, ulong p3) -> void</summary>
        public const ulong TASK_HORSE_ACTION = 0xA09CFD29100F06C3;
        /// <summary>PED::_HORSE_AGITATE(int mount, bool kickOffRider) -> void</summary>
        public const ulong HORSE_AGITATE = 0xBAE08F00021BFFB2;
        /// <summary>TASK::TASK_DISMOUNT_ANIMAL(int rider, int taskFlag, ulong p2, ulong p3, ulong p4, int targetPed) -> void</summary>
        public const ulong TASK_DISMOUNT_ANIMAL = 0x48E92D3DDE23C23A;
        /// <summary>TASK::TASK_START_SCENARIO_IN_PLACE_HASH(int ped, uint scenarioHash, int duration, bool playEnterAnim, uint conditionalHash, float heading, bool p6) -> void</summary>
        public const ulong TASK_START_SCENARIO_IN_PLACE_HASH = 0x524B54361229154F;
        /// <summary>TASK::TASK_FOLLOW_TO_OFFSET_OF_ENTITY(int ped, int entity, float offsetX, float offsetY, float offsetZ, float movementSpeed, int timeout, float stoppingRange, bool persistFollowing, bool p9, bool walkOnly, bool p11, bool p12, bool p13) -> void</summary>
        public const ulong TASK_FOLLOW_TO_OFFSET_OF_ENTITY = 0x304AE42E357B8C7E;
        /// <summary>TASK::TASK_SMART_FLEE_PED(int ped, int fleeFromTarget, float fleeDistance, int fleeTime, int fleeType, float fleeSpeed, int targetPed) -> void</summary>
        public const ulong TASK_SMART_FLEE_PED = 0x22B0D0E37CCB840D;
        /// <summary>TASK::TASK_COMBAT_PED(int ped, int targetPed, int p2, int p3) -> void</summary>
        public const ulong TASK_COMBAT_PED = 0xF166E48407BAC484;
        /// <summary>TASK::CLEAR_PED_TASKS(int ped, bool p1, bool p2) -> void</summary>
        public const ulong CLEAR_PED_TASKS = 0xE1EF3C1216AFF2CD;
        /// <summary>ZONE::_GET_MAP_ZONE_AT_COORDS(float x, float y, float z, int type) -> uint. Type 1 = TOWN.</summary>
        public const ulong GET_MAP_ZONE_AT_COORDS = 0x43AD8FC02B429D33;
        /// <summary>PLAYER::IS_PLAYER_FREE_AIMING(int player) -> bool</summary>
        public const ulong IS_PLAYER_FREE_AIMING = 0x936F967D4BE1CE9D;
        /// <summary>PED::IS_PED_IN_COMBAT(int ped, int target) -> bool</summary>
        public const ulong IS_PED_IN_COMBAT = 0x4859F1FC66A6278E;
        /// <summary>TASK::SET_PED_MAX_MOVE_BLEND_RATIO(int ped, float value) -> void</summary>
        public const ulong SET_PED_MAX_MOVE_BLEND_RATIO = 0x433083750C5E064A;
        /// <summary>TASK::_UNHITCH_HORSE(int horse) -> void</summary>
        public const ulong UNHITCH_HORSE = 0x0348469DAA17576C;
        /// <summary>ENTITY::GET_ENTITY_VELOCITY(int entity, int p1) -> Vector3</summary>
        public const ulong GET_ENTITY_VELOCITY = 0x4805D2B1D8CF94A9;
        /// <summary>PED::IS_PED_HOGTIED(int ped) -> bool</summary>
        public const ulong IS_PED_HOGTIED = 0x3AA24CCC0D451379;
        /// <summary>PED::GET_PED_STEALTH_MOVEMENT(int ped) -> bool</summary>
        public const ulong GET_PED_STEALTH_MOVEMENT = 0x7C2AC9CA66575FBF;
        /// <summary>INPUT_WHISTLE and INPUT_WHISTLE_HORSEBACK, from the hook's eInputType.</summary>
        public const uint INPUT_WHISTLE = 0x24978A28, INPUT_WHISTLE_HORSEBACK = 0xE7EB9185;
        /// <summary>ENTITY::IS_ENTITY_IN_WATER(int entity) -> bool</summary>
        public const ulong IS_ENTITY_IN_WATER = 0xDDE5C125AC446723;
        /// <summary>SCRIPTS::GET_NUMBER_OF_EVENTS(int eventGroup) -> int</summary>
        public const ulong GET_NUMBER_OF_EVENTS = 0x5CE8DE5909565748;
        /// <summary>SCRIPTS::GET_EVENT_AT_INDEX(int eventGroup, int eventIndex) -> uint</summary>
        public const ulong GET_EVENT_AT_INDEX = 0xA85E614430EFF816;
        /// <summary>SCRIPTS::GET_EVENT_DATA(int eventGroup, int eventIndex, ulong* eventData, int eventDataSize) -> bool</summary>
        public const ulong GET_EVENT_DATA = 0x57EC5FA4D4D6AFCA;
    }
}
