# Focus Movement System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CombatDirector의 포커스 연출에 위치 이동과 드리프트를 추가하여 DD 원작 느낌 재현

**Architecture:** 기존 CombatDirector만 수정. FocusIn/FocusOut에 위치 Lerp 추가, 별도 DriftRoutine 코루틴으로 포커스 중 지속 이동 처리. SkillSequence 팝업 순서 재배치 (상태이상/사망의문턱은 FocusOut 후)

**Tech Stack:** Unity 6, CombatDirector (기존), SkillType enum (Melee/Ranged)

---

## File Map

| 파일 | 상태 | 역할 |
|------|------|------|
| `Assets/Scripts/Combat/CombatDirector.cs` | MODIFY | 필드 추가, FocusIn/FocusOut 위치 Lerp, DriftRoutine, SkillSequence 재배치 |

---

## Task 1: 새 필드 추가 & Awake 수정

**Files:**
- Modify: `Assets/Scripts/Combat/CombatDirector.cs`

- [ ] **Step 1: SerializeField 추가**

기존 `[Header("Focus")]` 블록 아래에 새 헤더 추가:

```csharp
[Header("Focus Points")]
[SerializeField] private Transform m_NikkeFocusPoint;
[SerializeField] private Transform m_EnemyFocusPoint;

[Header("Drift")]
[SerializeField] private float m_DriftSpeed = 0.5f;
```

- [ ] **Step 2: 캐싱 필드 추가**

기존 `m_FocusBuffer` 아래에:

```csharp
private Dictionary<CombatUnit, Vector3> m_OriginalPositions;
private Coroutine m_DriftCoroutine;
```

- [ ] **Step 3: Awake에서 m_OriginalPositions 초기화**

```csharp
m_OriginalPositions = new Dictionary<CombatUnit, Vector3>();
```

---

## Task 2: FocusIn 위치 Lerp 추가

**Files:**
- Modify: `Assets/Scripts/Combat/CombatDirector.cs` — `FocusIn()` 메서드

기존 FocusIn은 스케일 + 알파만 처리. 여기에 원래 위치 캐싱 + 포커스 포인트로 위치 Lerp를 추가.

- [ ] **Step 1: m_OriginalPositions 캐싱 추가**

기존 `m_OriginalScales`, `m_OriginalAlphas` 캐싱 루프에 추가:

```csharp
m_OriginalPositions.Clear();

foreach(CombatUnit unit in m_AllLivingBuffer)
{
    CombatFieldView.UnitView view = m_FieldView.GetView(unit);
    m_OriginalScales[unit] = view.Renderer.transform.localScale;
    m_OriginalAlphas[unit] = view.Renderer.color.a;
    m_OriginalPositions[unit] = view.Renderer.transform.position;  // 추가
}
```

- [ ] **Step 2: Lerp 루프에 위치 Lerp 추가**

포커스 유닛의 스케일 확대 블록 안에, 타입별 포커스 포인트로 위치 Lerp 추가:

```csharp
if (m_FocusBuffer.Contains(unit))
{
    // 스케일 확대 (기존)
    view.Renderer.transform.localScale = Vector3.Lerp(
        m_OriginalScales[unit],
        m_OriginalScales[unit] * m_FocusScale,
        t);

    // 위치 이동 (추가)
    Vector3 focusPoint = (unit.UnitType == CombatUnitType.Nikke)
        ? m_NikkeFocusPoint.position
        : m_EnemyFocusPoint.position;
    view.Renderer.transform.position = Vector3.Lerp(
        m_OriginalPositions[unit],
        focusPoint,
        t);
}
```

---

## Task 3: DriftRoutine 구현

**Files:**
- Modify: `Assets/Scripts/Combat/CombatDirector.cs`

포커스 중 공격자/피격자가 스킬 타입에 따라 지속적으로 이동하는 코루틴.

- [ ] **Step 1: StartDrift / StopDrift 메서드 작성**

```csharp
private void StartDrift(CombatUnit user, SkillData skill)
// m_DriftCoroutine = StartCoroutine(DriftRoutine(user, skill))

private void StopDrift()
// if (m_DriftCoroutine != null) StopCoroutine(m_DriftCoroutine)
// m_DriftCoroutine = null
```

- [ ] **Step 2: DriftRoutine 코루틴 작성**

```csharp
private IEnumerator DriftRoutine(CombatUnit user, SkillData skill)
```

구현 내용:

```
// 방향 계산
Vector3 nikkeForward = (m_EnemyFocusPoint.position - m_NikkeFocusPoint.position).normalized
Vector3 enemyForward = (m_NikkeFocusPoint.position - m_EnemyFocusPoint.position).normalized

// 공격자 방향 결정
Vector3 userForward = user.UnitType == CombatUnitType.Nikke ? nikkeForward : enemyForward
Vector3 userDir = skill.SkillType == SkillType.Melee ? userForward : -userForward

CombatFieldView.UnitView userView = m_FieldView.GetView(user)

while (true):
    // 공격자 이동
    if (userView.Renderer != null)
        userView.Renderer.transform.position += userDir * m_DriftSpeed * Time.deltaTime

    // 피격자 이동 (m_FocusBuffer에서 user 제외)
    for (int i = 0; i < m_FocusBuffer.Count; ++i):
        if (m_FocusBuffer[i] == user) continue
        CombatUnit target = m_FocusBuffer[i]
        CombatFieldView.UnitView targetView = m_FieldView.GetView(target)
        if (targetView.Renderer == null) continue
        Vector3 targetForward = target.UnitType == CombatUnitType.Nikke ? nikkeForward : enemyForward
        targetView.Renderer.transform.position += -targetForward * m_DriftSpeed * Time.deltaTime

    yield return null
```

---

## Task 4: SkillSequence 재배치

**Files:**
- Modify: `Assets/Scripts/Combat/CombatDirector.cs` — `SkillSequence()` 메서드

기존 시퀀스 순서를 변경하고 드리프트 시작/중지를 삽입.

- [ ] **Step 1: 드리프트 삽입 — FocusIn 직후**

기존 `yield return FocusIn()` 바로 다음에:

```csharp
StartDrift(user, skill);
```

- [ ] **Step 2: 드리프트 중지 — AttackEnd 대기 후**

기존 "7. OnAttackEnd 대기" 코드 바로 다음에:

```csharp
StopDrift();
```

- [ ] **Step 3: 상태이상 팝업을 FocusOut 뒤로 이동**

현재 순서:
```
4. 히트 처리 + 데미지 팝업
5. 상태이상 팝업       ← FocusOut 전
6. 사망 처리
7. AttackEnd 대기
8. 콜백 정리
9. FocusOut
10. 후 딜레이
```

변경 순서:
```
4.  히트 처리 + 데미지 팝업
5.  AttackEnd 대기
6.  StopDrift
7.  콜백 정리
8.  FocusOut
9.  상태이상 팝업       ← FocusOut 후로 이동
10. 사망의 문턱 팝업    ← FocusOut 후로 이동
11. 사망 처리
12. 후 딜레이
```

구체적으로:
- 기존 `// 5. 상태이상 팝업` 블록 전체를 잘라내기
- `yield return FocusOut()` 바로 다음으로 이동
- 사망의 문턱 팝업도 이 블록에 추가 (아래 Step 4 참고)
- 기존 `// 6. 사망 처리`는 상태이상 팝업 다음으로 배치

- [ ] **Step 4: SpawnDamagePopup에서 사망의 문턱 제거 → SkillSequence로 이동**

`SpawnDamagePopup` 메서드에서 아래 코드 제거:

```csharp
// 제거할 코드:
if (result.PreviousState == UnitState.Alive && result.ResultState == UnitState.DeathsDoor)
    m_PopupPool.Spawn(pos, "사망의 문턱!", COLOR_CRIT);
```

대신 SkillSequence의 상태이상 팝업 블록 다음에 추가:

```csharp
// FocusOut 후, 상태이상 팝업 후
for (int i = 0; i < result.TargetResults.Length; ++i)
{
    TargetResult tr = result.TargetResults[i];
    if (tr.PreviousState == UnitState.Alive && tr.ResultState == UnitState.DeathsDoor)
    {
        CombatFieldView.UnitView v = m_FieldView.GetView(tr.Target);
        m_PopupPool.Spawn(v.Renderer.transform.position, "사망의 문턱!", COLOR_CRIT);
        yield return new WaitForSecondsRealtime(m_StatusPopupDelay);
    }
}
```

---

## Task 5: FocusOut 위치 복귀 Lerp 추가

**Files:**
- Modify: `Assets/Scripts/Combat/CombatDirector.cs` — `FocusOut()` 메서드

드리프트로 이동한 위치에서 원래 위치로 복귀.

- [ ] **Step 1: Lerp 루프에 위치 복귀 추가**

FocusOut의 포커스 유닛 블록에서 스케일 복귀와 함께 위치 복귀 추가.

주의: Lerp 시작값은 **현재 transform.position** (드리프트로 이동한 위치)이므로, 루프 시작 전에 현재 위치를 캐싱해야 함.

```csharp
private IEnumerator FocusOut()
{
    // 현재 위치 캐싱 (드리프트 후 위치)
    Dictionary<CombatUnit, Vector3> driftedPositions = ...
    // 또는 루프 전에 m_AllLivingBuffer 순회하며 현재 position 저장

    float elapsed = 0f;
    while (elapsed < m_FocusOutDuration)
    {
        ...
        if (m_FocusBuffer.Contains(unit))
        {
            // 스케일 복귀 (기존)
            ...
            // 위치 복귀 (추가)
            view.Renderer.transform.position = Vector3.Lerp(
                driftedPositions[unit],
                m_OriginalPositions[unit],
                t);
        }
        ...
    }
}
```

GC 방지: `driftedPositions`를 매번 `new`하면 GC 발생. 클래스 필드로 `m_DriftedPositions` Dictionary를 선언하고 Awake에서 초기화하여 재사용.

```csharp
// 필드 선언
private Dictionary<CombatUnit, Vector3> m_DriftedPositions;

// Awake
m_DriftedPositions = new Dictionary<CombatUnit, Vector3>();
```

---

## Task 6: Inspector 설정 (수동 작업)

- [ ] **Step 1: CombatScene에 포커스 포인트 배치**

1. 빈 GameObject 2개 생성:
   - `NikkeFocusPoint` — 아군이 모이는 위치 (화면 중앙 왼쪽 부근)
   - `EnemyFocusPoint` — 적이 모이는 위치 (화면 중앙 오른쪽 부근)
2. 씬 뷰에서 적절한 위치에 배치

- [ ] **Step 2: CombatDirector Inspector 연결**

- `m_NikkeFocusPoint` → NikkeFocusPoint 오브젝트
- `m_EnemyFocusPoint` → EnemyFocusPoint 오브젝트

- [ ] **Step 3: Play 모드 테스트**

확인 사항:
- 포커스 진입 시 유닛들이 포커스 포인트로 이동하는지
- 드리프트 중 공격자/피격자가 올바른 방향으로 이동하는지
- Melee 스킬: 공격자 전진, 피격자 후퇴
- Ranged 스킬: 공격자 후퇴, 피격자 후퇴
- FocusOut 후 원위치 복귀하는지
- 상태이상/사망의 문턱 팝업이 원위치에서 표시되는지
- m_DriftSpeed, m_FocusInDuration 등 Inspector 값 튜닝
