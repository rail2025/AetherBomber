// AetherBomber/Game/AIController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AetherBomber.Game;

public class AIController
{
    private readonly Character controlledCharacter;
    private readonly GameSession session;
    private readonly Random random = new();

    private float actionCooldown = 0f;
    private const float ActionDelay = 0.4f;

    public AIController(Character character, GameSession gameSession)
    {
        controlledCharacter = character;
        session = gameSession;
    }

    public void Update(float deltaTime)
    {
        if (!controlledCharacter.IsActive || session.CurrentRoundState != RoundState.InProgress) return;

        actionCooldown -= deltaTime;
        if (actionCooldown <= 0)
        {
            DecideNextAction();
            actionCooldown = ActionDelay + (float)(random.NextDouble() * 0.2);
        }
    }

    private void DecideNextAction()
    {
        if (IsPositionInDanger(controlledCharacter.GridPos, session.ActiveBombs))
        {
            var escapeMove = FindBestEscapeMove(controlledCharacter.GridPos, session.ActiveBombs);
            if (escapeMove.HasValue)
            {
                controlledCharacter.GridPos = escapeMove.Value;
            }
            return;
        }

        var targets = session.Characters.Where(c => c.IsActive && c != controlledCharacter).ToList();
        if (!targets.Any()) return;

        var closestTarget = targets.OrderBy(t => Vector2.Distance(controlledCharacter.GridPos, t.GridPos)).First();

        var (bombSpot, retreatSpot) = FindSafeBombPlacement(closestTarget);

        if (bombSpot.HasValue && retreatSpot.HasValue)
        {
            if (controlledCharacter.GridPos == bombSpot.Value)
            {
                if (!session.ActiveBombs.Any(b => b.GridPos == controlledCharacter.GridPos))
                {
                    session.ActiveBombs.Add(new Bomb(controlledCharacter.GridPos, controlledCharacter));
                }
            }
            else
            {
                var pathToBombSpot = FindPath(controlledCharacter.GridPos, bombSpot.Value, false);
                if (pathToBombSpot != null && pathToBombSpot.Count > 1)
                {
                    controlledCharacter.GridPos = pathToBombSpot[1];
                }
            }
            return;
        }

        var path = FindPath(controlledCharacter.GridPos, closestTarget.GridPos, true);
        if (path != null && path.Count > 1)
        {
            controlledCharacter.GridPos = path[1];
            return;
        }

        var safeMoves = GetSafeMoves(controlledCharacter.GridPos, session.ActiveBombs).Where(p => p != controlledCharacter.GridPos).ToList();
        if (safeMoves.Any())
        {
            controlledCharacter.GridPos = safeMoves[random.Next(safeMoves.Count)];
        }
    }

    private (Vector2?, Vector2?) FindSafeBombPlacement(Character target)
    {
        var potentialBombSpots = GetAdjacentWalkableTiles(controlledCharacter.GridPos, true);
        potentialBombSpots.Add(controlledCharacter.GridPos);

        foreach (var bombSpot in potentialBombSpots)
        {
            var bomb = new Bomb(bombSpot, controlledCharacter);
            var explosionPath = session.CalculateExplosionPath(bomb);

            if (explosionPath.Contains(target.GridPos))
            {
                var bombs = new List<Bomb>(session.ActiveBombs) { bomb };
                var escapeMove = FindBestEscapeMove(bombSpot, bombs);
                if (escapeMove.HasValue)
                {
                    return (bombSpot, escapeMove.Value);
                }
            }
        }

        return (null, null);
    }

    private Vector2? FindBestEscapeMove(Vector2 fromPos, List<Bomb> bombs)
    {
        var safeMoves = GetSafeMoves(fromPos, bombs);
        if (!safeMoves.Any()) return null;

        return safeMoves.OrderByDescending(m => bombs.Min(b => Vector2.Distance(m, b.GridPos))).First();
    }

    private List<Vector2> GetAdjacentWalkableTiles(Vector2 position, bool includeDestructible)
    {
        var tiles = new List<Vector2>();
        var directions = new List<Vector2> { new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };

        foreach (var dir in directions)
        {
            var nextPos = position + dir;
            var tile = session.GameBoard.GetTile((int)nextPos.X, (int)nextPos.Y);

            if (tile.Type == TileType.Empty || (includeDestructible && tile.Type == TileType.Destructible))
            {
                tiles.Add(nextPos);
            }
        }
        return tiles;
    }

    private bool IsPositionInDanger(Vector2 position, List<Bomb> bombs)
    {
        return bombs.Any(bomb => session.CalculateExplosionPath(bomb).Contains(position));
    }

    private List<Vector2> GetSafeMoves(Vector2 fromPos, List<Bomb> bombs)
    {
        var safeMoves = new List<Vector2>();
        var directions = new List<Vector2> { new(0, 0), new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };

        foreach (var dir in directions)
        {
            var targetPos = fromPos + dir;
            if (session.IsTileWalkable(targetPos) && !IsPositionInDanger(targetPos, bombs))
            {
                safeMoves.Add(targetPos);
            }
        }
        return safeMoves;
    }

    private List<Vector2>? FindPath(Vector2 start, Vector2 goal, bool avoidDestructible)
    {
        var queue = new Queue<List<Vector2>>();
        var visited = new HashSet<Vector2> { start };
        queue.Enqueue(new List<Vector2> { start });

        while (queue.Count > 0)
        {
            var path = queue.Dequeue();
            var pos = path.Last();

            if (pos == goal)
            {
                return path;
            }

            var directions = new List<Vector2> { new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };

            foreach (var dir in directions.OrderBy(d => Vector2.Distance(pos + d, goal)))
            {
                var nextPos = pos + dir;
                var tile = session.GameBoard.GetTile((int)nextPos.X, (int)nextPos.Y);

                if (visited.Contains(nextPos)) continue;

                if (tile.Type == TileType.Empty || (!avoidDestructible && tile.Type == TileType.Destructible))
                {
                    visited.Add(nextPos);
                    var newPath = new List<Vector2>(path) { nextPos };
                    queue.Enqueue(newPath);
                }
            }
        }
        return null;
    }
}
