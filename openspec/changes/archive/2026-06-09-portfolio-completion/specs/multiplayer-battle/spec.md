## ADDED Requirements

### Requirement: Mirror networking package integration
The project SHALL integrate Mirror networking package for multiplayer functionality.

#### Scenario: Package installed
- **WHEN** developer runs the project
- **THEN** Mirror networking package SHALL be available in Packages/manifest.json

#### Scenario: NetworkManager present
- **WHEN** developer loads any multiplayer scene
- **THEN** a NetworkManager GameObject SHALL exist with network address and port configured

### Requirement: 1v1 room creation and joining
The system SHALL allow one player to host a game room and another to join via IP address.

#### Scenario: Host creates a room
- **WHEN** player clicks "Host Game" in the lobby
- **THEN** a network server SHALL start on the host machine
- **THEN** the host SHALL be placed in the game scene as Player 1

#### Scenario: Client joins a room
- **WHEN** player enters a valid host IP address and clicks "Join Game"
- **THEN** the client SHALL connect to the host server
- **THEN** the client SHALL be placed in the game scene as Player 2

### Requirement: Unit and hero state synchronization
Units, heroes, and buildings SHALL synchronize their position, health, and state across the network.

#### Scenario: Unit moves on all clients
- **WHEN** a player right-clicks to move a selected unit
- **THEN** the unit SHALL move to the target position on both players' screens
- **WHEN** both players select units simultaneously
- **THEN** each player SHALL only control their own units

#### Scenario: Health changes synchronized
- **WHEN** a unit takes damage from an attack
- **THEN** the health change SHALL be reflected on both clients within 500ms

### Requirement: Combat synchronization
Combat actions SHALL be validated server-side to prevent cheating.

#### Scenario: Attack validated by server
- **WHEN** a player commands a unit to attack an enemy unit
- **THEN** the server SHALL validate range and line-of-sight before applying damage
- **WHEN** validation passes
- **THEN** the damage SHALL be applied and broadcast to all clients

### Requirement: Game session lifecycle
The system SHALL manage game session start, end, and disconnection.

#### Scenario: Game ends when all enemy units defeated
- **WHEN** one player's units are all defeated
- **THEN** the game SHALL declare the other player as winner
- **THEN** a result screen SHALL be shown

#### Scenario: Player disconnects mid-game
- **WHEN** a player disconnects during a match
- **THEN** the remaining player SHALL be notified
- **THEN** the game SHALL end and return to lobby

### Requirement: Networked resource synchronization
Gold and mana SHALL be synchronized between server and clients.

#### Scenario: Resource change broadcast
- **WHEN** a player's resource amount changes (earns or spends)
- **THEN** the new resource value SHALL be synced to the owning client
- **WHEN** a player builds a unit or building
- **THEN** the resource cost SHALL be deducted server-side

### Requirement: Ownership-based control restriction
Each player SHALL only control entities owned by their connection.

#### Scenario: Cannot select enemy units
- **WHEN** a player clicks on an enemy unit
- **THEN** the unit SHALL NOT be selected (no highlight, no command display)
- **WHEN** a player opens the building panel of an enemy building
- **THEN** no production or interaction buttons SHALL be available
