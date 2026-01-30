using Shooter.Models;
using System.Net;
using System.Net.Http.Json;

namespace Shooter.Services
{
    internal sealed class PlayerStateApiClient
    {
        private readonly HttpClient httpClient;

        public PlayerStateApiClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<PlayerState?> LoadAsync(string nickname, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return null;
            string key = nickname.Trim();

            var response = await httpClient.GetAsync($"playerstates/{Uri.EscapeDataString(key)}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlayerState>(cancellationToken: ct);
        }

        public async Task SaveAsync(Player player, CancellationToken ct = default)
        {
            string key = player.Nickname.Trim();
            var payload = new PlayerStateUpsertRequest(player.PlayerX, player.PlayerY, player.PlayerA);
            var response = await httpClient.PutAsJsonAsync(
                $"playerstates/{Uri.EscapeDataString(key)}",
                payload,
                ct);
            response.EnsureSuccessStatusCode();
        }

        private record PlayerStateUpsertRequest(float PlayerX, float PlayerY, float PlayerA);
    }
}
