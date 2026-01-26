using Shooter.Game;
using Shooter.Models;
using Shooter.Repositories;
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

        public GameHost(PlayersRepository playersRepository, IServiceScopeFactory scopeFactory)
        {
            this.playersRepository = playersRepository;
            this.scopeFactory = scopeFactory;
        }

        public bool HasSession(string nickname) =>
            !string.IsNullOrWhiteSpace(nickname) && sessions.ContainsKey(nickname.Trim());

        public IReadOnlyCollection<GameSession> GetSessionsSnapshot()
            => sessions.Values.ToList();

        public async Task RunSessionAsync(WebSocket socket, Player player)
        {
            var key = player.Nickname.Trim();
            // Закрываем прежнюю сессию, если была
            if (sessions.TryRemove(key, out var existing))
            {
                try { await existing.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Replaced", CancellationToken.None); }
                catch { /* ignore */ }
            }

            var session = new GameSession(player.Nickname, socket, player, sharedMiniMap);
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
                playersRepository.RemovePlayer(player.Nickname);
            }
        }
    }
}

