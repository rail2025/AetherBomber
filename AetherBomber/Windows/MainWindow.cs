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
    private readonly AudioManager audioManager;
    private readonly TextureManager textureManager;

    private GameSession? gameSession;
    private GameRenderer? gameRenderer;

    public static readonly Vector2 BaseWindowSize = new(720, 540);
    public static Vector2 ScaledWindowSize => BaseWindowSize * ImGuiHelpers.GlobalScale;

    public MainWindow(Plugin plugin, AudioManager audioManager) : base("AetherBomber")
    {
        this.plugin = plugin;
        this.audioManager = audioManager;

        this.textureManager = new TextureManager();
        this.gameRenderer = new GameRenderer(this.textureManager);

        this.Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    }

    public void Dispose()
    {
        this.textureManager.Dispose();
        this.gameRenderer?.Dispose();
    }

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
            gameSession = new GameSession(audioManager);
            gameSession.StartNewGame();
        }

        // Calculate cellSize here so it can be used by both Update and Draw
        var contentSize = ImGui.GetContentRegionAvail();
        float cellSize = Math.Min(contentSize.X / GameBoard.GridWidth, contentSize.Y / GameBoard.GridHeight);

        gameSession.Update(deltaTime, Plugin.GamepadState, cellSize);
        gameRenderer?.Draw(gameSession);
    }
}
