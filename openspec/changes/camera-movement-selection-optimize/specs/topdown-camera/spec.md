## ADDED Requirements

### Requirement: 탑다운 카메라 시점
카메라 SHALL 피치 75도의 탑다운 시점을 유지한다.

#### Scenario: 초기 카메라 위치
- **WHEN** 게임이 시작된다
- **THEN** 카메라는 y=35 높이에서 피치 75도로 설정된다

### Requirement: 카메라 팬
시스템은 WASD 또는 마우스 휠 클릭 드래그로 카메라를 이동할 수 있어야 한다(MUST).

#### Scenario: 키보드 팬
- **WHEN** 플레이어가 WASD를 누른다
- **THEN** 카메라가 해당 방향으로 이동한다

#### Scenario: 마우스 팬
- **WHEN** 플레이어가 마우스 휠을 클릭하고 드래그한다
- **THEN** 카메라가 드래그 방향으로 이동한다

### Requirement: 카메라 줌
시스템은 마우스 휠로 카메라 높이를 조절할 수 있어야 한다(MUST).

#### Scenario: 줌 인
- **WHEN** 플레이어가 마우스 휠을 위로 굴린다
- **THEN** 카메라가 아래로 내려온다 (높이 감소)

#### Scenario: 줌 아웃
- **WHEN** 플레이어가 마우스 휠을 아래로 굴린다
- **THEN** 카메라가 올라간다 (높이 증가)

### Requirement: 카메라 회전
시스템은 Q/E 키로 시점을 좌우 회전할 수 있어야 한다(MUST).

#### Scenario: 좌회전
- **WHEN** 플레이어가 Q를 누른다
- **THEN** 카메라가 반시계방향으로 회전한다

#### Scenario: 우회전
- **WHEN** 플레이어가 E를 누른다
- **THEN** 카메라가 시계방향으로 회전한다
