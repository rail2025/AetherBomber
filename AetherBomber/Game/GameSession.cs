// AetherBomber/Game/GameSession.cs
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using AetherBomber.Audio;
using AetherBomber.Networking;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Plugin.Services;

namespace AetherBomber.Game;

public class GameSession
{
    public GameState CurrentGameState { get; private set; }
    public GameBoard GameBoard { get; }
    public Player Player { get; }
    public List<Bomb> ActiveBombs { get; } = new();

    private readonly Configuration configuration;
    private readonly AudioManager audioManager;

    // Controller Input Handling
    private float moveCooldown = 0f;
    private const float MoveDelay = 0.15f;
    private bool bombButtonPressedLastFrame = false;

    public GameSession(Configuration configuration, AudioManager audioManager, NetworkManager? networkManager = null)
    {
        this.configuration = configuration;
        this.audioManager = audioManager;
        this.CurrentGameState = GameState.MainMenu;

        GameBoard = new GameBoard();
        Player = new Player(new Vector2(1, 1)); // Starting position
    }

    public void StartNewGame()
    {
        // Reset player, bombs, score etc. here
        Player.GridPos = new Vector2(1, 1);
        ActiveBombs.Clear();

        this.audioManager.StartBgmPlaylist();
        this.CurrentGameState = GameState.InGame;
    }

    public void Update(float deltaTime, IGamepadState? gamepadState)
    {
        if (this.CurrentGameState != GameState.InGame) return;

        HandleInput(gamepadState);
        UpdateBombs(deltaTime);
    }

    private void UpdateBombs(float deltaTime)
    {
        for (int i = this.ActiveBombs.Count - 1; i >= 0; i--)
        {
            this.ActiveBombs[i].Update(deltaTime);
            if (this.ActiveBombs[i].IsFinished())
            {
                // Logic for when an explosion finishes can go here
                this.ActiveBombs.RemoveAt(i);
            }
        }
    }

    private void HandleInput(IGamepadState? gamepadState)
    {
        if (gamepadState == null) return;

        // Player Movement
        if (this.moveCooldown > 0) this.moveCooldown -= Dalamud.Bindings.ImGui.ImGui.GetIO().DeltaTime;
        if (this.moveCooldown <= 0)
        {
            var moveDirection = Vector2.Zero;
            if (gamepadState.Raw(GamepadButtons.DpadUp) > 0) moveDirection.Y = -1;
            else if (gamepadState.Raw(GamepadButtons.DpadDown) > 0) moveDirection.Y = 1;
            else if (gamepadState.Raw(GamepadButtons.DpadLeft) > 0) moveDirection.X = -1;
            else if (gamepadState.Raw(GamepadButtons.DpadRight) > 0) moveDirection.X = 1;

            var leftStick = gamepadState.LeftStick;
            if (Math.Abs(leftStick.X) > 0.8f) moveDirection = new Vector2(Math.Sign(leftStick.X), 0);
            else if (Math.Abs(leftStick.Y) > 0.8f) moveDirection = new Vector2(0, -Math.Sign(leftStick.Y));

            if (moveDirection != Vector2.Zero)
            {
                var targetPos = this.Player.GridPos + moveDirection;
                if (GameBoard.IsWalkable(targetPos))
                {
                    this.Player.GridPos = targetPos;
                }
                this.moveCooldown = MoveDelay;
            }
        }

        // Place Bomb
        bool isBombButtonPressed = gamepadState.Raw(GamepadButtons.West) > 0; // Square button on PS
        if (isBombButtonPressed && !this.bombButtonPressedLastFrame)
        {
            // Prevent placing bombs on top of each other
            bool bombExists = ActiveBombs.Any(b => b.GridPos == Player.GridPos);
            if (!bombExists)
            {
                this.ActiveBombs.Add(new Bomb(this.Player.GridPos));
            }
        }
        this.bombButtonPressedLastFrame = isBombButtonPressed;
    }
}
