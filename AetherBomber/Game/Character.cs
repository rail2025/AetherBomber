using System;
using System.Numerics;
using System.Linq;
namespace AetherBomber.Game;

public enum CharacterType { Player, Tank, Healer, DPS }

public class Character
{
    public CharacterType Type { get; }
    public Vector2 GridPos { get; set; }

    // --- Bridge to Integer Grid ---
    public GridPos GridPosition => new GridPos((int)MathF.Round(GridPos.X), (int)MathF.Round(GridPos.Y));

    public Vector2 PixelPos { get; private set; }
    public bool IsActive { get; set; } = true;
    public bool IsLocalPlayer { get; set; } = false;
    public bool IsBeingYeeted { get; private set; } = false;
    public int Score { get; set; } = 0;

    public AIController? AiController { get; set; }

    // --- AI Intent Queues ---
    private readonly System.Collections.Generic.Queue<GridPos> moveQueue = new();
    private bool bombQueued = false;

    public bool HasQueuedActions => moveQueue.Count > 0 || bombQueued;

    public void QueueMove(GridPos target) { this.moveQueue.Enqueue(target); }
    public void QueueBombPlacement() { this.bombQueued = true; }

    public void ClearQueue() { this.moveQueue.Clear(); this.bombQueued = false; }
    // ----------------------------

    // Animation State
    private Vector2 yeetVelocity = Vector2.Zero;
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

    // --- Method to execute the AI's orders ---
    public void ExecuteAIIntent(float deltaTime, GameSession session)
    {
        // 1. Execute Move
        if (moveQueue.Count > 0)
        {
            // Peek at the next target; only dequeue when reached
            GridPos currentTarget = moveQueue.Peek();
            Vector2 targetVec = currentTarget.ToVector2();
            Vector2 dir = Vector2.Normalize(targetVec - this.GridPos);

            Vector2 newPos = this.GridPos + (dir * deltaTime * 4.0f);

            // Snap to grid if close enough
            if (Vector2.Distance(this.GridPos, targetVec) < 0.1f)
            {
                this.GridPos = targetVec;
                moveQueue.Dequeue(); // Movement complete, remove from queue
            }
            else if (session.IsTileWalkable(newPos))
            {
                this.GridPos = newPos;
            }
        }

        // 2. Execute Bomb
        if (bombQueued)
        {
            // Snap bomb position to integer grid to prevent blocking intersections
            var snapPos = this.GridPosition.ToVector2();
            if (!session.ActiveBombs.Any(b => b.GridPos == snapPos))
            {
                session.ActiveBombs.Add(new Bomb(snapPos, this));
            }
            bombQueued = false;
        }
    }
    // ---------------------------------------------

    public void Reset(Vector2 startPosition)
    {
        GridPos = startPosition;
        IsActive = true;
        IsBeingYeeted = false;
        yeetScale = 1.0f;
        yeetTimer = 0.0f;
        // Clear AI memory
        ClearQueue();
    }

    public void TriggerYeet(Vector2 explosionOrigin)
    {
        if (IsBeingYeeted || !IsActive) return;
        IsBeingYeeted = true;
        yeetTimer = YeetDuration;
        
        var offset = GridPos - explosionOrigin;
        if (offset == Vector2.Zero) offset = new Vector2(1, 0);

        var direction = Vector2.Normalize(offset);
    }

    public void UpdateAnimation(float deltaTime, float cellSize)
    {
        PixelPos = GridPos * cellSize;
        if (IsBeingYeeted)
        {
            yeetTimer -= deltaTime;
            if (yeetTimer <= 0) { IsBeingYeeted = false; IsActive = false; }
            else { PixelPos += yeetVelocity * (YeetDuration - yeetTimer); yeetScale += YeetGrowthRate * deltaTime; }
        }
    }

    public Vector2 GetRenderPosition(Vector2 gridOrigin, float cellSize)
    {
        if (IsBeingYeeted) return gridOrigin + PixelPos - new Vector2(cellSize / 2);
        return gridOrigin + (GridPos * cellSize);
    }

    public float GetRenderScale(float cellSize) => IsBeingYeeted ? cellSize * yeetScale : cellSize;
}
