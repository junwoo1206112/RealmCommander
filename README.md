# Realm Commander

Unity 6와 Mirror Networking으로 제작한 모바일 RTS 스타일 포트폴리오 프로젝트입니다.
Host/Client 구조에서 팀, 자원, 건설, 유닛 생산, 이동, 전투 명령을 서버 권한 중심으로 검증하는 1v1 RTS 프로토타입입니다.

## 포트폴리오 핵심

- Unity C# 기반 RTS 게임 시스템
- Mirror Networking 기반 Host/Client 멀티플레이
- 서버 권한 기반 이동, 건설, 생산, 전투 처리
- 팀별 자원 분리와 비소유 유닛 명령 차단
- PC 입력과 모바일 터치 입력 대응
- RTS 구조를 설명하기 위한 문서, 테스트, 검증 자료 포함

## 주요 구현

| 영역 | 구현 내용 |
|---|---|
| Network | Mirror, Telepathy TCP, Host/Client, 서버 권한 명령 처리 |
| RTS Loop | 건설, 자원 소비, 유닛 생산, 이동 명령, 전투 처리 |
| Authority | 팀 소유권, 명령 권한, 비소유 유닛 제어 차단 |
| Input | PC 박스 선택/우클릭 명령, 모바일 터치 선택/명령 |
| Data | CSV + Resources 기반 유닛/건물 스펙 관리 |
| Test | EditMode/PlayMode 테스트와 포트폴리오 검증 문서 |

## 대표 코드

- `Assets/Scripts/Core/RTSGameplayLoop.cs`
  건설 모드, 자원 비용 체크, 건물 배치, 생산 명령을 처리하는 핵심 게임 루프입니다.

- `Assets/Scripts/Network/NetworkGameManager.cs`
  Client 요청을 서버 권한으로 검증하고 실제 게임 상태에 반영합니다.

- `Assets/Scripts/RTS/Unit/`
  유닛 선택, 이동 명령, 모바일 입력, 선택 표시를 담당합니다.

- `Assets/Tests/EditMode/`, `Assets/Tests/PlayMode/`
  팀 소유권, 자원 격리, 명령 권한 검증 테스트를 포함합니다.

## 실행 방법

1. Unity Hub에서 Unity `6000.3.11f1` 이상으로 프로젝트를 엽니다.
2. `Assets/Scenes/MainMenuScene.unity`에서 실행합니다.
3. Host는 로비에서 `Host`를 선택합니다.
4. Client는 Host의 IP와 포트 `7777`로 접속합니다.
5. 팀이 나뉜 상태에서 이동, 건설, 생산, 전투 명령을 확인합니다.

## 검증 결과 요약

2026년 6월 기준 Windows Development Build에서 다음 흐름을 확인했습니다.

| 항목 | 결과 |
|---|---|
| Windows Player Build | PASS |
| Host TCP `0.0.0.0:7777` Listen | PASS |
| Client 접속 | PASS |
| 팀 0/1 분리 | PASS |
| 팀별 자원 격리 | PASS |
| 서버 권한 이동/건설/생산 | PASS |
| 비소유 유닛 명령 차단 | PASS |

상세 근거는 `Docs/PortfolioValidation.md`, `Docs/TestCases.md`, `Docs/Architecture.md`에 정리되어 있습니다.

## 포트폴리오에서 강조할 점

이 프로젝트는 그래픽 완성도보다 **멀티플레이 RTS 시스템 구조**를 보여주는 프로젝트입니다.
게임 회사 지원 시에는 `서버 권한`, `팀/자원/명령 검증`, `RTS 입력 처리`, `테스트 가능한 구조`를 중심으로 설명하는 것이 좋습니다.

## 기술 스택

- Unity 6
- C#
- Mirror Networking
- NavMesh
- UGUI
- EditMode / PlayMode Tests
