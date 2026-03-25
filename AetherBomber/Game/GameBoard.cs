// AetherBomber/Game/GameBoard.cs
using System;
using System.Collections.Generic;
using System.Numerics;

namespace AetherBomber.Game;

public enum TileType
{
    Empty,
    Wall,
    Destructible,
    PowerUp
}

public struct Tile
{
    public TileType Type { get; set; }
    public bool HasPowerUp { get; set; }
}

public class GameBoard
{
    public const int GridWidth = 15;
    public const int GridHeight = 11;

    public int Width => GridWidth;
    public int Height => GridHeight;

    private readonly Tile[,] grid;

    private static readonly int[,] Stage1Layout = new int[GridHeight, GridWidth]
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

    public GameBoard(int stageNumber = 1)
    {
        this.grid = new Tile[GridHeight, GridWidth];
        InitializeBoard(stageNumber);
    }

    private void InitializeBoard(int stageNumber)
    {
        GenerateWalls(stageNumber);
        PlaceDestructibles();
    }

    private bool IsSafeZone(int x, int y)
    {
        return (x >= 1 && x <= 2 && y >= 1 && y <= 2) ||
               (x >= GridWidth - 3 && x <= GridWidth - 2 && y >= 1 && y <= 2) ||
               (x >= 1 && x <= 2 && y >= GridHeight - 3 && y <= GridHeight - 2) ||
               (x >= GridWidth - 3 && x <= GridWidth - 2 && y >= GridHeight - 3 && y <= GridHeight - 2);
    }

    private void GenerateWalls(int stageNumber)
    {
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                bool isBorder = x == 0 || x == GridWidth - 1 || y == 0 || y == GridHeight - 1;

                bool isProceduralWall = (x % 2 == 0 && y % 2 == 0) ||
                                        (!IsSafeZone(x, y) && Random.Shared.NextDouble() < GameRules.ProceduralWallChance);

                bool isHardWall = isBorder || (stageNumber == 1 ? Stage1Layout[y, x] == 1 : isProceduralWall);

                this.grid[y, x] = new Tile
                {
                    Type = isHardWall ? TileType.Wall : TileType.Empty,
                    HasPowerUp = false
                };
            }
        }
    }

    private void PlaceDestructibles()
    {
        var possibleDestructibleTiles = new List<(int X, int Y)>();

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                if (this.grid[y, x].Type == TileType.Empty && !IsSafeZone(x, y))
                {
                    possibleDestructibleTiles.Add((x, y));
                }
            }
        }

        int tilesToPlace = (int)(possibleDestructibleTiles.Count * GameRules.DestructibleDensity);
        for (int i = 0; i < tilesToPlace; i++)
        {
            if (possibleDestructibleTiles.Count == 0) break;
            int randomIndex = Random.Shared.Next(possibleDestructibleTiles.Count);
            var pos = possibleDestructibleTiles[randomIndex];

            bool hasPowerUp = Random.Shared.NextDouble() < GameRules.PowerUpDropChance;

            this.grid[pos.Y, pos.X] = new Tile
            {
                Type = TileType.Destructible,
                HasPowerUp = hasPowerUp
            };

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
                grid[y, x] = new Tile
                {
                    Type = grid[y, x].HasPowerUp ? TileType.PowerUp : TileType.Empty,
                    HasPowerUp = false
                };
            }
            else if (grid[y, x].Type == TileType.PowerUp)
            {
                grid[y, x] = new Tile { Type = TileType.Empty, HasPowerUp = false };
            }
        }
    }
}
