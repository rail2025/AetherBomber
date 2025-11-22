using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AetherBomber.Game
{
    // Calculates where explosions will be and WHEN
    public class ThreatMap
    {
        private float[,] dangerTimers; // 0 = safe, >0 = seconds until boom

        public ThreatMap(GameSession session)
        {
            dangerTimers = new float[GameBoard.GridWidth, GameBoard.GridHeight];

            foreach (var bomb in session.ActiveBombs)
            {
                // Reuse the game's logic to get the blast tiles
                var path = session.CalculateExplosionPath(bomb);
                foreach (var tile in path)
                {
                    // If multiple bombs hit the same tile, take the earliest explosion
                    float current = dangerTimers[(int)tile.X, (int)tile.Y];
                    if (current == 0 || bomb.Timer < current)
                    {
                        dangerTimers[(int)tile.X, (int)tile.Y] = bomb.Timer;
                    }
                }
                // Don't forget the bomb itself!
                dangerTimers[(int)bomb.GridPos.X, (int)bomb.GridPos.Y] = bomb.Timer;
            }
        }

        // Checks if a tile is dangerous at a specific future time (assuming 1 move = 0.25s)
        public bool IsDangerAt(GridPos pos, int stepsInFuture)
        {
            if (pos.X < 0 || pos.X >= GameBoard.GridWidth || pos.Y < 0 || pos.Y >= GameBoard.GridHeight) return true;

            float timeAtTile = stepsInFuture * 0.25f; // Estimate time to reach tile
            float explosionTime = dangerTimers[pos.X, pos.Y];

            // It's dangerous if an explosion is happening roughly when we are there
            // Adding a buffer (0.5s) to avoid walking into a fire that is just fading
            return explosionTime > 0 && Math.Abs(explosionTime - timeAtTile) < 0.5f;
        }
    }
}
