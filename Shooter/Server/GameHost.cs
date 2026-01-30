using Shooter.Game;
using Shooter.Models;
using Shooter.Repositories;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Shooter.Server
{
    internal class GameHost
    {
        private readonly PlayersRepository playersRepository;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ConcurrentDictionary<string, GameSession> sessions = new(StringComparer.OrdinalIgnoreCase);
        private readonly MiniMap sharedMiniMap = new MiniMap();
        private readonly ConcurrentDictionary<string, PlayerSnapshot> snapshots = new(StringComparer.OrdinalIgnoreCase);

        public GameHost(PlayersRepository playersRepository, IServiceScopeFactory scopeFactory)
        {
            this.playersRepository = playersRepository;
            this.scopeFactory = scopeFactory;
        }

        public bool HasSession(string nickname) =>
            !string.IsNullOrWhiteSpace(nickname) && sessions.ContainsKey(nickname.Trim());

        public IReadOnlyCollection<GameSession> GetSessionsSnapshot()
            => sessions.Values.ToList();

        public IReadOnlyCollection<PlayerSnapshot> GetPlayerSnapshots()
            => snapshots.Values.ToList();

        public bool TryGetRandomSpawn(out float x, out float y)
        {
            var occupied = new List<(float X, float Y)>(snapshots.Count);
            foreach (var snap in snapshots.Values)
            {
                occupied.Add((snap.X, snap.Y));
            }
            return sharedMiniMap.TryGetRandomSpawn(out x, out y, occupied);
        }

        public void UpsertSnapshot(string nickname, float x, float y, float a)
        {
            var key = nickname.Trim();
            snapshots.AddOrUpdate(key, new PlayerSnapshot(key, x, y, a, DateTime.UtcNow),
                (_, __) => new PlayerSnapshot(key, x, y, a, DateTime.UtcNow));
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
            UpsertSnapshot(player.Nickname, player.PlayerX, player.PlayerY, player.PlayerA);

            var session = new GameSession(player.Nickname, socket, player, sharedMiniMap, (n, x, y, a) => UpsertSnapshot(n, x, y, a));
            sessions[key] = session;

            try
            {
                await session.RunReceiveLoopAsync(CancellationToken.None);
            }
            finally
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var stateService = scope.ServiceProvider.GetRequiredService<Shooter.Services.PlayerStateService>();
                    await stateService.SaveAsync(player, CancellationToken.None);
                }
                catch { /* ignore persistence errors */ }

                sessions.TryRemove(key, out _);
                snapshots.TryRemove(key, out _);
                playersRepository.RemovePlayer(player.Nickname);
            }
        }

        internal readonly record struct PlayerSnapshot(string Nickname, float X, float Y, float A, DateTime UpdatedAt);
    }
}

