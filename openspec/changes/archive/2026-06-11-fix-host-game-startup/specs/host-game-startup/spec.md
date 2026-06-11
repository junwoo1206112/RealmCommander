## ADDED Requirements

### Requirement: Host can start a game from the lobby
The system SHALL allow the host to start a game by clicking "Host Game" in the lobby, which loads the MainScene and begins gameplay with units spawned.

#### Scenario: Host starts a game
- **WHEN** the host clicks "Host Game" in the lobby
- **THEN** the MainScene loads
- **THEN** the host player is properly created with a NetworkPlayer component
- **THEN** friendly and enemy units are spawned on the terrain
- **THEN** the game transitions from WaitingForPlayers to Playing state after the auto-start delay
- **THEN** the HUD shows resource UI and the game is playable

#### Scenario: Auto-start after timeout
- **WHEN** the MainScene loads after hosting
- **AND** the auto-start delay (10 seconds) elapses
- **THEN** the game state changes to Playing
- **THEN** the local GameManager.StartGame() is called

### Requirement: NetworkManager has playerPrefab assigned before StartHost
The NetworkBootstrap SHALL assign the created player prefab to NetworkManager.playerPrefab immediately after creating it.

#### Scenario: playerPrefab is assigned at creation time
- **WHEN** NetworkBootstrap.EnsureNetworkManager() is called
- **AND** it creates the player prefab
- **THEN** NetworkManager.playerPrefab is set to the created prefab
- **THEN** NetworkManager.autoCreatePlayer is true

### Requirement: LobbyUI handles null references gracefully
The LobbyUI SHALL function correctly even when serialized UI element references are null.

#### Scenario: LobbyUI works with missing UI references
- **WHEN** LobbyUI loads with null serialized references (statusText, ipInputField, etc.)
- **THEN** Host Game and Join Game buttons remain functional
- **THEN** no NullReferenceException is thrown
