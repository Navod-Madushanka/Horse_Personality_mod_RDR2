using HorsePersonalities.Core.GameApi;

namespace HorsePersonalities.Core.Tracking
{
    public enum MountChangeKind { None, Mounted, Dismounted, Switched }

    public struct MountChange
    {
        public MountChangeKind Kind;
        public int Previous;
        public int Current;
    }

    /// <summary>
    /// Watches which horse the player is riding. Cheap enough for every frame: one GET_MOUNT call.
    /// Reports a change once, on the frame it happens.
    /// </summary>
    public sealed class MountWatcher
    {
        public int CurrentMount { get; private set; }

        public MountChange Update(IGameApi game)
        {
            int ped = game.PlayerPed;
            int mount = ped != 0 ? game.GetMount(ped) : 0;
            if (mount != 0 && !game.Exists(mount)) mount = 0;
            if (mount == CurrentMount) return default;

            var change = new MountChange { Previous = CurrentMount, Current = mount };
            if (CurrentMount == 0) change.Kind = MountChangeKind.Mounted;
            else if (mount == 0) change.Kind = MountChangeKind.Dismounted;
            else change.Kind = MountChangeKind.Switched;
            CurrentMount = mount;
            return change;
        }

        public void Reset() => CurrentMount = 0;
    }
}
