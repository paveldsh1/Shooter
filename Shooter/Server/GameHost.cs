using Shooter.Models;
using Shooter.Repositories;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Shooter.Server
{
    internal class GameHost
    {
        private readonly PlayersRepository playersRepository;
        private readonly ConcurrentDictionary<string, GameSession> sessions = new(StringComparer.OrdinalIgnoreCase);
        private readonly MiniMap sharedMiniMap = new MiniMap();

        public GameHost(PlayersRepository playersRepository)
        {
            this.playersRepository = playersRepository;
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
                sessions.TryRemove(key, out _);
                playersRepository.RemovePlayer(player.Nickname);
            }
        }
    }
}

