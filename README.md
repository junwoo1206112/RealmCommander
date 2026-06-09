# 영웅의 전장 (Realm Commander)

## 🎯 프로젝트 목적

> **신입 게임 개발자 취업을 위한 포트폴리오 프로젝트**
>
> 이 프로젝트는 게임 업계 신입 개발자 취업을 위해 제작되었습니다.
> RTS와 RPG 장르를 결합하여 클라이언트 프로그래밍, 네트워킹, UI/UX 설계 등
> 실무에서 요구하는 핵심 역량을 증명하는 것을 목표로 합니다.

### 지원 목표 공고
- **이스트게임즈** - 모바일 RTS 클라이언트 프로그래머 (신입) ⭐
- **이스트게임즈** - MMORPG 클라이언트 프로그래머 (신입) ⭐
- **이스트게임즈** - 모바일 RTS 서버 프로그래머 (신입) ⭐
- **111퍼센트** - QA인턴십 Quality Player 3기 (마감: 2026.06.14)

### 어필 포인트
| 역량 | 구현 내용 |
|------|-----------|
| C#/Unity 실무 | 유닛 제어, 건물 시스템, 리소스 관리, 영웅/인벤토리/퀘스트 |
| UGUI | HUD, 미니맵, 스킬바, 인벤토리 UI, 메인 메뉴, 로비 |
| 네트워킹 | Mirror 기반 1v1 실시간 대전, 서버 권한 검증, 상태 동기화 |
| 설계 능력 | 아키텍처 문서, 테스트 케이스 23건 (통과율 95%) |
| 데이터 관리 | OpenSpec CSV 기반 스펙 관리, 자동 문서 생성 |
| AI 시스템 | 적 유닛 AI 컨트롤러 (Easy/Normal/Hard 난이도) |

---

## 🎮 프로젝트 개요

**장르:** RTS + RPG 하이브리드  
**엔진:** Unity 6 (6000.3.11f1)  
**언어:** C#  
**네트워킹:** Mirror (Server Authority)

### 게임 컨셉
실시간 전략(RTS)으로 유닛을 생산하고 적 기지를 공략하면서, 영웅 캐릭터를 육성하는 하이브리드 게임

---

## ✨ 핵심 기능

### RTS 시스템 ✓
- **유닛 선택**: 드래그 박스 선택, Shift+클릭 다중 선택
- **유닛 제어**: 우클릭 이동/공격 명령
- **미니맵**: 실시간 위치 표시, 클릭으로 이동 명령
- **리소스 관리**: 골드/마나 자동 생성, 건물/유닛 생산에 사용
- **건물 시스템**: 기지, 병영, 자원 생산 건물, 건설 시스템

### RPG 시스템 ✓
- **영웅 캐릭터**: 고유 스킬, 레벨업, 장비 시스템
- **스킬 시스템**: 5종 스킬 (Fireball, Heal, Shield, Lightning, Ice Storm), 쿨다운, 마나 소모
- **인벤토리**: 20슬롯 + 장비 슬롯, 아이템 사용, 장비 bonuses
- **퀘스트 시스템**: 퀘스트 수락/진행/완료, 보상

### 멀티플레이 (Mirror) ✓
- **1v1 대전**: Mirror NetworkBehaviour 기반 실시간 유닛/건물/영웅 동기화
- **서버 권한 검증**: CombatManager에서 공격 유효성 서버 검증, 치트 방지
- **소유권 제어**: 각 플레이어는 자신의 유닛만 제어 가능 (hasAuthority 기반)
- **네트워크 동기화**: SyncVar로 체력/자원/레벨 실시간 동기화
- **게임 세션**: NetworkGameManager로 게임 시작/종료/연결 끊김 처리
- **게임 결과**: 승리/패배 UI 표시 및 로비 복귀

### 메뉴 및 로비 ✓
- **메인 메뉴**: 타이틀, 게임 시작, 종료
- **로비**: 호스트/접속 선택, IP 입력, 상태 표시
- **씬 전환**: MainMenu → Lobby → GameScene 순환 플로우

### AI 적 유닛 ✓
- **AI 컨트롤러**: 적 기지 탐색, 플레이어 기지 공격
- **3단계 난이도**: Easy / Normal / Hard (스폰 간격 차등)

### 시각 효과 및 사운드
- **유닛 스폰 이펙트**: 스폰 위치 시각적 피드백
- **스킬 이펙트**: Impact/Projectile/Aura 이펙트 시스템
- **AudioManager**: SFX/음악 재생 시스템 (싱글톤)

---

## 🛠️ 기술 스택

| 기술 | 용도 |
|------|------|
| Unity 6 | 게임 엔진 |
| C# | 프로그래밍 언어 |
| Mirror | 멀티플레이 네트워킹 |
| UGUI | UI 시스템 |
| NavMesh | 유닛 이동/경로 찾기 |
| ScriptableObject | 데이터 관리 |

---

## 📁 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, SelectionManager, CommandManager
│   ├── RTS/
│   │   ├── Unit/       # Unit (NetworkBehaviour), BoxSelector, CommandInput
│   │   ├── Building/   # Building (NetworkBehaviour), BuildingPlacer
│   │   ├── Resource/   # ResourceManager (NetworkBehaviour), ResourceGenerator
│   │   └── Minimap/    # MinimapController
│   ├── RPG/
│   │   ├── Hero/       # Hero (NetworkBehaviour), HeroData
│   │   ├── Inventory/  # Inventory, ItemData
│   │   └── Quest/      # QuestManager
│   ├── UI/
│   │   ├── HUD/        # HUDController
│   │   ├── SkillBar/   # SkillBarUI
│   │   ├── Inventory/  # InventoryUI, BuildingUI
│   │   └── Menu/       # MainMenuUI, LobbyUI, GameResultUI
│   ├── Network/        # NetworkPlayer, NetworkGameManager, LobbyManager, CombatManager
│   ├── AI/             # AIController (Easy/Normal/Hard)
│   ├── Audio/          # AudioManager
│   ├── Effects/        # UnitSpawnEffect, SkillEffect
│   ├── OpenSpec/       # SpecManager, UnitSpecExample
│   └── Editor/         # ProjectSetup, SpecImporter, NetworkSetup
├── Prefabs/
├── Scenes/             # MainMenuScene, LobbyScene, MainScene
├── UI/
└── Resources/

Docs/
├── GDD.md              # 게임 디자인 문서
├── TestCases.md        # 테스트 케이스
└── Architecture.md     # 아키텍처 문서
```

---

## 📅 개발 로드맵

### Week 1: 기본 구조 + 유닛 제어 ✅
- [x] 프로젝트 세팅, 폴더 구조
- [x] GameManager, SelectionManager, CommandManager
- [x] Unit 기본 클래스
- [x] BoxSelector (드래그 박스 선택)
- [x] CommandInput (우클릭 명령)

### Week 2: RTS 핵심 시스템 ✅
- [x] ResourceManager, ResourceGenerator
- [x] MinimapController
- [x] Building 시스템
- [x] 유닛 생산 UI

### Week 3: RPG 시스템 ✅
- [x] Hero, HeroData
- [x] Inventory 시스템
- [x] QuestManager
- [x] 스킬 이펙트/비주얼 시스템

### Week 4: UI 완성 ✅
- [x] HUDController
- [x] SkillBarUI
- [x] InventoryUI
- [x] 메인 메뉴, 로비 UI 스크립트

### Week 5: 멀티플레이 (Mirror) ✅
- [x] Mirror 패키지 설치 (manifest.json)
- [x] NetworkBehaviour 기반 유닛/건물/영웅 동기화
- [x] NetworkPlayer, NetworkGameManager
- [x] CombatManager (서버 권한 검증)
- [x] 로비 + 호스트/접속 시스템

### Week 6: 폴리싱 + 문서 ✅
- [x] AI 적 유닛 (Easy/Normal/Hard)
- [x] AudioManager 시스템
- [x] 스킬/스폰 이펙트 시스템
- [x] 테스트 케이스 실행 결과 기록
- [x] README 최종 업데이트
- [x] OpenSpec Change 아티팩트 완료

---

## 📊 OpenSpec 문서화 시스템

### 개요
게임 데이터(유닛, 건물, 스킬 등)를 CSV 프레드시트로 관리하고 Unity로 자동 import하는 시스템입니다.

### 파일 구조
```
Assets/Resources/Specs/
├── units.csv          # 유닛 스펙
├── buildings.csv      # 건물 스펙
── skills.csv         # 스킬 스
└── Docs/              # 자동 생성 문서
    ├── UnitSpecs.md
    ├── BuildingSpecs.md
    └── SkillSpecs.md
```

### 사용법

#### 1. 스펙 Import
```
Tools → OpenSpec → Import All Specs
```

#### 2. 문서 생성
```
Tools → OpenSpec → Generate Documentation
```

#### 3. 코드에서 사용
```csharp
// 유닛 스펙 가져오기
var unit = SpecManager.Instance.GetSpec("units", "unit_soldier");
float maxHealth = SpecManager.Instance.GetProperty<float>("units", "unit_soldier", "MaxHealth");

// 모든 유닛 목록
var units = SpecManager.Instance.GetAllSpecs("units");
```

### CSV 형식
```csv
ID,Name,Description,MaxHealth,AttackDamage,MoveSpeed
unit_soldier,Soldier,기본 보병,100,10,5
unit_archer,Archer,원거리 유닛,80,15,4
```

---

## 🎯 지원 공고 매칭

| 시스템 | 어필 공고 |
|--------|-----------|
| RTS (유닛 제어, 미니맵) | 이스트게임즈 - 모바일 RTS 클라이언트 |
| RPG (영웅 성장, 스킬) | 이스트게임즈 - MMORPG 클라이언트 |
| Mirror 멀티플레이 | 이스트게임즈 - 모바일 RTS 서버 |
| UGUI 기반 UI | 모든 공고 |
| 테스트 문서 | 111퍼센트 QA인턴십 |

---

## 📸 스크린샷

(개발 진행 후 추가 예정)

---

## 📚 참고 자료

- [Unity NavMesh Documentation](https://docs.unity3d.com/Manual/Navigation.html)
- [Mirror Networking](https://mirror-networking.gitbook.io/docs/)
- [ RTS Game Programming Patterns](https://gameprogrammingpatterns.com/)

---

## 📝 라이선스

이 프로젝트는 포트폴리오 목적으로 제작되었습니다.

---

**개발자:** junwoo1206112  
**이메일:** kddong135@naver.com  
**GitHub:** https://github.com/junwoo1206112
