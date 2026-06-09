## Context

Realm Commander는 RTS+RPG 하이브리드 포트폴리오 프로젝트로, 현재 Week 4 (UI 완성)까지 구현되었습니다. 남은 Week 5-6 범위의 멀티플레이, 폴리싱, 문서화 작업을 통해 이스트게임즈 및 111퍼센트 취업 지원에 필요한 포트폴리오를 완성합니다.

핵심 기술 스택: Unity 6 (6000.3.11f1), C#, Mirror Networking, UGUI, NavMesh.

## Goals / Non-Goals

**Goals:**
- Mirror 네트워킹 기반 1v1 실시간 대전 구현 (서버-클라이언트 아키텍처)
- 메인 메뉴/로비 UI 및 게임 플로우 완성
- 유닛 생산 및 스킬 시각 피드백 기초 구현
- AI 적 유닛 기초 행동 구현
- 사운드 이펙트 기초 시스템
- 포트폴리오 문서 완성 (테스트 결과, README, 빌드 설정)

**Non-Goals:**
- 풀스케일 MMORPG 네트워킹 (채팅, 길드, 파티 등)
- 서버 인프라 구축 (Dedicated Server)
- 3D 모델링/애니메이션 제작 (기존 리소스 활용)
- 밸런스 튜닝
- 모바일 빌드 최적화

## Decisions

### Network Architecture: Mirror (Authority-Based)
**Decision:** Mirror networking 라이브러리 사용, Server Authority 모델 채택.

**Rationale:**
- 기존 프로젝트가 Mirror를 네트워킹 레이어로 계획함
- C# Unity 환경에 최적화, NetworkBehaviour/RPC로 빠른 구현 가능
- Server Authority 모델로 치트 방지 및 일관된 상태 관리

**Alternatives considered:**
- Unity Netcode for GameObjects: 기능은 유사하나 Mirror에 비해 레퍼런스 부족
- Photon: 클라우드 의존성, 포트폴리오로 오프라인 데모에도 Mirror가 적합

### Multiplayer Scene Flow
**Decision:** MainMenu → Lobby → GameScene 순서의 씬 플로우, NetworkManager는 DontDestroyOnLoad.

**Rationale:**
- NetworkManager가 씬 전환 시 파괴되지 않아 연결 상태 유지
- Lobby 씬에서 Host/Join 선택 후 GameScene으로 전환
- 게임 종료 시 Lobby로 복귀

### Mixed Ownership Model
**Decision:** Player-owned entities는 Client Authority, combat/building validation은 Server Authority.

**Rationale:**
- 유닛 이동 명령은 클라이언트에서 즉시 반응 (UX)
- 전투/건설 같은 게임플레이 결정은 서버 검증 필요
- Mirror NetworkBehaviour의 isOwned/isServer 활용

### AI as Local Pseudo-Player
**Decision:** AI는 호스트 클라이언트에서만 실행되는 로컬 플레이어로 구현 (싱글플레이어 연습용).

**Rationale:**
- 포트폴리오 목적상 싱글플레이어 데모 중요
- 네트워크 AI 동기화는 복잡도 대비 효과 낮음
- 호스트가 AI 조종, 클라이언트는 AI 유닛 상태만 동기화 받음

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Mirror 패키지 Unity 6 호환성 문제 | 최신 Mirror 릴리스 확인, Unity Package Manager 통해 설치 |
| 네트워크 지연으로 인한 UX 저하 | Server Authority + 클라이언트 측 예측 이동 결합 고려 |
| AI 상태 동기화 복잡도 | AI는 호스트 전용으로 제한, 동기화 범위 최소화 |
| 빌드 시간 초과 | 불필요한 에셋 제거, incremental build 활용 |
| 기존 싱글플레이어 코드와 네트워크 코드 충돌 | 기존 로직을 NetworkBehaviour로 래핑, 조건부 컴파일로 싱글/멀티 모드 분리 |

## Open Questions

- Mirror 최신 안정 버전 확인 필요 (Unity 6 호환성)
- AI 유닛 스폰 웨이브 타이밍 및 난이도 곡선
- 빌드 타겟 플랫폼 결정 (Windows Standalone 우선, 이후 Android)
- 사운드 리소스 확보 경로 (무료 에셋 or 간단 생성)
