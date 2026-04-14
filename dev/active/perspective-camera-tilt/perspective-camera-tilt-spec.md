# Perspective Camera + Enemy Turn Tilt Spec

## Overview

전투 씬 카메라를 Orthographic에서 Perspective로 전환하고, 적 턴에 카메라 Y축 기울기를 적용하여 시각적 긴장감을 부여한다. 원작 Darkest Dungeon의 적 턴 연출 참고.

## 1. Camera Orthographic → Perspective 전환

### 씬 설정 (Inspector)

- Main Camera의 Projection을 **Perspective**로 변경
- FOV: **57** (유닛 기준으로 고정)
- 배경만 스케일과 Z 거리를 조절하여 기존 화면 구성 유지

## 2. CombatDirector 수정

`orthographicSize` 관련 코드를 `fieldOfView`로 변경.

| 위치 | 변경 전 | 변경 후 |
|------|---------|---------|
| SerializeField | `float m_FocusOrthoSize` | `float m_FocusFOV` |
| private 필드 | `float m_OriginalOrthoSize` | `float m_OriginalFOV` |
| FocusIn | `m_Camera.orthographicSize` 읽기/쓰기 | `m_Camera.fieldOfView` 읽기/쓰기 |
| FocusOut | `orthographicSize` Lerp | `fieldOfView` Lerp |

## 3. CombatCameraTilt — 새 컴포넌트

적 턴에 카메라를 Y축으로 살짝 기울여 원근 불균형을 만드는 컴포넌트.

### SerializeField

| 필드 | 타입 | 설명 | 기본값 |
|------|------|------|--------|
| `m_Camera` | Camera | 대상 카메라 | - |
| `m_TiltAngle` | float | Y축 회전 각도 (degree), 음수 사용 | -1.5 |
| `m_TiltDuration` | float | 전환 Lerp 시간 (초) | 0.3 |
| `m_CombatHUD` | CombatHUD | UI 슬롯 위치 갱신용 | - |

### private 필드

| 필드 | 타입 | 설명 |
|------|------|------|
| `m_TiltCoroutine` | Coroutine | 현재 실행 중인 Lerp 코루틴 |
| `m_IsTilted` | bool | 현재 기울어진 상태 여부 |
| `m_CurrentTiltY` | float | 현재 Y 회전값 (eulerAngles.y 대신 직접 추적, 음수 → 360 변환 문제 방지) |

### 동작 로직

1. `TurnStartedEvent` 수신 (OnEnable/OnDisable에서 구독/해제)
2. `Enemy` && `!m_IsTilted` → `LerpTilt(m_CurrentTiltY, m_TiltAngle)` 시작
3. `Nikke` && `m_IsTilted` → `LerpTilt(m_CurrentTiltY, 0)` 시작
4. 적 턴 연속 시 이미 기울어져 있으므로 무시

### LerpTilt 코루틴

- `m_CurrentTiltY`로 현재 각도를 직접 추적 (eulerAngles.y의 음수→360 변환 문제 회피)
- 매 프레임: 카메라 eulerAngles.y 갱신 + `m_CombatHUD.UpdateSlotPositionsForTilt()` 호출
- 완료 시 `to == 0f`이면 `m_CombatHUD.ResetSlotPositions()`, 아니면 최종 `UpdateSlotPositionsForTilt()`
- 새 코루틴 시작 전 기존 코루틴 StopCoroutine

## 4. CombatHUD 수정 — 슬롯 위치 연동

카메라 기울기에 따라 UI 슬롯 위치를 유닛 월드 좌표 기준으로 갱신.

### 추가 SerializeField

| 필드 | 타입 | 설명 |
|------|------|------|
| `m_NikkeSlotRoots` | RectTransform[] | 니케 슬롯 부모 (4개) |
| `m_EnemySlotRoots` | RectTransform[] | 적 슬롯 부모 (4개) |
| `m_LargeEnemySlotRoots` | RectTransform[] | 대형 적 슬롯 부모 (3개) |

### 추가 private 필드

- `m_OriginalNikkeSlotPositions`, `m_OriginalEnemySlotPositions`, `m_OriginalLargeEnemySlotPositions` (Vector3[])
- Awake에서 각 SlotRoot의 position 캐싱

### public 메서드

- `UpdateSlotPositionsForTilt()`: 각 슬롯 유닛의 `FieldView.GetSlotPosition()` → `Camera.WorldToScreenPoint()` → SlotRoot.position 갱신
- `ResetSlotPositions()`: 캐싱된 원래 위치로 복원
