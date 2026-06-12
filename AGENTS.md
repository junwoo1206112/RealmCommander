# RealmCommander AGENTS.md

## OpenSpec 사용법

이 프로젝트는 OpenSpec v1.4.1이 설치되어 있습니다. 사양 기반 개발(SDD) 워크플로우를 사용합니다.

### 명령어 사용

MiMoCode에서는 스킬 이름으로 OpenSpec 기능을 사용합니다:

| 작업 | 스킬 로드 |
|---|---|
| 새 change 제안/생성 | `openspec-propose` 또는 `openspec-new-change` |
| change 계속 (다음 아티팩트) | `openspec-continue-change` |
| 태스크 구현 | `openspec-apply-change` |
| 아카이브 | `openspec-archive-change` |
| 검증 | `openspec-verify-change` |
| 빠른 아티팩트 생성 | `openspec-ff-change` |
| 탐색/조사 | `openspec-explore` |
| 스펙 동기화 | `openspec-sync-specs` |
| 온보딩 | `openspec-onboard` |
| 전체 통합 | `openspec` |

### 사용 예시

```
사용자: "add-dark-mode 제안해줘"
→ openspec-propose 스킬 로드하여 change 생성

사용자: "이제 다음 아티팩트 만들어줘"
→ openspec-continue-change 스킬 로드

사용자: "구현 시작해줘"
→ openspec-apply-change 스킬 로드하여 태스크 구현

사용자: "아카이브 해줘"
→ openspec-archive-change 스킬 로드
```

### CLI 명령어 (bash로 직접 실행 가능)

```bash
openspec new change "<name>"                    # 새 change 생성
openspec status --change "<name>" --json        # 상태 확인
openspec list --json                            # 변경 목록
openspec instructions <id> --change "<name>"    # 아티팩트 지침
openspec schemas --json                         # 스키마 목록
```

### 워크플로우 (spec-driven)

```
propose → specs → design → tasks → apply → archive
```

## 프로젝트 구조

- `Assets/Scripts/` - 프로젝트 소유 C# 코드
- `Assets/Mirror/` - 벤더링된 외부 라이브러리
- `openspec/` - OpenSpec 사양 및 변경 관리
- `Docs/` - 프로젝트 문서
