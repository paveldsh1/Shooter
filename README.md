# Shooter — ASCII Multiplayer FPS

Real‑time ASCII shooter with a shared map, WebSocket sessions, local bot AI, and optional analytics/state services.

---

## English

### Features
- WebSocket multiplayer with a shared world (`GameHost` + `GameLoopService`)
- ASCII raycasting renderer + minimap
- Dynamic resizing + client zoom
- Bot mode (press `9` to replace players with bots)
- Optional player state persistence via external API
- Optional GameAnalytics events (session start/end, kills)

### Architecture
```mermaid
flowchart LR
  Browser[Browser client<br/>html/index.html]
  Server[Shooter ASP.NET Core]
  Host[GameHost + GameSession]
  Loop[GameLoopService]
  Game[Map / MiniMap / Window]
  State[PlayerState API<br/>(external)]
  GA[GameAnalytics API]

  Browser -- HTTP /players/register, /players/login --> Server
  Browser -- WebSocket /ws --> Server
  Server --> Host
  Server --> Loop
  Host --> Game
  Server -- HTTP --> State
  Server -- HTTP --> GA
```

### Project Structure
```
Shooter/
  Game/            core rendering, map, minimap
  Server/          sessions, host, loop, bots
  Models/          player and snapshot DTOs
  Repositories/    in‑memory player registry
  Services/        analytics + player state API client
  html/            browser client
```

### Run
1. `dotnet run --project Shooter/Shooter.csproj`
2. Open `http://localhost:51350`
3. Enter a nickname and join

### Player State Service (optional)
The game expects an external minimal API for persistence:
- Default URL: `http://localhost:51360`
- Configure in `appsettings.json` under `PlayerStateApi:BaseUrl`

### GameAnalytics (optional)
Set secrets and enable:
```bash
dotnet user-secrets set "GameAnalytics:GameKey" "<YOUR_GAME_KEY>"
dotnet user-secrets set "GameAnalytics:SecretKey" "<YOUR_SECRET_KEY>"
dotnet user-secrets set "GameAnalytics:Enabled" "true"
```
Optional (production endpoint):
```bash
dotnet user-secrets set "GameAnalytics:BaseUrl" "https://api.gameanalytics.com"
```

### Controls
- `W/A/S/D` — move/turn
- `M` — toggle minimap
- `1` — pistol
- `2` — shotgun
- `9` — bot mode
- `Space` — shoot
- `Enter` — toggle help
- `Esc` — exit
- `[` / `]` — zoom out / in
- `0` — reset zoom

---

## Русский

### Возможности
- Мультиплеер через WebSocket, общий мир (`GameHost` + `GameLoopService`)
- ASCII‑рендерер (raycasting) + миникарта
- Динамическое изменение размеров + масштабирование клиента
- Режим ботов (клавиша `9` заменяет игроков ботами)
- Опциональное сохранение состояния через внешний API
- Опциональная интеграция GameAnalytics (сессии/убийства)

### Архитектура
```mermaid
flowchart LR
  Browser[Браузерный клиент<br/>html/index.html]
  Server[Shooter ASP.NET Core]
  Host[GameHost + GameSession]
  Loop[GameLoopService]
  Game[Map / MiniMap / Window]
  State[PlayerState API<br/>(внешний сервис)]
  GA[GameAnalytics API]

  Browser -- HTTP /players/register, /players/login --> Server
  Browser -- WebSocket /ws --> Server
  Server --> Host
  Server --> Loop
  Host --> Game
  Server -- HTTP --> State
  Server -- HTTP --> GA
```

### Структура проекта
```
Shooter/
  Game/            рендеринг, карта, миникарта
  Server/          сессии, хост, игровой цикл, боты
  Models/          модели и снимки игроков
  Repositories/    in‑memory репозиторий игроков
  Services/        аналитика + клиент API состояния
  html/            браузерный клиент
```

### Запуск
1. `dotnet run --project Shooter/Shooter.csproj`
2. Открыть `http://localhost:51350`
3. Ввести ник и войти

### Сервис состояния (опционально)
Для сохранения состояния используется внешний minimal API:
- URL по умолчанию: `http://localhost:51360`
- Настройка в `appsettings.json` → `PlayerStateApi:BaseUrl`

### GameAnalytics (опционально)
Секреты и включение:
```bash
dotnet user-secrets set "GameAnalytics:GameKey" "<YOUR_GAME_KEY>"
dotnet user-secrets set "GameAnalytics:SecretKey" "<YOUR_SECRET_KEY>"
dotnet user-secrets set "GameAnalytics:Enabled" "true"
```
Для продакшена:
```bash
dotnet user-secrets set "GameAnalytics:BaseUrl" "https://api.gameanalytics.com"
```

### Управление
- `W/A/S/D` — движение/поворот
- `M` — миникарта
- `1` — пистолет
- `2` — дробовик
- `9` — режим ботов
- `Space` — выстрел
- `Enter` — скрыть/показать помощь
- `Esc` — выход
- `[` / `]` — масштаб
- `0` — сброс масштаба
