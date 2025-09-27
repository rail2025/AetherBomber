// AetherBomber/Game/Player.cs
using System.Numerics;

namespace AetherBomber.Game;

public class Player
{
    public Vector2 GridPos { get; set; }

    // Player-related constants can be moved here as well
    public static readonly Vector4 DefaultColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    public static readonly Vector4 FlashColor = new(1.0f, 0.0f, 0.0f, 1.0f);

    public Player(Vector2 startPosition)
    {
        GridPos = startPosition;
    }
}
