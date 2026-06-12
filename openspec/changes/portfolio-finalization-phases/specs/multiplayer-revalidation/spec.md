## ADDED Requirements

### Requirement: 독립 Host Client 1v1 재검증
시스템은 최신 Windows Player 두 프로세스에서 Host와 Client 연결을 재검증해야 한다(MUST).

#### Scenario: 두 플레이어 연결 및 팀 배정
- **WHEN** Host smoke와 Client smoke를 TCP 7777로 실행한다
- **THEN** 서버는 두 연결에 서로 다른 team 0과 team 1을 배정한다

### Requirement: 소유권과 자원 격리 검증
시스템은 각 플레이어의 유닛 및 건물 소유권과 팀별 자원 격리를 검증해야 한다(MUST).

#### Scenario: 권한과 자원 검증
- **WHEN** 두 플레이어가 MainScene에 접속한다
- **THEN** 각 팀의 객체 소유권이 해당 연결과 일치하고 한 팀의 시험 차감이 다른 팀 자원을 변경하지 않는다

### Requirement: 양방향 이동 동기화 검증
시스템은 Host 소유 유닛과 Client 소유 유닛의 이동 결과를 서버와 Client 양쪽에서 검증해야 한다(MUST).

#### Scenario: Client 이동 왕복
- **WHEN** Client가 소유 유닛에 이동 명령을 보낸다
- **THEN** Host와 Client가 동일 netId의 목표 도착과 위치 복제를 확인한다

#### Scenario: Host 이동
- **WHEN** Host가 소유 유닛에 이동 명령을 보낸다
- **THEN** 서버 유닛이 허용 오차 안에서 목표에 도착한다
