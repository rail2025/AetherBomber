using System;

namespace AetherBomber.Game;

public class AIThreatMap
{
    public const int Safe = int.MaxValue;

    private readonly int[,] dangerTimes;
    private readonly GameSession session;

    public int Width  => dangerTimes.GetLength(0);
    public int Height => dangerTimes.GetLength(1);

    public AIThreatMap(GameSession session)
    {
        this.session = session;
        dangerTimes = new int[
            session.GameBoard.Width,
            session.GameBoard.Height
        ];

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                dangerTimes[x, y] = Safe;

        Build();
    }

    private void Build()
    {
        foreach (var bomb in session.ActiveBombs)
        {
            int t = bomb.FuseRemainingTicks;

            foreach (var pos in session.CalculateExplosionPath(bomb))
            {
                int x = (int)pos.X;
                int y = (int)pos.Y;

                dangerTimes[x, y] = Math.Min(dangerTimes[x, y], t);
            }
        }
    }

    public bool InBounds(GridPos g)
        => g.X >= 0 && g.X < Width && g.Y >= 0 && g.Y < Height;

    public int GetDangerTime(GridPos g)
        => InBounds(g) ? dangerTimes[g.X, g.Y] : 0;

    /// <summary>Returns true if tile explodes *at or before* arrivalTurn.</summary>
    public bool IsDangerAt(GridPos g, int arrivalTurn)
    {
        int danger = GetDangerTime(g);
        if (danger == Safe) return false;
        return arrivalTurn >= danger;
    }
}
