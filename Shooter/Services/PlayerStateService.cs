using Microsoft.EntityFrameworkCore;
using Shooter.Data;
using Shooter.Models;

namespace Shooter.Services
{
    internal class PlayerStateService
    {
        private readonly ApplicationContext db;

        public PlayerStateService(ApplicationContext db)
        {
            this.db = db;
        }

        public async Task<PlayerState?> LoadAsync(string nickname, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return null;
            var key = nickname.Trim();
            return await db.PlayerStates.AsNoTracking().FirstOrDefaultAsync(x => x.Nickname == key, ct);
        }

        public async Task SaveAsync(Player player, CancellationToken ct = default)
        {
            var key = player.Nickname.Trim();
            var state = await db.PlayerStates.FirstOrDefaultAsync(x => x.Nickname == key, ct);
            if (state is null)
            {
                state = new PlayerState
                {
                    Nickname = key,
                    PlayerX = player.PlayerX,
                    PlayerY = player.PlayerY,
                    PlayerA = player.PlayerA,
                    UpdatedAt = DateTime.UtcNow
                };
                db.PlayerStates.Add(state);
            }
            else
            {
                state.PlayerX = player.PlayerX;
                state.PlayerY = player.PlayerY;
                state.PlayerA = player.PlayerA;
                state.UpdatedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
        }
    }
}

