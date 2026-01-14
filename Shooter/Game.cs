namespace Shooter
{
    internal class Game
    {
        public Game()
        {
            MiniMap miniMap = new MiniMap();
            Player player = new Player(miniMap);
            Window window = new Window();
            Map map = new Map(miniMap, player, window);
            window.Render();
        }
    }
}
