namespace Shooter.Game
{
    internal class MiniMap
    {
        private const byte height = 10, width = 20;
        private const float MinWallChance = 0.10f;
        private const float MaxWallChance = 0.35f;
        private const float MinOpenRatio = 0.40f;

        public char[][] Map { get; private set; } = Array.Empty<char[]>();

        public MiniMap()
        {
            InitMap();
        }

        private void InitMap() // будет использоваться при обновлении состояния игры
        {
            int interiorCells = (height - 2) * (width - 2);
            int minOpenCells = (int)MathF.Ceiling(interiorCells * MinOpenRatio);

            const int maxAttempts = 10;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float wallChance = RandomWallChance();
                int openCells = BuildMapWithRandomWalls(wallChance);
                if (openCells < minOpenCells) continue;

                if (TryGetAnyOpenCell(out int startX, out int startY) &&
                    IsFullyConnected(startX, startY, openCells))
                {
                    return;
                }
            }

            BuildMapWithRandomWalls(0f);
        }

        private int BuildMapWithRandomWalls(float wallChance)
        {
            int interiorCells = (height - 2) * (width - 2);
            int openCells = interiorCells;
            int targetWalls = (int)MathF.Round(interiorCells * wallChance);

            Map = new char[height][];
            for (int i = 0; i < height; ++i)
            {
                Map[i] = new char[width];
                for (int j = 0; j < width; ++j)
                {
                    if (i == 0 || i == height - 1 || j == 0 || j == width - 1)
                    {
                        Map[i][j] = '#';
                    }
                    else
                    {
                        Map[i][j] = ' ';
                    }
                }
            }

            int placedWalls = 0;
            int safety = 0;
            while (placedWalls < targetWalls && safety < interiorCells * 10)
            {
                int blockW = Random.Shared.Next(1, 5); // 1..4
                int blockH = Random.Shared.Next(1, 4); // 1..3
                int startX = Random.Shared.Next(1, width - 1 - blockW);
                int startY = Random.Shared.Next(1, height - 1 - blockH);

                for (int y = startY; y < startY + blockH && placedWalls < targetWalls; y++)
                {
                    for (int x = startX; x < startX + blockW && placedWalls < targetWalls; x++)
                    {
                        if (Map[y][x] == ' ')
                        {
                            Map[y][x] = '#';
                            placedWalls++;
                            openCells--;
                        }
                    }
                }
                safety++;
            }

            return openCells;
        }

        private static float RandomWallChance()
        {
            float range = MaxWallChance - MinWallChance;
            return MinWallChance + (float)Random.Shared.NextDouble() * range;
        }

        private bool TryGetAnyOpenCell(out int x, out int y)
        {
            for (int row = 1; row < Map.Length - 1; row++)
            {
                for (int col = 1; col < Map[row].Length - 1; col++)
                {
                    if (!MapUtils.IsWallCell(Map[row][col]))
                    {
                        x = col;
                        y = row;
                        return true;
                    }
                }
            }
            x = 0;
            y = 0;
            return false;
        }

        private bool IsFullyConnected(int startX, int startY, int openCells)
        {
            if (openCells <= 1) return true;
            if (MapUtils.IsWallCell(Map[startY][startX])) return false;

            var visited = new bool[Map.Length, Map[0].Length];
            var queue = new Queue<(int X, int Y)>();
            visited[startY, startX] = true;
            queue.Enqueue((startX, startY));
            int visitedCount = 1;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                TryVisit(x + 1, y);
                TryVisit(x - 1, y);
                TryVisit(x, y + 1);
                TryVisit(x, y - 1);
            }

            return visitedCount == openCells;

            void TryVisit(int x, int y)
            {
                if (y < 0 || y >= Map.Length) return;
                if (x < 0 || x >= Map[y].Length) return;
                if (visited[y, x]) return;
                if (MapUtils.IsWallCell(Map[y][x])) return;
                visited[y, x] = true;
                visitedCount++;
                queue.Enqueue((x, y));
            }
        }

        public static char GetDirectionMarker(float angle)
        {
            float normalized = angle % (2 * MathF.PI);
            if (normalized < 0) normalized += 2 * MathF.PI;

            return normalized switch
            {
                >= MathF.PI / 4f and < 3f * MathF.PI / 4f => 'v',     // 45°..135°
                >= 3f * MathF.PI / 4f and < 5f * MathF.PI / 4f => '<', // 135°..225°
                >= 5f * MathF.PI / 4f and < 7f * MathF.PI / 4f => '^', // 225°..315°
                _ => '>',                                             // 315°..45°
            };
        }

        public bool TryGetRandomSpawn(out float x, out float y, IReadOnlyCollection<(float X, float Y)>? occupied = null)
        {
            x = 0f;
            y = 0f;

            if (Map.Length == 0) return false;

            var candidates = new List<(int X, int Y)>();
            for (int row = 0; row < Map.Length; row++)
            {
                for (int col = 0; col < Map[row].Length; col++)
                {
                    if (MapUtils.IsWallCell(Map[row][col])) continue;
                    if (IsOccupiedCell(col, row, occupied)) continue;
                    candidates.Add((col, row));
                }
            }

            if (candidates.Count == 0) return false;

            var pick = candidates[Random.Shared.Next(candidates.Count)];
            x = pick.X + 0.5f;
            y = pick.Y + 0.5f;
            return true;
        }

        public bool IsWalkable(float x, float y)
        {
            int testX = (int)x;
            int testY = (int)y;

            if (testY < 0 || testY >= Map.Length) return false;
            if (testX < 0 || testX >= Map[testY].Length) return false;
            return !MapUtils.IsWallCell(Map[testY][testX]);
        }

        private static bool IsOccupiedCell(int x, int y, IReadOnlyCollection<(float X, float Y)>? occupied)
        {
            if (occupied == null || occupied.Count == 0) return false;
            foreach (var pos in occupied)
            {
                if ((int)pos.X == x && (int)pos.Y == y) return true;
            }
            return false;
        }
    }
}
