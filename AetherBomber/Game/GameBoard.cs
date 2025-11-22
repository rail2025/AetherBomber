// AetherBomber/Game/GameBoard.cs
using System;
using System.Collections.Generic;
using System.Numerics;

namespace AetherBomber.Game;

public enum TileType
{
    Empty,
    Wall,
    Destructible
}

public struct Tile
{
    public TileType Type { get; set; }
}

public class GameBoard
{
    public const int GridWidth = 15;
    public const int GridHeight = 11;

    public int Width => GridWidth;
    public int Height => GridHeight;

    private readonly Tile[,] grid;
    private readonly Random random = new();

    public GameBoard()
    {
        this.grid = new Tile[GridHeight, GridWidth];
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        var wallLayout = new int[GridHeight, GridWidth]
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

        var possibleDestructibleTiles = new List<Vector2>();

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                if (wallLayout[y, x] == 1)
                {
                    this.grid[y, x] = new Tile { Type = TileType.Wall };
                }
                else
                {
                    this.grid[y, x] = new Tile { Type = TileType.Empty };

                    bool isInSafeZone =
                        (x >= 1 && x <= 2 && y >= 1 && y <= 2) || // Top-left
                        (x >= GridWidth - 3 && x <= GridWidth - 2 && y >= 1 && y <= 2) || // Top-right
                        (x >= 1 && x <= 2 && y >= GridHeight - 3 && y <= GridHeight - 2) || // Bottom-left
                        (x >= GridWidth - 3 && x <= GridWidth - 2 && y >= GridHeight - 3 && y <= GridHeight - 2); // Bottom-right

                    if (!isInSafeZone)
                    {
                        possibleDestructibleTiles.Add(new Vector2(x, y));
                    }
                }
            }
        }

        int tilesToPlace = possibleDestructibleTiles.Count * 2 / 3;
        for (int i = 0; i < tilesToPlace; i++)
        {
            if (possibleDestructibleTiles.Count == 0) break;
            int randomIndex = this.random.Next(possibleDestructibleTiles.Count);
            var pos = possibleDestructibleTiles[randomIndex];
            this.grid[(int)pos.Y, (int)pos.X] = new Tile { Type = TileType.Destructible };
            possibleDestructibleTiles.RemoveAt(randomIndex);
        }
    }

    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
        {
            return new Tile { Type = TileType.Wall };
        }
        return this.grid[y, x];
    }

    public bool IsWalkable(Vector2 gridPos)
    {
        int x = (int)gridPos.X;
        int y = (int)gridPos.Y;
        return GetTile(x, y).Type == TileType.Empty;
    }

    public void DestroyTile(int x, int y)
    {
        if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
        {
            if (grid[y, x].Type == TileType.Destructible)
            {
                grid[y, x] = new Tile { Type = TileType.Empty };
            }
        }
    }
}
