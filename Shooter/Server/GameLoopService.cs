using Shooter.Game;
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

        private static async Task RenderAndSendAsync(GameSession session, CancellationToken ct)
        {
            if (session.Socket.State != WebSocketState.Open) return;

            // Обновляем рендер для конкретного игрока
            session.Map.Update(session.Window);
            // Наложение миникарты по флагу
            if (session.MiniMapVisible)
            {
                session.Window.Render(session.SharedMiniMap, session.Player);
            }
            else
            {
                session.Window.Render();
            }
            var text = Window.ToText(session.Window.Screen);
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

