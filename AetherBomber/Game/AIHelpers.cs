using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AetherBomber.Game
{
    public class ThreatMap
    {
        private float[,] dangerTimers; // 0 = safe, >0 = seconds until boom

        public ThreatMap(GameSession session)
        {
            dangerTimers = new float[GameBoard.GridWidth, GameBoard.GridHeight];

            foreach (var bomb in session.ActiveBombs)
            {
                var path = session.CalculateExplosionPath(bomb);
                foreach (var tile in path)
                {
                    float current = dangerTimers[(int)tile.X, (int)tile.Y];
                    if (current == 0 || bomb.Timer < current)
                    {
                        dangerTimers[(int)tile.X, (int)tile.Y] = bomb.Timer;
                    }
                }
                dangerTimers[(int)bomb.GridPos.X, (int)bomb.GridPos.Y] = bomb.Timer;
            }
        }

        public bool IsDangerAt(GridPos pos, int stepsInFuture)
        {
            if (pos.X < 0 || pos.X >= GameBoard.GridWidth || pos.Y < 0 || pos.Y >= GameBoard.GridHeight) return true;

            float timeAtTile = stepsInFuture * 0.25f; // Estimate time to reach tile
            float explosionTime = dangerTimers[pos.X, pos.Y];

            return explosionTime > 0 && Math.Abs(explosionTime - timeAtTile) < 0.5f;
        }
    }
}
