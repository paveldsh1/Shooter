using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Shooter.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Shooter.Repositories
{
    internal class PlayersRepository
    {
        private readonly ConcurrentDictionary<string, Player> players = new(StringComparer.OrdinalIgnoreCase);

        public bool TryAddPlayer(string nickname, Player player)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return false;
            string key = this.NormalizeNickname(nickname);
            return players.TryAdd(key, player);
        }

        public bool Exists(string nickname)
        {
            string key = this.NormalizeNickname(nickname);
            return !string.IsNullOrWhiteSpace(nickname) && players.ContainsKey(key);
        }

        public Player? TryGetPlayer(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return null;
            string key = this.NormalizeNickname(nickname);
            return players.TryGetValue(key, out var player) ? player : null;
        }

        private string NormalizeNickname(string nickname) => nickname.Trim();
    }
}
