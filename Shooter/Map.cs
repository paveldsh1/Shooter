namespace Shooter
{
    internal class Map
    {
        private float[,] depthBuffer;
        private const float depth = 16.0f;
        private const float fov = 3.14159f / 4.0f;
        private MiniMap miniMap;
        private Player player;
        public Map()
        {

        }

        public Map(MiniMap miniMap, Player player, Window window)
        {
            depthBuffer = new float[window.ScreenWidth, window.ScreenHeight];
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
                    this.depthBuffer[x, y] = float.MaxValue;
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
                float rayAngle = (player.PLAYERA - fov / 2.0f) + (x / (float)window.ScreenWidth) * fov;

                float stepSize = 0.1f;
                float distanceToWall = 0.0f;

                bool hitWall = false;
                bool boundary = false;

                float eyeX = (float)Math.Cos(rayAngle);
                float eyeY = (float)Math.Sin(rayAngle);

                while (!hitWall && distanceToWall < depth)
                {
                    distanceToWall += stepSize;
                    int testX = (int)(player.PLAYERX + eyeX * distanceToWall);
                    int testY = (int)(player.PLAYERY + eyeY * distanceToWall);
                    if (testY < 0 || testY >= miniMap.Map.Length || testX < 0 || testX >= miniMap.Map[testY].Length)
                    {
                        hitWall = true;
                        distanceToWall = depth;
                    }
                    else
                    {
                        if (miniMap.Map[testY][testX] == '#')
                        {
                            hitWall = true;
                            List<(float, float)> p = new();
                            for (int tx = 0; tx < 2; tx++)
                            {
                                for (int ty = 0; ty < 2; ty++)
                                {
                                    float vy = (float)testY + ty - player.PLAYERY;
                                    float vx = (float)testX + tx - player.PLAYERX;
                                    float d = (float)Math.Sqrt(vx * vx + vy * vy);
                                    float dot = (eyeX * vx / d) + (eyeY * vy / d);
                                    p.Add((d, dot));
                                }
                            }
                            p.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                            float fBound = 0.005f;
                            if (Math.Acos(p[0].Item2) < fBound) boundary = true;
                            if (Math.Acos(p[1].Item2) < fBound) boundary = true;
                            if (Math.Acos(p[2].Item2) < fBound) boundary = true;
                        }
                    }
                }
                int ceiling = (int)((float)(window.ScreenHeight / 2.0f) - window.ScreenHeight / ((float)distanceToWall));
                int floor = window.ScreenHeight - ceiling;

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
                            distanceToWall < depth / 3.00f ? '█' :
                            distanceToWall < depth / 1.75f ? '■' :
                            distanceToWall < depth / 1.00f ? '▪' :
                            ' ';
                    }
                    else
                    {
                        float b = 1.0f - ((y - window.ScreenHeight / 2.0f) / (window.ScreenHeight / 2.0f));
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
}
