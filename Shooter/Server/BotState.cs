using Shooter.Models;

namespace Shooter.Server
{
    internal sealed class BotState
    {
        public BotState(string nickname, float x, float y, float a)
        {
            Nickname = nickname;
            X = x;
            Y = y;
            A = a;
            TargetA = a;
            NextDecisionIn = 0f;
            TargetNickname = null;
        }

        public string Nickname { get; }
        public float X { get; set; }
        public float Y { get; set; }
        public float A { get; set; }
        public float TargetA { get; set; }
        public float NextDecisionIn { get; set; }
        public string? TargetNickname { get; set; }

        public PlayerSnapshot ToSnapshot()
            => new PlayerSnapshot(Nickname, X, Y, A, DateTime.UtcNow, true);
    }
}
