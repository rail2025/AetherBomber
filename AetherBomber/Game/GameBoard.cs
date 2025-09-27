// AetherBomber/Game/GameBoard.cs
using System.Numerics;

namespace AetherBomber.Game;

public class GameBoard
{
    public const int GridWidth = 15;
    public const int GridHeight = 11;

    private readonly int[,] grid;

    public GameBoard()
    {
        // Initial grid layout with walls (1) and open spaces (0)
        this.grid = new int[GridHeight, GridWidth]
        {
            {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
            {1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1},
            {1, 0, 1, 0, 1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 1},
            {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            {1, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 0, 1},
            {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            {1, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 0, 1},
            {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            {1, 0, 1, 0, 1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 1},
            {1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1},
            {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        };
    }

    public int GetTile(int x, int y)
    {
        if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
        {
            return 1; // Treat out of bounds as a wall
        }
        return this.grid[y, x];
    }

    public bool IsWalkable(Vector2 gridPos)
    {
        int x = (int)gridPos.X;
        int y = (int)gridPos.Y;
        return GetTile(x, y) == 0;
    }
}
