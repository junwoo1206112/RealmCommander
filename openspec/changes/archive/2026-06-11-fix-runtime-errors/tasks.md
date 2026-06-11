## 1. Fix NetworkIdentity on runtime UnitSpawner

- [x] 1.1 Add NetworkIdentity component to dynamically created UnitSpawner before NetworkServer.Spawn()

## 2. Fix PlayerPrefab empty assetid error

- [x] 2.1 Create RealmCommanderNetworkManager subclass overriding OnServerAddPlayer for manual player creation
- [x] 2.2 Update NetworkBootstrap to use RealmCommanderNetworkManager and remove playerPrefab/autoCreatePlayer
