# Combat Direction System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** DD 원작 스타일 전투 연출 시스템 — 스킬 사용 시 유닛 확대 포커스, 공격/피격 애니메이션, 데미지 팝업, 히트스탑 구현

**Architecture:** CombatDirector가 코루틴으로 연출 시퀀스를 총괄. SkillExecutor(로직)와 완전 분리. CombatFieldView를 UnitView struct로 개조해 Animator 지원. World Space DamagePopup + CombatFeedback(히트스탑/플래시)

**Tech Stack:** Unity 6, UGUI, TextMeshPro (World Space), Animator Controller (Sprite FlipBook), MaterialPropertyBlock

---

## File Map

| 파일 | 상태 | 역할 |
|------|------|------|
| `Assets/Scripts/Combat/UnitAnimBridge.cs` | NEW | Animation Event 브릿지 — 히트/공격종료 콜백 전달 |
| `Assets/Scripts/VFX/DamagePopup.cs` | NEW | World Space 데미지/힐 텍스트 + FloatUp 애니메이션 |
| `Assets/Scripts/VFX/DamagePopupPool.cs` | NEW | DamagePopup 오브젝트 풀 |
| `Assets/Scripts/VFX/CombatFeedback.cs` | NEW | 히트스탑(TimeScale), 스프라이트 플래시(MaterialPropertyBlock) |
| `Assets/Scripts/Combat/CombatDirector.cs` | NEW | 연출 시퀀스 총괄 코루틴 |
| `Assets/Scripts/Data/NikkeData.cs` | MODIFY | m_CombatAnimator 필드 추가 |
| `Assets/Scripts/Data/EnemyData.cs` | MODIFY | m_CombatAnimator 필드 추가 |
| `Assets/Scripts/Combat/CombatFieldView.cs` | MODIFY | UnitView struct 도입, Animator/AnimBridge 생성, public 접근 메서드 |
| `Assets/Scripts/Combat/CombatStateMachine.cs` | MODIFY | CombatDirector yield return 연동 |

---

## Task 1: UnitAnimBridge — Animation Event 브릿지

**Files:**
- Create: `Assets/Scripts/Combat/UnitAnimBridge.cs`

기존 프로젝트 패턴(RoundAnimEvent.cs)을 따름. Animator GameObject에 부착되어 Animation Event를 CombatDirector 콜백으로 전달.

- [ ] **Step 1: UnitAnimBridge 클래스 뼈대 작성**

```csharp
using UnityEngine;

public class UnitAnimBridge : MonoBehaviour
{
    private delegate void AnimEventHandler();
    private AnimEventHandler m_OnHitFrame;
    private AnimEventHandler m_OnAttackEnd;

    public void SetCallbacks(/* onHitFrame 콜백, onAttackEnd 콜백 */)
    // 두 콜백을 멤버에 저장

    // Animation Event에서 호출되는 public 메서드
    public void OnHitFrame()
    // m_OnHitFrame 호출 (null 체크)

    public void OnAttackEnd()
    // m_OnAttackEnd 호출 (null 체크)

    public void ClearCallbacks()
    // 두 콜백 null로 초기화
}
```

delegate 선언은 클래스 내부 private. `Action` 대신 `delegate void AnimEventHandler()` 사용 (프로젝트 컨벤션).

---

## Task 2: DamagePopup — World Space 텍스트 팝업

**Files:**
- Create: `Assets/Scripts/VFX/DamagePopup.cs`

유닛 위에 World Space로 뜨는 텍스트. 코루틴으로 위로 올라간 후 비활성화(풀 반환).

- [ ] **Step 1: DamagePopup 클래스 뼈대 작성**

```csharp
using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro m_Text;
    [SerializeField] private float m_FloatSpeed = 1f;
    [SerializeField] private float m_Duration = 0.8f;
    [SerializeField] private Vector3 m_Offset;

    private Coroutine m_FloatRoutine;

    public void Show(Vector3 worldPosition, string text, Color color, float scale = 1f)
    // m_Text.text 설정, m_Text.color 설정
    // transform.position = worldPosition + m_Offset
    // transform.localScale = Vector3.one * scale
    // gameObject.SetActive(true)
    // 기존 코루틴 중지 후 FloatUp 코루틴 시작

    private IEnumerator FloatUp()
    // elapsed = 0 ~ m_Duration 동안:
    //   transform.position += Vector3.up * m_FloatSpeed * Time.deltaTime
    //   elapsed += Time.deltaTime
    // 끝나면 gameObject.SetActive(false)

    private void OnDisable()
    // m_FloatRoutine이 있으면 StopCoroutine
}
```

`TextMeshPro` (World Space용)를 사용. `TextMeshProUGUI`가 아님에 주의.

---

## Task 3: DamagePopupPool — 오브젝트 풀

**Files:**
- Create: `Assets/Scripts/VFX/DamagePopupPool.cs`

DamagePopup 인스턴스를 풀링. Spawn 시 비활성 인스턴스 재활용, 부족하면 새로 생성.

- [ ] **Step 1: DamagePopupPool 클래스 뼈대 작성**

```csharp
using UnityEngine;
using System.Collections.Generic;

public class DamagePopupPool : MonoBehaviour
{
    [SerializeField] private DamagePopup m_Prefab;
    [SerializeField] private int m_InitialSize = 8;

    private List<DamagePopup> m_Pool;

    private void Awake()
    // m_Pool 초기화
    // m_InitialSize만큼 m_Prefab Instantiate → SetActive(false) → 리스트에 추가

    public DamagePopup Spawn(Vector3 position, string text, Color color, float scale = 1f)
    // m_Pool에서 비활성(gameObject.activeSelf == false) 인스턴스 탐색
    // 없으면 새로 Instantiate해서 m_Pool에 추가
    // 찾은 인스턴스의 Show(position, text, color, scale) 호출
    // 반환
}
```

DamagePopup.FloatUp 코루틴이 끝나면 자동으로 SetActive(false)되므로 별도 Return 메서드 불필요.

---

## Task 4: CombatFeedback — 히트스탑 & 스프라이트 플래시

**Files:**
- Create: `Assets/Scripts/VFX/CombatFeedback.cs`

히트 임팩트를 강화하는 보조 연출.

- [ ] **Step 1: CombatFeedback 클래스 뼈대 작성**

```csharp
using UnityEngine;
using System.Collections;

public class CombatFeedback : MonoBehaviour
{
    [Header("Hit Stop")]
    [SerializeField] private float m_HitStopDuration = 0.05f;

    [Header("Flash")]
    [SerializeField] private Color m_FlashColor = Color.white;
    [SerializeField] private float m_FlashDuration = 0.1f;

    private MaterialPropertyBlock m_PropBlock;

    private void Awake()
    // m_PropBlock = new MaterialPropertyBlock()

    public Coroutine PlayHitStop(bool isCrit = false)
    // StartCoroutine(HitStopRoutine(isCrit)) 반환
    // HitStopRoutine:
    //   float duration = isCrit ? m_HitStopDuration * 2f : m_HitStopDuration
    //   Time.timeScale = 0f
    //   yield return new WaitForSecondsRealtime(duration)
    //   Time.timeScale = 1f

    public Coroutine PlayFlash(SpriteRenderer target)
    // StartCoroutine(FlashRoutine(target)) 반환
    // FlashRoutine:
    //   target.GetPropertyBlock(m_PropBlock)
    //   m_PropBlock.SetColor("_Color", m_FlashColor)
    //   target.SetPropertyBlock(m_PropBlock)
    //   yield return new WaitForSecondsRealtime(m_FlashDuration)
    //   m_PropBlock.SetColor("_Color", Color.white)
    //   target.SetPropertyBlock(m_PropBlock)
}
```

**주의사항:**
- `PlayHitStop`은 `Time.timeScale = 0`으로 설정하므로 반드시 `WaitForSecondsRealtime` 사용
- `MaterialPropertyBlock`으로 색상 변경 — `SpriteRenderer.material` 직접 수정하면 인스턴스 생성되어 GC 발생
- `_Color`는 URP 2D Sprite 셰이더의 기본 프로퍼티명. 프로젝트 셰이더에 따라 확인 필요

---

## Task 5: NikkeData & EnemyData — CombatAnimator 필드 추가

**Files:**
- Modify: `Assets/Scripts/Data/NikkeData.cs` (Visuals 헤더 부근, 라인 53~55)
- Modify: `Assets/Scripts/Data/EnemyData.cs` (Visuals 헤더 부근, 라인 51~53)

- [ ] **Step 1: NikkeData에 RuntimeAnimatorController 필드 추가**

`[Header("Visuals")]` 블록의 `m_CombatIdleSprite` 아래에:

```csharp
[SerializeField] private RuntimeAnimatorController m_CombatAnimator;
```

프로퍼티:
```csharp
public RuntimeAnimatorController CombatAnimator => m_CombatAnimator;
```

- [ ] **Step 2: EnemyData에 RuntimeAnimatorController 필드 추가**

`[Header("Visuals")]` 블록의 `m_CorpseSprite` 아래에:

```csharp
[SerializeField] private RuntimeAnimatorController m_CombatAnimator;
```

프로퍼티:
```csharp
public RuntimeAnimatorController CombatAnimator => m_CombatAnimator;
```

Animator Controller가 null이면 CombatDirector가 폴백 타이머 사용 — 에셋 준비 전에도 동작.

---

## Task 6: CombatFieldView 개조 — UnitView struct & public 접근

**Files:**
- Modify: `Assets/Scripts/Combat/CombatFieldView.cs`

기존 `Dictionary<CombatUnit, SpriteRenderer>` → `Dictionary<CombatUnit, UnitView>`로 교체. Animator + UnitAnimBridge도 유닛 뷰 생성 시 함께 추가.

- [ ] **Step 1: UnitView struct 정의 & Dictionary 교체**

클래스 상단에 struct 추가:
```csharp
public struct UnitView
{
    public SpriteRenderer Renderer;
    public Animator Animator;
    public UnitAnimBridge AnimBridge;
}
```

기존 필드 교체:
```csharp
// 기존: private Dictionary<CombatUnit, SpriteRenderer> m_UnitViews;
// 변경:
private Dictionary<CombatUnit, UnitView> m_UnitViews;
```

- [ ] **Step 2: CreateUnitView 변경**

기존 메서드 시그니처를 `SpriteRenderer` → `UnitView` 반환으로 변경:

```csharp
private UnitView CreateUnitView(CombatUnit unit, Vector3 pos)
```

기존 SpriteRenderer 생성 코드 이후에 Animator + UnitAnimBridge 추가:

```csharp
// SpriteRenderer 생성 후, return 전에:
RuntimeAnimatorController animCtrl = null;
if (unit.UnitType == CombatUnitType.Nikke)
    animCtrl = unit.NikkeData.CombatAnimator;
else if (unit.UnitType == CombatUnitType.Enemy)
    animCtrl = unit.EnemyData.CombatAnimator;

Animator animator = null;
UnitAnimBridge animBridge = null;
if (animCtrl != null)
{
    animator = go.AddComponent<Animator>();
    animator.runtimeAnimatorController = animCtrl;
    animBridge = go.AddComponent<UnitAnimBridge>();
}

// 반환을 UnitView struct로:
UnitView view;
view.Renderer = sr;
view.Animator = animator;
view.AnimBridge = animBridge;
return view;
```

- [ ] **Step 3: 기존 SpriteRenderer 참조를 UnitView로 업데이트**

`CreateUnitView` 반환 타입 변경에 따라 m_UnitViews에 넣는 코드, `OnUnitDied`, `MoveAllToCurrentSlots`, `LerpToPosition` 등에서 `SpriteRenderer` 직접 참조를 `UnitView.Renderer`로 교체.

구체적 변경 지점:

`OnBattleStarted` (라인 53~67):
```csharp
// 기존: SpriteRenderer view = CreateUnitView(unit, pos);
// 변경:
UnitView view = CreateUnitView(unit, pos);
m_UnitViews[unit] = view;
```

`OnUnitDied` (라인 88~103):
```csharp
// 기존: if (!m_UnitViews.TryGetValue(e.Unit, out SpriteRenderer view))
// 변경:
if (!m_UnitViews.TryGetValue(e.Unit, out UnitView view))
    return;
// 이하 view.Renderer 사용:
// view.sprite → view.Renderer.sprite
// Destroy(view.gameObject) → Destroy(view.Renderer.gameObject)
```

`m_CorpseViews` Dictionary도 `SpriteRenderer` → `UnitView`로 교체하거나, 시체는 Animator 필요 없으니 별도 `SpriteRenderer` Dictionary로 유지. (시체는 Animator 불필요하므로 SpriteRenderer 유지 추천)

`MoveAllToCurrentSlots` (라인 153~176):
```csharp
// pair.Value가 UnitView이므로:
// SpriteRenderer view = pair.Value; → UnitView unitView = pair.Value;
// LerpToPosition 호출 시 unitView.Renderer 전달
```

`LerpToPosition` (라인 177~191):
```csharp
// 파라미터의 SpriteRenderer → 그대로 유지 (Renderer만 전달받으므로)
```

- [ ] **Step 4: Public 접근 메서드 추가**

```csharp
public UnitView GetView(CombatUnit unit)
// m_UnitViews[unit] 반환. 없으면 default(UnitView) 반환

public void GetAllLivingUnits(List<CombatUnit> result)
// result.Clear() 후 m_UnitViews.Keys를 result에 추가
// GC 방지를 위해 호출자가 리스트 제공하는 패턴
```

---

## Task 7: CombatDirector — 연출 시퀀스 총괄

**Files:**
- Create: `Assets/Scripts/Combat/CombatDirector.cs`

전체 시스템의 핵심. 코루틴으로 포커스 → 모션 → 히트 → 팝업 → 복귀 시퀀스를 관리.

- [ ] **Step 1: CombatDirector 클래스 뼈대 — 필드 & 참조**

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatFeedback m_Feedback;
    [SerializeField] private DamagePopupPool m_PopupPool;

    [Header("Focus")]
    [SerializeField] private float m_FocusScale = 1.5f;
    [SerializeField] private float m_FocusInDuration = 0.3f;
    [SerializeField] private float m_FocusOutDuration = 0.2f;
    [SerializeField] private float m_UnfocusedAlpha = 0.3f;

    [Header("Timing")]
    [SerializeField] private float m_PostHitDelay = 0.2f;
    [SerializeField] private float m_PopupDuration = 0.6f;
    [SerializeField] private float m_StatusPopupDelay = 0.3f;
    [SerializeField] private float m_PostSequenceDelay = 0.2f;
    [SerializeField] private float m_FallbackHitDelay = 0.3f;

    // Animation Event 동기화 플래그
    private bool m_HitFrameReceived;
    private bool m_AttackEndReceived;

    // 포커스 복원용 캐싱
    private Dictionary<CombatUnit, Vector3> m_OriginalScales;
    private Dictionary<CombatUnit, float> m_OriginalAlphas;
    private List<CombatUnit> m_AllLivingBuffer;
    private List<CombatUnit> m_FocusBuffer;

    private void Awake()
    // 모든 Dictionary, List 초기화
}
```

- [ ] **Step 2: PlaySkillSequence — 메인 시퀀스 코루틴**

```csharp
public Coroutine PlaySkillSequence(CombatUnit user, SkillData skill,
                                    List<CombatUnit> targets, SkillResult result)
// return StartCoroutine(SkillSequence(user, skill, targets, result))

private IEnumerator SkillSequence(CombatUnit user, SkillData skill,
                                   List<CombatUnit> targets, SkillResult result)
{
    // 1. 포커스 대상 구성 (user + targets, 중복 제거)
    //    m_FocusBuffer.Clear()
    //    m_FocusBuffer.Add(user)
    //    targets 순회하면서 user와 다른 유닛만 추가

    // 2. FocusIn
    //    yield return FocusIn()

    // 3. 공격 모션 + 히트 타이밍 대기
    //    CombatFieldView.UnitView userView = m_FieldView.GetView(user)
    //    if (userView.Animator != null)
    //        userView.AnimBridge.SetCallbacks(OnHitFrame, OnAttackEnd)
    //        m_HitFrameReceived = false
    //        m_AttackEndReceived = false
    //        userView.Animator.SetTrigger("Attack")
    //        while (!m_HitFrameReceived) yield return null
    //    else
    //        yield return new WaitForSeconds(m_FallbackHitDelay)

    // 4. 타겟별 히트 처리
    //    TargetResult[] targetResults = result.TargetResults
    //    bool isMultiTarget = targets.Count > 1
    //
    //    if (isMultiTarget):
    //        모든 타겟에 동시에 히트 연출 적용 (ProcessHitBatch)
    //    else:
    //        단일 타겟 히트 연출 (ProcessSingleHit)
    //
    //    yield return WaitForSecondsRealtime(m_PopupDuration)

    // 5. 상태이상 팝업 (각 타겟별)
    //    각 targetResult 순회:
    //      AppliedEffects 순회 → 팝업 Spawn (효과명, 타입별 색상)
    //      ResistedEffects 순회 → 팝업 Spawn ("저항!", 회색)
    //      각각 WaitForSecondsRealtime(m_StatusPopupDelay)

    // 6. 사망 처리
    //    각 targetResult에서 ResultState == Corpse 또는 Dead인 유닛:
    //      Animator 있으면 SetTrigger("Death")

    // 7. OnAttackEnd 대기 (Animator 있고 아직 안 끝났으면)
    //    while (!m_AttackEndReceived) yield return null

    // 8. 콜백 정리
    //    userView.AnimBridge?.ClearCallbacks()

    // 9. FocusOut
    //    yield return FocusOut()

    // 10. 후 딜레이
    //     yield return new WaitForSecondsRealtime(m_PostSequenceDelay)
}
```

- [ ] **Step 3: FocusIn / FocusOut 코루틴**

```csharp
private IEnumerator FocusIn()
{
    // m_AllLivingBuffer에 현재 살아있는 유닛 채우기
    // m_FieldView.GetAllLivingUnits(m_AllLivingBuffer)
    
    // 포커스 대상 원래 스케일/알파 캐싱
    // m_OriginalScales.Clear(), m_OriginalAlphas.Clear()
    // 모든 살아있는 유닛 순회:
    //   CombatFieldView.UnitView view = m_FieldView.GetView(unit)
    //   m_OriginalScales[unit] = view.Renderer.transform.localScale
    //   m_OriginalAlphas[unit] = view.Renderer.color.a
    
    // Lerp: m_FocusInDuration 동안
    //   포커스 대상: localScale → m_OriginalScales[unit] * m_FocusScale
    //   비포커스 대상: SpriteRenderer.color.a → m_UnfocusedAlpha
    // (focusTargets에 포함되는지는 m_FocusBuffer.Contains로 판단)
}

private IEnumerator FocusOut()
{
    // m_FocusOutDuration 동안 Lerp:
    //   모든 유닛: localScale → m_OriginalScales[unit]
    //   모든 유닛: SpriteRenderer.color.a → m_OriginalAlphas[unit]
}
```

- [ ] **Step 4: 히트 처리 헬퍼 메서드**

```csharp
private void ProcessSingleHit(TargetResult targetResult)
// 단일 타겟 히트:
//   CombatFieldView.UnitView targetView = m_FieldView.GetView(targetResult.Target)
//   if (targetResult.IsHit):
//     if (targetView.Animator != null) targetView.Animator.SetTrigger("Hit")
//     m_Feedback.PlayFlash(targetView.Renderer)
//     m_Feedback.PlayHitStop(targetResult.IsCrit)
//     팝업: 데미지 또는 힐
//   else:
//     팝업: "DODGE"

private void ProcessHitBatch(TargetResult[] results, List<CombatUnit> targets)
// 멀티 타겟 동시 히트:
//   bool anyCrit = false
//   모든 타겟 순회:
//     동일 로직이지만 PlayHitStop은 마지막에 한 번만 (anyCrit 기준)
//   m_Feedback.PlayHitStop(anyCrit)

private void SpawnDamagePopup(CombatUnit target, TargetResult result, SkillData skill)
// skill.MaxHeal > 0 이면:
//   팝업 "+" + result.HealAmount, Color.green
// else if result.IsHit:
//   text = result.IsCrit ? result.DamageDealt + "!" : result.DamageDealt.ToString()
//   color = result.IsCrit ? Color.yellow : Color.white
//   scale = result.IsCrit ? 1.3f : 1f
//   팝업 Spawn
// else:
//   팝업 "DODGE", Color.gray
//
// 사망의 문턱 진입 시:
//   result.PreviousState == Alive && result.ResultState == DeathsDoor이면
//   추가 팝업 "사망의 문턱!"

private Color GetEffectColor(StatusEffectType type)
// Bleed → red, Poison → green, Stun → yellow, Debuff → purple 등
// 기존 SkillTooltip.EffectColor 로직과 동일한 색상 매핑
```

- [ ] **Step 5: PlayDotTick — DOT 데미지 연출**

```csharp
public Coroutine PlayDotTick(CombatUnit unit, int damage, StatusEffectType type)
// return StartCoroutine(DotTickRoutine(unit, damage, type))
//
// DotTickRoutine:
//   CombatFieldView.UnitView view = m_FieldView.GetView(unit)
//   if (view.Renderer == null) yield break
//   m_Feedback.PlayFlash(view.Renderer)
//   m_PopupPool.Spawn(view.Renderer.transform.position, damage.ToString(), GetEffectColor(type))
//   yield return new WaitForSecondsRealtime(m_PopupDuration)
```

- [ ] **Step 6: Animation Event 콜백 메서드**

```csharp
private void OnHitFrame()
// m_HitFrameReceived = true

private void OnAttackEnd()
// m_AttackEndReceived = true
```

---

## Task 8: CombatStateMachine 연동

**Files:**
- Modify: `Assets/Scripts/Combat/CombatStateMachine.cs`

CombatDirector를 참조하고, 스킬 실행 후 연출 시퀀스를 yield return으로 대기.

- [ ] **Step 1: CombatDirector 참조 필드 추가**

`[Header("UI References")]` 근처에:
```csharp
[SerializeField] private CombatDirector m_CombatDirector;
```

- [ ] **Step 2: HandlePlayerTurn 수정 (라인 326~334)**

기존:
```csharp
if (m_SelectedSkill != null)
{
    SetState(CombatState.ExecuteSkill);
    SkillResult result = m_SkillExecutor.Execute(m_ActiveUnit, m_SelectedSkill, m_SelectedTarget);
    EventBus.Publish(new SkillExecutedEvent(result));
    ProcessDeadUnits(result);
    yield return null;
}
```

변경:
```csharp
if (m_SelectedSkill != null)
{
    SetState(CombatState.ExecuteSkill);
    // 타겟 리스트 구성 (SkillExecutor 내부에서 ResolveTargets와 동일)
    // SelectedTarget이 단일이면 리스트에 하나, Multi/All이면 여러 개
    SkillResult result = m_SkillExecutor.Execute(m_ActiveUnit, m_SelectedSkill, m_SelectedTarget);
    
    // 연출 대기
    List<CombatUnit> targets = ExtractTargets(result);
    yield return m_CombatDirector.PlaySkillSequence(m_ActiveUnit, m_SelectedSkill, targets, result);
    
    EventBus.Publish(new SkillExecutedEvent(result));
    ProcessDeadUnits(result);
}
```

- [ ] **Step 3: HandleEnemyTurn 수정 (라인 336~363)**

기존:
```csharp
SetState(CombatState.EnemyDecide);
yield return new WaitForSeconds(m_EnemyActionDelay);

EnemyAction action = m_EnemyAI.DecideAction(m_ActiveUnit);
SetState(CombatState.ExecuteSkill);
if (m_CombatHUD != null)
{
    if (!action.IsPass)
    {
        if (action.Target != null)
            m_CombatHUD.ShowEnemyTargetHighlight(action.Target.SlotIndex);
        m_CombatHUD.ShowEnemySkillName(action.Skill.SkillName);
        SkillResult result = m_SkillExecutor.Execute(m_ActiveUnit, action.Skill, action.Target);
        EventBus.Publish(new SkillExecutedEvent(result));
        ProcessDeadUnits(result);
    }
    else
    {
        m_CombatHUD.ShowPassLabel(m_ActiveUnit);
    }
}

yield return new WaitForSeconds(m_EnemyActionDelay);
```

변경:
```csharp
SetState(CombatState.EnemyDecide);
yield return new WaitForSeconds(m_EnemyActionDelay);

EnemyAction action = m_EnemyAI.DecideAction(m_ActiveUnit);
SetState(CombatState.ExecuteSkill);
if (!action.IsPass)
{
    if (m_CombatHUD != null)
    {
        if (action.Target != null)
            m_CombatHUD.ShowEnemyTargetHighlight(action.Target.SlotIndex);
        m_CombatHUD.ShowEnemySkillName(action.Skill.SkillName);
    }
    SkillResult result = m_SkillExecutor.Execute(m_ActiveUnit, action.Skill, action.Target);
    
    // 연출 대기
    List<CombatUnit> targets = ExtractTargets(result);
    yield return m_CombatDirector.PlaySkillSequence(m_ActiveUnit, action.Skill, targets, result);
    
    EventBus.Publish(new SkillExecutedEvent(result));
    ProcessDeadUnits(result);
}
else
{
    if (m_CombatHUD != null)
        m_CombatHUD.ShowPassLabel(m_ActiveUnit);
}
// m_EnemyActionDelay 대기 제거 — CombatDirector 타이밍이 대체
```

- [ ] **Step 4: ExtractTargets 헬퍼 메서드 추가**

```csharp
private List<CombatUnit> m_TargetExtractBuffer = new List<CombatUnit>(4);

private List<CombatUnit> ExtractTargets(SkillResult result)
// m_TargetExtractBuffer.Clear()
// result.TargetResults 순회:
//   m_TargetExtractBuffer.Add(targetResult.Target)
// return m_TargetExtractBuffer
```

SkillResult에 이미 TargetResults[]가 있으므로 거기서 Target을 추출.

---

## Task 9: Prefab 및 Inspector 설정 (수동 작업)

이 태스크는 Unity Editor에서 수동으로 수행해야 함. 코드가 아닌 에디터 작업.

- [ ] **Step 1: DamagePopup 프리팹 생성**

1. 빈 GameObject 생성 → 이름 "DamagePopup"
2. `TextMeshPro - Text` 컴포넌트 추가 (3D Object > Text - TextMeshPro)
3. DamagePopup.cs 컴포넌트 부착
4. Inspector에서 m_Text에 TextMeshPro 연결
5. fontSize, alignment(Center), sortingOrder 설정
6. Prefabs 폴더에 저장 후 씬에서 삭제

- [ ] **Step 2: CombatScene Hierarchy에 오브젝트 배치**

```
CombatScene
├── CombatStateMachine → m_CombatDirector 연결
├── CombatDirector (empty) → CombatDirector.cs
│   └── Inspector: m_FieldView, m_Feedback, m_PopupPool 연결
├── CombatFeedback (empty) → CombatFeedback.cs
├── DamagePopupPool (empty) → DamagePopupPool.cs
│   └── Inspector: m_Prefab에 DamagePopup 프리팹 연결
```

- [ ] **Step 3: CombatStateMachine Inspector 연결**

CombatStateMachine의 m_CombatDirector 필드에 CombatDirector 오브젝트 연결.

---

## 참고: 팝업 색상 상수

CombatDirector에서 사용할 색상 상수. 기존 SkillTooltip/TooltipHelper의 색상 체계와 통일.

```csharp
// CombatDirector 내부 또는 별도 static class
private static readonly Color COLOR_DAMAGE = Color.white;
private static readonly Color COLOR_CRIT = Color.yellow;
private static readonly Color COLOR_HEAL = Color.green;
private static readonly Color COLOR_MISS = Color.gray;
private static readonly Color COLOR_BLEED = new Color(0.8f, 0.1f, 0.1f);    // 빨강
private static readonly Color COLOR_POISON = new Color(0.1f, 0.7f, 0.1f);   // 녹색
private static readonly Color COLOR_STUN = new Color(0.9f, 0.8f, 0.1f);     // 노랑
private static readonly Color COLOR_DEBUFF = new Color(0.6f, 0.2f, 0.8f);   // 보라
private static readonly Color COLOR_RESIST = Color.gray;
```
