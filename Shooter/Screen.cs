using Shooter.Models;
using System.Text;

namespace Shooter
{
    internal class Window
    {
        public int ScreenWidth { get; }
        public int ScreenHeight { get; }
        public char[,] Screen { get; private set; }

        private bool firstMiniMapRender = true;

        public Window()
        {
            ScreenWidth = 120;
            ScreenHeight = 40;
            Screen = new char[ScreenWidth, ScreenHeight];
            Console.SetWindowSize(ScreenWidth, ScreenHeight);
        }

        public void Render(MiniMap? miniMap = null, Player? player = null)
        {
            if (miniMap != null && player != null)
            {
                if (!firstMiniMapRender) miniMap.Update(player);
                else firstMiniMapRender = !firstMiniMapRender;
                AddMiniMapToScreen(miniMap);
            }
            //StringBuilder render = new();
            //for (int y = 0; y < Screen.GetLength(1); y++)
            //{
            //    for (int x = 0; x < Screen.GetLength(0); x++)
            //    {
            //        render.Append(Screen[x, y]);
            //    }
            //    if (y < Screen.GetLength(1) - 1)
            //    {
            //        render.AppendLine();
            //    }
            //}
            //Console.CursorVisible = false;
            //Console.SetCursorPosition(0, 0);
            //string str = render.ToString();
            //Console.Write(render);
        }
        
        private void AddMiniMapToScreen(MiniMap miniMap)
        {
            for (int y = 0; y < miniMap.Map.GetLength(0); y++)
            {
                for (int x = 0; x < miniMap.Map[y].Length; x++)
                {
                    Screen[x, y] = miniMap.Map[y][x];
                }
            }
        }

        public static string ToText(char[,] grid)
        {
            int rows = grid.GetLength(1);
            int cols = grid.GetLength(0);
            StringBuilder result = new StringBuilder();
            for(int y = 0; y < rows; ++y)
            {
                for(int x = 0; x < cols; ++x)
                {
                    result.Append(grid[x, y]);
                }
                if (y < grid.GetLength(1) - 1)
                {
                    result.AppendLine();
                }
            }
            return result.ToString();
        }
    }
}
