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

    public bool CanEscape(GridPos bombAt, GridPos start)
    {
        // Simulate temp bomb
        var fake = new Bomb(bombAt.ToVector2(), owner: null);
        session.ActiveBombs.Add(fake);

        var threats = new AIThreatMap(session);
        bool canEscape = false;

        // Search every walkable tile for a safe spot
        for (int x = 0; x < session.GameBoard.Width; x++)
        {
            for (int y = 0; y < session.GameBoard.Height; y++)
            {
                var pos = new GridPos(x, y);
                // Must use ToVector2() for the session check
                if (!session.IsTileWalkable(pos.ToVector2())) continue;

                // If this tile itself explodes instantly, it's not a safe haven
                if (threats.GetDangerTime(pos) == 0) continue;

                // Attempt path
                var p = path.FindPath(start, pos, threats);
                if (p == null) continue;

                int arrival = p.Count;

                // Safe arrival?
                if (!threats.IsDangerAt(pos, arrival))
                {
                    canEscape = true;
                    break;
                }
            }
            if (canEscape) break;
        }

        session.ActiveBombs.Remove(fake);
        return canEscape;
    }
}
