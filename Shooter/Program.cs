using Microsoft.Extensions.Options;
using Shooter.Models;
using Shooter.Repositories;
using Shooter.Server;
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
            builder.Services.AddSingleton<GameHost>();
            builder.Services.AddHostedService<Shooter.Server.GameLoopService>();
            builder.Services.AddHttpClient<PlayerStateApiClient>(client =>
            {
                var baseUrl = builder.Configuration["PlayerStateApi:BaseUrl"] ?? "http://localhost:51360";
                client.BaseAddress = new Uri(baseUrl);
            });
            builder.Services.Configure<GameAnalyticsOptions>(builder.Configuration.GetSection("GameAnalytics"));
            builder.Services.AddHttpClient<GameAnalyticsClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<GameAnalyticsOptions>>().Value;
                var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                    ? "https://sandbox-api.gameanalytics.com"
                    : options.BaseUrl;
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
            builder.Services.AddSingleton<GameAnalyticsService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<GameAnalyticsService>());
            var app = builder.Build();

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
            app.Map("/ws", async (HttpContext context, PlayersRepository repository, GameHost host, PlayerStateApiClient stateClient) =>
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
                Player? player = repository.TryGetPlayer(nickname);
                if (player is null)
                {
                    context.Response.StatusCode = 404;
                    return;
                }
                // load persisted state
                var state = await stateClient.LoadAsync(nickname);
                if (state is not null && host.IsWalkable(state.PlayerX, state.PlayerY))
                {
                    player.SetState(state.PlayerX, state.PlayerY, state.PlayerA);
                }
                else if (host.TryGetRandomSpawn(out var spawnX, out var spawnY))
                {
                    player.SetState(spawnX, spawnY, player.PlayerA);
                }

                var socket = await context.WebSockets.AcceptWebSocketAsync();
                try
                {
                    Console.WriteLine("Client connected");
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.InputEncoding = Encoding.UTF8;

                    await host.RunSessionAsync(socket, player);
                }
                finally
                {
                    socket?.Dispose();
                }
            });

            app.Run();
        }
    }

    internal record RegisterRequest(string nickname);
}
