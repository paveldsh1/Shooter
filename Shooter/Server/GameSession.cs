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
        public Window Window { get; }
        public Map Map { get; }
        public MiniMap SharedMiniMap { get; }

        private readonly byte[] receiveBuffer = new byte[1024];

        public GameSession(string nickname, WebSocket socket, Player player, MiniMap sharedMiniMap)
        {
            Nickname = nickname;
            Socket = socket;
            Player = player;
            SharedMiniMap = sharedMiniMap;
            Window = new Window();
            Map = new Map(sharedMiniMap, player, Window);
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
                    const float dt = 0.05f;
                    switch (key)
                    {
                        case "Escape": return;
                        case "KeyW": Player.MoveForward(dt, SharedMiniMap); break;
                        case "KeyS": Player.MoveBack(dt, SharedMiniMap); break;
                        case "KeyA": Player.MoveLeft(dt); break;
                        case "KeyD": Player.MoveRight(dt); break;
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
    }
}

