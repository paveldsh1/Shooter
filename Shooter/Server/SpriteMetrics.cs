using Shooter.Game;
using Shooter.Game.Assets;

namespace Shooter.Server
{
    internal static class SpriteMetrics
    {
        public static float GetDistanceScale(int screenWidth, int screenHeight, float viewScale)
        {
            float baseArea = GameConstants.ScreenWidth * GameConstants.ScreenHeight;
            float currentArea = Math.Max(1, screenWidth) * Math.Max(1, screenHeight);
            float effectiveArea = currentArea * MathF.Max(0.1f, viewScale * viewScale);
            return MathF.Sqrt(baseArea / effectiveArea);
        }

        public static string[] SelectEnemySprite(float distance)
        {
            return
                distance <= 1f ? EnemySprites.EnemySprite8 :
                distance <= 2f ? EnemySprites.EnemySprite7 :
                distance <= 3f ? EnemySprites.EnemySprite6 :
                distance <= 4f ? EnemySprites.EnemySprite5 :
                distance <= 5f ? EnemySprites.EnemySprite4 :
                distance <= 6f ? EnemySprites.EnemySprite3 :
                distance <= 7f ? EnemySprites.EnemySprite2 :
                EnemySprites.EnemySprite1;
        }
    }
}
