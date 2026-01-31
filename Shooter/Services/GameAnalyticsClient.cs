using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Shooter.Services
{
    internal sealed class GameAnalyticsClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient httpClient;
        private readonly GameAnalyticsOptions options;
        private readonly ILogger<GameAnalyticsClient> logger;

        public GameAnalyticsClient(
            HttpClient httpClient,
            IOptions<GameAnalyticsOptions> options,
            ILogger<GameAnalyticsClient> logger)
        {
            this.httpClient = httpClient;
            this.options = options.Value;
            this.logger = logger;
        }

        public bool IsEnabled =>
            options.Enabled &&
            !string.IsNullOrWhiteSpace(options.GameKey) &&
            !string.IsNullOrWhiteSpace(options.SecretKey);

        public async Task SendEventsAsync(IReadOnlyCollection<Dictionary<string, object>> events, CancellationToken ct)
        {
            if (events.Count == 0 || !IsEnabled)
            {
                return;
            }

            string payload = JsonSerializer.Serialize(events, JsonOptions);
            string auth = ComputeHmac(payload, options.SecretKey);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"v2/{options.GameKey}/events");
            request.Headers.TryAddWithoutValidation("Authorization", auth);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("GameAnalytics send failed {StatusCode}: {Body}", response.StatusCode, body);
            }
        }

        private static string ComputeHmac(string payload, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            using var hmac = new HMACSHA256(keyBytes);
            var messageBytes = Encoding.UTF8.GetBytes(payload);
            var hash = hmac.ComputeHash(messageBytes);
            return Convert.ToBase64String(hash);
        }
    }
}
