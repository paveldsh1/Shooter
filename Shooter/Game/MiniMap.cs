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
                        Map[i][j] = ' '; // TODO RandomService.GetRandomField();
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
    }
}
