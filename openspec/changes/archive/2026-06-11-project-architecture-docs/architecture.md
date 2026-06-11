# System Architecture — Realm Commander

## Phase Map (8 Subsystems)

```
Input ──→ Selection ──→ Command ──→ Movement ──→ Combat
  │                                                   
  └──────────── Network Foundation ──── Spawning ──────┘
                                                         
  AI (enemy-only, runs parallel on server)
```

---

## Phase 1: Network Foundation

**Namespace:** `RealmCommander.Network`
**Classes:** `NetworkBootstrap`, `RealmCommanderNetworkManager`, `NetworkPlayer`, `NetworkGameManager`, `CombatManager`

### Responsibilities
- Initialize Mirror NetworkManager with Telepathy transport
- Manage player connections and team assignment (max 2 players)
- Host game mode (StartHost) loads `MainScene.unity` as online scene
- Singleton lifecycle: NetworkGameManager, CombatManager
- Network authority: ALL operations `[Server]` or `[ClientRpc]`

### Key Data Flow
```
LobbyUI.Start()
  → NetworkBootstrap.EnsureNetworkManager()
  → RealmCommanderNetworkManager.OnServerAddPlayer()
  → NetworkGameManager.OnStartServer()
    → EnsureUnitSpawner()
    → EnsureEnemyAI()
```

### Singleton Status
| Component | DontDestroyOnLoad | Created By |
|-----------|-------------------|------------|
| NetworkGameManager | ✅ Yes | Scene object (MainScene) |
| CombatManager | ✅ Yes | Scene object (MainScene) |

---

## Phase 2: Spawning

**Namespace:** `RealmCommander.Core`, `RealmCommander.Network`
**Classes:** `UnitSpawner`, `RealmCommanderNetworkManager`

### Responsibilities
- Spawn 5 friendly + 5 enemy units from `Resources/Unit.prefab`
- Assign team via `Unit.ConfigureTeam(bool)`
- Assign network ownership to player connections
- Scene units (Unit_1–5) assigned via `AssignExistingUnits()`

### Key Data Flow
```
NetworkGameManager.EnsureUnitSpawner()
  → UnitSpawner.SpawnUnitsNow()
    → Instantiate(prefab) → ConfigureTeam() → NetworkServer.Spawn(unit, owner)
```

### Network Authority
- `[Server]` — all spawning operations
- Friendly units owned by team 0 connection
- Enemy units owned by team 1 connection (or unowned if team 1 absent)

---

## Phase 3: Player Input

**Namespace:** `RealmCommander.RTS`
**Classes:** `CommandInput`, `BoxSelector`, `MobileRTSInput`

### Responsibilities
- Right-click → raycast all layers → `ProcessRightClick()` → Move or Attack command
- Left-click drag → `BoxSelector` → `SelectUnitsInBox()`
- `Physics.RaycastAll` for right-click (first hit enemy = attack; non-unit hit = move)

### Key Data Flow
```
Right-click → CommandInput.HandleRightClick()
  → Physics.RaycastAll(ray, combinedMask)
  → foreach hit:
      enemy unit → IssueAttackCommand(enemy)
      no enemy  → IssueMoveCommand(first non-unit hit.point)
```

### Singleton Status
| Component | DontDestroyOnLoad | Created By |
|-----------|-------------------|------------|
| CommandInput | ✅ Yes | EnsureManagers() or Editor setup |
| BoxSelector | ✅ Yes | EnsureManagers() or Editor setup |

---

## Phase 4: Selection

**Namespace:** `RealmCommander.Core`
**Classes:** `SelectionManager`

### Responsibilities
- Track `selectableUnits` (HashSet) and `selectedUnits` (List)
- Box selection via `SelectUnitsInBox(Rect)` — iterates all Units via FindObjectsByType, filters by IsAlive && CanIssueLocalCommands
- Single-click selection via `Unit.OnMouseDown()` or `BoxSelector.HandleSingleClick()`
- Auto-register unit on first selection if Start() missed registration

### Key Data Flow
```
BoxSelector.CompleteSelection()
  → SelectionManager.SelectUnitsInBox(rect)
    → cam.WorldToScreenPoint(unit.position) → Contains check → add to selection

Unit.OnMouseDown()
  → SelectionManager.SelectUnit(gameObject)
    → if not registered → auto-register → ClearSelection() → add
```

### Network Authority
- **Client-side** — selection is purely local (each player selects their own units)
- `CanIssueLocalCommands` guards which units a player can select

---

## Phase 5: Command

**Namespace:** `RealmCommander.Core`
**Classes:** `CommandManager`

### Responsibilities
- Event bus for commands: `OnMoveCommand`, `OnAttackCommand`, `OnBuildCommand`
- Routes right-click hits: enemy unit → `IssueAttackCommand()`, else → `IssueMoveCommand()`

### Key Data Flow
```
CommandManager.ProcessRightClick(hit.point, hitInfo)
  → hitInfo.collider.GetComponent<Unit>()?.IsEnemy?
    → YES: IssueAttackCommand(target)
    → NO:  IssueMoveCommand(hit.point)
```

### Singleton Status
| Component | DontDestroyOnLoad | Created By |
|-----------|-------------------|------------|
| CommandManager | ✅ Yes | EnsureManagers() or Editor setup |

---

## Phase 6: Movement

**Namespace:** `RealmCommander.RTS`
**Classes:** `Unit`

### Responsibilities
- `TrySetDestination(Vector3)` — NavMeshAgent movement with null/disabled/off-navmesh checks
- `HandleMoveCommand(position)` — ClearTarget → formation offset (Random.insideUnitSphere * 2f) → TrySetDestination
- `HandleAttackCommand(target)` — SetTarget(target) → chase/attack loop
- `AutoAcquireTarget()` — every 0.5s, scan for enemies within attackRange*2.5 (1.5s grace after player command)
- `Update()` — server-side combat pursuit loop

### Key Data Flow
```
Move Command:
  HandleMoveCommand(position)
    → lastCommandTime = now
    → ClearTarget()
    → dest = position + Random.insideUnitSphere * 2f
    → TrySetDestination(dest)

Attack Command:
  HandleAttackCommand(target)
    → lastCommandTime = now
    → SetTarget(target)
    → currentTarget = target
    → TrySetDestination(target.position)

Update() loop:
  if currentTarget != null:
    if IsValidHostileTarget(target):
      if distance <= attackRange: TryAttack()
      else: TrySetDestination(target.position)
    else: ClearTarget()
  else:
    AutoAcquireTarget()  // 1.5s grace after player command
```

### Network Authority
- `[Server]` — `TakeDamage()`, `Die()`, `ConfigureTeam()`
- `[Command]` — `CmdMove()`, `CmdSetTarget()` (client requests)
- `[ClientRpc]` — `RpcOnDeath()`
- `Update()` runs only when `!(NetworkClient.active && !isServer)` (server/host only)

---

## Phase 7: Combat

**Namespace:** `RealmCommander.Network`
**Classes:** `CombatManager`

### Responsibilities
- Validate attack: range check (attackRange * 1.2), team check, alive check
- Apply damage via `[Server] ApplyCombatDamage()`
- Sync damage event via `[ClientRpc] RpcOnDamageApplied(uint netId, float damage)`

### Key Data Flow
```
Unit.TryAttack()
  → CombatManager.ApplyCombatDamage(attacker, target, damage)
    → ValidateAttack(attacker, target)
    → target.TakeDamage(damage)
    → if targetNetId != 0: RpcOnDamageApplied(targetNetId, damage)
    → client: NetworkClient.spawned.TryGetValue(netId, out identity)
```

### Validation Chain
| Check | Location |
|-------|----------|
| Same team? | `ValidateAttack()` — `attackerIsEnemy != targetIsEnemy` |
| In range? | `ValidateAttack()` — `distance < attackRange * 1.2` |
| Alive? | `ValidateAttack()` — `target.IsAlive` |
| Valid target? | `IsValidHostileTarget()` — also checks alive + team |

---

## Phase 8: AI

**Namespace:** `RealmCommander.AI`
**Classes:** `AIController`

### Responsibilities
- Update every 1s: find nearest friendly unit (within 15) → SetTarget; else march to player base
- Spawn new units every 5s (scaled by difficulty)
- Register existing enemy units every 2s
- Auto-cleanup dead units

### Key Data Flow
```
AIController.Update()
  → UpdateAI() every 1s:
    foreach controlled unit:
      FindNearestEnemy(unit.position) within 15? → SetTarget
      else: agent.SetDestination(playerBase + randomOffset * 2)
  
  → TrySpawnUnit() every spawnInterval:
    Instantiate(prefab) → ConfigureTeam(true) → NetworkServer.Spawn
```

### Network Authority
- `[Server]` only — `if (!NetworkServer.active) return;`
- Controls enemy units only (skips `unit.IsEnemy == false`)

---

## Dependency Adjacency Matrix

| Component | Creates | Event Sub | Singleton | RPC | Instantiate |
|-----------|---------|-----------|-----------|-----|-------------|
| NetworkBootstrap | NetworkManager | — | — | — | — |
| RealmCommanderNetworkManager | Player prefab | — | — | — | — |
| NetworkGameManager | UnitSpawner, AIController | — | SelectionMgr, CommandMgr, CommandInput, BoxSelector | — | — |
| UnitSpawner | Unit prefab x10 | — | — | — | Instantiate |
| AIController | AI unit prefabs | — | — | — | Instantiate |
| CommandInput | — | — | CommandManager | — | — |
| BoxSelector | — | — | SelectionManager | — | — |
| SelectionManager | — | OnSelectionChanged | — | — | — |
| CommandManager | — | OnMoveCommand, OnAttackCommand | — | — | — |
| Unit | SelectionIndicator | MoveCmd, AttackCmd | CombatManager, SelectionMgr, CommandMgr | CmdMove, CmdSetTarget, RpcOnDeath | — |
| CombatManager | — | — | — | RpcOnDamageApplied | — |
| MobileRTSInput | — | — | CommandManager, SelectionManager | — | — |

**Key:** C=Creates, E=Event subscription, S=Singleton reference, R=RPC call, I=Instantiate/Spawning

---

## Bug Database

| # | Bug | Root Cause | Fix | Verification |
|---|-----|------------|-----|-------------|
| 1 | RPC: "unspawned GameObject" | `RpcOnDamageApplied(GameObject)` — Mirror can't serialize destroyed GameObjects | Changed to `RpcOnDamageApplied(uint netId)` — capture netId BEFORE TakeDamage | Damage RPC works without warnings; client looks up via `NetworkClient.spawned` |
| 2 | Compile: `isSpawned` not found | This Mirror version lacks `NetworkIdentity.isSpawned` | Used `netId != 0` + capture before damage instead | Compiles and runs |
| 3 | CommandInput NRE | `mainCamera` null (no MainCamera tag); `Camera.main` returned null | Added fallback chain: Camera.main → FindFirstObjectByType → warning log | Right-click works with any camera |
| 4 | Drag selection broken | `selectionMask` field inserted BETWEEN existing fields — binary scene uses INDEX-based serialization, shifting all subsequent field indices | Moved `selectionMask` to END of field list; added `DontDestroyOnLoad` | Left-click drag selects units |
| 5 | Selection always clears | `MobileRTSInput.HandleTap()` with `simulateTouchInEditor=true` hit ground → skipped ProcessRightClick → **fell through to ClearSelection()** | Added `return` after guard so ClearSelection is never reached | Selection persists through taps |
| 6 | Units "run away" (don't fight) | No auto-acquisition: after move command, `currentTarget=null`, enemies approach but `TryAttack()` never called | Added `AutoAcquireTarget()` — scans every 0.5s, 1.5s grace after player commands | Units fight back when enemies are near |
| 7 | Units converge on exact point | All selected units receive same position → `TrySetDestination(same)` → overlap | Added `Random.insideUnitSphere * 2f` formation offset per unit | Units spread around destination |
| 8 | Stale singleton after scene reload | NetworkGameManager, CombatManager lacked `DontDestroyOnLoad` | Added `DontDestroyOnLoad(gameObject)` in Awake | Instance persists across scene changes |
| 9 | NetworkTransform on Unit missing | Unit prefab had no NetworkTransform component in YAML | Added `NetworkTransformReliable` component to prefab | Client sees server transform changes |
| 10 | BoxSelector missing from runtime | Created by Editor script only → destroyed on scene transition | NetworkGameManager.EnsureManagers() creates all managers at runtime if missing | Components exist in game scene |
