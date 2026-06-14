## Why

RealmCommander의 제출 범위는 서버 권한형 RTS 1v1 수직 슬라이스로 축소한다. RPG 확장 기능과 별도 스킬 UI는 현재 코드와 검증 상태를 복잡하게 만들고 완료 기능으로 유지하지 않기로 했다. 따라서 활성 change는 RTS 범위 정리, RTS 검증 재확보, 포트폴리오 증거 정리를 목표로 한다.

## What Changes

- Phase 2에서 독립 Windows Player 두 프로세스로 Host/Client 1v1 소유권, 자원 격리, 양방향 이동 동기화를 재검증한다.
- Phase 3에서 RPG 확장 기능과 별도 스킬 UI를 완료 범위와 현재 문서에서 제거한다.
- Phase 4에서 Inventory와 Quest도 완료 기능이 아닌 제외 범위로 명확히 표시한다.
- Phase 5에서 Windows 제출 빌드, 검증 로그, 스크린샷 및 60~90초 RTS 영상 촬영 체크리스트를 확보한다.

## Capabilities

### New Capabilities

- `multiplayer-revalidation`: 최신 빌드에서 Host/Client 1v1 네트워크 동작을 재현하고 PASS 근거를 남기는 요구사항
- `rts-scope-cleanup`: RTS 범위 밖 완료 표현을 제거하고 RTS 1v1 범위만 문서화하는 요구사항
- `portfolio-evidence`: 제출 빌드와 영상 촬영에 필요한 재현 가능한 증거 패키지 요구사항

### Modified Capabilities

없음.

## Impact

- `Assets/Scripts/Editor`
- `Docs/PortfolioValidation.md`, `Docs/ProjectDirection.md`, `Docs/TestCases.md`, `Docs/GDD.md`, `Docs/Architecture.md`, `README.md`
- `openspec/changes/portfolio-finalization-phases`
- `Builds/Windows`, `Logs`, 포트폴리오 영상 촬영 문서
