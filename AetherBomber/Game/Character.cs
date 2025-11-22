using System; // For MathF
using System.Numerics;
using System.Linq;
namespace AetherBomber.Game;

public enum CharacterType { Player, Tank, Healer, DPS }

public class Character
{
    public CharacterType Type { get; }
    public Vector2 GridPos { get; set; }

    // --- NEW: Bridge to Integer Grid ---
    public GridPos GridPosition => new GridPos((int)MathF.Round(GridPos.X), (int)MathF.Round(GridPos.Y));
    // ----------------------------------

    public Vector2 PixelPos { get; private set; }
    public bool IsActive { get; set; } = true;
    public bool IsLocalPlayer { get; set; } = false;
    public bool IsBeingYeeted { get; private set; } = false;
    public int Score { get; set; } = 0;

    // --- NEW: Updated to use the new Smart Brain ---
    public AIController? AiController { get; set; }

    // --- NEW: AI Intent Queues ---
    private GridPos? nextMoveTarget;
    private bool bombQueued = false;

    public void QueueMove(GridPos target) { this.nextMoveTarget = target; }
    public void QueueBombPlacement() { this.bombQueued = true; }
    // ----------------------------

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

    // --- NEW: Method to execute the AI's orders ---
    public void ExecuteAIIntent(float deltaTime, GameSession session)
    {
        // 1. Execute Move
        if (nextMoveTarget.HasValue)
        {
            Vector2 targetVec = nextMoveTarget.Value.ToVector2();
            Vector2 dir = Vector2.Normalize(targetVec - this.GridPos);

            // Speed = 4.0f (1 tile = 0.25s)
            Vector2 newPos = this.GridPos + (dir * deltaTime * 4.0f);

            // Snap to grid if close enough
            if (Vector2.Distance(this.GridPos, targetVec) < 0.1f)
            {
                this.GridPos = targetVec;
                nextMoveTarget = null; // Movement complete
            }
            else if (session.IsTileWalkable(newPos))
            {
                this.GridPos = newPos;
            }
        }

        // 2. Execute Bomb
        if (bombQueued)
        {
            if (!session.ActiveBombs.Any(b => b.GridPos == this.GridPos))
            {
                session.ActiveBombs.Add(new Bomb(this.GridPos, this));
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
        nextMoveTarget = null;
        bombQueued = false;
    }

    public void TriggerYeet(Vector2 explosionOrigin)
    {
        if (IsBeingYeeted || !IsActive) return;
        IsBeingYeeted = true;
        yeetTimer = YeetDuration;
        var direction = Vector2.Normalize(GridPos - explosionOrigin);
        if (direction == Vector2.Zero) direction = new Vector2(1, 0);
        yeetVelocity = direction * YeetSpeed;
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
