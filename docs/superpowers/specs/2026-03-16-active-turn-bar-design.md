# Active Turn Bar Design Spec
**Date**: 2026-03-16
**Scope**: 현재 행동 중인 유닛을 가로 펄스 Image로 표시하는 UI

## 개요
현재 턴의 유닛(아군/적 모두) 슬롯 위치에 가로 바 Image를 표시한다.
바는 루프 애니메이션으로 가로 폭이 늘었다 줄었다 반복하며, 턴이 끝나면 사라진다.
단일 GameObject를 재사용하며 턴마다 슬롯 위치로 순간 이동한다.

## 추가 필드 (CombatHUD)
```csharp
[Header("Active Turn Bar")]
[SerializeField] private Image           m_ActiveTurnBar;
[SerializeField] private Animator        m_ActiveTurnBarAnimator;
[SerializeField] private RectTransform[] m_NikkeBarAnchors;   // 4개, SlotIndex 대응
[SerializeField] private RectTransform[] m_EnemyBarAnchors;   // 4개, SlotIndex 대응
```

## Animator 구성
- **Animator Controller**: `ActiveTurnBar`
- **Animation Clip**: `TurnBarPulse` — `RectTransform.sizeDelta.x`를 min/max 사이 루프
- 기본 상태: `TurnBarPulse` (Loop Time 활성화)
- show/hide는 `gameObject.SetActive(true/false)`로 처리 — active 시 자동 재생

## 이벤트 처리

| 이벤트 | 처리 내용 |
|--------|-----------|
| `BattleStartedEvent` | `m_ActiveTurnBar.gameObject.SetActive(false)` 초기화 |
| `TurnStartedEvent` | `SnapTurnBar(e.Unit)` 호출 |
| `TurnEndedEvent` | `m_ActiveTurnBar.gameObject.SetActive(false)` |

## 헬퍼 메서드
```csharp
private void SnapTurnBar(CombatUnit unit)
```
- `unit.UnitType`으로 `m_NikkeBarAnchors` / `m_EnemyBarAnchors` 선택
- `unit.SlotIndex`로 앵커 접근
- `m_ActiveTurnBar.rectTransform.position = anchor.position` 스냅
- `m_ActiveTurnBar.gameObject.SetActive(true)`
- null 체크 포함

## 이벤트 구독 변경
`OnEnable` / `OnDisable`에 `TurnStartedEvent` 구독/해제 추가.

## 설계 결정 사항
- **단일 GameObject 재사용**: 슬롯마다 별도 오브젝트 대신 하나를 이동하며 사용 — 오브젝트 수 최소화
- **앵커 분리**: CombatFieldView(월드 좌표)가 아닌 CombatHUD 전용 RectTransform 앵커 사용 — 좌표계 불일치 방지
- **Animator 방식**: Animation Clip으로 sizeDelta.x 키프레임 편집 — 코루틴 없이 Unity Editor에서 직관적 조절 가능
