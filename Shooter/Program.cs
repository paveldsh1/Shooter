using Shooter.Models;
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
            builder.Services.AddSingleton<PlayersRepository>();
            var app = builder.Build();

            // Включаем WebSocket
            app.UseWebSockets();

            app.MapPost("/players/register", async (HttpContext context, PlayersRepository repository) =>
            {
                var payload = await context.Request.ReadFromJsonAsync<RegisterRequest>();
                if (payload is null || string.IsNullOrWhiteSpace(payload.nickname))
                {
                    return Microsoft.AspNetCore.Http.Results.BadRequest(new { error = "Nickname is required" });
                }

                var player = new Player(payload.nickname);
                if (!repository.TryAddPlayer(payload.nickname, player))
                    return Microsoft.AspNetCore.Http.Results.Conflict(new { error = "Nickname already exists" });

                return Microsoft.AspNetCore.Http.Results.Ok(new { });
            });

            app.MapPost("/players/login", async (HttpContext context, PlayersRepository repository) =>
            {
                var payload = await context.Request.ReadFromJsonAsync<RegisterRequest>();
                if (payload is null || string.IsNullOrWhiteSpace(payload.nickname))
                {
                    return Microsoft.AspNetCore.Http.Results.BadRequest(new { error = "Nickname is required" });
                }

                var existing = repository.Exists(payload.nickname);
                if (existing)
                {
                    return Microsoft.AspNetCore.Http.Results.Conflict(new { message = "Nickname already exists" });
                }

                var created = new Shooter.Models.Player(payload.nickname);
                if (!repository.TryAddPlayer(payload.nickname, created))
                {
                    // На случай гонки: если кто-то успел создать между проверкой и добавлением
                    return Microsoft.AspNetCore.Http.Results.Conflict(new { message = "Nickname already exists" });
                }

                return Microsoft.AspNetCore.Http.Results.Ok(new { message = "Player created" });
            });

            // HTML страница
            app.MapGet("/", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";

                await context.Response.SendFileAsync("html/index.html");
            });

            // WebSocket endpoint
            app.Map("/ws", async (HttpContext context, PlayersRepository repository) =>
            {
                string nickname = context.Request.Query["nick"].ToString();
                if (string.IsNullOrWhiteSpace(nickname)) 
                { 
                    context.Response.StatusCode = 400; 
                    return; 
                }

                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                // Проверяем, что игрок существует до приёма WebSocket
                var existing = repository.TryGetPlayer(nickname);
                if (existing is null)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine("Client connected");

                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;

                Game game = new Game();
                await game.Start(socket, existing);
            });

            app.Run();
        }
    }

    internal record RegisterRequest(string nickname);
}