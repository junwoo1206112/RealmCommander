## Context

Two runtime errors occur when hosting a game: (1) Mirror requires NetworkIdentity on all NetworkServer.Spawn() objects, and (2) Mirror's NetworkClient.RegisterPrefab() requires a real prefab asset GUID, which runtime-created GameObjects lack.

## Goals / Non-Goals

**Goals:**
- Fix NetworkIdentity on runtime UnitSpawner
- Eliminate "empty assetid" error for player prefab registration

**Non-Goals:**
- No changes to Mirror library itself
- No changes to the binary MainScene

## Decisions

1. **Add NetworkIdentity before Spawn** — Simply add `spawnerGo.AddComponent<NetworkIdentity>()` before calling `NetworkServer.Spawn(spawnerGo)` in `EnsureUnitSpawner()`.

2. **NetworkManager subclass for player creation** — Create `RealmCommanderNetworkManager : NetworkManager` that overrides `OnServerAddPlayer()` to instantiate the cached player prefab via `Instantiate()` + `NetworkServer.AddPlayerForConnection()`. This bypasses Mirror's prefab registration system entirely.

3. **Remove playerPrefab and autoCreatePlayer** — Since OnServerAddPlayer handles creation manually, `NetworkManager.playerPrefab` and `autoCreatePlayer` are no longer needed in NetworkBootstrap.

## Risks / Trade-offs

- RealmCommanderNetworkManager is a minimal subclass; future Mirror API changes to OnServerAddPlayer would need updating
