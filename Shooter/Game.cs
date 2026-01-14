namespace Shooter
{
    internal class Game
    {
        private MiniMap miniMap;
        private Player player;
        private Window window;
        private Map map;
        public Game()
        {
            miniMap = new MiniMap();
            player = new Player(miniMap);
            window = new Window();
            map = new Map(miniMap, player, window);
        }

        public void Start()
        {
            window.Render(miniMap);
            bool closeRequested = false;
            bool isThereMiniMap = true;
            while (!closeRequested)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.Escape:
                        closeRequested = true;
                        return;
                    case ConsoleKey.W:
                        player.MoveForward();
                        break;
                    case ConsoleKey.M:
                        isThereMiniMap = !isThereMiniMap;
                        if (!isThereMiniMap)
                        {
                            map.Update(window);
                            window.Render();
                        }
                        else window.Render(miniMap);
                        break;
                }
            }
            //window.Render(miniMap);
            Console.ReadLine();
        }
    }
}
