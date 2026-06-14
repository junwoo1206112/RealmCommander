## ADDED Requirements

### Requirement: RTS scope cleanup
시스템과 현재 제출 문서는 RTS 범위 밖 기능을 완료 기능으로 표현하지 않아야 한다(MUST).

#### Scenario: 현재 문서 확인
- **WHEN** 리뷰어가 README, GDD, Architecture, TestCases, PortfolioValidation을 확인한다
- **THEN** RTS 범위 밖 기능이 현재 완료 범위가 아닌 제외 범위로 표시된다

### Requirement: RTS-only gameplay scope
포트폴리오 완료 범위는 유닛, 건물, 자원, 전투, 로비, Host/Client 동기화 중심의 RTS 1v1 수직 슬라이스여야 한다(MUST).

#### Scenario: 완료 기능 확인
- **WHEN** 리뷰어가 README의 검증된 핵심 기능을 확인한다
- **THEN** RTS 네트워크 기능만 완료 기능으로 확인할 수 있다
