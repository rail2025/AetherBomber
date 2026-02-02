using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AetherBomber.Game;

public class AIController
{
    private readonly Character me;
    private readonly GameSession session;

    private readonly AStarPathfinder pathfinder;
    private readonly EscapeSimulator escapeSim;


    public AIController(Character me, GameSession session)
    {
        this.me = me;
        this.session = session;

        pathfinder = new AStarPathfinder(session);
        escapeSim = new EscapeSimulator(session, pathfinder);
    }

    public void Update()
    {
        if (!me.IsActive || session.CurrentRoundState != RoundState.InProgress)
            return;

        if (me.HasQueuedActions) return;

        var threats = new AIThreatMap(session);
        var myPos = me.GridPosition;

        if (threats.IsDangerAt(myPos, 0))
        {
            var safe = FindNearestSafeTile(myPos, threats);
            if (safe != null)
                MoveAlong(safe.Value, threats);

            return;
        }

        if (TryKill(threats))
            return;

        if (TryFarmCrate(threats))
            return;

        WanderOrChase(threats);
    }

    private bool TryKill(AIThreatMap threats)
    {
        if (threats.IsDangerAt(me.GridPosition, 0)) return false;

        foreach (var enemy in session.Characters)
        {
            if (enemy == me || !enemy.IsActive) continue;

            var explosion = session.CalculateExplosionPath(new Bomb(me.GridPos, me));

            if (explosion.Any(v => (int)v.X == enemy.GridPosition.X && (int)v.Y == enemy.GridPosition.Y))
            {
                var escapePath = escapeSim.CanEscape(me.GridPosition, me.GridPosition);
                if (escapePath != null)
                {
                    ExecuteBombAndEscape(escapePath);
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryFarmCrate(AIThreatMap threats)
    {
        foreach (var dir in GridPos.Cardinal)
        {
            var p = me.GridPosition + dir;
            var tile = session.GameBoard.GetTile(p.X, p.Y);

            if (tile.Type == TileType.Destructible)
            {
                var escapePath = escapeSim.CanEscape(me.GridPosition, me.GridPosition);
                if (escapePath != null)
                {
                    ExecuteBombAndEscape(escapePath);
                    return true;
                }
            }
        }
        return false;
    }

    private void WanderOrChase(AIThreatMap threats)
    {
        var enemyTarget = FindRoamTarget();

        if (!MoveAlong(enemyTarget, threats))
        {
            var crate = FindNearestCrate();
            if (crate.HasValue)
            {
                if (GridPos.Manhattan(me.GridPosition, crate.Value) == 1)
                {
                    var escapePath = escapeSim.CanEscape(me.GridPosition, me.GridPosition);
                    if (escapePath != null)
                        ExecuteBombAndEscape(escapePath);
                }
                else
                {
                    foreach (var dir in GridPos.Cardinal)
                    {
                        var neighbor = crate.Value + dir;
                        if (session.IsTileWalkable(neighbor.ToVector2()))
                            if (MoveAlong(neighbor, threats)) break;
                    }
                }
            }
        }
    }

    private GridPos FindRoamTarget()
    {
        GridPos best = new GridPos(session.GameBoard.Width / 2, session.GameBoard.Height / 2);
        float minDst = float.MaxValue;

        foreach (var c in session.Characters)
        {
            if (c == me || !c.IsActive) continue;
            float dst = Vector2.Distance(me.GridPos, c.GridPos);
            if (dst < minDst) { minDst = dst; best = c.GridPosition; }
        }
        return best;
    }

    private bool MoveAlong(GridPos target, AIThreatMap threats)
    {
        var path = pathfinder.FindPath(me.GridPosition, target, threats);
        if (path == null || path.Count < 2)
            return false;

        me.QueueMove(path[1]);
        return true;
    }

    private GridPos? FindNearestCrate()
    {
        GridPos? best = null;
        int minD = int.MaxValue;
        for (int x = 0; x < session.GameBoard.Width; x++)
        {
            for (int y = 0; y < session.GameBoard.Height; y++)
            {
                if (session.GameBoard.GetTile(x, y).Type == TileType.Destructible)
                {
                    var d = GridPos.Manhattan(me.GridPosition, new GridPos(x, y));
                    if (d < minD) { minD = d; best = new GridPos(x, y); }
                }
            }
        }
        return best;
    }
    private GridPos? FindNearestSafeTile(GridPos start, AIThreatMap threats)
    {
        var q = new Queue<(GridPos pos, int turn)>();
        var visited = new HashSet<GridPos>();

        q.Enqueue((start, 0));
        visited.Add(start);

        while (q.Count > 0)
        {
            var (p, t) = q.Dequeue();

            if (threats.GetDangerTime(p) == AIThreatMap.Safe)
                return p;

            foreach (var dir in GridPos.Cardinal)
            {
                var next = p + dir;

                if (!visited.Contains(next) && session.IsTileWalkable(next.ToVector2()))
                {
                    if (!threats.IsDangerAt(next, t + 1))
                    {
                        visited.Add(next);
                        q.Enqueue((next, t + 1));
                    }
                }
            }
        }
        return null;
    }
    private void ExecuteBombAndEscape(List<GridPos> escapePath)
    {
        me.QueueBombPlacement();
        for (int i = 1; i < escapePath.Count; i++)
        {
            me.QueueMove(escapePath[i]);
        }
    }
}
