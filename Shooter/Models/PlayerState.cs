namespace Shooter.Models
{
    internal class PlayerState
    {
        public string Nickname { get; set; } = string.Empty;
        public float PlayerX { get; set; }
        public float PlayerY { get; set; }
        public float PlayerA { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

