using System; // Needed for Math.Ceiling
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Numerics;

namespace AetherBomber.Game;

public class Bomb
{
    public Character Owner { get; }
    public Vector2 GridPos { get; }
    public float Timer { get; private set; }
    public float ExplosionTimer { get; private set; }
    public bool IsExploding { get; private set; }
    public HashSet<Vector2> ExplosionPath { get; private set; } = new();

    // --- THE MISSING BRIDGE ---
    // AI calculates logic in 0.25s "Ticks". This converts float time to int ticks.
    public int FuseRemainingTicks => (int)Math.Ceiling(this.Timer / 0.25f);
    // --------------------------

    private const float DetonationTime = 4.0f;
    private const float ExplosionDuration = 0.5f;

    public Bomb(Vector2 gridPos, Character owner)
    {
        this.GridPos = gridPos;
        this.Owner = owner;
        this.Timer = DetonationTime;
        this.IsExploding = false;
    }

    public void Update(float deltaTime)
    {
        if (this.IsExploding)
        {
            this.ExplosionTimer -= deltaTime;
        }
        else
        {
            this.Timer -= deltaTime;
            if (this.Timer <= 0)
            {
                this.IsExploding = true;
                this.ExplosionTimer = ExplosionDuration;
            }
        }
    }

    public bool IsFinished() => this.IsExploding && this.ExplosionTimer <= 0;

    public void SetExplosionPath(HashSet<Vector2> path)
    {
        if (ExplosionPath.Count == 0) ExplosionPath = path;
    }

    public uint GetOutlineColor()
    {
        if (this.Timer <= 1.0f)
        {
            return (int)(this.Timer * 10) % 2 == 0
                ? ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f))
                : ImGui.GetColorU32(new Vector4(1.0f, 0.5f, 0.0f, 1.0f));
        }
        return (int)(this.Timer * 2) % 2 == 0
            ? ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f))
            : ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
    }
}
