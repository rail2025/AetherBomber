using System.Collections.Generic;
using System;

namespace AetherBomber.Game;

public class AStarPathfinder
{
    private readonly GameSession session;

    public AStarPathfinder(GameSession session)
    {
        this.session = session;
    }

    public List<GridPos>? FindPath(GridPos start, GridPos goal, AIThreatMap threats)
    {
        var open = new PriorityQueue<GridPos, int>();
        var came = new Dictionary<GridPos, GridPos>();
        var gScore = new Dictionary<GridPos, int>();

        open.Enqueue(start, 0);
        gScore[start] = 0;

        while (open.Count > 0)
        {
            var current = open.Dequeue();

            if (current == goal)
                return Reconstruct(came, current);

            int currCost = gScore[current];

            foreach (var dir in GridPos.Cardinal)
            {
                var next = current + dir;
                if (!session.IsTileWalkable(next.ToVector2())) continue;

                int arrivalTurn = currCost + 1;
                if (threats.IsDangerAt(next, arrivalTurn)) continue;

                int newCost = currCost + 1;

                if (!gScore.TryGetValue(next, out var oldCost) || newCost < oldCost)
                {
                    gScore[next] = newCost;
                    came[next] = current;
                    int priority = newCost + GridPos.Manhattan(next, goal);
                    open.Enqueue(next, priority);
                }
            }
        }

        return null;
    }

    private List<GridPos> Reconstruct(Dictionary<GridPos, GridPos> came, GridPos at)
    {
        var list = new List<GridPos>() { at };
        while (came.TryGetValue(at, out var prev))
        {
            at = prev;
            list.Add(at);
        }

        list.Reverse();
        return list;
    }
}
