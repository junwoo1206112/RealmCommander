## 1. Mirror Networking Setup

- [x] 1.1 Install Mirror networking package via Unity Package Manager
- [x] 1.2 Create NetworkManager prefab with network address/port configuration
- [x] 1.3 Create LobbyManager script for room host/join logic
- [x] 1.4 Create NetworkPlayer script with player connection tracking
- [x] 1.5 Add NetworkBehaviour inheritance to Unit.cs with ownership checks
- [x] 1.6 Add NetworkBehaviour inheritance to Building.cs with ownership checks
- [x] 1.7 Add NetworkBehaviour inheritance to Hero.cs with ownership checks
- [x] 1.8 Implement server-authoritative combat validation in CombatManager
- [x] 1.9 Implement networked resource synchronization in ResourceManager
- [x] 1.10 Create NetworkGameManager for game session lifecycle (start/end/disconnect)

## 2. UI Polish: Menus and Lobby

- [x] 2.1 Create MainMenu scene with title, Start Game, and Quit buttons
- [x] 2.2 Create Lobby scene with Host Game, Join Game (IP input), and Back buttons
- [x] 2.3 Create GameResultUI overlay with Victory/Defeat display and navigation
- [x] 2.4 Implement scene transition flow (MainMenu → Lobby → GameScene → Lobby)

## 3. Visual Feedback and Effects

- [x] 3.1 Implement unit spawn visual effect at building exit point
- [x] 3.2 Implement basic skill cast visual effects (Fireball, Heal, Shield, Lightning, Ice Storm)
- [x] 3.3 Add AudioManager singleton and sound effect playback on key actions

## 4. AI Enemy Units

- [x] 4.1 Create AIController script for basic enemy behavior (move toward base, attack)
- [x] 4.2 Implement AI difficulty presets (Easy, Normal, Hard) with stat modifiers

## 5. Portfolio Documentation and Build

- [x] 5.1 Run and record test case execution results in Docs/TestCases.md
- [x] 5.2 Update README.md with current feature status, setup guide, and build instructions
- [x] 5.3 Configure EditorBuildSettings with correct scene order
- [ ] 5.4 Verify standalone build succeeds (requires Unity Editor)
