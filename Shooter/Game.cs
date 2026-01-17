using System.Diagnostics;

namespace Shooter
{
    internal class Game
    {
        private MiniMap miniMap;
        private Player player;
        private Window window;
        private Map map;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        public Game()
        {
            miniMap = new MiniMap();
            player = new Player(miniMap);
            window = new Window();
            map = new Map(miniMap, player, window);
        }

        public void Start()
        {
            bool closeRequested = false;
            bool mapVisible = true;
            Refresh(mapVisible);
            //stopwatch.Restart();
            while (!closeRequested)
            {
                float elapsedSeconds = (float)stopwatch.Elapsed.TotalSeconds;
                elapsedSeconds = MathF.Min(elapsedSeconds, 0.1f);
                stopwatch.Restart();

                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.Escape:
                        closeRequested = true;
                        return;
                    case ConsoleKey.W:
                        player.MoveForward(elapsedSeconds, miniMap);
                        Refresh(mapVisible);
                        break;
                    case ConsoleKey.S:
                        player.MoveBack(elapsedSeconds, miniMap);
                        Refresh(mapVisible);
                        break;
                    case ConsoleKey.A:
                        player.MoveLeft(elapsedSeconds);
                        Refresh(mapVisible);
                        break;
                    case ConsoleKey.D:
                        player.MoveRight(elapsedSeconds);
                        Refresh(mapVisible);
                        break;
                    case ConsoleKey.M:
                        mapVisible = !mapVisible;
                        if (!mapVisible)
                        {
                            Refresh(renderWithMiniMap: false);
                        }
                        else
                        {
                            Refresh(mapVisible);
                        }
                        break;
                }
            }
            //window.Render(miniMap);
            Console.ReadLine();
        }

        private void Refresh(bool renderWithMiniMap)
        {
            map.Update(window);
            if (renderWithMiniMap)
            {
                window.Render(miniMap, player);
            }
            else
            {
                window.Render();
            }
        }
    }
}
