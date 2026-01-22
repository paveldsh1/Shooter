using Shooter.Repositories;
using Shooter.Services;
using System.Text;

namespace Shooter
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<PlayerRegistryService>();
            builder.Services.AddSingleton<PlayerRepository>();
            var app = builder.Build();

            // Включаем WebSocket
            app.UseWebSockets();

            app.MapPost("/players/register", async (HttpContext context, PlayerRegistryService registry) =>
            {
                var payload = await context.Request.ReadFromJsonAsync<RegisterRequest>();
                if (payload is null || string.IsNullOrWhiteSpace(payload.Nickname))
                {
                    return Microsoft.AspNetCore.Http.Results.BadRequest(new { error = "Nickname is required" });
                }

                if (!registry.TryRegister(payload.Nickname, out var playerId))
                {
                    return Microsoft.AspNetCore.Http.Results.Conflict(new { error = "Nickname already exists" });
                }

                return Microsoft.AspNetCore.Http.Results.Ok(new { playerId });
            });

            app.MapPost("/players/login", async (HttpContext context, PlayerRegistryService registry) =>
            {
                var payload = await context.Request.ReadFromJsonAsync<RegisterRequest>();
                if (payload is null || string.IsNullOrWhiteSpace(payload.Nickname))
                {
                    return Microsoft.AspNetCore.Http.Results.BadRequest(new { error = "Nickname is required" });
                }

                if (registry.TryGet(payload.Nickname, out var playerId))
                {
                    return Microsoft.AspNetCore.Http.Results.Ok(new { playerId });
                }

                return Microsoft.AspNetCore.Http.Results.NotFound(new { error = "Nickname not found" });
            });

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

    internal record RegisterRequest(string Nickname);
}