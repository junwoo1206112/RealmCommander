# Realm Commander 포트폴리오 검증 보고서

## 검증 환경

- 실행일: 2026-06-14
- OS: Windows 10 64-bit
- Unity: `6000.3.11f1`
- Build: Windows x64 Development Build
- Network: Mirror Telepathy TCP `7777`

## 현재 검증 범위

RTS 1v1 수직 슬라이스. 히어로/스킬/인벤토리/퀘스트 시스템은 제외.

- Base + Barracks 건물로 시작 (양 팀 동일)
- 유닛 8개 스폰 (워커 2, 병사 3, 궁수 2, 마법사 1)
- 자원 수집 → 건설 → 유닛 생산 루프
- AI 상대방 (그룹 공격, 후퇴, 집결)
- 건물 자동 방어
- 건물 생산 Progress 바

## 자동 smoke 범위

`MultiplayerSmokeLauncher`는 명령줄 인수가 있을 때만 활성화된다.

1. Host가 `0.0.0.0:7777`에서 Listen
2. Client가 지정 IP로 접속
3. 서버가 두 연결에 `team 0`과 `team 1` 배정
4. 각 팀 유닛 소유권 확인
5. 팀 1 자원 소비가 팀 0에 영향 없는지 확인
6. AI 비활성화
7. Client 소유 유닛 이동 명령 전송
8. Host가 서버 위치 변화 확인
9. Host/Client 양쪽에서 PASS 기록

## 검증 대상 (2026-06-14 범위 정리 후)

- [ ] Unity compile log PASS
- [ ] Windows Development Build PASS
- [ ] Host/Client loopback smoke PASS
- [ ] Client 이동 왕복 PASS
- [ ] 자원 격리 PASS
- [ ] 건물 생산 Progress 동기화 PASS
- [ ] 유닛 동기화 (이동/공격/사망) PASS

## 재현 명령

### 1. Windows 빌드

Unity Editor에서:
```
Tools > Realm Commander > Build Windows Portfolio Player
```

또는 명령줄:
```powershell
# Unity을 CLI로 빌드하는 경우
& "C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\kjunw\Unity\RealmCommander" -executeMethod RealmCommander.Editor.PortfolioBuildUtility.BuildWindowsPortfolioPlayer -quit
```

빌드 결과:
```
Builds\Windows\RealmCommander.exe
```

### 2. Host 실행

```powershell
cd "C:\Users\kjunw\Unity\RealmCommander"
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-host --rc-timeout=60 -logFile Logs\SmokeHost.log
```

### 3. Client 실행 (별도 터미널)

```powershell
cd "C:\Users\kjunw\Unity\RealmCommander"
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-client --rc-address=127.0.0.1 --rc-timeout=60 -logFile Logs\SmokeClient.log
```

### 4. 결과 확인

```powershell
# Host 로그에서 PASS 확인
Select-String "PASS" Logs\SmokeHost.log

# Client 로그에서 PASS 확인
Select-String "PASS" Logs\SmokeClient.log
```

## 예상 PASS 출력

### Host
```
[MultiplayerSmoke] HOST_START port=7777
[PortfolioBuild] PASS path=... size=...
RESOURCE_ISOLATION_PASS teams=0,1
HOST_PASS players=2 teams=0,1 ownership=ok
```

### Client
```
[MultiplayerSmoke] CLIENT_START address=127.0.0.1:7777
CLIENT_PASS team=1 ownedUnit=XX movementRoundTrip=ok
```

## 과거 PASS 근거

### 2026-06-14 범위 정리 후 최종 검증

빌드 결과:

```text
[PortfolioBuild] PASS path=C:\Users\kjunw\Unity\RealmCommander\Builds\Windows\RealmCommander.exe size=159414087
```

결과: **BUILD PASS**

Host/Client smoke 테스트는 Unity Editor에서 수동 실행 필요:
1. `Tools > Realm Commander > Build Windows Portfolio Player` 실행
2. 빌드된 exe를 두 개 실행하여 Host/Client 테스트
3. Unity Console에서 PASS/FAIL 확인

## 알려진 한계

- 두 개의 독립 Windows Player 프로세스로 검증
- LAN/IP 접속은 확인했지만 실제 별도 물리 장비 검증 필요
- 종료 시 transport thread 취소 메시지가 기록될 수 있음

**문서 버전:** 3.0  
**최종 수정일:** 2026-06-14
