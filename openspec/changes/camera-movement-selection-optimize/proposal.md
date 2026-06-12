## Why

카메라 시점, 유닛 이동, 박스 선택 시스템이 현재 제대로 동작하지 않는다. 좌클릭 드래그 선택 시 전체 유닛이 선택되고, 우클릭 이동 시 유닛이 지정 위치로 이동하지 않는다. RTS 핵심 조작이 불안정하면 포트폴리오 가치가 떨어지므로 즉시 해결이 필요하다.

## What Changes

- 카메라 시점을 탑다운(75도)으로 변경하여 박스 선택 정확도 향상
- 유닛 이동 명령 시 정확한 위치로 이동하도록 NavMesh 샘플링 로직 개선
- 좌클릭 드래그 선택 시 유닛 중심점 기반으로 정확한 영역 선택 구현
- 이동 명령 시 시각적 피드백(클릭 마커) 제공
- 자동 전투 탐지가 이동 명령을 방해하지 않도록 유예 시간 적용

## Capabilities

### New Capabilities
- `topdown-camera`: 탑다운 카메라 시점, 팬, 줌, 회전 제어
- `unit-movement`: 유닛 이동 명령 처리, NavMesh 경로 탐색, 포메이션 이동
- `box-selection`: 좌클릭 드래그 박스 선택, 유닛 중심점 기반 선택 판정
- `move-marker`: 이동/공격 명령 시 시각적 클릭 피드백

### Modified Capabilities
(기존 스펙 없음)

## Impact

- `Assets/Scripts/RTS/MobileRTSCameraController.cs` - 카메라 설정 변경
- `Assets/Scripts/RTS/Unit/Unit.cs` - 이동 로직 전면 개선
- `Assets/Scripts/RTS/Unit/BoxSelector.cs` - 선택 로직 개선
- `Assets/Scripts/RTS/Unit/CommandInput.cs` - 레이캐스트 마스크 수정
- `Assets/Scripts/Core/CommandManager.cs` - 명령 처리 개선
- `Assets/Scripts/Core/SelectionManager.cs` - 선택 판정 개선
- `Assets/Scripts/RTS/MoveMarker.cs` - 새 파일 (클릭 피드백)
