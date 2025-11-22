using System;
using System.Collections.Generic;
using System.Numerics; // Needed for Vector2

namespace AetherBomber.Game;

public readonly record struct GridPos(int X, int Y)
{
    public static readonly GridPos Up = new(0, -1);
    public static readonly GridPos Down = new(0, 1);
    public static readonly GridPos Left = new(-1, 0);
    public static readonly GridPos Right = new(1, 0);

    public static IEnumerable<GridPos> Cardinal
    {
        get
        {
            yield return Up;
            yield return Down;
            yield return Left;
            yield return Right;
        }
    }

    // --- THE MISSING BRIDGE ---
    public Vector2 ToVector2() => new Vector2(X, Y);
    // --------------------------

    public static GridPos operator +(GridPos a, GridPos b)
        => new(a.X + b.X, a.Y + b.Y);

    public static int Manhattan(GridPos a, GridPos b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    public override string ToString() => $"({X},{Y})";
}
