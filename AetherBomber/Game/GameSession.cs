// AetherBomber/Game/GameSession.cs
using AetherBomber.Audio;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Plugin.Services;
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
    private readonly bool isMultiplayer;
    private readonly int localPlayerIndex = -1;

    private float moveCooldown = 0f;
    private const float MoveDelay = 0.15f;
    private bool bombButtonPressedLastFrame = false;

    // Single Player Constructor
    public GameSession(AudioManager audioManager)
    {
        this.audioManager = audioManager;
        this.isMultiplayer = false;
        GameBoard = new GameBoard();

        var player = new Character(CharacterType.Player, Vector2.Zero) { IsLocalPlayer = true };
        Characters.Add(player);
        this.localPlayerIndex = 0;

        var dps = new Character(CharacterType.DPS, Vector2.Zero);
        var healer = new Character(CharacterType.Healer, Vector2.Zero);
        var tank = new Character(CharacterType.Tank, Vector2.Zero);

        dps.AiController = new AIController(dps, this);
        healer.AiController = new AIController(healer, this);
        tank.AiController = new AIController(tank, this);

        Characters.Add(dps);
        Characters.Add(healer);
        Characters.Add(tank);

        
    }

    // Multiplayer Constructor
    public GameSession(AudioManager audioManager, int localPlayerNumber, int totalPlayers)
    {
        this.audioManager = audioManager;
        this.isMultiplayer = true;
        GameBoard = new GameBoard();

        var remoteTypes = new List<CharacterType> { CharacterType.DPS, CharacterType.Healer, CharacterType.Tank };
        int remoteTypeIndex = 0;

        for (int i = 0; i < totalPlayers; i++)
        {
            Character character;
            if (i == localPlayerNumber - 1)
            {
                character = new Character(CharacterType.Player, Vector2.Zero) { IsLocalPlayer = true };
                this.localPlayerIndex = i;
            }
            else
            {
                character = new Character(remoteTypes[remoteTypeIndex % remoteTypes.Count], Vector2.Zero);
                remoteTypeIndex++;
            }
            Characters.Add(character);
        }
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
        var startPositions = new List<Vector2>
        {
            new(1, 1), // NW
            new(GameBoard.GridWidth - 2, 1), // NE
            new(GameBoard.GridWidth - 2, GameBoard.GridHeight - 2), // SE
            new(1, GameBoard.GridHeight - 2) // SW
        };

        for (int i = 0; i < Characters.Count; i++)
        {
            Characters[i].Reset(startPositions[i % startPositions.Count]);
        }

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

            if (!isMultiplayer)
            {
                foreach (var character in Characters)
                {
                    // 1. Brain Update
                    character.AiController?.Update();

                    // 2. Body Update
                    character.ExecuteAIIntent(deltaTime, this);
                }
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
        var targetTile = new GridPos((int)gridPos.X, (int)gridPos.Y);
        var character = Characters.FirstOrDefault(c => c.IsActive && c.GridPosition == targetTile);
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

            if (bomb.IsExploding && bomb.ExplosionPath.Count == 0)
            {
                var path = CalculateExplosionPath(bomb);
                bomb.SetExplosionPath(path);

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
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(0, 0)));
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(1, 0)));
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(-1, 0)));
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(0, 1)));
        path.UnionWith(CalculateRay(bomb.GridPos, new Vector2(0, -1)));
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

        if (localPlayerIndex < 0 || localPlayerIndex >= Characters.Count) return;
        var player = Characters[localPlayerIndex];
        if (!player.IsActive) return;

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
