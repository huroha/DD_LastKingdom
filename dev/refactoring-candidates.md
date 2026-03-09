# Refactoring Candidates

Phase 4 (Optimization & QA)에서 처리할 성능/구조 개선 목록.

---

## GC Allocation 제거 대상

### EventBus.Publish - 매 호출마다 List 복사
- 파일: `Assets/Scripts/Utils/EventBus.cs` (Publish 메서드)
- 문제: 구독자 리스트를 매번 `new List<Delegate>()`로 복사 후 순회
- 개선: 배열 캐싱 또는 콜백 도중 변경 감지 플래그 방식으로 전환

### PositionSystem.GetValidTargets - 매 호출마다 List 생성
- 파일: `Assets/Scripts/Combat/PositionSystem.cs` (GetValidTargets, GetAllUnits)
- 문제: 매 호출마다 `new List<CombatUnit>()` 할당
- 개선: 재사용 가능한 내부 버퍼 리스트 사용 (Clear + 재활용)


## EventBus.cs

### ContainsKey + 인덱서 → TryGetValue 통합
- **위치**: `Subscribe`, `Unsubscribe`, `Publish` 세 메서드
- **현재**: `ContainsKey` + `_handlers[type]` 로 딕셔너리 2회 조회
- **개선**: `TryGetValue`로 1회 조회로 통합
- **보류 이유**: O(1) 두 번이라 성능 영향 없음. 현재 코드가 더 읽기 쉬움
- **예시**:
  ```csharp
  // 현재
  if (!_handlers.ContainsKey(type))
      return;
  _handlers[type].Remove(handler);

  // 개선안
  if (!_handlers.TryGetValue(type, out List<Delegate> list))
      return;
  list.Remove(handler);
  ```
