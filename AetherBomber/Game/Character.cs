// AetherBomber/Game/Character.cs
using System.Numerics;

namespace AetherBomber.Game;

public enum CharacterType
{
    Player,
    Tank,
    Healer,
    DPS
}

public class Character
{
    public CharacterType Type { get; }
    public Vector2 GridPos { get; set; }
    public Vector2 PixelPos { get; private set; }
    public bool IsActive { get; set; } = true;
    public bool IsBeingYeeted { get; private set; } = false;
    public int Score { get; set; } = 0;
    public AIController? AiController { get; set; }

    // Animation State
    private Vector2 yeetVelocity;
    private float yeetScale = 1.0f;
    private float yeetTimer = 0.0f;
    private const float YeetDuration = 0.75f;
    private const float YeetSpeed = 400.0f;
    private const float YeetGrowthRate = 1.0f;

    public Character(CharacterType type, Vector2 startPosition)
    {
        Type = type;
        GridPos = startPosition;
    }

    public void Reset(Vector2 startPosition)
    {
        GridPos = startPosition;
        IsActive = true;
        IsBeingYeeted = false;
        yeetScale = 1.0f;
        yeetTimer = 0.0f;
    }

    public void TriggerYeet(Vector2 explosionOrigin)
    {
        if (IsBeingYeeted || !IsActive) return;

        IsBeingYeeted = true;
        yeetTimer = YeetDuration;

        var direction = Vector2.Normalize(GridPos - explosionOrigin);
        if (direction == Vector2.Zero)
        {
            direction = new Vector2(1, 0);
        }
        yeetVelocity = direction * YeetSpeed;
    }

    public void UpdateAnimation(float deltaTime, float cellSize)
    {
        PixelPos = GridPos * cellSize;

        if (IsBeingYeeted)
        {
            yeetTimer -= deltaTime;
            if (yeetTimer <= 0)
            {
                IsBeingYeeted = false;
                IsActive = false;
            }
            else
            {
                PixelPos += yeetVelocity * (YeetDuration - yeetTimer);
                yeetScale += YeetGrowthRate * deltaTime;
            }
        }
    }

    public Vector2 GetRenderPosition(Vector2 gridOrigin, float cellSize)
    {
        if (IsBeingYeeted)
        {
            return gridOrigin + PixelPos - new Vector2(cellSize / 2);
        }
        return gridOrigin + (GridPos * cellSize);
    }

    public float GetRenderScale(float cellSize)
    {
        return IsBeingYeeted ? cellSize * yeetScale : cellSize;
    }
}
