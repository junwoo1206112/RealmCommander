# Dependency Audit — Singleton Lifecycle & Cross-Scene Persistence

## Singleton Lifecycle Table

| Component | Instance Type | Created By | DontDestroyOnLoad | Destroyed When |
|-----------|--------------|------------|-------------------|----------------|
| `NetworkGameManager` | `static Instance` | Scene object (MainScene) | ✅ Yes (Awake) | Scene unload (if DontDestroyOnLoad fails) |
| `CombatManager` | `static Instance` | Scene object (MainScene) | ✅ Yes (Awake) | Scene unload (if DontDestroyOnLoad fails) |
| `SelectionManager` | `static Instance` | EnsureManagers() or Editor setup | ✅ Yes (Awake) | Never (explicit Destroy on duplicate) |
| `CommandManager` | `static Instance` | EnsureManagers() or Editor setup | ✅ Yes (Awake) | Never (explicit Destroy on duplicate) |
| `CommandInput` | `GameObject.Find()` | EnsureManagers() or Editor setup | ✅ Yes (Awake) | Never (DontDestroyOnLoad) |
| `BoxSelector` | `GameObject.Find()` | EnsureManagers() or Editor setup | ✅ Yes (Awake) | Never (DontDestroyOnLoad) |

## Creation Order (Host Game Startup)

```
1. LobbyScene loads
2. LobbyUI.Start()
3. NetworkBootstrap.EnsureNetworkManager()
   → Creates NetworkManager (DontDestroyOnLoad)
   → Sets onlineScene = "MainScene.unity"
4. User clicks "Host Game"
5. NetworkManager.StartHost()
6. Mirror connects local client → OnServerAddPlayer()
   → Creates Player prefab with NetworkPlayer (teamId=0)
   → AssignExistingUnits() for team 0
7. Scene changes to MainScene
   → DontDestroyOnLoad objects persist (NetworkManager, SelectionManager, CommandManager)
   → New scene's GameObjects load
8. NetworkGameManager.Awake()
   → Instance = this, DontDestroyOnLoad
   → EnsureManagers()
     → SelectionManager (if missing)
     → CommandManager (if missing)
     → CommandInput (if missing)
     → BoxSelector (if missing)
9. Mirror spawns scene NetworkIdentities
10. NetworkGameManager.OnStartServer()
    → EnsureUnitSpawner()
      → Creates UnitSpawner (runtime GO)
      → SpawnUnitsNow(): 5 friendly + 5 enemy units
    → EnsureEnemyAI()
      → Creates AIController (runtime GO)
11. CombatManager.Awake()
    → Instance = this, DontDestroyOnLoad
12. Unit.Start() (for each unit)
    → Registers with SelectionManager
    → Subscribes to CommandManager events
13. AIController.Start()
    → Finds player base
    → Registers existing enemy units
```

## Auto-Creation Safety Net

`NetworkGameManager.EnsureManagers()` (static, called in Awake):

```csharp
SelectionManager.Instance == null  → new GameObject + AddComponent → DontDestroyOnLoad
CommandManager.Instance == null   → new GameObject + AddComponent → DontDestroyOnLoad
GameObject.Find("CommandInput")   → new GameObject + AddComponent → DontDestroyOnLoad
GameObject.Find("BoxSelector")    → new GameObject + AddComponent → DontDestroyOnLoad
```

**Design note:** All four auto-created components call `DontDestroyOnLoad()` in their own Awake, ensuring they survive any subsequent scene transition. This eliminates the need for Editor setup scripts at runtime.

## Cross-Scene Persistence Analysis

### DontDestroyOnLoad Survivors

| Component | Survives Lobby→Main? | Survives Main→Lobby? | Notes |
|-----------|---------------------|---------------------|-------|
| NetworkManager (Mirror) | ✅ Yes | ✅ Yes | Created by NetworkBootstrap |
| SelectionManager | ✅ Yes | ✅ Yes | Auto-created if missing |
| CommandManager | ✅ Yes | ✅ Yes | Auto-created if missing |
| CommandInput | ✅ Yes | ✅ Yes | Auto-created if missing |
| BoxSelector | ✅ Yes | ✅ Yes | Auto-created if missing |
| NetworkGameManager | ✅ Yes | ✅ Yes | Scene object + DontDestroyOnLoad |
| CombatManager | ✅ Yes | ✅ Yes | Scene object + DontDestroyOnLoad |
| NetworkPlayer (Player prefab) | ✅ Yes | ✅ Yes | Mirror handles this |

### Scene-Bound Objects (Destroyed on Scene Change)

| Component | Scene | Destroyed When | Risk |
|-----------|-------|---------------|------|
| Unit (all 15) | MainScene | Return to lobby | ✅ Watch for null Unit references |
| AIController | MainScene (runtime GO) | Return to lobby | ✅ Recreated by EnsureEnemyAI() |
| UnitSpawner | MainScene (runtime GO) | Return to lobby | ✅ Recreated by EnsureUnitSpawner() |

### Stale-Reference Risks

| Risk | Scenario | Impact |
|------|----------|--------|
| ⚠️ Unit reference after scene change | Any script holding a Unit reference from MainScene after returning to lobby | NullReferenceException on access |
| ⚠️ NetworkBehaviour.isServer after shutdown | `NetworkServer.active` becomes false but `isServer` still true on destroyed objects | Warning in NetworkIdentity.OnDestroy (patched with `&& NetworkServer.active`) |
| ✅ NetworkGameManager.Instance | DontDestroyOnLoad ensures valid reference | Safe |
| ✅ CombatManager.Instance | DontDestroyOnLoad ensures valid reference | Safe |
| ✅ SelectionManager.Instance | DontDestroyOnLoad ensures valid reference | Safe |
| ✅ CommandManager.Instance | DontDestroyOnLoad ensures valid reference | Safe |

## Key Findings

1. **All singletons now have DontDestroyOnLoad** — No stale-reference risks for manager access
2. **Auto-creation safety net** — `EnsureManagers()` guarantees all components exist, eliminating Editor-setup dependency
3. **No circular dependencies** — The dependency matrix shows no cycles
4. **Singleton access order matters** — EnsureManagers() must run before any Unit.Start() that accesses SelectionManager or CommandManager. This holds because NetworkGameManager.Awake() runs before Unit.Start()
