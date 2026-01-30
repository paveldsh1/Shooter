namespace Shooter.Models
{
    internal readonly record struct PlayerSnapshot(
        string Nickname,
        float X,
        float Y,
        float A,
        DateTime UpdatedAt,
        bool IsAlive);
}
