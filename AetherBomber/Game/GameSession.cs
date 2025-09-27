// AetherBomber/Game/GameSession.cs
using AetherBomber.Audio;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AetherBomber.Game;

public enum RoundState
{
    Countdown,
    InProgress,
    RoundOver
}

public class GameSession
{
    public GameBoard GameBoard { get; private set; }
    public List<Character> Characters { get; } = new();
    public List<Bomb> ActiveBombs { get; } = new();
    public RoundState CurrentRoundState { get; private set; }
    public float StageTimer { get; private set; }
    public float StartCountdownTimer { get; private set; }

    private readonly AudioManager audioManager;
    private float moveCooldown = 0f;
    private const float MoveDelay = 0.15f;
    private bool bombButtonPressedLastFrame = false;

    public GameSession(AudioManager audioManager)
    {
        this.audioManager = audioManager;
        GameBoard = new GameBoard();

        Characters.Add(new Character(CharacterType.Player, Vector2.Zero));
        var dps = new Character(CharacterType.DPS, Vector2.Zero);
        var healer = new Character(CharacterType.Healer, Vector2.Zero);
        var tank = new Character(CharacterType.Tank, Vector2.Zero);

        // Assign AI controllers to non-player characters
        dps.AiController = new AIController(dps, this);
        healer.AiController = new AIController(healer, this);
        tank.AiController = new AIController(tank, this);

        Characters.Add(dps);
        Characters.Add(healer);
        Characters.Add(tank);
    }

    public void StartNewGame()
    {
        foreach (var character in Characters) character.Score = 0;
        StartRound();
        this.audioManager.StartBgmPlaylist();
    }

    public void RestartRound()
    {
        GameBoard = new GameBoard(); // Create new random layout
        StartRound();
    }

    private void StartRound()
    {
        Characters[0].Reset(new Vector2(1, 1)); // Player
        Characters[1].Reset(new Vector2(GameBoard.GridWidth - 2, 1)); // DPS
        Characters[2].Reset(new Vector2(GameBoard.GridWidth - 2, GameBoard.GridHeight - 2)); // Healer
        Characters[3].Reset(new Vector2(1, GameBoard.GridHeight - 2)); // Tank

        ActiveBombs.Clear();
        StageTimer = 120.0f;
        StartCountdownTimer = 5.0f;
        CurrentRoundState = RoundState.Countdown;
    }

    public void Update(float deltaTime, IGamepadState? gamepadState, float cellSize)
    {
        if (CurrentRoundState == RoundState.Countdown)
        {
            StartCountdownTimer -= deltaTime;
            if (StartCountdownTimer <= 0)
            {
                CurrentRoundState = RoundState.InProgress;
            }
        }
        else if (CurrentRoundState == RoundState.InProgress)
        {
            HandleInput(gamepadState);
            StageTimer -= deltaTime;

            foreach (var character in Characters)
            {
                character.AiController?.Update(deltaTime);
            }

            var activeCharacters = Characters.Count(c => c.IsActive);
            if (activeCharacters <= 1)
            {
                var winner = Characters.FirstOrDefault(c => c.IsActive);
                if (winner != null)
                {
                    winner.Score++;
                }
                CurrentRoundState = RoundState.RoundOver;
            }
            if (StageTimer <= 0)
            {
                CurrentRoundState = RoundState.RoundOver;
            }
        }

        UpdateBombs(deltaTime);
        foreach (var character in Characters)
        {
            character.UpdateAnimation(deltaTime, cellSize);
        }
    }

    public void HitCharacterAt(Vector2 gridPos, Bomb sourceBomb)
    {
        var character = Characters.FirstOrDefault(c => c.IsActive && c.GridPos == gridPos);
        if (character != null)
        {
            character.TriggerYeet(sourceBomb.GridPos);
            // Award point if it's not suicide
            if (character != sourceBomb.Owner)
            {
                sourceBomb.Owner.Score++;
            }
        }
    }

    public bool IsTileWalkable(Vector2 gridPos)
    {
        var tile = GameBoard.GetTile((int)gridPos.X, (int)gridPos.Y);
        if (tile.Type != TileType.Empty) return false;

        if (Characters.Any(c => c.IsActive && c.GridPos == gridPos)) return false;

        return true;
    }

    private void UpdateBombs(float deltaTime)
    {
        for (int i = ActiveBombs.Count - 1; i >= 0; i--)
        {
            var bomb = ActiveBombs[i];
            bomb.Update(deltaTime);

            // If the bomb just started exploding this frame, calculate its path and handle hits
            if (bomb.IsExploding && bomb.ExplosionPath.Count == 0)
            {
                var path = CalculateExplosionPath(bomb);
                bomb.SetExplosionPath(path);

                // Handle hits and tile destruction
                foreach (var tilePos in path)
                {
                    HitCharacterAt(tilePos, bomb);
                    GameBoard.DestroyTile((int)tilePos.X, (int)tilePos.Y);
                }
            }

            if (bomb.IsFinished())
            {
                ActiveBombs.RemoveAt(i);
            }
        }
    }

    public HashSet<Vector2> CalculateExplosionPath(Bomb bomb)
    {
        var path = new HashSet<Vector2>();
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(0, 0))); // Center
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(1, 0)));  // Right
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(-1, 0))); // Left
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(0, 1)));   // Down
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(0, -1)));  // Up
        return path;
    }

    private IEnumerable<Vector2> CalculateRay(Vector2 startPos, Vector2 direction)
    {
        var rayPath = new List<Vector2>();
        for (int i = 0; i <= 3; i++)
        {
            if (direction != Vector2.Zero && i == 0) continue;

            var currentGridPos = startPos + direction * i;
            var tile = GameBoard.GetTile((int)currentGridPos.X, (int)currentGridPos.Y);

            if (tile.Type == TileType.Wall) break;

            rayPath.Add(currentGridPos);

            if (tile.Type == TileType.Destructible) break;
        }
        return rayPath;
    }

    private void HandleInput(IGamepadState? gamepadState)
    {
        if (gamepadState == null || CurrentRoundState != RoundState.InProgress) return;

        var player = Characters.FirstOrDefault(c => c.Type == CharacterType.Player);
        if (player == null || !player.IsActive) return;

        if (this.moveCooldown > 0) this.moveCooldown -= Dalamud.Bindings.ImGui.ImGui.GetIO().DeltaTime;
        if (this.moveCooldown <= 0)
        {
            var moveDirection = Vector2.Zero;
            if (gamepadState.Raw(GamepadButtons.DpadUp) > 0) moveDirection.Y = -1;
            else if (gamepadState.Raw(GamepadButtons.DpadDown) > 0) moveDirection.Y = 1;
            else if (gamepadState.Raw(GamepadButtons.DpadLeft) > 0) moveDirection.X = -1;
            else if (gamepadState.Raw(GamepadButtons.DpadRight) > 0) moveDirection.X = 1;

            if (moveDirection != Vector2.Zero)
            {
                var targetPos = player.GridPos + moveDirection;
                if (IsTileWalkable(targetPos))
                {
                    player.GridPos = targetPos;
                }
                this.moveCooldown = MoveDelay;
            }
        }

        bool isBombButtonPressed = gamepadState.Raw(GamepadButtons.West) > 0;
        if (isBombButtonPressed && !this.bombButtonPressedLastFrame)
        {
            bool bombExists = ActiveBombs.Any(b => b.GridPos == player.GridPos);
            if (!bombExists)
            {
                this.ActiveBombs.Add(new Bomb(player.GridPos, player));
            }
        }
        this.bombButtonPressedLastFrame = isBombButtonPressed;
    }
}
