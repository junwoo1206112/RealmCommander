# Realm Commander

Unity 6와 Mirror로 제작한 서버 권한형 모바일 RTS 포트폴리오입니다. 핵심 목표는 Host/Client 두 플레이어가 각자 소유한 유닛을 선택하고 이동·공격하며, 서버가 권한과 전투 결과를 검증하는 작은 1v1 수직 슬라이스입니다.

## 검증된 핵심 기능

- `MainMenu -> Lobby -> MainScene` Host 흐름과 자동 게임 시작
- Mirror Host/Client 연결, TCP `7777`, 플레이어별 `team 0/1` 배정
- 서버가 생성한 플레이어 및 유닛 소유권
- 팀별 Gold/Mana 분리와 서버 권한 생산 비용 처리
- 건물 팀/소유권 동기화와 팀별 유닛 생산
- 비소유 유닛 명령 차단과 서버 권한 이동·전투
- `NetworkTransformReliable` 기반 위치 동기화
- PC 박스 선택/우클릭 명령과 모바일 터치 선택/명령
- 모바일 두 손가락 카메라, Safe Area, 가로 화면 대응
- NavMesh 이동, 적 AI, CSV 기반 유닛·건물·스킬 데이터
- 연결 주소와 상태를 표시하는 Host/Client 로비

## 실제 검증 결과

2026년 6월 11일, Unity `6000.3.11f1` Windows Development Build에서 다음을 확인했습니다.

| 검증 | 결과 |
|---|---|
| Windows Player 빌드 | PASS, 약 159 MB |
| Host TCP `0.0.0.0:7777` Listen | PASS |
| 별도 Client -> `192.168.0.90:7777` | PASS |
| 별도 Client -> `100.80.202.35:7777` | PASS |
| 플레이어 `team 0/1` 및 유닛 소유권 | PASS |
| 팀별 자원 격리 및 건물 권한 일치 | PASS |
| Client 이동 요청 -> Server 수신 -> 양쪽 위치 반영 | PASS |

자동 검증은 클라이언트가 소유한 동일 `netId` 유닛의 이동을 양쪽에서 확인한 뒤에만 성공합니다. 자세한 절차와 근거는 [Docs/PortfolioValidation.md](Docs/PortfolioValidation.md)에 있습니다.

> `100.80.202.35` 검증은 이 PC의 오버레이 네트워크 주소를 사용한 별도 프로세스 테스트입니다. 실제 다른 물리 PC 및 공인 인터넷 NAT/공유기 포트포워딩까지 증명한 결과는 아닙니다.

## 실행

1. Unity Hub에서 Unity `6000.3.11f1`로 프로젝트를 엽니다.
2. `Assets/Scenes/MainMenuScene.unity`를 실행합니다.
3. Host는 로비에서 `Host`를 선택합니다.
4. Client는 Host 화면에 표시되는 LAN IP와 포트 `7777`을 입력합니다.

다른 네트워크에서 직접 접속하려면 Host 방화벽의 TCP `7777` 인바운드 허용과 공유기 포트포워딩이 필요합니다. 포트 개방을 요구하지 않는 배포를 목표로 한다면 Relay 또는 전용 서버 전송 계층이 추가로 필요합니다.

Windows 빌드는 Unity 메뉴 `Tools > Realm Commander > Build Windows Portfolio Player`에서 생성합니다. 결과물은 Git에서 제외된 `Builds/Windows/RealmCommander.exe`에 만들어집니다.

## 기술 구조

| 영역 | 구현 |
|---|---|
| Engine | Unity 6, C# |
| Network | Mirror, Telepathy TCP, Server Authority |
| Gameplay | Unit, Building, CombatManager, NetworkGameManager |
| Movement | NavMeshAgent, NetworkTransformReliable |
| Input/UI | PC RTS 입력, Mobile RTS 입력, UGUI |
| Data | CSV + Resources, SpecManager |
| Verification | Host flow smoke test, standalone two-process multiplayer smoke test |

프로젝트 소유 코드는 `Assets/Scripts`에 있으며 `Assets/Mirror`는 벤더링된 외부 라이브러리입니다. 세부 설계는 [Docs/Architecture.md](Docs/Architecture.md), 범위 판단은 [Docs/ProjectDirection.md](Docs/ProjectDirection.md)를 참고하세요.

## 범위와 남은 한계

- RPG의 Hero, Inventory, Quest, Skill 코드는 프로토타입 모듈이며 현재 핵심 1v1 완료 기능으로 주장하지 않습니다.
- 경쟁형 자원은 팀별로 분리됐지만, 원격 Client의 신규 건물 배치는 아직 지원하지 않으며 UI에서 명시적으로 차단합니다.
- 실제 외부 장비 테스트, 공인 WAN/NAT 통과, 자동 EditMode/PlayMode 테스트는 후속 검증 항목입니다.
- 포트폴리오 제출 전 대표 스크린샷과 60~90초 플레이 영상을 추가해야 합니다.

## 프로젝트 문서

- [검증 보고서](Docs/PortfolioValidation.md)
- [프로젝트 방향](Docs/ProjectDirection.md)
- [아키텍처](Docs/Architecture.md)
- [테스트 시나리오](Docs/TestCases.md)
- [모바일 RTS 지원 근거](Docs/ESTgamesMobileRTSApplication.md)

개발자: `junwoo1206112`
GitHub: <https://github.com/junwoo1206112>
