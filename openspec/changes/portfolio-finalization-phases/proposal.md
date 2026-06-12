## Why

RealmCommander의 핵심 네트워크 수직 슬라이스는 검증 이력이 있지만 최근 입력과 이동 변경 이후 재검증이 필요하며, RPG 모듈은 포트폴리오 완료 기능과 프로토타입 범위가 혼재되어 있다. 제출 가능한 상태를 만들기 위해 1v1 재검증, 최소 영웅 기능, 범위 표기, 빌드 및 영상 증거를 단계별 완료 조건으로 고정한다.

## What Changes

- Phase 2에서 독립 Windows Player 두 프로세스로 Host/Client 1v1 소유권, 자원 격리, 양방향 이동 동기화를 재검증한다.
- Phase 3에서 서버 권한 영웅 1기와 공격 스킬 1개, 자가 회복 스킬 1개를 실제 씬과 UI에 연결한다.
- Phase 4에서 Inventory와 Quest를 완료 기능이 아닌 `Prototype`으로 코드, UI, 문서에 일관되게 표시한다.
- Phase 5에서 Windows 제출 빌드, 검증 로그, 스크린샷 및 60~90초 영상 촬영 체크리스트를 확보한다.

## Capabilities

### New Capabilities

- `multiplayer-revalidation`: 최신 빌드에서 Host/Client 1v1 네트워크 동작을 재현하고 PASS 근거를 남기는 요구사항
- `minimal-network-hero`: 서버 권한 영웅 1기와 두 개의 제한된 스킬을 플레이 가능한 형태로 제공하는 요구사항
- `prototype-scope-labeling`: Inventory와 Quest를 프로토타입 범위로 명확히 표시하는 요구사항
- `portfolio-evidence`: 제출 빌드와 영상 촬영에 필요한 재현 가능한 증거 패키지 요구사항

### Modified Capabilities


## Impact

- `Assets/Scripts/Network`, `Assets/Scripts/RPG/Hero`, `Assets/Scripts/UI/SkillBar`, `Assets/Scripts/RPG/Inventory`, `Assets/Scripts/RPG/Quest`
- `Assets/Scenes/MainScene.unity`, 영웅 프리팹 또는 런타임 생성 경로
- `Docs/PortfolioValidation.md`, `Docs/ProjectDirection.md`, `Docs/TestCases.md`, `README.md`
- `Builds/Windows`, `Logs`, 포트폴리오 영상 촬영 문서
