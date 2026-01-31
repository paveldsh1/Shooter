namespace Shooter.Services
{
    internal sealed class GameAnalyticsOptions
    {
        public bool Enabled { get; set; }
        public string GameKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://sandbox-api.gameanalytics.com";
        public string? Build { get; set; }
    }
}
