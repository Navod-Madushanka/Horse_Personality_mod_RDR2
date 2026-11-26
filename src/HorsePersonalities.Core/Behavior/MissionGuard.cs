namespace HorsePersonalities.Core.Behavior
{
    /// <summary>What the game shows about missions and cutscenes (Phase 1: each of these flips with a cutscene).</summary>
    public struct GuardSignals
    {
        public bool MissionFlag;
        public bool LetterBox;
        public bool AnimSceneCamera;
        public bool GameplayCameraRendering;
        public bool PlayerControlOn;
    }

    /// <summary>
    /// "Spooks, bucks, kicks and quirks are off during story missions and cutscenes" (Build plan: Hard rules).
    /// Any one signal holds behaviors, so a cutscene one signal misses is still covered by another.
    /// </summary>
    public static class MissionGuard
    {
        /// <summary>The reason behaviors are held, or null when they may run.</summary>
        public static string Holds(GuardSignals s, bool disableDuringMissions)
        {
            if (!disableDuringMissions) return null;
            if (s.MissionFlag) return "story mission";
            if (s.LetterBox) return "cutscene (letterbox)";
            if (s.AnimSceneCamera) return "cutscene (anim scene camera)";
            if (!s.GameplayCameraRendering) return "cutscene (gameplay camera off)";
            if (!s.PlayerControlOn) return "player control off";
            return null;
        }
    }
}
