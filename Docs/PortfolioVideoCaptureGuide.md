# 포트폴리오 영상 촬영 가이드

## 영상 구성 (60~90초)

### 1. MainMenu와 Lobby 연결 (0~8초)
- MainMenuScene에서 게임 시작
- Host/Client 선택 화면
- 연결 주소와 상태 표시

### 2. Host/Client 접속 확인 (8~20초)
- Host 프로세스와 Client 프로세스를 나란히 표시
- `team 0/1` 배정 확인
- 각 플레이어가 자기 팀 유닛만 제어하는 장면 표시

### 3. 양측 유닛 선택과 이동 (20~38초)
- 팀 0 유닛 선택 후 이동
- 팀 1 유닛 선택 후 이동
- Host와 Client 화면에서 위치 동기화 확인

### 4. 전투와 서버 권한 데미지 (38~56초)
- 적 유닛 우클릭 공격 명령
- 사거리 접근 후 공격
- HP 변화와 사망/전투 결과 표시

### 5. 건물과 생산 루프 (56~72초)
- 건물 선택 정보 표시
- 생산 큐 또는 유닛 생성 장면
- 팀별 Gold/Mana 분리 표시

### 6. 검증 로그와 기술 요약 (72~90초)
- 자동 smoke PASS 로그 캡처
- Unity, Mirror, Telepathy TCP, NavMesh, UGUI 요약

## 촬영 체크리스트

- [ ] Windows Development Build 생성
- [ ] Host/Client 독립 프로세스 실행
- [ ] 로비 연결 확인
- [ ] 팀별 소유권 확인
- [ ] 양측 이동 동기화 확인
- [ ] 전투 및 HP 변화 확인
- [ ] 건물/생산 루프 확인
- [ ] smoke PASS 로그 캡처

## 권장 저장 경로

- `Portfolio/Video/RealmCommander_1v1_RTS_VerticalSlice.mp4`
- `Portfolio/Video/RealmCommander_SmokeTest_Log.txt`

## 필수 PASS 문자열

- `[PortfolioBuild] PASS`
- `RESOURCE_ISOLATION_PASS`
- `HOST_MOVE_PASS`
- `CLIENT_PASS`
- `HOST_PASS`

## 재현 명령

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'C:\Users\kjunw\Unity\RealmCommander' `
  -executeMethod RealmCommander.Editor.PortfolioBuildUtility.BuildWindowsPortfolioPlayer `
  -logFile 'Logs\FinalPortfolioBuild.log'

Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-host --rc-timeout=60 -logFile Logs\FinalHost.log
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-client --rc-address=127.0.0.1 --rc-timeout=60 -logFile Logs\FinalClient.log
```
