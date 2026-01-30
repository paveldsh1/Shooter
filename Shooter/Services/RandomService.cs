namespace Shooter.Services
{
    internal class RandomService
    {
        public static char GetRandomField()
        {
            Random random = new Random();
            int number = random.Next(0, 100);
            if (number < 50) return ' ';
            else return '█';
        }
    }
}
