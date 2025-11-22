// AetherBomber/UI/GameRenderer.cs
using AetherBomber.Game;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AetherBomber.UI;

public class GameRenderer : IDisposable
{
    private readonly TextureManager textureManager;

    private readonly uint blockColor = ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.35f, 1.0f));
    private readonly uint explosionColor = ImGui.GetColorU32(new Vector4(1.0f, 0.2f, 0.1f, 1.0f));
    private readonly uint characterOutlineColor = ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

    public GameRenderer(TextureManager textureManager)
    {
        this.textureManager = textureManager;
    }

    public void Draw(GameSession session)
    {
        var drawList = ImGui.GetWindowDrawList();
        var contentMin = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
        var contentSize = ImGui.GetContentRegionAvail();

        float scoreboardHeight = 50 * ImGuiHelpers.GlobalScale;
        float gameAreaHeight = contentSize.Y - scoreboardHeight;

        float cellSize = Math.Min(contentSize.X / GameBoard.GridWidth, gameAreaHeight / GameBoard.GridHeight);
        var gridPixelSize = new Vector2(GameBoard.GridWidth * cellSize, GameBoard.GridHeight * cellSize);
        var gridOrigin = contentMin + (contentSize - gridPixelSize - new Vector2(0, scoreboardHeight)) / 2;

        DrawGameUI(drawList, contentMin, contentSize, session);
        DrawGrid(drawList, gridOrigin, cellSize, session.GameBoard);
        DrawBombsAndExplosions(drawList, gridOrigin, cellSize, session);
        DrawCharacters(drawList, gridOrigin, cellSize, session.Characters);
        DrawScoreboard(drawList, contentMin, contentSize, session.Characters);

        if (session.CurrentRoundState == RoundState.Countdown)
        {
            DrawCountdown(drawList, contentMin, contentSize, session.StartCountdownTimer);
        }
    }

    private void DrawGameUI(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentSize, GameSession session)
    {
        var time = TimeSpan.FromSeconds(session.StageTimer < 0 ? 0 : session.StageTimer);
        string timerText = $"{time.Minutes:D2}:{time.Seconds:D2}";
        drawList.AddText(contentMin + new Vector2(10, 10), 0xFFFFFFFF, timerText);

        string restartText = "Restart";
        var buttonSize = ImGui.CalcTextSize(restartText) + (ImGui.GetStyle().FramePadding * 2);
        var buttonPos = contentMin + new Vector2(contentSize.X - buttonSize.X - 10, 10);

        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.Button(restartText))
        {
            session.RestartRound();
        }
    }

    private void DrawCountdown(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentSize, float countdown)
    {
        string text = Math.Ceiling(countdown).ToString("F0");
        if (countdown <= 1 && countdown > 0) text = "START!";
        else if (countdown <= 0) text = "";

        if (string.IsNullOrEmpty(text)) return;

        var fontSize = 80 * ImGuiHelpers.GlobalScale;
        var textSize = ImGui.CalcTextSize(text) * (fontSize / ImGui.GetFontSize());
        var textPos = contentMin + (contentSize - textSize - new Vector2(0, 50 * ImGuiHelpers.GlobalScale)) / 2;

        drawList.AddText(ImGui.GetFont(), fontSize, textPos + new Vector2(2, 2), 0xFF000000, text);
        drawList.AddText(ImGui.GetFont(), fontSize, textPos, 0xFFFFFFFF, text);
    }

    private void DrawGrid(ImDrawListPtr drawList, Vector2 gridOrigin, float cellSize, GameBoard gameBoard)
    {
        var chestTexture = textureManager.GetTexture("chest");
        var mirrorTexture = textureManager.GetTexture("mirror");

        for (int y = 0; y < GameBoard.GridHeight; y++)
        {
            for (int x = 0; x < GameBoard.GridWidth; x++)
            {
                var tile = gameBoard.GetTile(x, y);
                var cellPos = gridOrigin + new Vector2(x * cellSize, y * cellSize);

                if (tile.Type == TileType.Wall)
                {
                    drawList.AddRectFilled(cellPos, cellPos + new Vector2(cellSize, cellSize), this.blockColor);
                }
                else if (tile.Type == TileType.Destructible)
                {
                    var texture = (x + y) % 2 == 0 ? chestTexture : mirrorTexture;
                    if (texture != null)
                    {
                        drawList.AddImage(texture.Handle, cellPos, cellPos + new Vector2(cellSize, cellSize));
                    }
                }
            }
        }
    }

    private void DrawBombsAndExplosions(ImDrawListPtr drawList, Vector2 gridOrigin, float cellSize, GameSession session)
    {
        // Draw all bombs and explosions based on the current state
        foreach (var bomb in session.ActiveBombs)
        {
            if (bomb.IsExploding)
            {
                float explosionProgress = 1.0f - (bomb.ExplosionTimer / 0.5f);
                float explosionRadius = cellSize / 2 * explosionProgress;

                foreach (var tilePos in bomb.ExplosionPath)
                {
                    var cellCenter = gridOrigin + new Vector2(tilePos.X * cellSize, tilePos.Y * cellSize) + new Vector2(cellSize / 2);
                    drawList.AddCircleFilled(cellCenter, explosionRadius, this.explosionColor);
                }
            }
            else
            {
                var bombCellOrigin = gridOrigin + (bomb.GridPos * cellSize);
                var bombCenter = bombCellOrigin + new Vector2(cellSize / 2);
                var bombTexture = this.textureManager.GetTexture("bomb");
                if (bombTexture != null)
                {
                    drawList.AddImage(bombTexture.Handle, bombCellOrigin, bombCellOrigin + new Vector2(cellSize, cellSize));
                }
                drawList.AddCircle(bombCenter, cellSize * 0.45f, bomb.GetOutlineColor(), 12, 2.0f);
            }
        }
    }

    // This is now purely for calculation, not drawing.
    private HashSet<Vector2> GetExplosionPath(Bomb bomb, GameSession session)
    {
        var path = new HashSet<Vector2>();

        // Calculate each ray's path and add it to the total path
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(0, 0), session)); // Center
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(1, 0), session));  // Right
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(-1, 0), session)); // Left
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(0, 1), session));   // Down
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(0, -1), session));  // Up

        return path;
    }

    private IEnumerable<Vector2> CalculateRay(Vector2 startPos, Vector2 direction, GameSession session)
    {
        var rayPath = new List<Vector2>();
        for (int i = 0; i <= 3; i++)
        {
            if (direction != Vector2.Zero && i == 0) continue;

            var currentGridPos = startPos + direction * i;
            var tile = session.GameBoard.GetTile((int)currentGridPos.X, (int)currentGridPos.Y);

            Plugin.Log.Debug($"Ray Dir({direction.X},{direction.Y}), Step {i}: Checking ({currentGridPos.X}, {currentGridPos.Y}). Found TileType: {tile.Type}");

            if (tile.Type == TileType.Wall)
            {
                Plugin.Log.Debug(" -> Hit Wall. Stopping ray.");
                break;
            }

            rayPath.Add(currentGridPos);

            if (tile.Type == TileType.Destructible)
            {
                Plugin.Log.Debug(" -> Hit Destructible. Stopping ray after this tile.");
                break;
            }
        }
        return rayPath;
    }

    private void DrawCharacters(ImDrawListPtr drawList, Vector2 gridOrigin, float cellSize, List<Character> characters)
    {
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();

        foreach (var character in characters)
        {
            if (!character.IsActive && !character.IsBeingYeeted) continue;

            var size = character.GetRenderScale(cellSize);
            var center = character.GetRenderPosition(gridOrigin, cellSize) + new Vector2(size / 2, size / 2);
            var radius = size / 2;

            if (center.X < windowPos.X - size || center.X > windowPos.X + windowSize.X + size ||
                center.Y < windowPos.Y - size || center.Y > windowPos.Y + windowSize.Y + size)
            {
                continue;
            }


            IDalamudTextureWrap? texture = character.Type switch
            {
                CharacterType.Player => textureManager.GetTexture("bird"),
                CharacterType.Tank => textureManager.GetTexture("tank"),
                CharacterType.Healer => textureManager.GetTexture("healer"),
                CharacterType.DPS => textureManager.GetTexture("dps"),
                _ => null
            };

            drawList.AddCircleFilled(center, radius, characterOutlineColor);

            if (texture != null && texture.Handle != IntPtr.Zero)
            {
                drawList.AddImageRounded(texture.Handle,
                                  center - new Vector2(radius),
                                  center + new Vector2(radius),
                                  Vector2.Zero, Vector2.One,
                                  ImGui.GetColorU32(new Vector4(1, 1, 1, 1)),
                                  radius
                );
            }
        }
    }

    private void DrawScoreboard(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentSize, List<Character> characters)
    {
        float scoreboardHeight = 50 * ImGuiHelpers.GlobalScale;
        Vector2 scoreboardPos = new Vector2(contentMin.X, contentMin.Y + contentSize.Y - scoreboardHeight);
        Vector2 scoreboardSize = new Vector2(contentSize.X, scoreboardHeight);

        drawList.AddRectFilled(scoreboardPos, scoreboardPos + scoreboardSize, 0x80000000);

        float iconSize = scoreboardHeight * 0.8f;
        float padding = (scoreboardHeight - iconSize) / 2;
        float sectionWidth = contentSize.X / 4;

        for (int i = 0; i < characters.Count; i++)
        {
            var character = characters[i];
            if (character == null) continue;
            var sectionStart = scoreboardPos.X + (i * sectionWidth);

            var iconCenter = new Vector2(sectionStart + padding + iconSize / 2, scoreboardPos.Y + padding + iconSize / 2);
            var iconRadius = iconSize / 2;

            IDalamudTextureWrap? texture = character.Type switch
            {
                CharacterType.Player => textureManager.GetTexture("bird"),
                CharacterType.Tank => textureManager.GetTexture("tank"),
                CharacterType.Healer => textureManager.GetTexture("healer"),
                CharacterType.DPS => textureManager.GetTexture("dps"),
                _ => null
            };

            if (texture != null && texture.Handle != IntPtr.Zero)
            {
                drawList.AddCircleFilled(iconCenter, iconRadius, characterOutlineColor);
                drawList.AddImageRounded(texture.Handle,
                    iconCenter - new Vector2(iconRadius),
                    iconCenter + new Vector2(iconRadius),
                    Vector2.Zero, Vector2.One,
                    ImGui.GetColorU32(new Vector4(1, 1, 1, 1)),
                    iconRadius);
            }

            string scoreText = character.Score.ToString();
            Vector2 textSize = ImGui.CalcTextSize(scoreText);
            Vector2 textPos = new Vector2(sectionStart + padding + iconSize + padding, scoreboardPos.Y + (scoreboardHeight - textSize.Y) / 2);

            var scoreColor = character.IsActive ? 0xFFFFFFFF : 0xFF808080;
            drawList.AddText(textPos, scoreColor, scoreText);
        }
    }

    public void Dispose() { }
}
