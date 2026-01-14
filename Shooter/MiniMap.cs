using Shooter.Services;

namespace Shooter
{
    internal class MiniMap
    {
        private const byte height = 10, width = 20;

        public char[][] Map { get; private set; }

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
                        if(i == 3 && j == 3) Map[i][j] = '^';
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
    }
}
