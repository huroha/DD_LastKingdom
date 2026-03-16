# Large Enemy (2-Slot) Design Spec
**Date**: 2026-03-16
**Scope**: 2슬롯을 점유하는 대형 적 유닛 시스템

## 개요
적 유닛이 2개의 슬롯을 동시에 점유할 수 있다.
플레이어는 해당 유닛이 차지하는 슬롯 중 하나를 커버하는 스킬로 타겟팅할 수 있다.
DD 원작과 동일한 방식.

## 전제 조건
적 구성은 항상 슬롯 합계가 4가 되도록 구성됨:
- 2슬롯 1마리 + 1슬롯 2마리
- 2슬롯 2마리
- 1슬롯 4마리 (기존)

---

## Section 1: 데이터 & CombatUnit

### EnemyData.cs
```csharp
[SerializeField] private int m_SlotSize = 1;  // 기본값 1
public int SlotSize => m_SlotSize;
```

### CombatUnit
- `SlotSize: int` 필드 추가
- Nikke 생성자: 항상 1
- Enemy 생성자: `EnemyData.SlotSize`로 초기화

---

## Section 2: PositionSystem

### 배열 크기
`m_EnemySlots`를 항상 **4칸 고정**으로 초기화.
기존 `new CombatUnit[enemies.Count]` → `new CombatUnit[4]`

### Initialize
2슬롯 유닛 배치 시 앵커 슬롯과 다음 슬롯 모두 같은 참조 저장:
```
slots[unit.SlotIndex]     = unit
slots[unit.SlotIndex + 1] = unit   // SlotSize == 2일 때
```

### GetValidTargets
슬롯 순회 중 이미 result에 있는 유닛은 Add 스킵 (중복 제거):
```csharp
if (target != null && target.State != UnitState.Dead && !result.Contains(target))
    result.Add(target);
```

### RemoveUnit
SlotSize만큼 반복해서 점유 슬롯 모두 null 처리 후 앞으로 당김:
```
slots[SlotIndex] ~ slots[SlotIndex + SlotSize - 1] 를 null로
이후 기존 압축 로직 적용
```

### Move (밀어내기)
2슬롯 유닛 이동 시:
- 이동 방향 끝 슬롯에 다른 유닛이 있으면 먼저 그 유닛을 1칸 밀어냄
- 밀려나는 유닛이 경계(슬롯 3)를 초과하면 이동 불가 (move 실패)
- 이동 성공 시 SlotSize 슬롯 모두 업데이트

---

## Section 3: SkillExecutor

변경 없음. 중복 제거는 PositionSystem.GetValidTargets 안에서 처리.

---

## Section 4: CombatFieldView

### 스프라이트 위치
2슬롯 유닛의 위치는 점유 슬롯 중앙:
```csharp
Vector3 pos = (m_EnemySlots[unit.SlotIndex].position + m_EnemySlots[unit.SlotIndex + 1].position) / 2f;
```

### 스케일
```csharp
[SerializeField] private float m_LargeUnitScale = 0.6f;  // Inspector에서 조절
```
`unit.SlotSize == 2`이면 `m_LargeUnitScale` 적용.

---

## Section 5: CombatHUD / TurnTicker / ActiveTurnBar

### 추가 필드
```csharp
[Header("Large Enemy Slots")]
[SerializeField] private Slider[]        m_LargeEnemyHpBars;      // 2슬롯 전용 HP바
[SerializeField] private Image[]         m_LargeEnemyTurnTickers; // 2슬롯 전용 ticker
[SerializeField] private RectTransform[] m_LargeEnemyBarAnchors;  // 2슬롯 전용 ActiveTurnBar 앵커
```

### 분기 기준
`unit.SlotSize`로 분기:
- `SlotSize == 1` → 기존 배열 사용 (SlotIndex 기준)
- `SlotSize == 2` → Large 전용 배열 사용 (앵커 SlotIndex 기준으로 매핑)

### 영향받는 메서드
- `OnBattleStarted`: Large HP바/ticker 초기화 추가
- `RefreshEnemySlots`: SlotSize 분기 추가
- `RefreshHpBar`: SlotSize 분기 추가
- `SetTickerVisible`: SlotSize 분기 추가
- `SnapTurnBar`: SlotSize 분기 추가

---

## 엣지 케이스
- **RemoveUnit 후 압축**: 2슬롯 유닛 제거 시 점유한 두 슬롯 모두 해제 후 압축
- **중복 타겟**: GetValidTargets에서 Contains 체크로 방지
- **이동 경계**: 밀려나는 유닛이 슬롯 3 초과 시 이동 전체 실패
- **Corpse**: 2슬롯 유닛도 기존 Corpse 시스템 동일 적용 (SlotSize 유지)
