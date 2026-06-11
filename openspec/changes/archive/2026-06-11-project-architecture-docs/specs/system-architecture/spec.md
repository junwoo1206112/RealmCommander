## ADDED Requirements

### Requirement: Phase map documents all 8 subsystems

The architecture document SHALL contain a phase-by-phase breakdown of the 8 subsystems: NetworkFoundation, Spawning, PlayerInput, Selection, Command, Movement, Combat, AI.

#### Scenario: Phase map has 8 entries
- **WHEN** the system-architecture document is read
- **THEN** it SHALL list exactly 8 phases in data-flow order

#### Scenario: Each phase lists owning namespace
- **WHEN** a phase is described
- **THEN** it SHALL include the C# namespace and primary class names for that phase

### Requirement: Data flow maps show component interaction

Each phase document SHALL include a data flow diagram showing how data moves between components within the phase and across phase boundaries.

#### Scenario: Data flow shows direction
- **WHEN** a data flow arrow is drawn between two components
- **THEN** it SHALL indicate direction (→ for input, ⇒ for network RPC, ⇢ for events)

#### Scenario: Cross-phase flows documented
- **WHEN** data flows from one phase to another (e.g., Input → Selection)
- **THEN** the connecting interfaces SHALL be documented (event names, method signatures)

### Requirement: Dependency matrix provided

The document SHALL include an adjacency matrix showing which components reference, create, or depend on which other components.

#### Scenario: Matrix has all components
- **WHEN** the matrix is read
- **THEN** every component from all 8 phases SHALL appear as both source and target rows

#### Scenario: Matrix captures dependency type
- **WHEN** a dependency exists between two components
- **THEN** the cell SHALL indicate the type: "C" for Creates, "E" for Event subscription, "S" for Singleton reference, "R" for RPC call, "I" for Instantiate/Spawning

### Requirement: Network authority documented per operation

Each phase SHALL document which side (Server, Client, or Host) has authority for each operation.

#### Scenario: Server-authoritative operations flagged
- **WHEN** an operation only executes on the server
- **THEN** it SHALL be marked with `[Server]` in the phase document

#### Scenario: Host-only behavior documented
- **WHEN** `NetworkClient.active && isServer` creates special behavior (e.g., Update() not returning)
- **THEN** the document SHALL explain the host mode flow

### Requirement: Bug database records root cause and fix

The document SHALL include a section listing every bug identified during the bug-fixing sessions, with root cause, fix, and verification.

#### Scenario: All known bugs recorded
- **WHEN** the bug database section is read
- **THEN** it SHALL contain at least the following known issues: unspawned GameObject RPC, NetworkIdentity.isSpawned missing, CommandInput NRE, selection serialization corruption, movement override by auto-acquire, singleton lifecycle gaps, NetworkTransform on unit prefab missing

#### Scenario: Each bug entry has RC/Fix/Verification
- **WHEN** a bug is listed
- **THEN** it SHALL have: root cause description, what was changed to fix it, and how to verify it still works
