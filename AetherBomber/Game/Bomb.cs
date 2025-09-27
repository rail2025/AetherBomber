// AetherBomber/Game/Bomb.cs
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace AetherBomber.Game;

public class Bomb
{
    public Vector2 GridPos { get; }
    public float Timer { get; private set; }
    public float ExplosionTimer { get; private set; }
    public bool IsExploding { get; private set; }

    private const float DetonationTime = 4.0f; // 3 seconds flashing + 1 second final warning
    private const float ExplosionDuration = 0.5f;

    public Bomb(Vector2 gridPos)
    {
        this.GridPos = gridPos;
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

    public uint GetOutlineColor()
    {
        // Final second warning (flashing orange and red)
        if (this.Timer <= 1.0f)
        {
            // Flashes between orange and red every 0.1 seconds
            return (int)(this.Timer * 10) % 2 == 0
                ? ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f)) // Red
                : ImGui.GetColorU32(new Vector4(1.0f, 0.5f, 0.0f, 1.0f)); // Orange
        }
        // First 3 seconds (flashing red)
        return (int)(this.Timer * 2) % 2 == 0
            ? ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f)) // Red
            : ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.0f));  // Transparent
    }
}
