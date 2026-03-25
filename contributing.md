# CONTRIBUTING.md

## Contributing Quick Start
AetherBomber is built for tight, deterministic arcade gameplay. Because the core loop is highly centralized, changes to state management or entity logic must be handled with extreme care to avoid breaking the game's strict grid timing.

### ⚠️ Critical Warning
The architecture heavily relies on the `GameSession` update loop. Do not introduce asynchronous logic, multi-threading, or `Task.Delay` for gameplay mechanics. All entity logic, movement, and explosions must resolve synchronously within the `Update(float deltaTime)` tick to ensure multiplayer state parity.

### Getting Started
1. Build the plugin using the standard Dalamud development environment.
2. Launch FFXIV with the plugin loaded via `/xlplugins`.
3. Open the main interface via the Dalamud plugin menu or appropriate slash command.

### Example Walkthrough: Adding a New Power-Up
To implement a new board mechanic (e.g., a "Blast Radius Up" item), you must touch the following systems in order:

1. **`GameBoard.cs`**
   * Update the `TileType` enum (e.g., add `PowerUpBlastRadius`).
   * Modify `InitializeBoard()` to inject this new type into the destructible tile replacement logic.
   * Ensure `IsWalkable()` accounts for the item (typically, power-ups are walkable so the player can pick them up).
2. **`GameSession.cs`**
   * Inside the `HandleInput()` movement block, or a new `CheckCollisions()` method, detect when a character's `GridPos` overlaps the power-up tile.
   * Apply the modifier to the `Character` object.
   * Call a method to revert the `TileType` back to `Empty`.
3. **`Bomb.cs` & `GameSession.cs` (Raycasting)**
   * Modify the hardcoded `for (int i = 0; i <= 3; i++)` loop in `CalculateRay()` to dynamically read the bomb owner's blast radius stat instead of the static `3`.

### Procedural Generation Constraints
If you are tweaking the algorithm for Stage 2+ generation in `GameBoard.cs`, you **must not** alter the corner safe-zone logic. 
* The arrays defined by `(x >= 1 && x <= 2 && y >= 1 && y <= 2)`, etc., are mathematically required to ensure all 4 characters can spawn without instantly detonating each other. 
* Any pull request that breaks mirrored spawn safety will be rejected.

### Submitting Changes
* Keep pull requests scoped to a single feature or fix.
* **Test Local vs. AI:** Ensure the AI controllers (`SmartAIController`) understand how to navigate around or interact with your new features.
* **Test Multiplayer:** Verify that network serialization (if touching network logic) properly synchronizes your new state variables. Single-player testing does not expose packet desyncs.
