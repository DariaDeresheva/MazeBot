using System.Reflection;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.UnfairBot
{
    public class UnfairBot : IPlayerController
    {
        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            ((Level)typeof(LevelView).GetField("level", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(levelView))?.Complete();

            return Turn.None;
        }
    }
}