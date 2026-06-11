## ADDED Requirements

### Requirement: Singleton lifecycle audit table

The dependency-audit document SHALL contain a table listing every singleton in the project with its creation point, DontDestroyOnLoad status, and destruction conditions.

#### Scenario: All singletons listed
- **WHEN** the audit table is read
- **THEN** it SHALL contain at least: NetworkGameManager, CombatManager, SelectionManager, CommandManager

#### Scenario: Each entry has lifecycle columns
- **WHEN** a singleton row is read
- **THEN** it SHALL have columns: Component, Instance Type, Created By, Has DontDestroyOnLoad, Destroyed When

### Requirement: Creation order documented

The audit SHALL document the creation order of all singletons during a typical host game startup flow.

#### Scenario: Startup sequence captured
- **WHEN** the audit is read
- **THEN** it SHALL list singletons in the order they are created from Lobby Start through game scene load and unit spawning

#### Scenario: NetworkGameManager auto-creates managers
- **WHEN** the sequence is traced
- **THEN** the audit SHALL note that NetworkGameManager.Awake() calls EnsureManagers() which creates SelectionManager, CommandManager, CommandInput, and BoxSelector if missing

### Requirement: Cross-scene persistence analysis

The audit SHALL document which singletons survive scene transitions and which are destroyed.

#### Scenario: DontDestroyOnLoad status verified
- **WHEN** the persistence section is read
- **THEN** it SHALL list every component that calls DontDestroyOnLoad and explain what happens to its references after a scene change

#### Scenario: Stale reference risk flagged
- **WHEN** a singleton does NOT call DontDestroyOnLoad but is referenced across scenes
- **THEN** it SHALL be flagged as a stale-reference risk with the specific failure scenario documented
