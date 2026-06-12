## Context

프로젝트는 Mirror 서버 권한형 모바일 RTS 수직 슬라이스를 핵심으로 한다. 기존 두 프로세스 smoke는 유닛 소유권과 이동을 검증하지만 최근 입력/카메라/이동 변경 이후 기준선을 다시 확보해야 한다. Hero, Inventory, Quest 코드는 존재하나 Hero는 씬 연결과 명확한 두 스킬 역할이 부족하고, 나머지 RPG 모듈은 완료 범위로 오해될 수 있다.

## Goals / Non-Goals

**Goals:**
- 최신 Windows 빌드에서 Host/Client 1v1 PASS 로그를 다시 확보한다.
- 기존 `Hero`를 서버 권한으로 유지하면서 영웅 1기와 두 스킬만 완성한다.
- Inventory/Quest의 프로토타입 상태를 사용자와 리뷰어가 즉시 알 수 있게 한다.
- 동일 절차로 재생산 가능한 빌드 및 영상 증거 문서를 남긴다.

**Non-Goals:**
- 다수 영웅, 스킬 트리, 아이템 저장, 퀘스트 진행 저장을 구현하지 않는다.
- 공인 WAN/NAT, Relay, 전용 서버를 이번 change에서 추가하지 않는다.
- 영상 파일을 자동 편집하거나 Git에 대용량 바이너리로 포함하지 않는다.

## Decisions

### Phase 2는 기존 standalone smoke를 확장하지 않고 먼저 재사용한다
이미 소유권, 자원 격리, Host/Client 이동 왕복을 검증하므로 최신 빌드로 재실행해 회귀 여부를 먼저 확인한다. 실패할 때만 smoke 범위를 수정한다.

### Hero는 별도 RPG 게임 루프가 아니라 RTS 전투 유닛으로 제한한다
영웅은 팀당 1기만 서버가 생성하고 기존 `CombatManager`, `SyncVar`, Mirror 소유권을 사용한다. 스킬은 대상 지정 공격 `Arc Strike`와 무대상 자가 회복 `Rally Heal` 두 개로 고정한다.

### 스킬 판정은 서버가 최종 검증한다
Client UI는 요청만 보내고 서버가 소유권, 생존, 마나, 재사용 대기시간, 거리, 적대 관계를 다시 검사한다. 쿨다운과 마나는 서버 상태를 기준으로 동기화한다.

### Inventory/Quest는 삭제하지 않고 Prototype으로 격리한다
코드는 학습 및 확장 근거로 보존하되 README, 문서, UI 제목과 코드 메타데이터에서 `Prototype`을 명시한다. 핵심 1v1 완료 기능 목록에는 포함하지 않는다.

### 제출 증거는 재현 가능한 텍스트와 외부 영상 파일로 분리한다
빌드/로그/체크리스트는 저장소 문서에 남기고, 영상 파일은 `Portfolio/Video` 경로 안내만 제공하며 Git 추적 대상에서 제외한다.

## Risks / Trade-offs

- [Hero 네트워크 소유권 연결 실패] → 기존 player/unit 소유권 할당 패턴을 재사용하고 Host/Client에서 각각 1기만 제어 가능한지 검증한다.
- [무대상 회복 스킬이 기존 target 검증과 충돌] → SkillData에 명시적 스킬 종류를 추가하고 서버에서 종류별 검증을 분리한다.
- [기존 dirty worktree와 변경 충돌] → 관련 파일만 수정하고 사용자 변경을 되돌리지 않는다.
- [영상 자동 확보 한계] → 빌드와 촬영 시나리오를 자동 준비하고 최종 화면 녹화는 수동 증거 단계로 명시한다.

## Migration Plan

1. 최신 Windows Player를 빌드하고 기존 Host/Client smoke를 재실행한다.
2. Hero 데이터/스킬 판정/생성/UI를 구현하고 컴파일 및 로컬 Host 검증을 수행한다.
3. Inventory/Quest 표기와 문서를 정리한다.
4. 최종 Windows 빌드와 Host/Client smoke를 다시 수행한다.
5. 검증 보고서와 영상 촬영 체크리스트를 갱신한다.

## Open Questions

- 제출 영상의 실제 저장 위치와 공개 링크는 사용자가 선택한다.
