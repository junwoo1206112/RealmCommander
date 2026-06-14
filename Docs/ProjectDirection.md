# Realm Commander 프로젝트 방향

## 결론

Realm Commander의 포트폴리오 범위는 **서버 권한형 모바일 RTS 1v1 수직 슬라이스**로 고정한다. RPG 성장, 별도 스킬 UI, Inventory, Quest는 현재 완료 범위에서 제외한다.

## 핵심 루프

1. MainMenu에서 Lobby로 이동한다.
2. Host 또는 Client로 접속한다.
3. 각 플레이어가 자기 팀 유닛과 건물을 선택한다.
4. 이동, 공격, 생산, 자원 소모를 서버 권한으로 처리한다.
5. 전투 결과와 승패를 모든 클라이언트에 동기화한다.

## 완료 기능으로 표현할 수 있는 범위

- Mirror Host/Client 연결과 `team 0/1` 배정
- 플레이어별 유닛/건물 소유권
- 비소유 유닛 명령 차단
- 서버 권한 이동, 공격, 데미지 처리
- 팀별 Gold/Mana 분리
- 건물 생산 큐와 유닛 생성
- PC 박스 선택/우클릭 명령
- 모바일 터치 선택/명령과 카메라 조작
- MainMenu, Lobby, MainScene 기본 흐름

## 제외 범위

- 별도 스킬 UI와 스킬 쿨다운 시스템
- RPG 레벨업, 장비, 인벤토리, 퀘스트
- 공인 WAN/NAT 통과, Relay, 전용 서버

## 다음 우선순위

### P0: 현재 RTS 범위 정합성

- README, GDD, Architecture, TestCases에서 RTS 범위 밖의 내용을 제거한다.
- OpenSpec의 `minimal-network-hero` 요구사항을 폐기하고 RTS 범위 정리로 대체한다.
- Unity compile log와 Host/Client smoke PASS 로그를 새로 확보한다.

### P1: 검증 신뢰성

- 자동 테스트 소스와 문서의 PASS 표기를 일치시킨다.
- 수동 테스트 문서에는 실행 날짜, Unity 버전, Host/Client 조건을 기록한다.
- 핵심 규칙을 EditMode 테스트 가능한 순수 C# 서비스로 분리한다.

### P2: 포트폴리오 완성도

- 60~90초 영상은 로비 연결, 양측 유닛 이동, 전투, 생산, smoke PASS 로그 중심으로 구성한다.
- 코드 규모가 큰 `Unit`, `Building`, `NetworkGameManager`는 안정화 후 기능 단위로 나눈다.
- Runtime, Editor, Tests asmdef를 분리한다.

## 완료 기준

README에 완료 기능으로 적는 항목은 다음 조건을 모두 만족해야 한다.

- 씬 또는 프리팹에 연결되어 있다.
- 에디터 재생에서 사용 가능하다.
- 네트워크 기능은 Host와 Client 양쪽에서 검증되었다.
- 재현 절차와 결과가 문서 또는 자동 테스트로 남아 있다.
