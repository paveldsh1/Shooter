using Shooter.Models;
using Shooter.Repositories;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;

namespace Shooter
{
    internal class Game
    {
        private MiniMap miniMap;
        private Window window;
        private Map map;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private byte[] buffer = new byte[1024];
        public bool GameStarted { get; private set; } = false;

        public Game()
        {
            miniMap = new MiniMap();
            window = new Window();
        }

		private async Task SendScreenAsync(WebSocket socket)
        {
			if (socket.State != WebSocketState.Open) return;
			string text = Window.ToText(window.Screen);
			var response = Encoding.UTF8.GetBytes(text);
			await socket.SendAsync(
				new ArraySegment<byte>(response),
				WebSocketMessageType.Text,
				true,
				CancellationToken.None);
        }

        public async Task Start(WebSocket socket, Player player) // почему в start switch
        {
            bool closeRequested = false;
            bool mapVisible = true;
            map = new Map(miniMap, player, window);
            Refresh(mapVisible, player);
			await SendScreenAsync(socket);

            //stopwatch.Restart();
            try
            {
                while (!closeRequested && socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            CancellationToken.None);

                    //подумать
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Client disconnected");
                        return;
                    }

                    float elapsedSeconds = (float)stopwatch.Elapsed.TotalSeconds;
                    elapsedSeconds = MathF.Min(elapsedSeconds, 0.1f);
                    stopwatch.Restart();

                    var key = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Key from browser: {key}");

                    switch (key)
                    {
                        case "Escape":
                            closeRequested = true;
                            return;
                        case "KeyW":
                            player.MoveForward(elapsedSeconds, miniMap);
                            Refresh(mapVisible, player);
                            break;
                        case "KeyS":
                            player.MoveBack(elapsedSeconds, miniMap);
                            Refresh(mapVisible, player);
                            break;
                        case "KeyA":
                            player.MoveLeft(elapsedSeconds);
                            Refresh(mapVisible, player);
                            break;
                        case "KeyD":
                            player.MoveRight(elapsedSeconds);
                            Refresh(mapVisible, player);
                            break;
                        case "KeyM":
                            mapVisible = !mapVisible;
                            if (!mapVisible)
                            {
                                Refresh(renderWithMiniMap: false, player);
                            }
                            else
                            {
                                Refresh(mapVisible, player);
                            }
                            break;
					}
					await SendScreenAsync(socket);
                }
            }
            catch (WebSocketException)
            {
                // Клиент закрыл соединение без корректного завершения рукопожатия
                return;
            }
            catch (ObjectDisposedException)
            {
                // Контекст/сокет уже освобождён — выходим тихо
                return;
            }
            //window.Render(miniMap);
            return;
        }

        private void Refresh(bool renderWithMiniMap, Player player)
        {
            map.Update(window);
            if (renderWithMiniMap)
            {
                window.Render(miniMap, player);
            }
            else
            {
                window.Render();
            }
        }
    }
}
