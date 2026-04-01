# Focus Movement System — Design Spec
> 2026-04-01 | DD 원작 스타일 포커스 이동 연출 추가

## 목표
기존 CombatDirector의 포커스 연출에 위치 이동을 추가하여 DD 원작 느낌 재현:
- 포커스 진입 시 유닛이 타입별 포커스 포인트로 모임
- 포커스 중 공격자/피격자가 스킬 타입에 따라 지속적으로 드리프트
- FocusOut 후 원위치에서 상태이상/사망의 문턱 팝업 표시

## 확정된 요구사항

| 항목 | 결정 |
|------|------|
| 포커스 포인트 | 아군/적군 2개 Transform (Inspector 배치) |
| FocusIn | 스케일 확대 + 포커스 포인트로 위치 Lerp (빠르게) |
| 드리프트 | FocusIn~StopDrift 구간 동안 매 프레임 이동 |
| 드리프트 방향 | 피격자: 후퇴 / 공격자(Melee): 전진 / 공격자(Ranged): 후퇴 |
| FocusOut | 스케일 복귀 + 원래 위치로 Lerp |
| 팝업 타이밍 | 데미지: 포커스 중 히트 시점 / 상태이상+사망의문턱: FocusOut 후 |

---

## 아키텍처 변경

### CombatDirector 수정 사항

기존 CombatDirector에 필드 추가 및 FocusIn/FocusOut/SkillSequence 수정. 새 파일 없음.

### 새 필드

```csharp
[Header("Focus Points")]
[SerializeField] private Transform m_NikkeFocusPoint;
[SerializeField] private Transform m_EnemyFocusPoint;

[Header("Drift")]
[SerializeField] private float m_DriftSpeed = 0.5f;

// 캐싱
private Dictionary<CombatUnit, Vector3> m_OriginalPositions;  // Awake에서 초기화
private Coroutine m_DriftCoroutine;
```

---

## 방향 계산

포커스 포인트 기준으로 유닛 타입별 전진/후퇴 방향 결정:

```
Nikke 전진 = (m_EnemyFocusPoint.position - m_NikkeFocusPoint.position).normalized
Enemy 전진 = (m_NikkeFocusPoint.position - m_EnemyFocusPoint.position).normalized
후퇴 = 전진 * -1
```

드리프트 중 이동 방향:
- **피격자** (targets): 항상 후퇴
- **공격자 (Melee)**: 전진 (타겟 방향)
- **공격자 (Ranged)**: 후퇴 (발사 반동)

SkillType은 `skill.SkillType` (SkillType.Melee / SkillType.Ranged)로 판단.

---

## 변경된 시퀀스 흐름

```
기존:
FocusIn(스케일) → 히트 → 데미지팝업 + 상태이상팝업 → 사망처리 → FocusOut

변경:
1.  FocusIn (스케일 + 포커스 포인트 위치 Lerp, 빠르게)
2.  StartDrift (코루틴 시작)
3.  공격 모션 (SetTrigger + HitFrame 대기)
4.  히트 처리 + 데미지 팝업
5.  AttackEnd 대기
6.  StopDrift (코루틴 중지)
7.  콜백 정리
8.  FocusOut (스케일 + 원래 위치 Lerp)
9.  상태이상 팝업 (원위치에서)
10. 사망의 문턱 팝업 (원위치에서)
11. 사망 처리 (Death 트리거)
12. 후 딜레이
```

---

## FocusIn 변경

기존 스케일 + 알파 Lerp에 위치 Lerp 추가:

```
m_OriginalPositions 캐싱 (모든 살아있는 유닛)

Lerp (m_FocusInDuration):
  포커스 유닛:
    - 스케일: 원래 → 원래 * m_FocusScale
    - 위치: 원래 → 타입별 포커스 포인트 (Nikke→m_NikkeFocusPoint, Enemy→m_EnemyFocusPoint)
  비포커스 유닛:
    - 알파: 원래 → m_UnfocusedAlpha
    - 위치 변경 없음
```

---

## 드리프트 코루틴

```
StartDrift(CombatUnit user, SkillData skill):
  m_DriftCoroutine = StartCoroutine(DriftRoutine(user, skill))

DriftRoutine:
  // 방향 계산
  Vector3 nikkeForward = (m_EnemyFocusPoint.position - m_NikkeFocusPoint.position).normalized
  Vector3 enemyForward = (m_NikkeFocusPoint.position - m_EnemyFocusPoint.position).normalized

  while (true):
    // 공격자 이동
    Vector3 userForward = user가 Nikke ? nikkeForward : enemyForward
    Vector3 userDir = skill.SkillType == Melee ? userForward : -userForward
    userView.Renderer.transform.position += userDir * m_DriftSpeed * Time.deltaTime

    // 피격자 이동 (m_FocusBuffer에서 user 제외한 유닛)
    for 각 target in m_FocusBuffer (user 제외):
      Vector3 targetForward = target이 Nikke ? nikkeForward : enemyForward
      targetView.Renderer.transform.position += -targetForward * m_DriftSpeed * Time.deltaTime

    yield return null

StopDrift:
  if (m_DriftCoroutine != null) StopCoroutine(m_DriftCoroutine)
  m_DriftCoroutine = null
```

---

## FocusOut 변경

기존 스케일 + 알파 Lerp에 위치 복귀 Lerp 추가:

```
Lerp (m_FocusOutDuration):
  포커스 유닛:
    - 스케일: 현재 → m_OriginalScales[unit]
    - 위치: 현재 → m_OriginalPositions[unit]
  비포커스 유닛:
    - 알파: 현재 → m_OriginalAlphas[unit]
```

FocusOut에서는 드리프트로 인해 이동한 현재 위치에서 원래 위치로 Lerp하므로,
Lerp 시작값을 캐싱값이 아닌 **현재 transform.position**으로 사용.

---

## SpawnDamagePopup 사망의 문턱 분리

기존 `SpawnDamagePopup` 내의 사망의 문턱 팝업 코드를 제거.
SkillSequence에서 FocusOut 후 별도로 처리:

```
// FocusOut 후
for 각 targetResult:
  상태이상 팝업 (AppliedEffects, ResistedEffects)
  사망의 문턱 체크 (PreviousState == Alive && ResultState == DeathsDoor)
```

---

## 수정 파일 목록

| 파일 | 변경 내용 |
|------|-----------|
| `Scripts/Combat/CombatDirector.cs` | 필드 추가, FocusIn/FocusOut 위치 Lerp, DriftRoutine 추가, SkillSequence 재배치 |

Inspector 추가 작업:
- CombatScene에 빈 오브젝트 2개 (NikkeFocusPoint, EnemyFocusPoint) 배치
- CombatDirector Inspector에서 두 Transform 연결
