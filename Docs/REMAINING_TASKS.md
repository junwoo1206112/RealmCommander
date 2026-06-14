# RealmCommander 남은 작업

최종 수정일: 2026-06-14  
범위: 서버 권한형 RTS 1v1 수직 슬라이스

## 완료 범위에서 제거됨

다음 항목은 현재 프로젝트 완료 기능으로 취급하지 않는다.

- 별도 스킬 UI
- RPG 레벨업, 장비, 인벤토리, 퀘스트
- 스킬 아이콘, 스킬 쿨다운 UI

## P0: 범위 정합성

- [x] 현재 문서 범위 정리
- [x] 별도 스킬 UI editor menu 제거
- [ ] OpenSpec 활성 change에서 RTS 범위 정리 확인
- [ ] README, GDD, Architecture, TestCases, PortfolioValidation에서 완료 범위 밖 기능 검색 결과 확인
- [ ] Unity compile log 확보
- [ ] Host/Client smoke 재실행

## P1: RTS 검증

- [ ] Windows Development Build 생성
- [ ] Host smoke 실행
- [ ] Client smoke 실행
- [ ] `RESOURCE_ISOLATION_PASS` 확인
- [ ] `HOST_MOVE_PASS` 확인
- [ ] `CLIENT_PASS` 확인
- [ ] `HOST_PASS` 확인
- [ ] 검증 로그를 `Docs/PortfolioValidation.md`에 최신 날짜로 갱신

## P2: 포트폴리오 자료

- [ ] 60~90초 RTS 영상 촬영
- [ ] MainMenu/Lobby 연결 장면 캡처
- [ ] 양측 유닛 선택/이동 장면 캡처
- [ ] 전투/HP 변화 장면 캡처
- [ ] 건물/생산 루프 장면 캡처
- [ ] smoke PASS 로그 장면 캡처

## P3: 코드 안정화

- [ ] `Unit.cs` 책임 분리 후보 정리
- [ ] `Building.cs` 생산/전투/시각화 분리 후보 정리
- [ ] `NetworkGameManager.cs` 런타임 보정 로직 정리
- [ ] Runtime/Editor/Tests asmdef 도입 검토
- [ ] EditMode 테스트 추가

## 제외된 과거 작업

과거 OpenSpec archive에는 이전 범위 이력이 남아 있을 수 있다. 현재 제출 기준 문서와 활성 change는 RTS 1v1만 완료 기능으로 표현한다.
