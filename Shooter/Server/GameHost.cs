using Shooter.Game;
using Shooter.Models;
using Shooter.Repositories;
using Shooter.Services;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Shooter.Server
{
    internal class GameHost
    {
        private const int BotCount = 6;
        private readonly PlayersRepository playersRepository;
        private readonly PlayerStateApiClient stateClient;
        private readonly GameAnalyticsService analytics;
        private readonly ConcurrentDictionary<string, GameSession> sessions = new(StringComparer.OrdinalIgnoreCase);
        private readonly MiniMap sharedMiniMap = new MiniMap();
        private readonly ConcurrentDictionary<string, PlayerSnapshot> snapshots = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, PlayerSnapshot> bots = new(StringComparer.OrdinalIgnoreCase);
        private volatile bool botsMode;

        public GameHost(
            PlayersRepository playersRepository,
            PlayerStateApiClient stateClient,
            GameAnalyticsService analytics)
        {
            this.playersRepository = playersRepository;
            this.stateClient = stateClient;
            this.analytics = analytics;
        }

        public bool HasSession(string nickname) =>
            !string.IsNullOrWhiteSpace(nickname) && sessions.ContainsKey(nickname.Trim());

        public IReadOnlyCollection<GameSession> GetSessionsSnapshot()
            => sessions.Values.ToList();

        public IReadOnlyCollection<PlayerSnapshot> GetPlayerSnapshots()
            => snapshots.Values.ToList();

        public IReadOnlyCollection<PlayerSnapshot> GetAliveSnapshots()
            => snapshots.Values.Where(x => x.IsAlive).ToList();

        public IReadOnlyCollection<PlayerSnapshot> GetVisibleSnapshots()
        {
            if (botsMode)
            {
                return bots.Values.Where(x => x.IsAlive).ToList();
            }
            return snapshots.Values.Where(x => x.IsAlive).ToList();
        }

        public bool TryGetSnapshot(string nickname, out PlayerSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                snapshot = default;
                return false;
            }
            return snapshots.TryGetValue(nickname.Trim(), out snapshot);
        }

        public bool TryGetRandomSpawn(out float x, out float y)
        {
            var occupied = new List<(float X, float Y)>(snapshots.Count);
            foreach (var snap in snapshots.Values)
            {
                if (snap.IsAlive)
                {
                    occupied.Add((snap.X, snap.Y));
                }
            }
            foreach (var bot in bots.Values)
            {
                if (bot.IsAlive)
                {
                    occupied.Add((bot.X, bot.Y));
                }
            }
            return sharedMiniMap.TryGetRandomSpawn(out x, out y, occupied);
        }

        public bool IsWalkable(float x, float y) => sharedMiniMap.IsWalkable(x, y);

        public void UpsertSnapshot(string nickname, float x, float y, float a, bool? isAlive = null)
        {
            var key = nickname.Trim();
            snapshots.AddOrUpdate(
                key,
                _ => new PlayerSnapshot(key, x, y, a, DateTime.UtcNow, isAlive ?? true),
                (_, existing) => new PlayerSnapshot(key, x, y, a, DateTime.UtcNow, isAlive ?? existing.IsAlive));
        }

        public void HandleShoot(string shooterNickname, float sx, float sy, float sa, int screenWidth, int screenHeight, float viewScale)
        {
            if (!CanShoot(shooterNickname)) return;

            var targets = botsMode ? bots.Values : snapshots.Values;
            if (TryFindHitTarget(shooterNickname, targets, sx, sy, sa, screenWidth, screenHeight, viewScale, out var hitNickname))
            {
                if (botsMode)
                {
                    KillBot(hitNickname);
                }
                else
                {
                    KillPlayer(hitNickname);
                }
                analytics.TrackKill(shooterNickname, hitNickname);
            }
        }

        private bool CanShoot(string shooterNickname)
        {
            if (string.IsNullOrWhiteSpace(shooterNickname)) return false;
            return !TryGetSnapshot(shooterNickname, out var selfSnap) || selfSnap.IsAlive;
        }

        private bool TryFindHitTarget(
            string shooterNickname,
            IEnumerable<PlayerSnapshot> candidates,
            float sx,
            float sy,
            float sa,
            int screenWidth,
            int screenHeight,
            float viewScale,
            out string hitNickname)
        {
            hitNickname = string.Empty;
            float bestDistance = float.MaxValue;
            float spriteScale = SpriteMetrics.GetDistanceScale(screenWidth, screenHeight, viewScale);

            foreach (var snap in candidates)
            {
                if (!IsEnemyTarget(shooterNickname, snap)) continue;
                if (!TryProjectTarget(sx, sy, sa, screenWidth, snap, out int enemyScreenX, out float distance)) continue;
                if (!HasLineOfSight(sx, sy, snap.X, snap.Y)) continue;

                string[] enemySprite = SpriteMetrics.SelectEnemySprite(distance * spriteScale);
                if (!IsCrosshairHit(enemyScreenX, screenWidth, enemySprite)) continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    hitNickname = snap.Nickname;
                }
            }

            return hitNickname.Length > 0;
        }

        private static bool IsEnemyTarget(string shooterNickname, PlayerSnapshot snap)
        {
            return snap.IsAlive && !string.Equals(snap.Nickname, shooterNickname, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryProjectTarget(
            float sx,
            float sy,
            float sa,
            int screenWidth,
            PlayerSnapshot snap,
            out int enemyScreenX,
            out float distance)
        {
            enemyScreenX = 0;

            float dx = snap.X - sx;
            float dy = snap.Y - sy;
            distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance <= 0.05f || distance > GameConstants.MaxDepth) return false;

            float angle = MathF.Atan2(dy, dx);
            if (angle < 0) angle += 2f * MathF.PI;

            float fovA = sa - GameConstants.FieldOfView / 2f;
            if (fovA < 0) fovA += 2f * MathF.PI;

            float diff = angle < fovA && fovA - 2f * MathF.PI + GameConstants.FieldOfView > angle
                ? angle + 2f * MathF.PI - fovA
                : angle - fovA;
            if (diff < 0 || diff > GameConstants.FieldOfView) return false;

            float ratio = diff / GameConstants.FieldOfView;
            enemyScreenX = (int)(screenWidth * ratio);
            return true;
        }

        private static bool IsCrosshairHit(int enemyScreenX, int screenWidth, string[] enemySprite)
        {
            int halfWidth = enemySprite[0].Length / 2;
            int minX = enemyScreenX - halfWidth;
            int maxX = enemyScreenX + halfWidth;
            int centerX = screenWidth / 2;
            return centerX >= minX && centerX <= maxX;
        }

        private void KillPlayer(string nickname)
        {
            SetAlive(nickname, false);
            if (sessions.TryGetValue(nickname.Trim(), out var session))
            {
                _ = CloseSessionAsync(session, "Killed");
            }
        }

        private void KillBot(string nickname)
        {
            bots.TryRemove(nickname.Trim(), out _);
        }

        public void ToggleBotsMode(string requestedBy)
        {
            botsMode = !botsMode;
            if (botsMode)
            {
                SpawnBots();
            }
            else
            {
                bots.Clear();
            }
        }

        private void SpawnBots()
        {
            bots.Clear();
            var occupied = new List<(float X, float Y)>(snapshots.Count + BotCount);
            foreach (var snap in snapshots.Values)
            {
                if (snap.IsAlive)
                {
                    occupied.Add((snap.X, snap.Y));
                }
            }

            for (int i = 1; i <= BotCount; i++)
            {
                if (!sharedMiniMap.TryGetRandomSpawn(out float x, out float y, occupied))
                {
                    break;
                }
                occupied.Add((x, y));
                float a = Random.Shared.NextSingle() * 2f * MathF.PI;
                string name = $"Bot{i}";
                bots[name] = new PlayerSnapshot(name, x, y, a, DateTime.UtcNow, true);
            }
        }

        private void SetAlive(string nickname, bool alive)
        {
            var key = nickname.Trim();
            snapshots.AddOrUpdate(
                key,
                _ => new PlayerSnapshot(key, 0f, 0f, 0f, DateTime.UtcNow, alive),
                (_, existing) => new PlayerSnapshot(key, existing.X, existing.Y, existing.A, DateTime.UtcNow, alive));
        }

        private static async Task CloseSessionAsync(GameSession session, string reason)
        {
            try
            {
                await session.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
            }
            catch
            {
                try { session.Socket.Abort(); } catch { }
            }
        }

        private bool HasLineOfSight(float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance <= 0.05f) return true;

            float step = 0.05f;
            float stepX = dx / distance * step;
            float stepY = dy / distance * step;
            float x = x0;
            float y = y0;
            float traveled = 0f;

            while (traveled < distance)
            {
                x += stepX;
                y += stepY;
                traveled += step;
                if (!sharedMiniMap.IsWalkable(x, y)) return false;
            }
            return true;
        }

        public async Task RunSessionAsync(WebSocket socket, Player player)
        {
            var key = player.Nickname.Trim();
            // Закрываем прежнюю сессию, если была
            if (sessions.TryRemove(key, out var existing))
            {
                try { await existing.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Replaced", CancellationToken.None); }
                catch { /* ignore */ }
            }

            // первичная фиксация позиции подключившегося игрока
            UpsertSnapshot(player.Nickname, player.PlayerX, player.PlayerY, player.PlayerA, isAlive: true);

            var session = new GameSession(
                player.Nickname,
                socket,
                player,
                sharedMiniMap,
                (n, x, y, a) => UpsertSnapshot(n, x, y, a),
                (n) => ToggleBotsMode(n),
                (n, x, y, a, w, h, s) => HandleShoot(n, x, y, a, w, h, s));
            sessions[key] = session;
            analytics.TrackSessionStart(player.Nickname);

            try
            {
                await session.RunReceiveLoopAsync(CancellationToken.None);
            }
            finally
            {
                analytics.TrackSessionEnd(player.Nickname);
                try
                {
                    await stateClient.SaveAsync(player, CancellationToken.None);
                }
                catch { /* ignore persistence errors */ }

                sessions.TryRemove(key, out _);
                snapshots.TryRemove(key, out _);
                playersRepository.RemovePlayer(player.Nickname);
            }
        }

    }
}

