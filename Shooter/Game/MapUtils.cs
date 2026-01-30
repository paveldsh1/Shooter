namespace Shooter.Game
{
    internal static class MapUtils
    {
        public static bool IsWallCell(char cell) => cell is '#' or '█';
    }
}
