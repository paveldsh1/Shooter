using Shooter.Game;
using Shooter.Game.Assets;
using Shooter.Models;
using System.Net.WebSockets;
using System.Text;

namespace Shooter.Server
{
    internal class GameLoopService : BackgroundService
    {
        private readonly GameHost host;

        public GameLoopService(GameHost host)
        {
            this.host = host;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            const int targetFps = 20;
            var frameDuration = TimeSpan.FromMilliseconds(1000.0 / targetFps);
            var lastFrame = sw.Elapsed;

            while (!stoppingToken.IsCancellationRequested)
            {
                var frameStart = sw.Elapsed;
                var delta = frameStart - lastFrame;
                lastFrame = frameStart;
                host.UpdateBots((float)delta.TotalSeconds);

                var sessions = host.GetSessionsSnapshot();
                if (sessions.Count > 0)
                {
                    var tasks = new List<Task>(sessions.Count);
                    foreach (var s in sessions)
                    {
                        tasks.Add(RenderAndSendAsync(s, stoppingToken));
                    }
                    try { await Task.WhenAll(tasks); } catch { /* ignore per-session errors */ }
                }

                var elapsed = sw.Elapsed - frameStart;
                var delay = frameDuration - elapsed;
                if (delay > TimeSpan.Zero)
                {
                    try { await Task.Delay(delay, stoppingToken); } catch { }
                }
            }
        }

        private async Task RenderAndSendAsync(GameSession session, CancellationToken ct)
        {
            if (!TryBuildFrame(session, out string text)) return;
            var bytes = Encoding.UTF8.GetBytes(text);
            try
            {
                await session.Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: ct);
            }
            catch (WebSocketException) { }
            catch (ObjectDisposedException) { }
        }

        private bool TryBuildFrame(GameSession session, out string text)
        {
            text = string.Empty;
            if (session.Socket.State != WebSocketState.Open) return false;

            lock (session.RenderLock)
            {
                if (session.Socket.State != WebSocketState.Open) return false;

                session.Map.Update(session.Window);

                bool selfAlive = IsSelfAlive(session);
                float spriteScale = SpriteMetrics.GetDistanceScale(
                    session.Window.ScreenWidth,
                    session.Window.ScreenHeight,
                    session.ViewScale);

                var miniMapOthers = DrawOtherPlayers(session, spriteScale);
                RenderMiniMap(session, selfAlive, miniMapOthers);
                DrawWeapon(session, selfAlive);
                session.Window.DrawHelpOverlay(session.HelpVisible);

                text = Window.ToText(session.Window.Screen);
                return true;
            }
        }

        private bool IsSelfAlive(GameSession session)
        {
            return !host.TryGetSnapshot(session.Nickname, out var selfSnap) || selfSnap.IsAlive;
        }

        private IReadOnlyCollection<(float X, float Y, float A)>? DrawOtherPlayers(GameSession session, float spriteScale)
        {
            var others = host.GetVisibleSnapshots();
            if (others.Count == 0) return null;

            var miniMapOthers = new List<(float X, float Y, float A)>(others.Count);
            foreach (var snap in others)
            {
                if (string.Equals(snap.Nickname, session.Nickname, StringComparison.OrdinalIgnoreCase)) continue;

                miniMapOthers.Add((snap.X, snap.Y, snap.A));
                DrawEnemyIfVisible(session, snap, spriteScale);
            }

            return miniMapOthers.Count == 0 ? null : miniMapOthers;
        }

        private void DrawEnemyIfVisible(GameSession session, PlayerSnapshot snap, float spriteScale)
        {
            if (!TryProjectEnemy(session, snap, out int enemyScreenX, out int enemyScreenY, out float distance))
            {
                return;
            }

            string[] enemySprite = SpriteMetrics.SelectEnemySprite(distance * spriteScale);
            DrawEnemySprite(session, enemySprite, enemyScreenX, enemyScreenY, distance);
            DrawEnemyName(session, snap.Nickname, enemyScreenX, enemyScreenY, enemySprite.Length, distance);
        }

        private bool TryProjectEnemy(GameSession session, PlayerSnapshot snap, out int enemyScreenX, out int enemyScreenY, out float distance)
        {
            enemyScreenX = 0;
            enemyScreenY = 0;

            float dx = snap.X - session.Player.PlayerX;
            float dy = snap.Y - session.Player.PlayerY;
            float angle = MathF.Atan2(dy, dx);
            if (angle < 0) angle += 2f * MathF.PI;
            distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance <= 0.05f || distance > GameConstants.MaxDepth) return false;

            float fovA = session.Player.PlayerA - GameConstants.FieldOfView / 2f;
            if (fovA < 0) fovA += 2f * MathF.PI;
            float diff = angle < fovA && fovA - 2f * MathF.PI + GameConstants.FieldOfView > angle
                ? angle + 2f * MathF.PI - fovA
                : angle - fovA;
            if (diff < 0 || diff > GameConstants.FieldOfView) return false;

            float ratio = diff / GameConstants.FieldOfView;
            enemyScreenX = (int)(session.Window.ScreenWidth * ratio);

            int ceiling = (int)(session.Window.ScreenHeight / 2.0f - session.Window.ScreenHeight / distance);
            int floor = session.Window.ScreenHeight - ceiling;
            enemyScreenY = Math.Min(floor, session.Window.ScreenHeight);
            return true;
        }

        private void DrawEnemySprite(GameSession session, string[] enemySprite, int enemyScreenX, int enemyScreenY, float distance)
        {
            for (int y = 0; y < enemySprite.Length; y++)
            {
                var row = enemySprite[y];
                for (int x = 0; x < row.Length; x++)
                {
                    char ch = row[x];
                    if (ch == '!') continue; // прозрачность
                    int screenX = x - row.Length / 2 + enemyScreenX;
                    int screenY = y - enemySprite.Length + enemyScreenY;
                    if (screenX < 0 || screenX >= session.Window.ScreenWidth ||
                        screenY < 0 || screenY >= session.Window.ScreenHeight)
                        continue;
                    // Z‑test: если игрок ближе стены на этом столбце
                    if (screenX < session.Map.ColumnDepths.Length &&
                        distance < session.Map.ColumnDepths[screenX])
                    {
                        session.Window.Screen[screenX, screenY] = ch;
                    }
                }
            }
        }

        private void DrawEnemyName(GameSession session, string nickname, int enemyScreenX, int enemyScreenY, int spriteHeight, float distance)
        {
            int nameY = enemyScreenY - spriteHeight - 1;
            session.Window.DrawName(
                nickname,
                enemyScreenX,
                nameY,
                distance,
                session.Map.ColumnDepths);
        }

        private void RenderMiniMap(GameSession session, bool selfAlive, IReadOnlyCollection<(float X, float Y, float A)>? miniMapOthers)
        {
            if (session.MiniMapVisible)
            {
                session.Window.Render(session.SharedMiniMap, selfAlive ? session.Player : null, miniMapOthers);
            }
            else
            {
                session.Window.Render();
            }
        }

        private void DrawWeapon(GameSession session, bool selfAlive)
        {
            if (!selfAlive) return;

            var weaponSprite = GetWeaponSprite(session);
            session.Window.DrawSprite(
                weaponSprite,
                session.Window.ScreenWidth / 2,
                session.Window.ScreenHeight - 1,
                session.ViewScale);
        }


        private static string[] GetWeaponSprite(GameSession session)
        {
            if (session.EquippedWeapon == WeaponType.Shotgun)
            {
                return session.IsShooting ? WeaponSprites.PlayerShotgunShoot : WeaponSprites.PlayerShotgun;
            }
            return session.IsShooting ? WeaponSprites.PlayerPistolShoot : WeaponSprites.PlayerPistol;
        }

    }
}

