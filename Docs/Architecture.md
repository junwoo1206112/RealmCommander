# 아키텍처 문서
## 영웅의 전장 (Realm Commander)

---

## 1. 시스템 아키텍처 개요

```
┌─────────────────────────────────────────────────────────────────┐
│                         Presentation Layer                       │
│  ┌─────────┐  ┌──────────┐  ┌──────────┐  ┌───────────────┐   │
│  │ HUD UI  │  │ Minimap  │  │ SkillBar │  │  InventoryUI  │   │
│  └────┬────┘  └────┬─────┘  └────┬─────┘  └──────┬────────┘   │
└───────┼────────────┼─────────────┼───────────────┼─────────────┘
        │            │             │               │
┌───────┼────────────┼─────────────┼───────────────┼─────────────┐
│       ▼            ▼             ▼               ▼             │
│                        Game Logic Layer                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │ Selection   │  │  Command    │  │       Game Manager      │ │
│  │  Manager    │  │  Manager    │  │  (Singleton, Lifecycle) │ │
│  └──────┬──────┘  └──────┬──────┘  └─────────────────────────┘ │
│         │                │                                      │
│  ┌──────┴────────────────┴──────────────────────────────────┐  │
│  │                      Entity Layer                         │  │
│  │  ┌─────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  │  │
│  │  │  Unit   │  │   Hero   │  │ Building │  │ Resource │  │  │
│  │  │ (RTS)   │  │  (RPG)   │  │  (RTS)   │  │  (RTS)   │  │  │
│  │  └─────────┘  └──────────┘  └──────────┘  └──────────┘  │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                     Data Layer                            │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │  │
│  │  │   Quest     │  │  Inventory  │  │  ScriptableObj  │  │  │
│  │  │  Manager    │  │   System    │  │     Data        │  │  │
│  │  └─────────────┘  └─────────────┘  └─────────────────┘  │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
        │
┌───────┼─────────────────────────────────────────────────────────┐
│       ▼              Network Layer (Mirror)                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  NetworkManager  │  NetworkIdentity  │  ClientRpc/ServerRpc │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. 디자인 패턴

### 2.1 Singleton Pattern
전역 관리자가 필요한 클래스에 적용

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

**적용 클래스:**
- GameManager
- SelectionManager
- CommandManager
- ResourceManager
- QuestManager

### 2.2 Observer Pattern (이벤트 시스템)
객체 간 느슨한 결합을 위한 이벤트 기반 통신

```csharp
// 이벤트 정의
public event Action<float, float> OnHealthChanged;
public event Action<List<GameObject>> OnSelectionChanged;

// 이벤트 발생
OnHealthChanged?.Invoke(currentHealth, maxHealth);

// 이벤트 구독
ResourceManager.Instance.OnGoldChanged += UpdateGoldUI;
```

**적용 클래스:**
- Unit (OnHealthChanged, OnDeath)
- SelectionManager (OnSelectionChanged)
- ResourceManager (OnGoldChanged, OnManaChanged)
- Hero (OnStatsChanged, OnLevelUp)
- QuestManager (OnQuestAccepted, OnQuestCompleted)

### 2.3 Component Pattern
유닛, 영웅 등의 엔티티를 독립적 컴포넌트로 구성

```csharp
// Unit 컴포넌트
public class Unit : MonoBehaviour
{
    // NavMeshAgent는 별도 컴포넌트로 분리
    private NavMeshAgent agent;
    
    // 유닛 고유의 스탯과 로직
    public float MaxHealth { get; private set; }
    public void TakeDamage(float damage) { ... }
}
```

---

## 3. 클래스 다이어그램

### 3.1 Core 시스템

```
┌─────────────────────────────────────────────────────────────┐
│                         GameManager                          │
├─────────────────────────────────────────────────────────────┤
│ - Instance: GameManager                                      │
│ - IsPaused: bool                                             │
│ - GameSpeed: float                                           │
├─────────────────────────────────────────────────────────────┤
│ + StartGame(): void                                          │
│ + PauseGame(): void                                          │
│ + ResumeGame(): void                                         │
│ + SetGameSpeed(float): void                                  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                      SelectionManager                        │
├─────────────────────────────────────────────────────────────┤
│ - Instance: SelectionManager                                 │
│ - selectedUnits: List<GameObject>                            │
│ - selectableUnits: HashSet<GameObject>                       │
├─────────────────────────────────────────────────────────────┤
│ + SelectUnit(GameObject): void                               │
│ + AddToSelection(GameObject): void                           │
│ + ClearSelection(): void                                     │
│ + SelectUnitsInBox(Rect): void                               │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                       CommandManager                         │
├─────────────────────────────────────────────────────────────┤
│ - Instance: CommandManager                                   │
├─────────────────────────────────────────────────────────────┤
│ + IssueMoveCommand(Vector3): void                            │
│ + IssueAttackCommand(GameObject): void                       │
│ + ProcessRightClick(Vector3, RaycastHit): void               │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 RTS 시스템

```
┌─────────────────────────────────────────────────────────────┐
│                            Unit                              │
├─────────────────────────────────────────────────────────────┤
│ - maxHealth: float                                           │
│ - currentHealth: float                                       │
│ - attackDamage: float                                        │
│ - moveSpeed: float                                           │
│ - agent: NavMeshAgent                                        │
├─────────────────────────────────────────────────────────────┤
│ + TakeDamage(float): void                                    │
│ + Heal(float): void                                          │
│ + SetTarget(GameObject): void                                │
│ + SetSelected(bool): void                                    │
└─────────────────────────────────────────────────────────────┘
         ▲
         │ 상속
┌─────────────────────────────────────────────────────────────┐
│                           Hero                               │
├─────────────────────────────────────────────────────────────┤
│ - heroData: HeroData                                         │
│ - skills: List<SkillData>                                    │
├─────────────────────────────────────────────────────────────┤
│ + GainExp(float): void                                       │
│ + TryCastSkill(int, GameObject): bool                        │
│ + LevelUp(): void                                            │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 RPG 시스템 (Prototype)

> **참고:** Inventory와 Quest 시스템은 현재 Prototype 상태입니다.
> 저장, 드롭, 상점, 실제 경제 루프가 없으며 1v1 gameplay loop와 연결되지 않았습니다.

```
┌─────────────────────────────────────────────────────────────┐
│                 Inventory (Prototype)                        │
├─────────────────────────────────────────────────────────────┤
│ - items: List<InventorySlot>                                 │
│ - equipment: InventorySlot[]                                 │
├─────────────────────────────────────────────────────────────┤
│ + AddItem(ItemData, int): bool                               │
│ + RemoveItem(string, int): bool                              │
│ + EquipItem(int): bool                                       │
│ + UnequipItem(int): bool                                     │
│ + GetEquipmentBonuses(): (float, float, float, float)        │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│               QuestManager (Prototype)                       │
├─────────────────────────────────────────────────────────────┤
│ - availableQuests: List<QuestData>                           │
│ - activeQuests: List<QuestData>                              │
│ - completedQuests: List<QuestData>                           │
├─────────────────────────────────────────────────────────────┤
│ + AcceptQuest(string): bool                                  │
│ + UpdateObjectiveProgress(QuestObjectiveType, string, int)   │
│ + TurnInQuest(string): QuestReward                           │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. 데이터 흐름

### 4.1 유닛 선택 → 이동 명령 흐름

```
[사용자 입력]
      │
      ▼
┌─────────────┐     ┌─────────────────┐
│ BoxSelector │────▶│ SelectionManager│
│ (Input)     │     │ (Selection)     │
└─────────────┘     └────────┬────────┘
                             │ OnSelectionChanged
                             ▼
                      ┌─────────────┐
                      │   HUD UI    │
                      │ (Update)    │
                      └─────────────┘

[우클릭 입력]
      │
      ▼
┌─────────────┐     ┌─────────────────┐
│ CommandInput│────▶│ CommandManager  │
│ (Input)     │     │ (Command)       │
└─────────────┘     └────────┬────────┘
                             │ OnMoveCommand
                             ▼
                      ┌─────────────┐
                      │    Unit     │
                      │ (Movement)  │
                      └─────────────┘
                             │
                             ▼
                      ┌─────────────┐
                      │ NavMeshAgent│
                      │ (Pathfind)  │
                      └─────────────┘
```

### 4.2 전투 흐름

```
[공격 명령]
      │
      ▼
┌─────────────┐     ┌─────────────────┐
│ CommandMgr  │────▶│      Unit       │
│             │     │ (SetTarget)     │
└─────────────┘     └────────┬────────┘
                             │
                             ▼
                      ┌─────────────┐
                      │   Update    │
                      │ (Distance   │
                      │  Check)     │
                      └──────┬──────┘
                             │
              ┌──────────────┴──────────────┐
              ▼                              ▼
       [범위 내]                        [범위 밖]
              │                              │
              ▼                              ▼
       ┌─────────────┐               ┌─────────────┐
       │  TryAttack  │               │   Move To   │
       │  (Damage)   │               │   Target    │
       └──────┬──────┘               └─────────────┘
              │
              ▼
       ┌─────────────┐
       │  Target     │
       │ TakeDamage  │
       └──────┬──────┘
              │
              ▼
       ┌─────────────┐
       │  OnDeath?   │
       └─────────────┘
```

---

## 5. 네트워킹 아키텍처 (Mirror)

### 5.1 네트워크 토폴로지

```
┌─────────────────────────────────────────────────────────────┐
│                        Host Server                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              NetworkManager (Host)                   │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │   │
│  │  │  Game State │  │   Players   │  │    Units    │  │   │
│  │  │  (Authoritative) │  (Sync)   │  │  (SyncVar)  │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
              ▲                           ▲
              │                           │
         ClientRpc                   Command
              │                           │
┌─────────────┴───────────────────────────┴───────────────────┐
│                                                              │
│  ┌─────────────────┐              ┌─────────────────┐       │
│  │   Client 1      │              │   Client 2      │       │
│  │  (Local Player) │              │  (Remote Player)│       │
│  └─────────────────┘              └─────────────────┘       │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### 5.2 동기화 전략

| 데이터 | 동기화 방식 | 설명 |
|--------|-------------|------|
| 유닛 위치 | SyncVar + Cmd | 호스트가 권한, 클라이언트는 예측 |
| 유닛 HP | SyncVar | 모든 클라이언트에 자동 동기화 |
| 플레이어 명령 | Cmd (Command) | 클라이언트 → 호스트 요청 |
| 게임 이벤트 | ClientRpc | 호스트 → 모든 클라이언트 브로드캐스트 |

---

## 6. 성능 최적화

### 6.1 오브젝트 풀링
```csharp
public class ObjectPool<T> where T : Component
{
    private Queue<T> pool = new Queue<T>();
    
    public T Get() { ... }
    public void Return(T item) { ... }
}
```

**적용 대상:**
- 투사체 (화살표, 마법진)
- 이펙트 (피격, 폭발)
- 데미지 숫자

### 6.2 NavMesh 최적화
- NavMeshSurface를 청크로 분할
- 동적 장애물은 NavMeshObstacle 사용
- 유닛 그룹화는 NavMeshAgent.groupingEnabled

### 6.3 UI 최적화
- Canvas를 정적/동적으로 분리
- 자주 업데이트되는 UI는 별도 Canvas
- Raycast Target 최적화

---

**문서 버전:** 1.0  
**최종 수정일:** 2026-06-08  
**작성자:** junwoo1206112
