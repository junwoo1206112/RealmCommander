## ADDED Requirements

### Requirement: Dynamically spawned NetworkBehaviours have NetworkIdentity
Any GameObject passed to NetworkServer.Spawn() SHALL have a NetworkIdentity component attached.

#### Scenario: UnitSpawner is spawned correctly
- **WHEN** NetworkGameManager creates a runtime UnitSpawner
- **AND** adds a NetworkIdentity component to the GameObject
- **THEN** NetworkServer.Spawn() succeeds without error

### Requirement: Player creation does not require a real prefab asset
The system SHALL create host/join player objects without requiring a prefab asset file with a valid GUID.

#### Scenario: Host player is created on host
- **WHEN** the host clicks "Host Game"
- **AND** the server starts
- **THEN** OnServerAddPlayer creates the player from NetworkBootstrap.CachedPlayerPrefab
- **THEN** NetworkServer.AddPlayerForConnection succeeds without "empty assetid" error
