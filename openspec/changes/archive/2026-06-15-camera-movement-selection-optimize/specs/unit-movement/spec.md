## ADDED Requirements

### Requirement: 우클릭 이동 명령
시스템은 선택된 유닛을 우클릭 위치로 이동시켜야 한다(MUST).

#### Scenario: 단일 유닛 이동
- **WHEN** 유닛 1개를 선택하고 우클릭한다
- **THEN** 유닛이 클릭 위치로 이동한다

#### Scenario: 다중 유닛 이동
- **WHEN** 유닛 여러개를 선택하고 우클릭한다
- **THEN** 각 유닛이 포메이션을 유지하며 클릭 위치 주변으로 이동한다

### Requirement: NavMesh 경로 탐색
시스템은 유닛을 NavMesh 경로를 따라 이동시켜야 한다(MUST).

#### Scenario: NavMesh 위 이동
- **WHEN** 목표 위치가 NavMesh 위에 있다
- **THEN** 유닛이 NavMesh를 따라 이동한다

#### Scenario: NavMesh 밖 이동 시도
- **WHEN** 목표 위치가 NavMesh에서 벗어나 있다
- **THEN** 가장 가까운 NavMesh 지점으로 이동한다

### Requirement: 이동 명령 즉시 반응
시스템은 이동 명령 시 에이전트가 즉시 목표를 향해 움직이게 해야 한다(MUST).

#### Scenario: 정지 상태에서 이동
- **WHEN** 정지된 유닛에게 이동 명령을 내린다
- **THEN** 유닛이 즉시 이동을 시작한다

#### Scenario: 이동 중 새로운 명령
- **WHEN** 이동 중인 유닛에게 새로운 이동 명령을 내린다
- **THEN** 유닛이 즉시 새로운 목표로 방향을 전환한다

### Requirement: 자동 전투 탐지 유예
시스템은 이동 명령 후 일정 시간 동안 자동 전투 탐지를 억제해야 한다(MUST).

#### Scenario: 이동 중 적 탐지
- **WHEN** 이동 명령을 받은 후 1.5초 내에 적이 탐지된다
- **THEN** 유닛이 이동을 계속한다
