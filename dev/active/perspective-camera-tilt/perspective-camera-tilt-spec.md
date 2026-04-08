# Perspective Camera + Enemy Turn Tilt Spec

## Overview

전투 씬 카메라를 Orthographic에서 Perspective로 전환하고, 적 턴에 카메라 Y축 기울기를 적용하여 시각적 긴장감을 부여한다. 원작 Darkest Dungeon의 적 턴 연출 참고.

## 1. Camera Orthographic → Perspective 전환

### 씬 설정 (Inspector)

- Main Camera의 Projection을 **Perspective**로 변경
- FOV 계산: `FOV = 2 * atan(orthoSize / cameraDistance) * Rad2Deg`
  - cameraDistance = abs(camera.z - sprite평면.z)
  - 기존 orthographicSize와 동일한 화면 영역이 나오도록 FOV를 맞춤

### Z 위치에 따른 스케일 보정

- Perspective에서는 Z 거리가 다르면 렌더링 크기가 달라짐
- 배경, 유닛, 이펙트 등 Z값이 다른 오브젝트들의 스케일을 씬 에디터에서 시각적으로 재조정
- 이 작업은 코드가 아닌 Inspector 수작업

## 2. CombatDirector 수정

`orthographicSize` 관련 코드를 `fieldOfView`로 변경한다.

### 변경 대상

| 위치 | 변경 전 | 변경 후 |
|------|---------|---------|
| SerializeField (line 27) | `float m_FocusOrthoSize` | `float m_FocusFOV` |
| private 필드 (line 81) | `float m_OriginalOrthoSize` | `float m_OriginalFOV` |
| FocusIn (line 274) | `m_OriginalOrthoSize = m_Camera.orthographicSize` | `m_OriginalFOV = m_Camera.fieldOfView` |
| FocusIn (line 275) | `m_Camera.orthographicSize = m_FocusOrthoSize` | `m_Camera.fieldOfView = m_FocusFOV` |
| FocusOut (line 340) | `m_Camera.orthographicSize = Mathf.Lerp(m_FocusOrthoSize, m_OriginalOrthoSize, t)` | `m_Camera.fieldOfView = Mathf.Lerp(m_FocusFOV, m_OriginalFOV, t)` |

## 3. CombatCameraTilt — 새 컴포넌트

적 턴에 카메라를 Y축으로 살짝 기울여 원근 불균형을 만드는 컴포넌트.

### 클래스 구조

- **MonoBehaviour**, CombatScene의 카메라 또는 별도 오브젝트에 부착
- EventBus로 `TurnStartedEvent` 구독/해제 (`OnEnable`/`OnDisable`)

### SerializeField

| 필드 | 타입 | 설명 | 기본값 |
|------|------|------|--------|
| `m_Camera` | Camera | 대상 카메라 | - |
| `m_TiltAngle` | float | Y축 회전 각도 (degree) | 3 |
| `m_TiltDuration` | float | 전환 Lerp 시간 (초) | 0.3 |

### 동작 로직

1. `TurnStartedEvent` 수신
2. `unit.UnitType == Enemy` && 현재 기울어지지 않은 상태 → 코루틴으로 `Lerp(0 → m_TiltAngle)` 시작
3. `unit.UnitType == Nikke` && 현재 기울어진 상태 → 코루틴으로 `Lerp(m_TiltAngle → 0)` 시작
4. 적 턴이 연속되면 이미 기울어져 있으므로 무시 (중복 코루틴 방지)
5. Lerp 대상: `m_Camera.transform.rotation`의 Y 성분 (eulerAngles.y)

### 코루틴 관리

- 새 코루틴 시작 전 기존 코루틴이 있으면 StopCoroutine 후 시작
- Lerp는 현재 Y rotation에서 목표값까지 진행 (중간에 끊겨도 자연스럽게 이어감)

## 4. 작업 순서

1. CombatDirector의 `orthographicSize` → `fieldOfView` 코드 변경
2. 씬에서 카메라 Perspective 전환 + FOV 설정
3. Z 위치에 따른 스케일 재조정 (Inspector)
4. CombatCameraTilt 컴포넌트 구현
5. 씬에 부착 및 파라미터 튜닝
