## 1. System Architecture Document

- [x] 1.1 Create phase map document with all 8 subsystems: NetworkFoundation, Spawning, PlayerInput, Selection, Command, Movement, Combat, AI
- [x] 1.2 Document each phase with namespace, primary classes, and responsibilities
- [x] 1.3 Create data flow maps: Input → Selection → Command → Movement → Combat, with arrow direction conventions (→ ⇒ ⇢)
- [x] 1.4 Document cross-phase interfaces (event names, method signatures)
- [x] 1.5 Build dependency adjacency matrix with all components, marking dependency types (C/E/S/R/I)
- [x] 1.6 Document network authority per operation (Server/Client/Host/All)
- [x] 1.7 Create bug database with all known issues: root cause, fix, verification

## 2. Dependency Audit

- [x] 2.1 Create singleton lifecycle audit table: Component, Instance Type, Created By, DontDestroyOnLoad, Destroyed When
- [x] 2.2 Document creation order during host game startup flow
- [x] 2.3 Note NetworkGameManager.EnsureManagers() auto-creation behavior
- [x] 2.4 Cross-scene persistence analysis — flag stale-reference risks

## 3. Review and Publish

- [x] 3.1 Review all documents for accuracy against current codebase
- [x] 3.2 Add document references to README or project documentation index
