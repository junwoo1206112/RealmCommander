## ADDED Requirements

### Requirement: 팀당 최소 영웅 1기
시스템은 각 팀에 소유권이 지정된 영웅 1기를 제공해야 한다(MUST).

#### Scenario: 영웅 생성
- **WHEN** Host와 Client가 MainScene에 접속한다
- **THEN** 각 플레이어는 자신의 팀 영웅 1기만 로컬 명령으로 제어할 수 있다

### Requirement: 공격 스킬 Arc Strike
영웅은 사거리 내 적에게 서버 권한 피해를 주는 `Arc Strike`를 제공해야 한다(MUST).

#### Scenario: 유효한 공격 스킬
- **WHEN** 소유 플레이어가 충분한 마나와 준비된 쿨다운으로 사거리 내 적을 지정한다
- **THEN** 서버가 마나와 쿨다운을 적용하고 대상에게 한 번 피해를 준다

#### Scenario: 유효하지 않은 공격 스킬
- **WHEN** 대상이 아군이거나 사거리 밖이거나 마나가 부족하다
- **THEN** 서버는 피해와 자원 소모를 적용하지 않는다

### Requirement: 자가 회복 스킬 Rally Heal
영웅은 자신을 회복하는 `Rally Heal`을 제공해야 한다(MUST).

#### Scenario: 회복 스킬 사용
- **WHEN** 살아 있는 영웅이 체력이 부족하고 충분한 마나와 준비된 쿨다운으로 Rally Heal을 사용한다
- **THEN** 서버가 최대 체력을 넘지 않는 범위에서 영웅을 회복한다

### Requirement: 영웅 상태 동기화
시스템은 영웅의 체력, 마나, 레벨과 스킬 사용 결과를 Host와 Client에 동기화해야 한다(MUST).

#### Scenario: 스킬 사용 상태 반영
- **WHEN** 서버가 영웅 스킬을 승인한다
- **THEN** 소유 Client UI에 변경된 마나, 체력 및 쿨다운이 반영된다
