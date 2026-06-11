## Why

Host game does not start properly after clicking "Host Game" in the lobby. The MainScene loads but no units are spawned, the host player is not properly created, and there is no visible feedback that the game has started.

## What Changes

- Fix `NetworkBootstrap` to immediately assign `playerPrefab` to `NetworkManager` when creating the player prefab
- Fix `NetworkGameManager.OnStartServer()` to properly count the host connection and trigger visual feedback
- Dynamically create a `UnitSpawner` in `NetworkGameManager.OnStartServer()` if none exists in the scene
- Make `NetworkGameManager.StartGame()` trigger the local `GameManager.StartGame()` for Time.timeScale and event propagation
- Fix `LobbyUI` null reference handling for better robustness

## Capabilities

### New Capabilities
- `host-game-startup`: Ensures the host can successfully start a game from the lobby with proper player creation, unit spawning, and visual feedback

### Modified Capabilities
- (none)

## Impact

- `Assets/Scripts/Network/NetworkBootstrap.cs` — playerPrefab assignment
- `Assets/Scripts/Network/NetworkGameManager.cs` — OnStartServer, StartGame, UnitSpawner creation
- `Assets/Scripts/UI/Menu/LobbyUI.cs` — null safety
