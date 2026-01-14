using System.Text;

namespace Shooter
{
    internal class Window
    {
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public char[,] Screen { get; set; }

        public Window()
        {
            ScreenWidth = 120;
            ScreenHeight = 40;
            Screen = new char[ScreenWidth, ScreenHeight];
            Console.SetWindowSize(ScreenWidth, ScreenHeight);
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, 0);
        }

        public void Render()
        {
            StringBuilder render = new();
            for (int y = 0; y < Screen.GetLength(1); y++)
            {
                for (int x = 0; x < Screen.GetLength(0); x++)
                {
                    render.Append(Screen[x, y]);
                }
                if (y < Screen.GetLength(1) - 1)
                {
                    render.AppendLine();
                }
            }
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, 0);
            Console.Write(render);
        }
    }
}
