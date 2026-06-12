# 포트폴리오 영상 촬영 가이드

## 영상 구성 (60~90초)

### 1. MainMenu와 Host/Client Lobby 연결 (0~8초)
- MainMenuScene에서 게임 시작
- Host/Client 선택 화면
- 네트워크 연결 상태 표시

### 2. 팀별 유닛 선택과 이동 명령 (8~20초)
- 팀 0 유닛 선택
- 팀 1 유닛 선택
- 각 팀의 유닛을 서로 다른 위치로 이동 명령

### 3. Client 명령 동기화 (20~35초)
- Client에서 명령 실행
- Host 화면에서 동기화 확인
- 실시간 위치 업데이트 확인

### 4. Commander Hero 선택과 Arc Strike (35~50초)
- Commander Hero 선택
- 적 유닛을 대상으로 Arc Strike 사용
- 스킬 효과 및 데미지 표시

### 5. Rally Heal과 마나/cooldown UI (50~62초)
- Hero 피해 상태 확인
- Rally Heal 사용으로 회복
- 마나/cooldown UI 업데이트 확인

### 6. 건물 또는 전투 장면과 서버 권한 설명 (62~75초)
- 건물 선택 및 정보 표시
- 전투 장면 (서버 권한 데미지)
- 네트워크 동기화 상태

### 7. 기술 스택 및 자동 smoke PASS 로그 (75~90초)
- 사용 기술 스택 표시 (Unity, Mirror, etc.)
- 자동 smoke 테스트 PASS 로그 캡처
- 프로젝트 완료 상태 요약

## 촬영 체크리스트

### 준비물
- [ ] Windows Development Build 완료
- [ ] Host/Client 독립 프로세스 실행 가능
- [ ] 포트폴리오 폴더 준비

### 촬영 환경
- [ ] 해상도: 1920x1080 (권장)
- [ ] 프레임: 60fps
- [ ] 녹화 소프트웨어: OBS Studio 또는 Windows Game Bar

### 촬영 순서
1. MainMenuScene 시작
2. Host 프로세스 실행
3. Client 프로세스 실행
4. 네트워크 연결 확인
5. 유닛 선택 및 이동 명령
6. Hero 선택 및 스킬 사용
7. 전체 gameplay 녹화
8. 로그 캡처

## 파일 저장

### 권장 경로
- `Portfolio/Video/RealmCommander_1v1_VerticalSlice.mp4`
- `Portfolio/Video/RealmCommander_SmokeTest_Log.txt`

### README 업데이트
- YouTube 또는 Google Drive 공개 링크 추가
- 영상 설명 포함

## 자동 smoke 테스트 로그

### 필수 PASS 문자열
- `[PortfolioBuild] PASS`
- `RESOURCE_ISOLATION_PASS`
- `HERO_SKILLS_PASS heroes=2`
- `HOST_MOVE_PASS`
- `CLIENT_PASS`
- `HOST_PASS`

### 로그 캡처 방법
```powershell
# Windows build 실행
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'C:\Users\admin\Unity\RealmCommander' `
  -executeMethod RealmCommander.Editor.PortfolioBuildUtility.BuildWindowsPortfolioPlayer `
  -logFile 'Logs\FinalPortfolioBuild.log'

# Host 실행
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-host --rc-timeout=60 -logFile Logs\FinalHost.log

# Client 실행
Builds\Windows\RealmCommander.exe -batchmode --rc-smoke-client --rc-address=127.0.0.1 --rc-timeout=60 -logFile Logs\FinalClient.log
```

## 편집 가이드

### 영상 편집 포인트
- 각 섹션 간 자연스러운 전환
- 중요 기능 하이라이트
- 텍스트 오버레이로 기능 설명
- 배경 음악 선택 (선택사항)

### 최종 확인
- [ ] 전체 영상 길이: 60~90초
- [ ] 모든 핵심 기능 표시
- [ ] 텍스트 설명 명확
- [ ] 화면 품질 양호
- [ ] 로그 PASS 표시
