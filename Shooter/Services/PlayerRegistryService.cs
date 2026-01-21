using System.Collections.Concurrent;

namespace Shooter.Services
{
	internal class PlayerRegistryService
	{
		private readonly ConcurrentDictionary<string, System.Guid> nicknameToPlayerId =
			new(StringComparer.OrdinalIgnoreCase);

		public bool Exists(string nickname) =>
			!string.IsNullOrWhiteSpace(nickname) && nicknameToPlayerId.ContainsKey(nickname.Trim());

		public bool TryRegister(string nickname, out Guid playerId)
		{
			playerId = System.Guid.Empty;
			if (string.IsNullOrWhiteSpace(nickname)) return false;
			var key = nickname.Trim();
			playerId = System.Guid.NewGuid();
			return nicknameToPlayerId.TryAdd(key, playerId);
		}
	}
}

