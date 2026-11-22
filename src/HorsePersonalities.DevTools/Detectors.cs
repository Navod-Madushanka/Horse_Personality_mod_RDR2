using System;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Timing;

namespace HorsePersonalities.DevTools
{
    /// <summary>
    /// The Phase 1 detectors: pat, brush, feed, calm, saddle swap, mission active, plus wild-horse breaking and bond
    /// changes. Each reports what the game told it, so Phase 1 can confirm the signal exists before Phase 6 and 7 rely on it.
    /// </summary>
    public sealed class Detectors
    {
        private readonly Action<string> _report;
        private readonly IntervalGate _fast = new IntervalGate(250);
        private readonly IntervalGate _slow = new IntervalGate(1000);

        private int _saddleHorse = -1;
        private bool? _mission, _faded, _control;
        private int _watchedHorse;
        private int _tamingState = -1;
        private int _bondRank = -1, _bondPoints = -1;

        public Detectors(Action<string> report)
        {
            _report = report;
        }

        /// <summary>Call every frame: the event queue only holds this frame's events.</summary>
        public void Update(IGameApi game, int horse, int now)
        {
            ReadEvents(game);
            if (_fast.Due(now)) PollFast(game, horse);
            if (_slow.Due(now)) PollBond(game, horse);
        }

        private void ReadEvents(IGameApi game)
        {
            int player = game.PlayerPed;
            foreach (GameEvent e in game.ReadAiEvents())
            {
                if (!GameEvents.TryGet(e.Hash, out GameEventType type)) continue;
                long[] d = e.Data;
                if (type == GameEvents.PedAnimalInteraction && d.Length >= 3)
                {
                    if (d[0] != player) continue;
                    _report($"Interaction: {GameEvents.DescribeInteraction(unchecked((uint)d[2]))} on horse {d[1]} (pat/brush/feed detector)");
                }
                else if (type == GameEvents.CalmPed && d.Length >= 4)
                    _report($"Calm: by {d[0]} on mount {d[1]}, calm type {d[2]}, fully calmed {d[3] != 0}");
                else if (type == GameEvents.HorseBroken && d.Length >= 3)
                    _report($"Horse broken: rider {d[0]}, horse {d[1]}, type {d[2]}");
                else if (type == GameEvents.HorseStartedBreaking)
                    _report("Horse started breaking");
                else if (type == GameEvents.PlayerMountWildHorse && d.Length >= 1)
                    _report($"Mounted wild horse {d[0]}");
                else if (type == GameEvents.PlayerHorseAgitatedByAnimal && d.Length >= 2)
                    _report($"Your horse {d[0]} agitated by animal {d[1]}");
                else if (type.DataSize > 0 && d.Length == 0)
                    _report($"{type.Name}: GET_EVENT_DATA returned nothing (data size {type.DataSize} may be wrong)");
            }
        }

        private void PollFast(IGameApi game, int horse)
        {
            int saddle = game.GetSaddleHorse();
            if (saddle != _saddleHorse)
            {
                if (_saddleHorse != -1) _report($"Saddle horse changed: {_saddleHorse} -> {saddle} (saddle swap detector)");
                _saddleHorse = saddle;
            }

            Flag(ref _mission, game.IsMissionActive, "Mission flag (GET_MISSION_FLAG)");
            Flag(ref _faded, game.IsScreenFadedOut, "Screen faded out");
            Flag(ref _control, game.IsPlayerControlOn, "Player control on");

            if (horse != _watchedHorse)
            {
                _watchedHorse = horse;
                _tamingState = -1;
                _bondRank = _bondPoints = -1;
            }
            if (horse != 0)
            {
                int taming = game.GetTamingState(horse);
                if (taming != _tamingState)
                {
                    if (_tamingState != -1) _report($"Taming state: {TamingName(_tamingState)} -> {TamingName(taming)}");
                    _tamingState = taming;
                }
            }
        }

        private void PollBond(IGameApi game, int horse)
        {
            if (horse == 0) return;
            AttributeReading b = game.ReadAttribute(horse, PedAttribute.Bonding);
            if (b.Rank != _bondRank || b.Points != _bondPoints)
            {
                if (_bondRank != -1) _report($"Bond: rank {_bondRank} -> {b.Rank}, XP {_bondPoints} -> {b.Points}/{b.MaxPoints}");
                _bondRank = b.Rank;
                _bondPoints = b.Points;
            }
        }

        private void Flag(ref bool? last, bool now, string label)
        {
            if (last.HasValue && last.Value != now) _report($"{label}: {now}");
            last = now;
        }

        /// <summary>eTamingState from the native DB (_GET_HORSE_TAMING_STATE).</summary>
        public static string TamingName(int state)
        {
            switch (state)
            {
                case 0: return "INVALID";
                case 1: return "INACTIVE";
                case 2: return "TARGET_DETECTED";
                case 3: return "CALLED_OUT";
                case 4: return "MOUNTABLE";
                case 5: return "BEING_PATTED";
                case 6: return "BREAKING_ACTIVE";
                case 7: return "SPOOKED";
                case 8: return "RETREATING";
                case 9: return "FLEEING";
                default: return state.ToString();
            }
        }
    }
}
