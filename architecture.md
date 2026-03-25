# ARCHITECTURE.md

## 1. System Overview
AetherBomber is a real-time, grid-based multiplayer arcade game built as a plugin for Final Fantasy XIV via the Dalamud framework. It relies on a central, authoritative state manager (`GameSession`) that processes a continuous update loop, handling inputs, entity AI, physics/grid validation, and event resolution.

## 2. Core Architectural Patterns
The application utilizes a tightly-coupled update loop pattern, deeply integrated with Dalamud's input and rendering hooks.

* **State Management:** The ultimate source of truth is the `GameSession` class. It holds the active `GameBoard`, lists of `Characters` (both local players and AI/remote entities), and `ActiveBombs`.
* **Update Loop:** The game advances via `GameSession.Update(deltaTime)`, which propagates time deltas down to round timers, AI controllers, character animations, and bomb fuses.
* **Grid Coordination:** Movement and hit detection are strictly locked to a 15x11 logical grid. Continuous floating-point coordinates are primarily used for rendering interpolation, while actual game logic resolves exclusively on discrete integer coordinates (`Vector2` cast to `int`).

## 3. Subsystem Breakdown

### A. Data Models & Board Management (`GameBoard.cs`)
* **Grid Structure:** A 2D array (`Tile[,]`) defining `TileType.Empty`, `TileType.Wall`, `TileType.Destructible`, and `TileType.PowerUp`.
* **Procedural Generation:** * **Stage 1:** Utilizes a static, hardcoded integer matrix for classic layout familiarization.
    * **Stage 2+:** Utilizes procedural generation algorithms to randomize static walls and destructible blocks.
    * **Spawn Fairness:** Regardless of stage, the board generation strictly enforces 2x2 "Safe Zones" in all four corners. This guarantees mirrored, equitable spawn points for up to 4 players without immediate threat of trapping.
    * **Item Injection:** Power-ups are randomly injected into the pool of destructible tiles during initialization, replacing a percentage of standard blocks.

### B. Session & Entity Logic (`GameSession.cs`)
* **`Character` Lifecycle:** Characters are instantiated with a `CharacterType` (Player, DPS, Healer, Tank). Single-player mode attaches an `AIController` to non-local characters, dividing updates into a "Brain Update" (intent) and "Body Update" (execution).
* **Input Handling:** Driven by `IGamepadState`. Movement is locked behind a strict `moveCooldown` (0.15s) to simulate grid-snapping. Inputs are evaluated against `IsTileWalkable()` before modifying the logical `GridPos`.
* **Bomb Resolution:** Bombs tick down independently. Upon expiration, `CalculateExplosionPath()` fires raycasts in four cardinal directions (max length 3). The raycast halts immediately upon hitting a `Wall` tile, or halts *after* appending a `Destructible` tile to the blast path.

### C. Combat & Hit Detection
* Collision is not physics-based; it is positional.
* During a bomb's explosion frame, the system checks if any active `Character` shares a `GridPos` with any tile in the calculated `ExplosionPath`.
* Hits trigger a `TriggerYeet()` state on the character and increment the score of the bomb's `Owner`.

## 4. Execution Flow: Bomb Detonation
1.  **Tick:** `UpdateBombs()` decrements the fuse timer by `deltaTime`.
2.  **Trigger:** Timer hits zero, setting `IsExploding`.
3.  **Raycast:** `CalculateExplosionPath()` evaluates the grid. It steps outward +X, -X, +Y, -Y.
4.  **Path Validation:** If a tile is `TileType.Wall`, the ray terminates. If `TileType.Destructible`, the tile is added to the path, and the ray terminates. If `TileType.Empty`, the tile is added, and the ray continues.
5.  **Resolution:** The system iterates over the final `HashSet<Vector2>` path. 
    * Any block at those coordinates calls `GameBoard.DestroyTile()`.
    * Any character at those coordinates calls `HitCharacterAt()`.
6.  **Cleanup:** The bomb is flagged as finished and removed from `ActiveBombs` via reverse-iteration.

## 5. Technical Debt & Scaling Bottlenecks
* **God Class:** `GameSession.cs` handles too many domains. It manages input polling, AI lifecycle execution, bomb physics, and win-state tracking. 
* **Hardcoded Input:** Controller logic is directly embedded in `GameSession.HandleInput()`. Rebinding keys or supporting alternative input methods (keyboard/mouse) will require detangling this logic.
* **List Management:** `ActiveBombs` is iterated backward for removal. While functional for small counts, an event-driven or pooling approach would be more performant for high-chaos scenarios.
* **Lack of Data-Driven Design:** Entity stats, raycast lengths, and spawn coordinates are hardcoded rather than loaded from external configuration files.
