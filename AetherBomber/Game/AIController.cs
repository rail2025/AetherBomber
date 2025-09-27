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
    private const float ActionDelay = 0.5f; // How often the AI makes a decision

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
            actionCooldown = ActionDelay;
        }
    }

    private void DecideNextAction()
    {
        // Step 1: Check for immediate danger from explosions
        var safeMoves = GetSafeMoves();
        if (IsPositionInDanger(controlledCharacter.GridPos) && safeMoves.Any())
        {
            // Flee to the safest available spot
            controlledCharacter.GridPos = safeMoves.First();
            return;
        }

        // Step 2: Decide whether to place a bomb
        if (random.Next(100) < 20) // 20% chance to place a bomb
        {
            bool bombExists = session.ActiveBombs.Any(b => b.GridPos == controlledCharacter.GridPos);
            if (!bombExists)
            {
                session.ActiveBombs.Add(new Bomb(controlledCharacter.GridPos, controlledCharacter));
                return; // Don't move after placing a bomb
            }
        }

        // Step 3: If not placing a bomb and not in danger, move randomly
        if (safeMoves.Any())
        {
            // Move to a random valid adjacent tile
            var moveTarget = safeMoves[random.Next(safeMoves.Count)];
            controlledCharacter.GridPos = moveTarget;
        }
    }

    private bool IsPositionInDanger(Vector2 position)
    {
        return session.ActiveBombs.Any(bomb => bomb.IsExploding && bomb.ExplosionPath.Contains(position));
    }

    private List<Vector2> GetSafeMoves()
    {
        var possibleMoves = new List<Vector2>();
        var currentPos = controlledCharacter.GridPos;

        // Possible directions (including staying put)
        var directions = new List<Vector2>
        {
            new(0, 0), new(0, -1), new(0, 1), new(-1, 0), new(1, 0)
        };

        foreach (var dir in directions)
        {
            var targetPos = currentPos + dir;
            if (session.IsTileWalkable(targetPos) && !IsPositionInDanger(targetPos))
            {
                possibleMoves.Add(targetPos);
            }
        }
        return possibleMoves;
    }
}
