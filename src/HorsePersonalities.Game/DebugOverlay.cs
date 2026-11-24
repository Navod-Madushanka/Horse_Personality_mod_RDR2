using System.Collections.Generic;
using System.Globalization;
using HorsePersonalities.Core.GameApi;

namespace HorsePersonalities.Game
{
    /// <summary>
    /// The debug overlay (Build plan: Debug overlay): a small text panel on the right of the screen (the DevTools
    /// menu uses the left), toggled with
    /// DebugKey and off by default. Draws lines Core already built, so the per-frame cost is only the drawing.
    /// </summary>
    public sealed class DebugOverlay
    {
        private IReadOnlyList<string> _lines = new[] { "HORSE PERSONALITIES - debug overlay", "Mount a horse to see its values." };

        public bool Visible { get; set; }

        public void SetLines(IReadOnlyList<string> lines) => _lines = lines;

        /// <param name="tickMs">The last frame's measured cost, shown live (Build plan: Tick loop rules).</param>
        /// <summary>Left edge in the hook's 1280x720 screen space; lines stay under about 90 characters to fit.</summary>
        private const float X = 760;

        public void Draw(IGameApi game, double tickMs)
        {
            if (!Visible) return;
            float y = 20;
            for (int i = 0; i < _lines.Count; i++)
            {
                game.DrawText(_lines[i], X, y, i == 0 ? 0.35f : 0.3f);
                y += i == 0 ? 22 : 18;
            }
            game.DrawText("Tick " + tickMs.ToString("0.000", CultureInfo.InvariantCulture) + " ms (budget 1 ms)", X, y, 0.3f);
        }
    }
}
