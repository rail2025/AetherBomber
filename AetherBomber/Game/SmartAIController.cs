using System.Collections.Generic;
using System.Linq;
using System.Numerics; // Needed for vector conversion

namespace AetherBomber.Game;

public class AIController
{
    private readonly Character me;
    private readonly GameSession session;

    private readonly AStarPathfinder pathfinder;
    private readonly EscapeSimulator escapeSim;

    private GridPos? roamingTarget;

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

        var threats = new AIThreatMap(session);
        var myPos = me.GridPosition; // Use the integer grid property

        // 1. SURVIVAL
        if (threats.IsDangerAt(myPos, 0))
        {
            var safe = FindNearestSafeTile(myPos, threats);
            if (safe != null)
                MoveAlong(safe.Value, threats);

            return;
        }

        // 2. OFFENSE
        if (TryKill(threats))
            return;

        // 3. FARMING
        if (TryFarmCrate(threats))
            return;

        // 4. ROAMING / SEEK ENEMY
        WanderOrChase(threats);
    }

    private bool TryKill(AIThreatMap threats)
    {
        if (threats.IsDangerAt(me.GridPosition, 0)) return false;

        foreach (var enemy in session.Characters)
        {
            if (enemy == me || !enemy.IsActive) continue;

            var explosion = session.CalculateExplosionPath(new Bomb(me.GridPos, me));

            // Check if enemy is in the blast path
            if (explosion.Any(v => (int)v.X == enemy.GridPosition.X && (int)v.Y == enemy.GridPosition.Y))
            {
                if (escapeSim.CanEscape(me.GridPosition, me.GridPosition))
                {
                    me.QueueBombPlacement();
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
                if (escapeSim.CanEscape(me.GridPosition, me.GridPosition))
                {
                    me.QueueBombPlacement();
                    return true;
                }
            }
        }
        return false;
    }

    private void WanderOrChase(AIThreatMap threats)
    {
        if (roamingTarget == null)
            roamingTarget = FindRoamTarget();

        MoveAlong(roamingTarget.Value, threats);

        if (me.GridPosition.Equals(roamingTarget.Value))
            roamingTarget = FindRoamTarget();
    }

    private GridPos FindRoamTarget()
    {
        return new GridPos(
            session.GameBoard.Width / 2,
            session.GameBoard.Height / 2
        );
    }

    private void MoveAlong(GridPos target, AIThreatMap threats)
    {
        var path = pathfinder.FindPath(me.GridPosition, target, threats);
        if (path == null || path.Count < 2)
            return;

        me.QueueMove(path[1]);
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
            if (!threats.IsDangerAt(p, t))
                return p;

            foreach (var dir in GridPos.Cardinal)
            {
                var next = p + dir;
                if (!visited.Contains(next) && session.IsTileWalkable(next.ToVector2()))
                {
                    visited.Add(next);
                    q.Enqueue((next, t + 1));
                }
            }
        }
        return null;
    }
}
