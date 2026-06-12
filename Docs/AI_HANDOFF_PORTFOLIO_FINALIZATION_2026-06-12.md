# RealmCommander 포트폴리오 마무리 AI Handoff

작성일: 2026-06-12  
프로젝트: `C:\Users\admin\Unity\RealmCommander`  
Unity: `6000.3.11f1`  
핵심 OpenSpec change: `openspec/changes/portfolio-finalization-phases/`

## 1. 다음 AI가 가장 먼저 할 일

1. `AGENTS.md`를 읽고 OpenSpec workflow를 따른다.
2. `openspec/changes/portfolio-finalization-phases/proposal.md`, `design.md`, `specs/`, `tasks.md`를 읽는다.
3. 현재 worktree가 매우 dirty하므로 기존 변경을 되돌리거나 덮어쓰지 않는다.
4. 아래의 "남은 작업"부터 이어서 수행한다.
5. 최종적으로 Unity compile, Windows build, Host+Client smoke, OpenSpec strict validate를 다시 실행한다.

## 2. 완료된 작업

### Phase 2: 네트워크 1v1 재검증

최신 Windows Development Build의 독립 Host/Client 두 프로세스 검증이 완료됐다.

검증 로그:

- `Logs/Phase2Build.log`
- `Logs/Phase2Host.log`
- `Logs/Phase2Client.log`
- 최종 기능 포함 검증: `Logs/FinalPortfolioBuild.log`, `Logs/FinalHost.log`, `Logs/FinalClient.log`

최종 PASS 근거:

```text
[PortfolioBuild] PASS size=159428598
RESOURCE_ISOLATION_PASS teams=0,1
HERO_SKILLS_PASS heroes=2 arcDamage=55.0 heal=70.0
HOST_MOVE_PASS targetError=0.28
CLIENT_PASS team=1 movementRoundTrip=ok targetError=0.27
HOST_PASS players=2 teams=0,1 ownership=ok targetError=0.26 replicationError=0.00
```

네트워크 재검증 도중 발견하여 수정한 중요 버그:

- `RealmCommanderNetworkManager.OnServerAddPlayer()`가 연결 수만 보고 두 번째 Player 생성을 거부하던 race condition 수정
- `NetworkGameManager`가 identity가 아직 없는 연결을 생성 완료 Player로 계산하던 문제 수정
- 게임 시작 판정을 `NetworkConnectionToClient.identity`가 생성된 실제 Player 수 기준으로 변경

### Phase 3: 최소 네트워크 영웅 기반 구현

현재 구현된 범위:

- 팀당 `CommanderHero` 1기 자동 생성
- 각 Hero를 해당 팀 NetworkConnection에 소유권 할당
- `Assets/Resources/CommanderHero.prefab` 생성
- `NetworkBootstrap`에서 Hero prefab을 Mirror spawn prefab으로 등록
- `UnitSpawner`에서 팀 0/1 Hero 생성
- Hero 선택, 이동 명령, 공격 대상 명령 연결
- `Arc Strike`: 사거리 내 적 대상 서버 권한 피해 55
- `Rally Heal`: 서버 권한 자가 회복 70
- 마나, 체력, 레벨, 경험치와 두 스킬 cooldown SyncVar 연결
- Host smoke에서 Hero 2기 소유권과 두 스킬 효과 검증 완료

주요 파일:

- `Assets/Scripts/RPG/Hero/Hero.cs`
- `Assets/Resources/CommanderHero.prefab`
- `Assets/Scripts/Editor/PortfolioHeroBuilder.cs`
- `Assets/Scripts/Core/UnitSpawner.cs`
- `Assets/Scripts/Network/NetworkBootstrap.cs`
- `Assets/Scripts/Network/NetworkGameManager.cs`
- `Assets/Scripts/Network/MultiplayerSmokeLauncher.cs`
- `Assets/Scripts/Core/SelectionManager.cs`
- `Assets/Scripts/RTS/Unit/BoxSelector.cs`
- `Assets/Scripts/UI/SkillBar/SkillBarUI.cs`

### Phase 4: Prototype 범위 표기

현재 반영된 내용:

- Inventory component menu를 `Realm Commander/Prototype/Inventory`로 표시
- Inventory UI를 `Realm Commander/Prototype/Inventory UI`로 표시
- QuestManager component menu를 `Realm Commander/Prototype/Quest Manager`로 표시
- MainScene 이름을 `QuestManager (Prototype)`으로 변경
- `README.md`, `Docs/ProjectDirection.md`, `Docs/GDD.md`에 완료 Hero 범위와 Prototype 범위를 분리

Prototype의 의미:

- Inventory: 저장, 드롭, 상점, 실제 경제 루프가 없는 Prototype
- Quest: 저장 및 핵심 1v1 gameplay loop와 연결되지 않은 Prototype

## 3. 남은 작업

### A. SkillBar UI 실제 연결 확인 및 보정

가장 먼저 확인해야 한다.

- `MainScene.unity`에는 `SkillBar_Panel`, `Skill0`~`Skill3` 오브젝트가 존재한다.
- `SkillBarUI.cs`는 로컬 소유 Hero를 런타임 검색하도록 수정됐다.
- 아직 씬의 `SkillBar_Panel`에 `SkillBarUI` 컴포넌트와 두 버튼 callback이 실제 연결됐는지 수동/런타임 확인하지 못했다.
- 완료 범위는 스킬 두 개뿐이므로 UI도 두 슬롯만 활성화하는 편이 좋다.
- Skill 0은 현재 Hero의 `CurrentTarget`을 대상으로 `Arc Strike`를 요청한다.
- Skill 1은 target 없이 `Rally Heal`을 요청한다.
- 플레이어가 적을 우클릭한 뒤 Hero가 공격 대상으로 기억하고, Skill 0 버튼으로 피해가 발생하는 흐름을 확인한다.
- 필요하면 단축키 `Q`, `W` 또는 버튼 label을 추가한다.

완료 조건:

- Host와 Client에서 자신의 Hero UI만 조작 가능
- 체력/마나 bar가 SyncVar 변경을 반영
- Arc Strike 버튼은 적 대상이 유효할 때만 성공
- Rally Heal 버튼은 손상된 Hero에서만 성공
- cooldown overlay가 두 Client에서 일관되게 보임

### B. Hero 시각 및 gameplay 수동 검증

자동 smoke는 기능 판정을 통과했지만 다음은 화면에서 확인해야 한다.

- 팀 0 Hero는 노란색, 팀 1 Hero는 주황/빨간색으로 구분되는지
- Hero 선택 indicator가 정상 표시되는지
- Hero가 우클릭 위치로 이동하는지
- Hero 공격 대상 명령과 일반 유닛 명령이 충돌하지 않는지
- Arc Strike와 Rally Heal에 최소한의 화면 피드백이 있는지
- Hero가 사망했을 때 선택/UI가 이상하게 남지 않는지

권장 최소 polish:

- Arc Strike: 대상 위 짧은 번개/원형 flash
- Rally Heal: Hero 주위 초록 pulse
- 스킬 사용 로그 대신 화면에서 읽히는 feedback 추가

### C. OpenSpec task 상태 갱신

`openspec/changes/portfolio-finalization-phases/tasks.md`의 Phase 3/4/5 체크박스는 실제 진행보다 뒤처져 있다.

현재 증거상 완료로 바꿀 수 있는 항목:

- `2.1`, `2.2`, `2.3`, `2.4`, `2.5`
- `3.1`, `3.2`, `3.3`
- `4.1`, `4.2`

단, `2.4`의 Skill UI 연결은 A 항목을 화면에서 검증한 뒤 최종 완료 처리하는 것이 안전하다.

남아 있는 Phase 5 task:

- `4.3` 60~90초 영상 촬영 체크리스트와 evidence folder guide
- `4.4` `PortfolioValidation.md`, `TestCases.md` 최종 근거 및 한계 갱신
- `4.5` OpenSpec strict validate와 최종 Unity compile log

### D. 포트폴리오 영상 문서 작성

새 문서 권장 경로:

- `Docs/PortfolioVideoCaptureGuide.md`

권장 60~90초 구성:

1. 0~8초: MainMenu와 Host/Client Lobby 연결
2. 8~20초: 팀별 유닛 선택과 서로 다른 이동 명령
3. 20~35초: Client 명령이 Host 화면에도 동기화되는 장면
4. 35~50초: Commander Hero 선택과 Arc Strike
5. 50~62초: Hero 피해 후 Rally Heal, 마나/cooldown UI
6. 62~75초: 건물 또는 전투 장면과 서버 권한 설명
7. 75~90초: 기술 스택 및 자동 smoke PASS 로그 캡처

영상 파일은 Git에 직접 넣지 않는 것을 권장한다. 저장 경로 예시:

- `Portfolio/Video/RealmCommander_1v1_VerticalSlice.mp4`
- README에는 YouTube 또는 Google Drive 공개 링크만 추가

### E. 문서 최종 정리

확인할 문서:

- `README.md`
- `Docs/PortfolioValidation.md`
- `Docs/ProjectDirection.md`
- `Docs/TestCases.md`
- `Docs/GDD.md`
- `Docs/Architecture.md`

특히 `Docs/Architecture.md`에는 Inventory와 Quest가 아직 일반 완성 모듈처럼 보이는 다이어그램/설명이 남아 있다. `Prototype` 표시를 추가해야 한다.

`Docs/PortfolioValidation.md`에 최종 추가할 근거:

```text
실행일: 2026-06-12
Build size: 159428598 bytes
Hero: heroes=2, Arc Strike damage=55.0, Rally Heal=70.0
Host movement targetError=0.28
Client movement targetError=0.27
Replication error=0.00
```

### F. 최종 검증 및 Git 정리

최종 명령:

```powershell
cmd /c openspec validate portfolio-finalization-phases --strict
```

Unity compile:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'C:\Users\admin\Unity\RealmCommander' `
  -logFile 'Logs\FinalCompile.log'
```

Windows build:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'C:\Users\admin\Unity\RealmCommander' `
  -executeMethod RealmCommander.Editor.PortfolioBuildUtility.BuildWindowsPortfolioPlayer `
  -logFile 'Logs\FinalPortfolioBuild.log'
```

Host:

```powershell
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-host --rc-timeout=60 -logFile Logs\FinalHost.log
```

Client:

```powershell
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-client --rc-address=127.0.0.1 --rc-timeout=60 -logFile Logs\FinalClient.log
```

필수 PASS 문자열:

- `[PortfolioBuild] PASS`
- `RESOURCE_ISOLATION_PASS`
- `HERO_SKILLS_PASS heroes=2`
- `HOST_MOVE_PASS`
- `CLIENT_PASS`
- `HOST_PASS`

최종 검증 전에는 commit/push하지 않는다. 현재 worktree에는 이 작업 이전부터 존재한 다수 변경과 recovery/generated 항목이 있으므로 staging 대상을 파일별로 세밀하게 확인해야 한다.

## 4. 중요 구현 주의사항

- `UnitSpawner`는 MainScene 직렬화 오브젝트가 아니라 `NetworkGameManager.EnsureUnitSpawner()`가 런타임 생성한다.
- Hero prefab은 반드시 `Assets/Resources/CommanderHero.prefab`에 유지한다.
- `NetworkBootstrap.ConfigureNetworkPrefabs()`가 Unit과 CommanderHero를 Mirror spawn prefab으로 등록한다.
- Host/Client 최대 2인 판정은 연결 수가 아니라 identity가 생성된 Player 수를 사용해야 한다.
- Hero 스킬은 Client 결과를 신뢰하지 않고 서버에서 마나, cooldown, 대상, 거리, 적대 관계를 검증해야 한다.
- Inventory와 Quest를 완료 기능으로 다시 표현하지 않는다.
- 자동 smoke 종료 시 Telepathy의 `WSACancelBlockingCall` 메시지는 정상 종료 과정에서 발생할 수 있다.

## 5. 현재 판단

네트워크 1v1과 최소 Hero 두 스킬의 자동 기능 검증은 완료됐다. 남은 핵심은 SkillBar UI의 실제 화면 연결, 시각 피드백, 문서 정리, 영상 촬영 가이드, 최종 검증 및 선택적 commit/push다.
