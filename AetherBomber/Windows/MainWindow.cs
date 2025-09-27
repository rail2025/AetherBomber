// AetherBomber/Windows/MainWindow.cs
using System;
using System.Linq;
using System.Numerics;
using AetherBomber.Audio;
using AetherBomber.Game;
using AetherBomber.UI;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.GamePad;

namespace AetherBomber.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly TextureManager textureManager;
    private readonly AudioManager audioManager;

    private GameSession? gameSession;
    private bool isMultiplayerMode = false; // Add this line if you plan multiplayer

    public static readonly Vector2 BaseWindowSize = new(720, 540);
    public static Vector2 ScaledWindowSize => BaseWindowSize * ImGuiHelpers.GlobalScale;

    // --- Colors ---
    private readonly uint blockColor = ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.35f, 1.0f));
    private readonly uint explosionColor = ImGui.GetColorU32(new Vector4(1.0f, 0.2f, 0.1f, 1.0f));

    public MainWindow(Plugin plugin, AudioManager audioManager) : base("AetherBomber")
    {
        this.plugin = plugin;
        this.audioManager = audioManager;
        this.textureManager = new TextureManager();
        this.Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    }

    public void StartSinglePlayerGame()
    {
        this.gameSession = new GameSession(plugin.Configuration, audioManager);
        this.gameSession.StartNewGame();
    }

    public void Dispose() => this.textureManager.Dispose();

    public override void OnClose()
    {
        this.audioManager.EndPlaylist();
        this.gameSession = null;
        base.OnClose();
    }

    public override void PreDraw()
    {
        this.Size = ScaledWindowSize;
        if (this.plugin.Configuration.IsGameWindowLocked) this.Flags |= ImGuiWindowFlags.NoMove;
        else this.Flags &= ~ImGuiWindowFlags.NoMove;
    }

    public override void Draw()
    {
        var deltaTime = ImGui.GetIO().DeltaTime;

        if (gameSession == null)
        {
            // For now, we will just start the game. The main menu UI can be re-implemented later.
            // You can replace this with UIManager.DrawMainMenu(...) if you re-add that functionality.
            StartSinglePlayerGame();
            return;
        }

        // The game session is now responsible for updating itself
        gameSession.Update(deltaTime, Plugin.GamepadState);
        DrawInGame(gameSession);
    }

    private void DrawInGame(GameSession session)
    {
        var drawList = ImGui.GetWindowDrawList();
        var contentMin = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
        var contentSize = ImGui.GetContentRegionAvail();

        float cellSize = Math.Min(contentSize.X / GameBoard.GridWidth, contentSize.Y / GameBoard.GridHeight);
        var gridPixelSize = new Vector2(GameBoard.GridWidth * cellSize, GameBoard.GridHeight * cellSize);
        var gridOrigin = contentMin + (contentSize - gridPixelSize) / 2;

        DrawGrid(drawList, gridOrigin, cellSize, session.GameBoard);
        DrawBombsAndExplosions(drawList, gridOrigin, cellSize, session.ActiveBombs);
        DrawPlayer(drawList, gridOrigin, cellSize, session.Player);
    }

    private void DrawGrid(ImDrawListPtr drawList, Vector2 gridOrigin, float cellSize, GameBoard gameBoard)
    {
        for (int y = 0; y < GameBoard.GridHeight; y++)
        {
            for (int x = 0; x < GameBoard.GridWidth; x++)
            {
                if (gameBoard.GetTile(x, y) == 1) // 1 represents a solid block
                {
                    var cellPos = gridOrigin + new Vector2(x * cellSize, y * cellSize);
                    drawList.AddRectFilled(cellPos, cellPos + new Vector2(cellSize, cellSize), this.blockColor);
                }
            }
        }
    }

    private void DrawBombsAndExplosions(ImDrawListPtr drawList, Vector2 gridOrigin, float cellSize, System.Collections.Generic.List<Bomb> bombs)
    {
        foreach (var bomb in bombs)
        {
            var bombPixelPos = gridOrigin + (bomb.GridPos * cellSize) + new Vector2(cellSize / 2);

            if (bomb.IsExploding)
            {
                float explosionProgress = 1.0f - (bomb.ExplosionTimer / 0.5f);
                float explosionRadius = cellSize / 2 * explosionProgress;

                // Draw center and cross-shaped explosion
                DrawExplosionCell(drawList, gridOrigin, cellSize, (int)bomb.GridPos.X, (int)bomb.GridPos.Y, explosionRadius);
                for (int i = 1; i <= 3; i++) // Explosion range of 3 tiles
                {
                    DrawExplosionCell(drawList, gridOrigin, cellSize, (int)bomb.GridPos.X + i, (int)bomb.GridPos.Y, explosionRadius);
                    DrawExplosionCell(drawList, gridOrigin, cellSize, (int)bomb.GridPos.X - i, (int)bomb.GridPos.Y, explosionRadius);
                    DrawExplosionCell(drawList, gridOrigin, cellSize, (int)bomb.GridPos.X, (int)bomb.GridPos.Y + i, explosionRadius);
                    DrawExplosionCell(drawList, gridOrigin, cellSize, (int)bomb.GridPos.X, (int)bomb.GridPos.Y - i, explosionRadius);
                }
            }
            else
            {
                // Draw flashing bomb outline
                drawList.AddCircle(bombPixelPos, cellSize * 0.45f, bomb.GetOutlineColor(), 12, 2.0f);
            }
        }
    }

    private void DrawExplosionCell(ImDrawListPtr drawList, Vector2 gridOrigin, float cellSize, int x, int y, float radius)
    {
        if (gameSession != null && gameSession.GameBoard.IsWalkable(new Vector2(x, y)))
        {
            var cellCenter = gridOrigin + new Vector2(x * cellSize, y * cellSize) + new Vector2(cellSize / 2);
            drawList.AddCircleFilled(cellCenter, radius, this.explosionColor);
        }
    }

    private void DrawPlayer(ImDrawListPtr drawList, Vector2 gridOrigin, float cellSize, Player player)
    {
        var playerPixelPos = gridOrigin + (player.GridPos * cellSize) + new Vector2(cellSize / 2);
        drawList.AddCircleFilled(playerPixelPos, cellSize * 0.4f, ImGui.GetColorU32(Player.DefaultColor));
    }
}
