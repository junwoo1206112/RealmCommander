## Context

The host game startup flow has several issues preventing gameplay from beginning after clicking "Host Game" in the lobby. The MainScene loads but player creation fails (playerPrefab not assigned), units are never spawned (UnitSpawner not in scene), and no visible feedback signals game start.

## Goals / Non-Goals

**Goals:**
- Fix playerPrefab assignment so host player is properly created during StartHost()
- Dynamically create UnitSpawner so units appear on the field
- Ensure NetworkGameManager.StartGame() triggers local GameManager
- Make LobbyUI resilient to null references

**Non-Goals:**
- Not modifying the binary MainScene (changes are code-only)
- Not redesigning the lobby flow or multiplayer architecture

## Decisions

1. **Immediate playerPrefab assignment in NetworkBootstrap** — Assign `nm.playerPrefab = _cachedPlayerPrefab` and `nm.autoCreatePlayer = true` right after creating the prefab in `EnsureNetworkManager()`, instead of the late assignment in `NetworkGameManager.Update()`.

2. **Dynamic UnitSpawner creation** — In `NetworkGameManager.OnStartServer()`, if no UnitSpawner exists in the scene, create one dynamically. This avoids needing to edit the binary scene file. The UnitSpawner can be created with default settings and will spawn units via its own `OnStartServer()`.

3. **GameManager integration** — `NetworkGameManager.StartGame()` calls `GameManager.Instance.StartGame()` to set Time.timeScale and fire local events.

4. **LobbyUI null safety** — Already mostly null-safe. The `ipInputField?.text` pattern is sufficient. No code changes needed for existing null checks.

## Risks / Trade-offs

- Dynamic UnitSpawner creation means spawn positions/counts are hardcoded defaults rather than configurable in-editor
- If GameManager.Instance is null (not in scene), StartGame will silently fail — mitigated by Awake() creating it with DontDestroyOnLoad
