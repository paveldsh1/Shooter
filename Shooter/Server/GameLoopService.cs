using Shooter.Game;
using Shooter.Game.Assets;
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

            while (!stoppingToken.IsCancellationRequested)
            {
                var frameStart = sw.Elapsed;

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
            if (session.Socket.State != WebSocketState.Open) return;

            string? text = null;
            lock (session.RenderLock)
            {
                if (session.Socket.State != WebSocketState.Open) return;

                // Обновляем рендер для конкретного игрока
                session.Map.Update(session.Window);

                // Нарисовать других игроков как спрайты с простым Z‑тестом
                var others = host.GetPlayerSnapshots();
                List<(float X, float Y, float A)>? miniMapOthers = null;
                if (others.Count > 0)
                {
                    miniMapOthers = new List<(float X, float Y, float A)>(others.Count);
                    foreach (var snap in others)
                    {
                        if (string.Equals(snap.Nickname, session.Nickname, StringComparison.OrdinalIgnoreCase)) continue;

                        miniMapOthers.Add((snap.X, snap.Y, snap.A));

                        float dx = snap.X - session.Player.PlayerX;
                        float dy = snap.Y - session.Player.PlayerY;
                        float angle = MathF.Atan2(dy, dx);
                        if (angle < 0) angle += 2f * MathF.PI;
                        float distance = MathF.Sqrt(dx * dx + dy * dy);

                        // Отбрасываем сильно дальних
                        if (distance <= 0.05f || distance > GameConstants.MaxDepth) continue;

                        float fovA = session.Player.PlayerA - GameConstants.FieldOfView / 2f;
                        if (fovA < 0) fovA += 2f * MathF.PI;
                        float diff = angle < fovA && fovA - 2f * MathF.PI + GameConstants.FieldOfView > angle
                            ? angle + 2f * MathF.PI - fovA
                            : angle - fovA;
                        if (diff < 0 || diff > GameConstants.FieldOfView) continue; // вне экрана

                        float ratio = diff / GameConstants.FieldOfView;
                        int enemyScreenX = (int)(session.Window.ScreenWidth * ratio);

                        // Выбор кадра по расстоянию
                        string[] enemySprite =
                            distance <= 1f ? EnemySprites.EnemySprite8 :
                            distance <= 2f ? EnemySprites.EnemySprite7 :
                            distance <= 3f ? EnemySprites.EnemySprite6 :
                            distance <= 4f ? EnemySprites.EnemySprite5 :
                            distance <= 5f ? EnemySprites.EnemySprite4 :
                            distance <= 6f ? EnemySprites.EnemySprite3 :
                            distance <= 7f ? EnemySprites.EnemySprite2 :
                            EnemySprites.EnemySprite1;

                        int ceiling = (int)(session.Window.ScreenHeight / 2.0f - session.Window.ScreenHeight / distance);
                        int floor = session.Window.ScreenHeight - ceiling;
                        int enemyScreenY = Math.Min(floor, session.Window.ScreenHeight);

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

                        int nameY = enemyScreenY - enemySprite.Length - 1;
                        session.Window.DrawName(
                            snap.Nickname,
                            enemyScreenX,
                            nameY,
                            distance,
                            session.Map.ColumnDepths);
                    }
                    if (miniMapOthers.Count == 0) miniMapOthers = null;
                }
                // Наложение миникарты по флагу
                if (session.MiniMapVisible)
                {
                    session.Window.Render(session.SharedMiniMap, session.Player, miniMapOthers);
                }
                else
                {
                    session.Window.Render();
                }
                text = Window.ToText(session.Window.Screen);
            }
            if (text is null) return;
            var bytes = Encoding.UTF8.GetBytes(text);
            try
            {
                await session.Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: ct);
            }
            catch (WebSocketException) { }
            catch (ObjectDisposedException) { }
        }

    }
}

