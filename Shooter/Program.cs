using System.Net.WebSockets;
using System.Text;

namespace Shooter
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            // Включаем WebSocket
            app.UseWebSockets();

            // HTML страница
            app.MapGet("/", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";

                await context.Response.SendFileAsync("html/index.html");
            });

            // WebSocket endpoint
            app.Map("/ws", async context =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine("Client connected");

                var buffer = new byte[1024];

                while (true)
                {
                    var result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Client disconnected");
                        break;
                    }

                    var key = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Key from browser: {key}");

                    var response = Encoding.UTF8.GetBytes($"You pressed: {key}");
                    await socket.SendAsync(
                        new ArraySegment<byte>(response),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            });

            app.Run();
        }
    }
}