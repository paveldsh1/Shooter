using Shooter.Game;
using Shooter.Models;
using System.Net.WebSockets;
using System.Text;

namespace Shooter.Server
{
    internal class GameSession
    {
        public string Nickname { get; }
        public WebSocket Socket { get; }
        public Player Player { get; }
        public Window Window { get; private set; }
        public Map Map { get; private set; }
        public MiniMap SharedMiniMap { get; }
        public volatile bool MiniMapVisible = true;
        private readonly Action<string, float, float, float> positionUpdated;
        private readonly object renderLock = new();

        private readonly byte[] receiveBuffer = new byte[1024];

        public GameSession(string nickname, WebSocket socket, Player player, MiniMap sharedMiniMap, Action<string, float, float, float> positionUpdated)
        {
            Nickname = nickname;
            Socket = socket;
            Player = player;
            SharedMiniMap = sharedMiniMap;
            this.positionUpdated = positionUpdated ?? ((_, _, _, _) => { });
            Window = new Window();
            Map = new Map(sharedMiniMap, player, Window);

            // начальный снимок
            this.positionUpdated?.Invoke(Nickname, Player.PlayerX, Player.PlayerY, Player.PlayerA);
        }

        public async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && Socket.State == WebSocketState.Open)
                {
                    var result = await Socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    var key = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                    if (TryHandleResize(key))
                    {
                        continue;
                    }
                    const float dt = 0.05f;
                    switch (key)
                    {
                        case "Escape": return;
                        case "KeyW": Player.MoveForward(dt, SharedMiniMap); Notify(); break;
                        case "KeyS": Player.MoveBack(dt, SharedMiniMap); Notify(); break;
                        case "KeyA": Player.MoveLeft(dt); Notify(); break;
                        case "KeyD": Player.MoveRight(dt); Notify(); break;
                        case "KeyM": MiniMapVisible = !MiniMapVisible; break;
                        default: break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (WebSocketException)
            {
                // connection closed abruptly
            }
            catch (ObjectDisposedException)
            {
                // socket disposed
            }
        }

        private void Notify() => positionUpdated?.Invoke(Nickname, Player.PlayerX, Player.PlayerY, Player.PlayerA);

        public object RenderLock => renderLock;

        private bool TryHandleResize(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            if (!message.StartsWith("RESIZE", StringComparison.OrdinalIgnoreCase)) return false;

            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return true;

            if (!int.TryParse(parts[1], out int cols) || !int.TryParse(parts[2], out int rows))
            {
                return true;
            }

            cols = Math.Clamp(cols, Window.MinCols, Window.MaxCols);
            rows = Math.Clamp(rows, Window.MinRows, Window.MaxRows);

            if (cols == Window.ScreenWidth && rows == Window.ScreenHeight) return true;

            lock (renderLock)
            {
                var newWindow = new Window(cols, rows);
                var newMap = new Map(SharedMiniMap, Player, newWindow);
                Window = newWindow;
                Map = newMap;
            }
            return true;
        }
    }
}

