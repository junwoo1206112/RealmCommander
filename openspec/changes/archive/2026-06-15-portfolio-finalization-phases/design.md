## Context

프로젝트는 Mirror 서버 권한형 모바일 RTS 수직 슬라이스를 핵심으로 한다. 현재 방향은 RPG 확장 기능을 제거하고 유닛/건물/자원/전투/로비 중심의 1v1 RTS를 제출 범위로 고정하는 것이다.

## Goals / Non-Goals

**Goals:**
- 최신 Windows 빌드에서 Host/Client 1v1 PASS 로그를 다시 확보한다.
- RTS 범위 밖 기능을 현재 완료 범위에서 제거한다.
- README와 문서가 RTS 1v1 기능만 완료 기능으로 표현하게 한다.
- 동일 절차로 재생산 가능한 빌드 및 영상 증거 문서를 남긴다.

**Non-Goals:**
- RPG 확장 기능 또는 별도 스킬 UI를 다시 구현하지 않는다.
- Inventory/Quest를 완료 기능으로 복구하지 않는다.
- 공인 WAN/NAT, Relay, 전용 서버를 이번 change에서 추가하지 않는다.
- 영상 파일을 자동 편집하거나 Git에 대용량 바이너리로 포함하지 않는다.

## Decisions

### RTS 범위 밖 기능은 제거된 범위로 고정한다

RPG 확장 기능은 현재 포트폴리오의 설득력을 높이기보다 범위를 흐리는 요소다. 삭제된 관련 코드와 프리팹을 복구하지 않고, 문서에서도 완료 기능으로 표현하지 않는다.

### RTS 1v1 검증을 우선한다

소유권, 자원 격리, Host/Client 이동 왕복, 전투/생산 루프가 제출 영상과 로그의 중심이다. PASS 근거가 없는 기능은 README 완료 목록에 넣지 않는다.

### 과거 archive는 이력으로 둔다

OpenSpec archive와 오래된 handoff 문서는 과거 결정의 기록일 수 있다. 현재 상태를 판단하는 문서는 README, Docs의 최신 문서, 활성 change를 기준으로 한다.

## Risks / Trade-offs

- [포트폴리오 기능 수가 줄어 보임] → 네트워크 권한, 소유권, 자원 격리, RTS 조작의 완성도를 더 명확하게 보여준다.
- [과거 검증 로그에 제거된 범위의 PASS가 남아 있음] → 최신 문서에서는 범위 정리 이후 재검증 필요로 명시한다.
- [스킬 CSV가 남아 있음] → 현재 완료 기능에는 포함하지 않고, 필요하면 후속 정리에서 제거한다.

## Migration Plan

1. RTS 범위 밖 활성 코드와 메뉴를 제거한다.
2. README와 Docs의 현재 문서에서 완료 범위 밖 기능 표현을 제거한다.
3. OpenSpec 활성 change를 RTS scope cleanup 요구사항으로 정리한다.
4. Unity compile log를 확보한다.
5. Windows 빌드와 Host/Client smoke를 다시 수행한다.
6. 검증 보고서와 영상 촬영 체크리스트를 갱신한다.

## Open Questions

- 과거 archive 문서의 이전 범위 이력까지 삭제할지, 변경 이력으로 보존할지는 별도 결정한다.
