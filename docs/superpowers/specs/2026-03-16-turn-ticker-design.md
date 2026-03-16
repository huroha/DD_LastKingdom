# TurnTicker Design Spec
**Date**: 2026-03-16
**Scope**: CombatHUD 내 턴 대기 상태 표시 UI

## 개요
현재 라운드에서 아직 턴이 오지 않은 유닛을 Image로 표시한다.
라운드 시작 시 모든 유닛에게 표시되고, 해당 유닛의 턴이 끝나면 숨긴다.

## 구현 위치
**CombatHUD.cs** 에 통합. 별도 스크립트 분리 없음.

## 추가 필드
```csharp
[Header("Turn Tickers")]
[SerializeField] private Image[] m_NikkeTurnTickers;  // 4개, 슬롯 인덱스 대응
[SerializeField] private Image[] m_EnemyTurnTickers;  // 4개, 슬롯 인덱스 대응
```

## 이벤트 처리

| 이벤트 | 처리 내용 |
|--------|-----------|
| `BattleStartedEvent` | 모든 ticker 비활성화 (초기 상태) |
| `RoundStartedEvent` | 모든 ticker 비활성화 → TurnOrder 순회하며 해당 슬롯 ticker 활성화 |
| `TurnEndedEvent` | `e.Unit` 슬롯의 ticker 비활성화 |
| `UnitDiedEvent` | 해당 유닛 슬롯의 ticker 비활성화 |

## 헬퍼 메서드
```csharp
private void SetTickerVisible(CombatUnit unit, bool visible)
```
- `unit.UnitType`으로 `m_NikkeTurnTickers` / `m_EnemyTurnTickers` 선택
- `unit.SlotIndex`로 배열 접근
- `gameObject.SetActive(visible)` 호출

## 이벤트 구독 변경
`OnEnable` / `OnDisable`에 `RoundStartedEvent` 구독/해제 추가.

## TurnOrder 기준 선택 이유
- `TurnManager.BuildTurnOrder()`가 이미 `IsAlive` 유닛만 필터링
- 라운드 중간 합류 기능 없음 → TurnOrder가 "이번 라운드에 행동할 유닛" 집합과 동일
- PositionSystem 순회보다 정확하고 단순

## 엣지 케이스
- **라운드 중 사망**: `UnitDiedEvent`에서 ticker 숨김 처리
- **배틀 시작 전**: `BattleStartedEvent`에서 전체 비활성화로 초기 상태 보장
- **Corpse 유닛**: `IsAlive` 필터에서 제외되므로 ticker 표시 안 됨
