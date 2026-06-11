## Why

Host game startup produces two runtime errors: (1) dynamically created UnitSpawner has no NetworkIdentity, preventing unit spawning, and (2) runtime-created PlayerPrefab lacks a real asset GUID, causing Mirror to fail registering it as a spawnable prefab.

## What Changes

- Add NetworkIdentity to dynamically created UnitSpawner before calling NetworkServer.Spawn()
- Create RealmCommanderNetworkManager subclass overriding OnServerAddPlayer to instantiate player objects manually instead of relying on playerPrefab asset registration

## Capabilities

### New Capabilities
- `runtime-error-fixes`: Ensures host game starts without runtime errors related to NetworkIdentity and prefab asset registration

### Modified Capabilities
- (none)

## Impact

- `Assets/Scripts/Network/RealmCommanderNetworkManager.cs` — new file, NetworkManager subclass for manual player creation
- `Assets/Scripts/Network/NetworkBootstrap.cs` — use RealmCommanderNetworkManager instead of base NetworkManager, remove playerPrefab/autoCreatePlayer assignment
- `Assets/Scripts/Network/NetworkGameManager.cs` — add NetworkIdentity to runtime UnitSpawner
