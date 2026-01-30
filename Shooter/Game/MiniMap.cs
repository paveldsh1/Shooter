using Shooter.Models;

namespace Shooter.Game
{
    internal class MiniMap
    {
        private const byte height = 10, width = 20;

        public char[][] Map { get; private set; } = Array.Empty<char[]>();

        public MiniMap()
        {
            InitMap();
        }

        private void InitMap() //будет использоваться при обновлении состояния игры
        {
            Map = new char[height][];
            for (int i = 0; i < height; ++i)
            {
                Map[i] = new char[width];
                for (int j = 0; j < width; ++j)
                {
                    if (i == 0 || i == height - 1 || j == 0 || j == width - 1) Map[i][j] = '#';
                    else
                    {
                        Map[i][j] = ' ';
                    }
                }
            }
        }

        public void PrintMiniMap()
        {
            for(int i = 0; i < height; ++i)
            {
                for(int j = 0; j < width; ++j)
                {
                    Console.Write(Map[i][j]);
                }
                Console.WriteLine();
            }
        }

        private void DeleteMarker()
        {
            for(int i = 0; i < Map.Length; ++i)
            {
                for (int j = 0; j < Map[i].Length; ++j)
                {
                    if (Map[i][j] is 'v' or '<' or '^' or '>' or '.')
                    {
                        Map[i][j] = ' ';
                        return;
                    }
                }
            }
        }

        public void Update(Player player)
        {
            DeleteMarker();
            int testX = (int)player.PlayerX;
            int testY = (int)player.PlayerY;

            // Проверяем границы: сначала по строкам (Y), затем по столбцам (X)
            if (testY < 0 || testY >= Map.Length) return;
            if (testX < 0 || testX >= Map[testY].Length) return;

            Map[testY][testX] = '.';
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
                    if (IsWallCell(Map[row][col])) continue;
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

        private static bool IsWallCell(char cell) => cell is '#' or '█';

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
