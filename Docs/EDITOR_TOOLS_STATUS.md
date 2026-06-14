# RealmCommander Editor 툴 현황

최종 수정일: 2026-06-14

## 현재 사용하는 툴

| 툴 | 메뉴 경로 | 기능 | 상태 |
|---|---|---|---|
| AudioPlaceholderGenerator | Tools > Realm Commander > Generate Audio Placeholders | 더미 AudioClip 생성 | 필요 시 실행 |
| AutoConnectAssets | Tools > Realm Commander > Auto-Connect VFX & Audio | 오디오 참조 자동 연결 | 필요 시 실행 |
| PortfolioBuildUtility | Tools > Realm Commander > Build Windows Portfolio Player | Windows 제출 빌드 | 빌드 전 실행 |
| CompleteProjectSetup | Tools > Realm Commander > Complete Setup (All-in-One) | 초기 씬/프리팹 구성 | 기존 적용 |
| NetworkSetup | Tools > Realm Commander > Setup Network | NetworkManager 구성 | 기존 적용 |
| UnitRendererFixer | Tools > Realm Commander > Fix Unit Renderers | 유닛 렌더러 보정 | 문제 발생 시 |
| PrefabNetworkIdentityFixer | Tools > Realm Commander > Validate Prefabs | 네트워크 프리팹 검증 | 빌드 전 확인 |
| NetworkIdentityFixer | Tools > Realm Commander > Validate Network Setup | 네트워크 설정 검증 | 빌드 전 확인 |
| MaterialFixer | Tools > Realm Commander > Fix Materials | 머티리얼 보정 | 문제 발생 시 |
| HostFlowSmokeTest | Tools > Realm Commander > Run Host Flow Smoke Test | 호스트 흐름 스모크 | 테스트용 |

## 제거된 범위

RPG 확장 기능과 별도 스킬 UI 관련 툴은 더 이상 현재 프로젝트 범위가 아니다. 해당 기능을 다시 추가하지 않는 한 실행 순서나 완료 기준에 포함하지 않는다.

## 권장 실행 순서

1. `Validate Network Setup`
2. `Validate Prefabs`
3. `Build Windows Portfolio Player`
4. Host/Client standalone smoke 실행
5. 문서에 PASS 로그 기록

## OpenSpec/CSV 툴

| 툴 | 메뉴 경로 | 기능 |
|---|---|---|
| SpecImporter | Tools > OpenSpec > Import All Specs | CSV 전체 임포트 |
| SpecImporter | Tools > OpenSpec > Import Unit Specs | 유닛 스펙 임포트 |
| SpecImporter | Tools > OpenSpec > Import Building Specs | 건물 스펙 임포트 |
| SpecImporter | Tools > OpenSpec > Generate Documentation | 스펙 문서 생성 |

`skills.csv`는 과거 실험 데이터로 남아 있을 수 있으나, 현재 완료 기능에는 포함하지 않는다.
