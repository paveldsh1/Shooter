using Shooter.Models;

namespace Shooter.Game
{
    internal sealed class Map
    {
        private readonly float[,] depthBuffer;
        private readonly MiniMap miniMap;
        private readonly Player player;
        public float[] ColumnDepths { get; }

        public Map(MiniMap miniMap, Player player, Window window)
        {
            depthBuffer = new float[window.ScreenWidth, window.ScreenHeight];
            ColumnDepths = new float[window.ScreenWidth];
            this.miniMap = miniMap;
            this.player = player;
            InitDepthBuffer(window);
            RaycastAndRenderWalls(window);
        }

        private void InitDepthBuffer(Window window)
        {
            for (int y = 0; y < window.ScreenHeight; y++)
            {
                for (int x = 0; x < window.ScreenWidth; x++)
                {
                    depthBuffer[x, y] = float.MaxValue;
                }
            }
        }

        public void Update(Window window)
        {
            RaycastAndRenderWalls(window);
        }

        private void RaycastAndRenderWalls(Window window)
        {
            for (int x = 0; x < window.ScreenWidth; x++)
            {
                float rayAngle = player.PlayerA - GameConstants.FieldOfView / 2.0f
                                 + x / (float)window.ScreenWidth * GameConstants.FieldOfView;

                var hit = CastRay(rayAngle);
                RenderColumn(window, x, hit.Distance, hit.IsBoundary);
            }
        }

        private (float Distance, bool IsBoundary) CastRay(float rayAngle)
        {
            const float stepSize = 0.1f;
            float distanceToWall = 0.0f;

            float eyeX = (float)Math.Cos(rayAngle);
            float eyeY = (float)Math.Sin(rayAngle);

            while (distanceToWall < GameConstants.MaxDepth)
            {
                distanceToWall += stepSize;
                int testX = (int)(player.PlayerX + eyeX * distanceToWall);
                int testY = (int)(player.PlayerY + eyeY * distanceToWall);

                if (IsOutOfBounds(testX, testY))
                {
                    return (GameConstants.MaxDepth, false);
                }

                if (IsWallCell(testX, testY))
                {
                    bool boundary = IsBoundaryEdge(eyeX, eyeY, testX, testY);
                    return (distanceToWall, boundary);
                }
            }

            return (GameConstants.MaxDepth, false);
        }

        private bool IsOutOfBounds(int x, int y)
        {
            return y < 0 || y >= miniMap.Map.Length || x < 0 || x >= miniMap.Map[y].Length;
        }

        private bool IsWallCell(int x, int y) => MapUtils.IsWallCell(miniMap.Map[y][x]);

        private bool IsBoundaryEdge(float eyeX, float eyeY, int testX, int testY)
        {
            List<(float Dist, float Dot)> p = new();
            for (int tx = 0; tx < 2; tx++)
            {
                for (int ty = 0; ty < 2; ty++)
                {
                    float vy = (float)testY + ty - player.PlayerY;
                    float vx = (float)testX + tx - player.PlayerX;
                    float d = (float)Math.Sqrt(vx * vx + vy * vy);
                    float dot = eyeX * vx / d + eyeY * vy / d;
                    p.Add((d, dot));
                }
            }
            p.Sort((a, b) => a.Dist.CompareTo(b.Dist));
            float fBound = 0.005f;
            return Math.Acos(p[0].Dot) < fBound ||
                   Math.Acos(p[1].Dot) < fBound ||
                   Math.Acos(p[2].Dot) < fBound;
        }

        private void RenderColumn(Window window, int x, float distanceToWall, bool boundary)
        {
            int ceiling = (int)((window.ScreenHeight / 2.0f) - window.ScreenHeight / distanceToWall);
            int floor = window.ScreenHeight - ceiling;

            ColumnDepths[x] = distanceToWall;

            for (int y = 0; y < window.ScreenHeight; y++)
            {
                depthBuffer[x, y] = distanceToWall;

                if (y <= ceiling)
                {
                    window.Screen[x, y] = ' ';
                }
                else if (y > ceiling && y <= floor)
                {
                    window.Screen[x, y] =
                        boundary ? ' ' :
                        distanceToWall < GameConstants.MaxDepth / 3.00f ? '█' :
                        distanceToWall < GameConstants.MaxDepth / 1.75f ? '■' :
                        distanceToWall < GameConstants.MaxDepth / 1.00f ? '▪' :
                        ' ';
                }
                else
                {
                    float b = 1.0f - (y - window.ScreenHeight / 2.0f) / (window.ScreenHeight / 2.0f);
                    window.Screen[x, y] = b switch
                    {
                        < 0.20f => '●',
                        < 0.40f => '•',
                        < 0.60f => '·',
                        _ => ' ',
                    };
                }
            }
        }
    }
}
