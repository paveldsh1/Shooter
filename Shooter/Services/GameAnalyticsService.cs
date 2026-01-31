using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Shooter.Services
{
    internal sealed class GameAnalyticsService : BackgroundService
    {
        private const string Platform = "server";
        private const string SdkVersion = "rest api v2";

        private readonly GameAnalyticsClient client;
        private readonly GameAnalyticsOptions options;
        private readonly ILogger<GameAnalyticsService> logger;
        private readonly Channel<Dictionary<string, object>> channel;
        private readonly ConcurrentDictionary<string, SessionContext> sessions =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, int> sessionCounts =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly string device;
        private readonly string manufacturer;
        private readonly string osVersion;

        public GameAnalyticsService(
            GameAnalyticsClient client,
            IOptions<GameAnalyticsOptions> options,
            ILogger<GameAnalyticsService> logger)
        {
            this.client = client;
            this.options = options.Value;
            this.logger = logger;
            channel = Channel.CreateUnbounded<Dictionary<string, object>>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            device = Sanitize(Environment.MachineName, "server");
            manufacturer = "server";
            osVersion = BuildOsVersion();
        }

        public bool IsEnabled => client.IsEnabled;

        public void TrackSessionStart(string nickname)
        {
            if (!IsEnabled) return;

            string? userId = NormalizeUserId(nickname);
            if (userId is null) return;

            var session = StartSession(userId);
            var evt = CreateBaseEvent("user", session, userId);
            Enqueue(evt);
        }

        public void TrackSessionEnd(string nickname)
        {
            if (!IsEnabled) return;

            string? userId = NormalizeUserId(nickname);
            if (userId is null) return;

            if (!sessions.TryRemove(userId, out var session)) return;

            var evt = CreateBaseEvent("session_end", session, userId);
            int length = (int)Math.Clamp((DateTime.UtcNow - session.StartedAtUtc).TotalSeconds, 0, 172800);
            evt["length"] = length;
            Enqueue(evt);
        }

        public void TrackKill(string killerNickname, string victimNickname)
        {
            TrackDesignEvent(killerNickname, "Combat:Kill");
            TrackDesignEvent(victimNickname, "Combat:Death");
        }

        public void TrackDesignEvent(string nickname, string eventId, float? value = null)
        {
            if (!IsEnabled) return;

            string? userId = NormalizeUserId(nickname);
            if (userId is null) return;

            if (!sessions.TryGetValue(userId, out var session)) return;

            var evt = CreateBaseEvent("design", session, userId);
            evt["event_id"] = eventId;
            if (value.HasValue)
            {
                evt["value"] = value.Value;
            }

            Enqueue(evt);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!IsEnabled)
            {
                logger.LogInformation("GameAnalytics disabled.");
                return;
            }

            try
            {
                while (await channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    while (channel.Reader.TryRead(out var evt))
                    {
                        try
                        {
                            await client.SendEventsAsync(new[] { evt }, stoppingToken);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to send GameAnalytics event.");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }

        private SessionContext StartSession(string userId)
        {
            int sessionNum = sessionCounts.AddOrUpdate(userId, 1, (_, current) => current + 1);
            var session = new SessionContext(
                Guid.NewGuid().ToString("D").ToLowerInvariant(),
                sessionNum,
                DateTime.UtcNow);
            sessions[userId] = session;
            return session;
        }

        private Dictionary<string, object> CreateBaseEvent(string category, SessionContext session, string userId)
        {
            var evt = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["category"] = category,
                ["v"] = 2,
                ["user_id"] = userId,
                ["session_id"] = session.SessionId,
                ["session_num"] = session.SessionNum,
                ["sdk_version"] = SdkVersion,
                ["platform"] = Platform,
                ["os_version"] = osVersion,
                ["manufacturer"] = manufacturer,
                ["device"] = device,
                ["client_ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            if (!string.IsNullOrWhiteSpace(options.Build))
            {
                evt["build"] = options.Build!;
            }

            return evt;
        }

        private void Enqueue(Dictionary<string, object> evt)
        {
            if (!channel.Writer.TryWrite(evt))
            {
                logger.LogWarning("GameAnalytics queue is full.");
            }
        }

        private static string? NormalizeUserId(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return null;
            return nickname.Trim();
        }

        private static string BuildOsVersion()
        {
            string osName =
                OperatingSystem.IsWindows() ? "windows" :
                OperatingSystem.IsLinux() ? "linux" :
                OperatingSystem.IsMacOS() ? "mac_osx" :
                "windows";
            var version = Environment.OSVersion.Version.ToString();
            return $"{osName} {version}";
        }

        private static string Sanitize(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            string trimmed = value.Trim();
            return trimmed.Length <= 64 ? trimmed : trimmed.Substring(0, 64);
        }

        private sealed record SessionContext(string SessionId, int SessionNum, DateTime StartedAtUtc);
    }
}
