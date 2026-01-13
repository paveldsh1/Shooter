namespace Shooter
{
    internal class Screen
    {
        private const int screenWidth = 120;
        private const int screenHeight = 40;
        private char[,] screen = new char[screenWidth, screenHeight];

        public Screen()
        {
            Console.SetWindowSize(screenWidth, screenHeight);
        }
    }
}
