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

                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
                Game game = new Game();
                await game.Start(socket);
            });

            app.Run();
        }
    }
}