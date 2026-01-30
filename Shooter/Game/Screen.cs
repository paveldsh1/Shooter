using Shooter.Models;
using System;
using System.Text;

namespace Shooter.Game
{
    internal sealed class Window
    {
        public const int MinCols = 40;
        public const int MinRows = 20;
        public const int MaxCols = 300;
        public const int MaxRows = 120;

        private static readonly string[] HelpLinesShown =
        [
            "Controls:",
            "W/A/S/D - move/turn",
            "M - toggle minimap",
            "1 - pistol",
            "2 - shotgun",
            "Space - shoot",
            "Enter - hide help",
            "Esc - exit",
            "[ - zoom out",
            "] - zoom in",
            "0 - reset zoom"
        ];

        private static readonly string[] HelpLinesHidden =
        [
            "Enter - show help"
        ];

        public int ScreenWidth { get; }
        public int ScreenHeight { get; }
        public char[,] Screen { get; private set; }

        public Window(int width = GameConstants.ScreenWidth, int height = GameConstants.ScreenHeight)
        {
            ScreenWidth = Math.Clamp(width, MinCols, MaxCols);
            ScreenHeight = Math.Clamp(height, MinRows, MaxRows);
            Screen = new char[ScreenWidth, ScreenHeight];
            try
            {
                Console.SetWindowSize(ScreenWidth, ScreenHeight);
            }
            catch
            {
                // Игровой сервер может работать без реального консольного окна
            }
        }

        public void Render(
            MiniMap? miniMap = null,
            Player? player = null,
            IReadOnlyCollection<(float X, float Y, float A)>? otherPlayers = null)
        {
            if (miniMap != null)
            {
                AddMiniMapToScreen(miniMap);
                if (otherPlayers != null)
                {
                    foreach (var other in otherPlayers)
                    {
                        AddMiniMapMarker(miniMap, other.X, other.Y, MiniMap.GetDirectionMarker(other.A));
                    }
                }
                if (player != null)
                {
                    AddMiniMapMarker(miniMap, player.PlayerX, player.PlayerY, '.');
                }
            }
            //StringBuilder render = new();
            //for (int y = 0; y < Screen.GetLength(1); y++)
            //{
            //    for (int x = 0; x < Screen.GetLength(0); x++)
            //    {
            //        render.Append(Screen[x, y]);
            //    }
            //    if (y < Screen.GetLength(1) - 1)
            //    {
            //        render.AppendLine();
            //    }
            //}
            //Console.CursorVisible = false;
            //Console.SetCursorPosition(0, 0);
            //string str = render.ToString();
            //Console.Write(render);
        }

        public void DrawName(string nickname, int centerX, int nameY, float distance, float[] columnDepths)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return;
            if (nameY < 0 || nameY >= ScreenHeight) return;

            string label = nickname.Trim();
            int startX = centerX - label.Length / 2;
            for (int i = 0; i < label.Length; i++)
            {
                int x = startX + i;
                if (x < 0 || x >= ScreenWidth) continue;
                if (x < columnDepths.Length && distance < columnDepths[x])
                {
                    Screen[x, nameY] = label[i];
                }
            }
        }

        public void DrawSprite(string[] sprite, int centerX, int bottomY, float scale, char transparent = '!')
        {
            if (sprite == null || sprite.Length == 0) return;
            if (scale <= 0f) return;

            int srcH = sprite.Length;
            int srcW = sprite[0].Length;
            int dstW = Math.Max(1, (int)MathF.Round(srcW * scale));
            int dstH = Math.Max(1, (int)MathF.Round(srcH * scale));

            for (int y = 0; y < dstH; y++)
            {
                int srcY = Math.Min(srcH - 1, (int)(y / scale));
                var row = sprite[srcY];
                for (int x = 0; x < dstW; x++)
                {
                    int srcX = Math.Min(srcW - 1, (int)(x / scale));
                    char ch = row[srcX];
                    if (ch == transparent) continue;

                    int screenX = centerX - dstW / 2 + x;
                    int screenY = bottomY - dstH + 1 + y;
                    if (screenX < 0 || screenX >= ScreenWidth ||
                        screenY < 0 || screenY >= ScreenHeight)
                    {
                        continue;
                    }
                    Screen[screenX, screenY] = ch;
                }
            }
        }

        public void DrawTextLines(IReadOnlyList<string> lines, int startX, int startY)
        {
            if (lines == null || lines.Count == 0) return;

            int y = startY;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.Length == 0)
                {
                    y++;
                    continue;
                }

                if (y < 0)
                {
                    y++;
                    continue;
                }
                if (y >= ScreenHeight) break;

                int x = startX;
                for (int j = 0; j < line.Length; j++)
                {
                    if (x >= 0 && x < ScreenWidth)
                    {
                        Screen[x, y] = line[j];
                    }
                    x++;
                    if (x >= ScreenWidth) break;
                }
                y++;
            }
        }

        public void DrawHelpOverlay(bool visible)
        {
            var lines = visible ? HelpLinesShown : HelpLinesHidden;
            if (lines.Length == 0) return;

            int maxLen = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > maxLen) maxLen = lines[i].Length;
            }

            int startX = Math.Max(0, ScreenWidth - maxLen - 2);
            int startY = 1;
            DrawTextLines(lines, startX, startY);
        }
        
        private void AddMiniMapToScreen(MiniMap miniMap)
        {
            for (int y = 0; y < miniMap.Map.GetLength(0); y++)
            {
                for (int x = 0; x < miniMap.Map[y].Length; x++)
                {
                    Screen[x, y] = miniMap.Map[y][x];
                }
            }
        }

        private void AddMiniMapMarker(MiniMap miniMap, float x, float y, char marker)
        {
            int testX = (int)x;
            int testY = (int)y;

            if (testY < 0 || testY >= miniMap.Map.Length) return;
            if (testX < 0 || testX >= miniMap.Map[testY].Length) return;
            if (MapUtils.IsWallCell(miniMap.Map[testY][testX])) return;

            Screen[testX, testY] = marker;
        }

        public static string ToText(char[,] grid)
        {
            int rows = grid.GetLength(1);
            int cols = grid.GetLength(0);
            var result = new StringBuilder(rows * (cols + 1));
            for(int y = 0; y < rows; ++y)
            {
                for(int x = 0; x < cols; ++x)
                {
                    result.Append(grid[x, y]);
                }
                if (y < rows - 1)
                {
                    result.AppendLine();
                }
            }
            return result.ToString();
        }
    }
}
