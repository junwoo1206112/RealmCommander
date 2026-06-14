# 게임 디자인 문서 (GDD)
## Realm Commander

## 1. 게임 개요

Realm Commander는 Unity 6와 Mirror로 제작하는 서버 권한형 모바일 RTS 수직 슬라이스다. 목표는 두 플레이어가 각자 소유한 유닛과 건물을 조작하고, 서버가 이동, 공격, 생산, 자원 소모, 승패를 검증하는 1v1 전투 루프를 보여주는 것이다.

## 2. 핵심 재미

1. 유닛 선택과 포지셔닝
2. 실시간 이동/공격 명령
3. 서버 권한형 1v1 네트워크 동기화
4. 자원 관리와 유닛 생산

## 3. RTS 시스템

### 유닛 선택

- 좌클릭: 단일 유닛 선택
- 드래그: 박스 선택
- Shift + 클릭: 선택 추가/제거
- 모바일 터치: 단일 선택과 명령

### 유닛 명령

- 우클릭 빈 공간: 이동
- 우클릭 적 유닛/건물: 공격
- 선택된 유닛만 명령 수신
- 비소유 유닛 명령은 차단

### 리소스 시스템

| 리소스 | 용도 | 처리 |
|---|---|---|
| Gold | 유닛 생산, 건물 건설 | 팀별 서버 권한 값 |
| Mana | 특수 생산/건물 비용 | 팀별 서버 권한 값 |

### 건물 시스템

| 건물 | 기능 |
|---|---|
| Base | 시작 거점 |
| Barracks | 기본 유닛 생산 |
| RangedBarracks | 원거리/마법 유닛 생산 |
| ResourceGenerator | 자원 생성 |
| DefenseTower | 자동 공격 |

## 4. 제외 범위

다음 기능은 현재 완료 범위가 아니다.

- RPG 레벨업, 장비, 인벤토리, 퀘스트
- 별도 스킬 UI와 스킬 쿨다운 시스템
- 저장/로드
- Relay, 전용 서버, 공인 WAN/NAT 보장

## 5. UI/UX

- 상단 HUD: Gold, Mana, 일시정지, 속도
- 선택 패널: 선택 수, HP, 유닛/건물 정보
- 로비 UI: Host, Join, IP 입력, 로컬 주소 표시
- 결과 UI: 승리/패배 표시와 로비 복귀

## 6. 기술 설계

- Engine: Unity 6
- Network: Mirror + Telepathy TCP
- Authority: 서버 권한 이동/전투/자원 처리
- Movement: NavMeshAgent + NetworkTransformReliable
- UI: UGUI + TextMeshPro
- Data: CSV + Resources + SpecManager

## 7. 개발 일정

| 단계 | 목표 | 산출물 |
|---|---|---|
| 1 | 기본 RTS 조작 | Unit, Selection, Command |
| 2 | 네트워크 1v1 | Mirror Host/Client, 팀/소유권 |
| 3 | 자원/건물/생산 | ResourceManager, Building |
| 4 | 모바일 조작/UI | Safe Area, 터치 입력, HUD |
| 5 | 검증/포트폴리오 | smoke 로그, 영상, 문서 |

## 8. 참고 게임

| 게임 | 참고 요소 |
|---|---|
| StarCraft | 유닛 제어, 미니맵, RTS 전투 |
| Clash Royale | 모바일에서 빠르게 읽히는 전투 흐름 |

**문서 버전:** 2.0  
**최종 수정일:** 2026-06-14  
**작성자:** junwoo1206112
