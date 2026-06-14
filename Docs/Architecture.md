# 아키텍처 문서
## Realm Commander

## 1. 시스템 개요

Realm Commander는 Mirror 기반 서버 권한형 RTS 프로젝트다. 현재 완료 범위는 유닛, 건물, 자원, 로비, Host/Client 동기화로 구성된 1v1 수직 슬라이스다.

```text
Presentation Layer
  HUDController
  BuildingUI
  LobbyUI
  MainMenuUI
  GameResultUI
  MinimapController

Game Logic Layer
  GameManager
  SelectionManager
  CommandManager
  EntityRegistry
  RTSGameplayLoop

RTS Entity Layer
  Unit
  Building
  ResourceGenerator
  ResourceNode

Network Layer
  RealmCommanderNetworkManager
  NetworkGameManager
  NetworkPlayer
  CombatManager
  NetworkBootstrap

Data / Tools
  SpecManager
  CSV Resources
  Editor setup and validation tools
```

## 2. 주요 설계 원칙

### 서버 권한

이동 요청, 공격 대상, 데미지, 생산 비용, 자원 변경은 서버가 최종 판단한다. Client는 입력 요청과 표시를 담당한다.

### 소유권 분리

플레이어는 자신의 `teamId`에 속한 유닛과 건물만 명령할 수 있다. 비소유 유닛 명령은 `Unit.CanIssueLocalCommands`와 선택/명령 경로에서 차단한다.

### 런타임 보정

`NetworkGameManager`, `NetworkBootstrap`, `UnitSpawner`가 누락된 매니저와 시작 유닛/건물을 런타임에 보정한다. 포트폴리오 안정화 후에는 명시적 씬 구성과 검증 도구 중심으로 줄이는 것이 좋다.

## 3. 핵심 클래스

| 클래스 | 역할 |
|---|---|
| `NetworkBootstrap` | NetworkManager 생성, Player/Unit prefab 등록 |
| `RealmCommanderNetworkManager` | 플레이어 생성, 팀 배정, 기존 엔티티 소유권 연결 |
| `NetworkGameManager` | 게임 상태, 시작/종료, 런타임 매니저 보정 |
| `Unit` | 이동, 공격, 체력, 선택 표시, 팀 구분 |
| `Building` | 체력, 팀, 생산 큐, 자동 공격, 건설 상태 |
| `ResourceManager` | 팀별 Gold/Mana 관리 |
| `SelectionManager` | 선택 목록과 선택 이벤트 |
| `CommandManager` | 이동/공격/건설 명령 이벤트 |
| `CombatManager` | 서버 권한 데미지 적용과 전투 피드백 |

## 4. 데이터 흐름

### 선택과 이동

```text
Input
  -> BoxSelector / CommandInput / MobileRTSInput
  -> SelectionManager / CommandManager
  -> Unit.RequestMove
  -> CmdMove
  -> Server ApplyMoveCommand
  -> NetworkTransform 동기화
```

### 공격

```text
Input
  -> CommandManager.IssueAttackCommand
  -> Unit.CmdSetTarget
  -> Server target validation
  -> CombatManager.ApplyCombatDamage
  -> Unit/Building.TakeDamage
  -> ClientRpc feedback
```

### 생산

```text
BuildingUI or hotkey
  -> Building.QueueProduction
  -> ResourceManager.TrySpend(teamId)
  -> production timer
  -> NetworkServer.Spawn(unit, owner)
```

## 5. 제외된 구조

RPG 확장 구조, 별도 스킬 UI, 저장형 진행 시스템은 현재 아키텍처의 완료 범위에서 제거한다. 현재 구현 기준 문서는 RTS 범위만 설명한다.

## 6. 개선 과제

- `Unit`, `Building`, `NetworkGameManager`의 책임 분리
- Runtime, Editor, Tests asmdef 분리
- `GameObject.Find`와 `Resources.Load` 의존 축소
- EditMode/PlayMode 자동 테스트 추가
- Unity compile log와 two-process smoke를 CI 비슷한 절차로 고정

**문서 버전:** 2.0  
**최종 수정일:** 2026-06-14
