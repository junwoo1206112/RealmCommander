# Realm Commander 포트폴리오 검증 보고서

## 검증 환경

- 실행일: 2026-06-11
- OS: Windows 10 64-bit
- Unity: 6000.3.11f1
- Build: Windows x64 Development Build
- Network: Mirror Telepathy TCP `7777`

## 자동 검증 범위

`MultiplayerSmokeLauncher`는 명령줄 인수가 있을 때만 활성화됩니다. 일반 게임 실행에는 영향을 주지 않습니다.

1. Host가 `0.0.0.0:7777`에서 Listen합니다.
2. Client가 지정 IP로 접속합니다.
3. 서버가 두 연결에 `team 0`과 `team 1`을 배정합니다.
4. 각 팀 유닛이 올바른 연결에 소유되었는지 확인합니다.
5. 팀 1 자원을 시험 차감하고 팀 0 자원이 변하지 않는지 확인한 뒤 원상 복구합니다.
6. 권한이 지정된 건물의 소유 연결과 `teamId`가 일치하는지 확인합니다.
7. 테스트 중 AI를 비활성화해 AI 이동을 사용자 명령으로 오인하지 않게 합니다.
8. Client가 소유한 최소 `netId` 유닛에 이동 명령을 전송합니다.
9. Client가 위치 변화를 확인하고 해당 `netId`를 서버에 보고합니다.
10. Host가 같은 `netId`의 서버 위치 변화를 확인해야 양쪽이 PASS합니다.
11. Host가 소유한 팀 0 유닛도 별도 목표점에 도착해야 최종 PASS합니다.

## 결과

### LAN 주소

```text
CLIENT_START address=192.168.0.90:7777
RESOURCE_ISOLATION_PASS teams=0,1
CLIENT_MOVE_REQUESTED
CLIENT_PASS team=1 ownedUnit=11 movementRoundTrip=ok targetError=0.00
HOST_RECEIVED_CLIENT_PASS netId=11
HOST_PASS players=2 teams=0,1 ownership=ok remoteMoveNetId=11 moved=4.47 targetError=0.00 replicationError=0.00
```

결과: PASS

2026년 6월 11일 최종 전체 회귀 검증에서는 Host 목표 오차 `0.00m`, Client 목표 오차 `0.00m`, Host/Client 복제 오차 `0.00m`를 확인했습니다.

### 오버레이 주소

```text
CLIENT_START address=100.80.202.35:7777
CLIENT_MOVE_REQUESTED
CLIENT_PASS team=1 ownedUnit=11 movementRoundTrip=ok
HOST_RECEIVED_CLIENT_PASS netId=11
HOST_PASS players=2 teams=0,1 ownership=ok remoteMoveNetId=11
```

결과: PASS

### 빌드

```text
Build Finished, Result: Success.
[PortfolioBuild] PASS totalSize=159393915
```

결과: PASS

## 재현 명령

Host:

```powershell
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-host --rc-timeout=60 -logFile Logs\MultiplayerHost.log
```

Client:

```powershell
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-client --rc-address=HOST_IP --rc-timeout=60 -logFile Logs\MultiplayerClient.log
```

## 판정 한계

- 두 개의 독립 Windows Player 프로세스로 검증했습니다.
- LAN 및 오버레이 IP 바인딩 경로를 검증했지만, 두 번째 물리 장비에서의 입력과 렌더링은 아직 수동 검증이 필요합니다.
- 원격 Client 신규 건물 배치는 현재 지원 범위 밖이며 UI에서 차단됩니다.
- 공인 인터넷 접속은 방화벽, 공유기 NAT, 포트포워딩 환경에 따라 달라집니다.
- 종료 시 Host transport thread의 소켓 취소 예외가 한 번 기록될 수 있습니다. 이는 `Application.Quit`에 따른 정상 종료 과정이며 smoke FAIL이나 컴파일 오류는 아닙니다.
