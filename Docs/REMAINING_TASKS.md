# RealmCommander 포트폴리오 마무리 남은 작업

작성일: 2026-06-12
프로젝트: `C:\Users\admin\Unity\RealmCommander`
Unity: `6000.3.11f1`

## 현재 상태 요약

- **완료된 작업**: Phase 2 (네트워크 1v1 재검증), Phase 3 대부분 (영웅 구현), Phase 4 (Prototype 범위 표기)
- **진행 중**: SkillBar UI 연결 및 시각 피드백
- **OpenSpec 상태**: `portfolio-finalization-phases` 변경 진행 중 (완료 4/17 태스크)

## 남은 작업 목록

### 1. SkillBar UI 실제 연결 및 검증 (가장 먼저)

**목적**: SkillBarUI 컴포넌트를 씬에 연결하고 버튼 callback 설정

**작업 내용**:
- Unity 에디터에서 `Tools > Realm Commander > Setup SkillBar UI` 메뉴 실행
- SkillBar_Panel에 SkillBarUI 컴포넌트 자동 추가
- Skill0/1에 CooldownOverlay + CooldownText 자동 생성
- Button callback `OnSkillClicked(0/1)` 자동 연결
- HealthBar/ManaBar/ExpBar/LevelText Slider/Text 연결

**확인 사항**:
- Host와 Client에서 자신의 Hero UI만 조작 가능
- 체력/마나 bar가 SyncVar 변경을 반영
- Arc Strike 버튼은 적 대상이 유효할 때만 성공
- Rally Heal 버튼은 손상된 Hero에서만 성공
- cooldown overlay가 두 Client에서 일관되게 보임

### 2. Hero 시각 및 gameplay 수동 검증

**목적**: 화면에서 실제 gameplay 확인

**확인 사항**:
- 팀 0 Hero는 노란색, 팀 1 Hero는 주황/빨간색으로 구분
- Hero 선택 indicator 정상 표시
- Hero가 우클릭 위치로 이동
- Hero 공격 대상 명령과 일반 유닛 명령이 충돌하지 않음
- Arc Strike와 Rally Heal에 최소한의 화면 피드백
- Hero가 사망했을 때 선택/UI가 이상하게 남지 않음

**권장 최소 polish**:
- Arc Strike: 대상 위 짧은 번개/원형 flash
- Rally Heal: Hero 주위 초록 pulse
- 스킬 사용 로그 대신 화면에서 읽히는 feedback 추가

### 3. OpenSpec task 상태 갱신

**완료로 변경 가능한 항목**:
- `2.1`, `2.2`, `2.3`, `2.5` (영웅 구현)
- `3.1`, `3.2`, `3.3` (Prototype 범위 표기)
- `4.1`, `4.2` (빌드 및 smoke)

**미완료 유지 항목**:
- `2.4` (SkillBar UI 연결 - 화면 검증 후 완료)
- `4.3` (영상 촬영 체크리스트)
- `4.4` (PortfolioValidation/TestCases 갱신)
- `4.5` (OpenSpec strict validate)

### 4. 포트폴리오 영상 촬영 가이드 작성

**권장 경로**: `Docs/PortfolioVideoCaptureGuide.md`

**60~90초 구성**:
1. 0~8초: MainMenu와 Host/Client Lobby 연결
2. 8~20초: 팀별 유닛 선택과 서로 다른 이동 명령
3. 20~35초: Client 명령이 Host 화면에도 동기화되는 장면
4. 35~50초: Commander Hero 선택과 Arc Strike
5. 50~62초: Hero 피해 후 Rally Heal, 마나/cooldown UI
6. 62~75초: 건물 또는 전투 장면과 서버 권한 설명
7. 75~90초: 기술 스택 및 자동 smoke PASS 로그 캡처

**저장 경로**:
- `Portfolio/Video/RealmCommander_1v1_VerticalSlice.mp4`
- README에는 YouTube 또는 Google Drive 공개 링크만 추가

### 5. 문서 최종 정리

**확인할 문서**:
- `README.md`
- `Docs/PortfolioValidation.md`
- `Docs/ProjectDirection.md`
- `Docs/TestCases.md`
- `Docs/GDD.md`
- `Docs/Architecture.md`

**특히 수정 필요**:
- `Docs/Architecture.md`: Inventory와 Quest에 `Prototype` 표시 추가
- `Docs/PortfolioValidation.md`: 최종 검증 결과 추가

**PortfolioValidation.md에 추가할 근거**:
```text
실행일: 2026-06-12
Build size: 159428598 bytes
Hero: heroes=2, Arc Strike damage=55.0, Rally Heal=70.0
Host movement targetError=0.28
Client movement targetError=0.27
Replication error=0.00
```

### 6. 최종 검증 및 Git 정리

**최종 검증 명령**:
```powershell
# OpenSpec strict validate
cmd /c openspec validate portfolio-finalization-phases --strict

# Unity compile
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'C:\Users\admin\Unity\RealmCommander' `
  -logFile 'Logs\FinalCompile.log'

# Windows build
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'C:\Users\admin\Unity\RealmCommander' `
  -executeMethod RealmCommander.Editor.PortfolioBuildUtility.BuildWindowsPortfolioPlayer `
  -logFile 'Logs\FinalPortfolioBuild.log'

# Host smoke
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-host --rc-timeout=60 -logFile Logs\FinalHost.log

# Client smoke
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-client --rc-address=127.0.0.1 --rc-timeout=60 -logFile Logs\FinalClient.log
```

**필수 PASS 문자열**:
- `[PortfolioBuild] PASS`
- `RESOURCE_ISOLATION_PASS`
- `HERO_SKILLS_PASS heroes=2`
- `HOST_MOVE_PASS`
- `CLIENT_PASS`
- `HOST_PASS`

**Git 정리**:
- 최종 검증 전에는 commit/push하지 않음
- 파일별로 세밀하게 staging 대상 확인
- recovery/generated 항목 제외

## 중요 구현 주의사항

1. `UnitSpawner`는 MainScene 직렬화 오브젝트가 아니라 `NetworkGameManager.EnsureUnitSpawner()`가 런타임 생성
2. Hero prefab은 반드시 `Assets/Resources/CommanderHero.prefab`에 유지
3. `NetworkBootstrap.ConfigureNetworkPrefabs()`가 Unit과 CommanderHero를 Mirror spawn prefab으로 등록
4. Host/Client 최대 2인 판정은 연결 수가 아니라 identity가 생성된 Player 수 사용
5. Hero 스킬은 Client 결과를 신뢰하지 않고 서버에서 검증
6. Inventory와 Quest를 완료 기능으로 다시 표현하지 않음
7. 자동 smoke 종료 시 Telepathy의 `WSACancelBlockingCall` 메시지는 정상 종료 과정

## 진행 순서 권장

1. **SkillBar UI 연결** (Unity 에디터에서 Setup 스크립트 실행)
2. **화면 수동 검증** (Host/Client 두 프로세스로 gameplay 확인)
3. **OpenSpec tasks.md 업데이트** (완료된 항목 체크)
4. **문서 최종 정리** (Architecture, PortfolioValidation 등)
5. **영상 촬영 가이드 작성** (PortfolioVideoCaptureGuide.md)
6. **최종 검증** (Unity compile → Windows build → smoke)
7. **Git 정리 및 commit** (파일별 staging 후 commit)

## 완료 기준

프로젝트가 다음 조건을 모두 만족할 때 포트폴리오로 제출 가능:
- [ ] SkillBar UI가 씬에 연결되고 동작
- [ ] Host/Client 두 프로세스에서 Hero 스킬 검증
- [ ] 시각 피드백 (스킬 이펙트, cooldown 표시)
- [ ] 문서에 완료 범위와 Prototype 범위가 명확히 구분
- [ ] 60~90초 포트폴리오 영상 촬영
- [ ] 자동 smoke 테스트 모든 PASS 확인
- [ ] OpenSpec strict validate 통과
