## ADDED Requirements

### Requirement: Windows 제출 빌드
시스템은 MainMenu, Lobby, MainScene을 포함하는 Windows x64 Development Build를 생성해야 한다(MUST).

#### Scenario: 제출 빌드 생성
- **WHEN** PortfolioBuildUtility를 실행한다
- **THEN** `Builds/Windows/RealmCommander.exe`가 성공적으로 생성되고 빌드 로그에 PASS가 기록된다

### Requirement: 검증 증거 기록
프로젝트는 최종 빌드의 Host/Client PASS 로그와 실행 환경을 문서화해야 한다(MUST).

#### Scenario: 검증 보고서 확인
- **WHEN** 리뷰어가 PortfolioValidation 문서를 확인한다
- **THEN** 실행 날짜, Unity 버전, 빌드 결과, Host/Client 핵심 PASS 로그를 확인할 수 있다

### Requirement: 영상 촬영 체크리스트
프로젝트는 60~90초 제출 영상의 장면 순서와 성공 조건을 제공해야 한다(MUST).

#### Scenario: 영상 촬영 준비
- **WHEN** 개발자가 영상 체크리스트를 따른다
- **THEN** 로비 연결, 양측 이동, 영웅 두 스킬, 승패 또는 전투 장면, 기술 요약을 순서대로 촬영할 수 있다
