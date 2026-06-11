## 1. Fix playerPrefab assignment in NetworkBootstrap

- [x] 1.1 Assign _cachedPlayerPrefab to NetworkManager.playerPrefab and set autoCreatePlayer in EnsureNetworkManager()

## 2. Fix unit spawning in NetworkGameManager

- [x] 2.1 Create UnitSpawner dynamically in NetworkGameManager.OnStartServer() if none exists in scene

## 3. Fix game state transition with GameManager integration

- [x] 3.1 Call GameManager.Instance.StartGame() in NetworkGameManager.StartGame()
- [x] 3.2 Remove late playerPrefab assignment from NetworkGameManager.Update() since it's now handled by NetworkBootstrap

## 4. Verify LobbyUI null safety

- [x] 4.1 Confirm LobbyUI handles all null reference cases without exceptions

## 5. Fix runtime errors

- [x] 5.1 Add NetworkIdentity to dynamically created UnitSpawner before NetworkServer.Spawn()
- [x] 5.2 Create RealmCommanderNetworkManager subclass to handle player creation without real prefab asset
