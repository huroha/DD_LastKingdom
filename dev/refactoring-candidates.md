# Refactoring Candidates

Phase 4 (Optimization & QA)에서 처리할 성능/구조 개선 목록.

---

## GC Allocation 제거 대상

### ~~EventBus.Publish - 매 호출마다 List 복사~~ ✅
- 파일: `Assets/Scripts/Utils/EventBus.cs` (Publish 메서드)
- 문제: 구독자 리스트를 매번 `new List<Delegate>()`로 복사 후 순회
- 해결: `s_TempHandlers` 정적 버퍼 재사용 (Clear + AddRange)

### ~~PositionSystem.GetValidTargets - 매 호출마다 List 생성~~ ✅
- 파일: `Assets/Scripts/Combat/PositionSystem.cs` (GetValidTargets, GetAllUnits)
- 문제: 매 호출마다 `new List<CombatUnit>()` 할당
- 해결: fill-based 패턴으로 전환 — 호출자가 버퍼 리스트를 넘겨서 채움

### SkillExecutor - StatusEffectData[] 타겟당 배열 할당
- 파일: `Assets/Scripts/Combat/SkillExecutor.cs` (Execute 메서드, 162-167줄)
- 문제: 타겟마다 `new StatusEffectData[applied.Count]`, `new StatusEffectData[resisted.Count]` 배열 할당
- 스킵 이유: `TargetResult` 구조체의 `AppliedEffects`/`ResistedEffects` 타입 변경 필요. 버퍼 공유 시 멀티 타겟에서 이전 결과 오염 위험. 스킬 실행 빈도(턴당 1회)가 낮아 실제 GC 영향 미미.
- 개선 방향: `TargetResult.AppliedEffects`/`ResistedEffects`를 `IReadOnlyList<StatusEffectData>`로 변경하거나 별도 List 풀 구현

---

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
