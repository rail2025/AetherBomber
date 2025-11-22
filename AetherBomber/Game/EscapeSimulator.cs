using System.Collections.Generic;

namespace AetherBomber.Game;

public class EscapeSimulator
{
    private readonly GameSession session;
    private readonly AStarPathfinder path;

    public EscapeSimulator(GameSession s, AStarPathfinder p)
    {
        session = s;
        path = p;
    }

    public List<GridPos>? CanEscape(GridPos bombAt, GridPos start)
    {
        // Simulate temp bomb (using player 0 as dummy owner)
        var fake = new Bomb(bombAt.ToVector2(), session.Characters[0]);
        session.ActiveBombs.Add(fake);

        var threats = new AIThreatMap(session);
        List<GridPos>? safePath = null;

        // Search every walkable tile for a safe spot
        for (int x = 0; x < session.GameBoard.Width; x++)
        {
            for (int y = 0; y < session.GameBoard.Height; y++)
            {
                var pos = new GridPos(x, y);
                // Must use ToVector2() for the session check
                if (!session.IsTileWalkable(pos.ToVector2())) continue;

                // Only consider tiles that are completely safe from all bombs
                if (threats.GetDangerTime(pos) != AIThreatMap.Safe) continue;

                // Attempt path
                var p = path.FindPath(start, pos, threats);
                if (p == null) continue;

                int arrival = p.Count;

                // Safe arrival?
                if (!threats.IsDangerAt(pos, arrival))
                {
                    safePath = p;
                    break;
                }
            }
            if (safePath != null) break;
        }

        session.ActiveBombs.Remove(fake);
        return safePath;
    }
}
