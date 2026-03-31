# Combat Direction System — Design Spec
> 2026-03-31 | DD 원작 스타일 전투 연출 시스템

## 목표
스킬 사용 시 DD 원작과 동일한 연출 흐름 구현:
공격자+타겟 포커스(확대) → 공격 모션 → 히트 반응 + 데미지 팝업 → 복귀

## 확정된 요구사항

| 항목 | 결정 |
|------|------|
| 포커스 방식 | 카메라 고정, 유닛 스케일+위치로 포커스 (B안) |
| 공격자 전진 | 없음. 확대만으로 임팩트 (C안) |
| 데미지 팝업 | World Space TMP — 유닛과 함께 확대됨 (A안) |
| 연출 중 입력 | 완전 차단, 스킵 불가 (A안) |
| 힐/버프 연출 | 공격과 동일한 파이프라인 (A안) |
| 애니메이션 | Animator Controller 기반, Sprite FlipBook |
| 설계 패턴 | CombatDirector 중앙 관리 (A안) |

---

## 아키텍처

### 컴포넌트 구성

```
CombatStateMachine
    │
    ├── CombatDirector (NEW) — 연출 시퀀스 총괄 코루틴
    │       │
    │       ├── CombatFieldView (기존, 개조) — 유닛 뷰 생성/관리, UnitView 접근 제공
    │       ├── DamagePopupPool (NEW) — World Space 데미지/힐/상태 팝업 오브젝트 풀
    │       └── CombatFeedback (NEW) — 히트스탑, 스프라이트 플래시
    │
    └── CombatHUD (기존) — UI 업데이트는 이벤트 기반으로 연출과 독립 유지
```

### 데이터 흐름

```
CombatStateMachine
    → SkillExecutor.Execute() : 데이터만 처리 (HP 감소, 상태이상 적용)
    → SkillResult 반환
    → yield return CombatDirector.PlaySkillSequence(user, skill, targets, result)
    → 연출 완료 후 다음 턴 진행
```

로직(SkillExecutor)과 연출(CombatDirector) 완전 분리.

---

## CombatDirector

### SerializeField

```csharp
// 참조
CombatFieldView m_FieldView
CombatFeedback m_Feedback
DamagePopupPool m_PopupPool

// 포커스 설정
float m_FocusScale = 1.5f           // 확대 배율
float m_FocusInDuration = 0.3f      // 확대 시간
float m_FocusOutDuration = 0.2f     // 복귀 시간
float m_UnfocusedAlpha = 0.3f       // 비포커스 유닛 알파

// 타이밍
float m_PostHitDelay = 0.2f         // 히트 후 팝업까지 대기
float m_PopupDuration = 0.6f        // 팝업 표시 시간
float m_StatusPopupDelay = 0.3f     // 상태이상 팝업 간격
float m_PostSequenceDelay = 0.2f    // 시퀀스 종료 후 여유
float m_FallbackHitDelay = 0.3f     // Animator 없을 때 히트 타이밍 폴백
```

### 핵심 메서드

```
PlaySkillSequence(CombatUnit user, SkillData skill, List<CombatUnit> targets, SkillResult result)
    — 전체 연출 코루틴. CombatStateMachine이 yield return으로 대기

FocusIn(CombatUnit user, List<CombatUnit> targets)
    — user+targets 확대, 나머지 알파 감소. 동시 Lerp

FocusOut()
    — 모든 유닛 원래 스케일/알파로 복귀

PlayDotTick(CombatUnit unit, int damage)
    — DOT 틱: 포커스 없이 해당 유닛 플래시 + 데미지 팝업만
```

### 연출 시퀀스 상세 (공격 스킬 기준)

```
PlaySkillSequence:

1. FocusIn
   - focusTargets = [user] + targets (중복 제거)
   - unfocusedUnits = 전체 유닛 - focusTargets
   - 동시에: focusTargets 스케일 확대 / unfocusedUnits 알파 감소
   - m_FocusInDuration 대기

2. 공격 모션
   - m_HitFrameReceived = false
   - userAnimator.SetTrigger("Attack")
   - Animation Event "OnHitFrame" 대기
   - (Animator 없으면 m_FallbackHitDelay 타이머 폴백)

3. 타겟별 히트 처리
   - 단일 타겟: 순차 처리
   - 멀티 타겟: 동시 처리 (모든 타겟 Hit + 플래시 + 팝업 한 번에)
   
   IsHit인 경우:
     - targetAnimator.SetTrigger("Hit")
     - CombatFeedback.PlayFlash(targetRenderer)
     - CombatFeedback.PlayHitStop()
     - WaitForSecondsRealtime(m_PostHitDelay)
     - 데미지/힐 팝업 Spawn
   
   IsHit 아닌 경우:
     - "DODGE" 팝업 Spawn
   
   - WaitForSecondsRealtime(m_PopupDuration)

4. 상태이상 팝업
   - 적용된 효과: 효과명 + 타입별 색상
   - 저항된 효과: "저항!" + 회색
   - 각각 m_StatusPopupDelay 간격

5. 사망 처리
   - 사망/시체 전환 유닛: targetAnimator.SetTrigger("Death")
   - Death 애니메이션 대기

6. OnAttackEnd 대기 (아직 안 끝났으면)

7. FocusOut
   - 모든 유닛 스케일/알파 원복 Lerp
   - m_FocusOutDuration 대기
   - WaitForSecondsRealtime(m_PostSequenceDelay)
```

---

## CombatFeedback

```csharp
// SerializeField
float m_HitStopDuration = 0.05f     // 히트스탑 길이 (크리티컬 시 2배)
Color m_FlashColor = Color.white     // 피격 플래시 색상
float m_FlashDuration = 0.1f        // 플래시 지속

// 메서드
PlayHitStop(bool isCrit = false)
    — Time.timeScale = 0 → WaitForSecondsRealtime → Time.timeScale = 1
    — 크리티컬이면 duration 2배

PlayFlash(SpriteRenderer target)
    — MaterialPropertyBlock으로 색상 변경 → 원복
    — material 직접 수정 아님 (인스턴스 생성 방지)

PlayDeathEffect(SpriteRenderer target)
    — 사망 시 알파 감소 또는 Death 애니메이션 재생
```

히트스탑 중에는 Time.timeScale = 0이므로 CombatDirector 코루틴도 
히트스탑 구간에서는 WaitForSecondsRealtime 사용 필수.

---

## DamagePopup & DamagePopupPool

### DamagePopup

```csharp
// SerializeField
TextMeshPro m_Text              // World Space TMP
float m_FloatSpeed = 1f         // 위로 올라가는 속도
float m_Duration = 0.8f         // 표시 시간
Vector3 m_Offset                // 유닛 위 오프셋
```

코루틴으로 FloatUp 후 자동으로 풀에 반환.

### 표시 종류

| 종류 | 텍스트 | 색상 |
|------|--------|------|
| 일반 데미지 | "12" | 흰색 |
| 크리티컬 | "24!" | 노란색 (크게) |
| MISS | "MISS" | 회색 |
| DODGE | "DODGE" | 회색 |
| 힐 | "+8" | 초록색 |
| 상태이상 적용 | "출혈" | 효과 타입별 색상 |
| 상태이상 저항 | "저항!" | 회색 |

### DamagePopupPool

```csharp
// SerializeField
DamagePopup m_Prefab
int m_InitialSize = 8

// 메서드
Spawn(Vector3 position, string text, Color color) → DamagePopup
    — 풀에서 꺼내서 설정 후 활성화, FloatUp 끝나면 자동 반환
```

---

## Animator 구조

### 공통 상태 머신 (니케/적 공통)

```
States:
    Idle (default)  → 루프
    Attack          → 재생 후 Idle 자동 전환
    Hit             → 재생 후 Idle 자동 전환
    Death           → 마지막 프레임 유지

Triggers:
    "Attack"  → Idle → Attack
    "Hit"     → AnyState → Hit
    "Death"   → AnyState → Death

Animation Events:
    Attack 클립: "OnHitFrame" (타겟 히트 프레임), "OnAttackEnd" (모션 완료)
```

유닛 종류별 별도 Controller, 파라미터/트리거 이름 통일.

### UnitAnimBridge (Animation Event 브릿지)

```csharp
// 기존 프로젝트 패턴 (RoundAnimEvent 등) 따름
delegate void AnimEventHandler()
AnimEventHandler m_OnHitFrame
AnimEventHandler m_OnAttackEnd

SetCallbacks(onHitFrame, onAttackEnd)  // CombatDirector가 연출 시작 시 등록
OnHitFrame()   // Animation Event → 콜백 호출
OnAttackEnd()  // Animation Event → 콜백 호출
```

---

## CombatFieldView 개조

### UnitView struct 도입

```csharp
private struct UnitView
{
    public SpriteRenderer Renderer;
    public Animator Animator;
    public UnitAnimBridge AnimBridge;
}
private Dictionary<CombatUnit, UnitView> m_UnitViews;
```

기존 `Dictionary<CombatUnit, SpriteRenderer>` 대체.

### CreateUnitView 변경

기존에 SpriteRenderer만 추가하던 것을 확장:
- Animator 컴포넌트 추가
- RuntimeAnimatorController 할당 (NikkeData/EnemyData에서 가져옴)
- UnitAnimBridge 컴포넌트 추가

### 데이터 SO 추가 필드

- `NikkeData`: `[SerializeField] private RuntimeAnimatorController m_CombatAnimator`
- `EnemyData`: `[SerializeField] private RuntimeAnimatorController m_CombatAnimator`

### Public 접근 메서드 추가

```
GetView(CombatUnit unit) → UnitView
GetAllLivingUnits() → List<CombatUnit>  // 비포커스 처리용
```

---

## CombatStateMachine 수정

### HandlePlayerTurn 변경

```csharp
// 기존
SkillResult result = m_SkillExecutor.Execute(user, skill, target);
EventBus.Publish(new SkillExecutedEvent(result));

// 변경
SkillResult result = m_SkillExecutor.Execute(user, skill, target);
yield return m_CombatDirector.PlaySkillSequence(user, skill, targets, result);
EventBus.Publish(new SkillExecutedEvent(result));
```

### HandleEnemyTurn 변경

동일하게 Execute 후 PlaySkillSequence yield return 추가.

기존 `m_EnemyActionDelay` 대기는 제거 — CombatDirector 타이밍이 대체.

---

## Animator 폴백 (스프라이트 미준비 시)

Animator Controller가 없는 유닛도 연출 흐름이 동작하도록:

```csharp
if (userView.Animator != null)
{
    m_HitFrameReceived = false;
    userView.Animator.SetTrigger("Attack");
    while (!m_HitFrameReceived) yield return null;
}
else
{
    yield return new WaitForSeconds(m_FallbackHitDelay);
}
```

에셋 준비 전 테스트 가능, 에셋 연결 시 자연스럽게 전환.

---

## 엣지 케이스

| 상황 | 처리 |
|------|------|
| Animator 없는 유닛 | 타이머 폴백 (m_FallbackHitDelay) |
| 멀티타겟 스킬 | 모든 타겟 동시에 Hit + 플래시 + 팝업 |
| 타겟이 이미 사망 (멀티타겟) | SkillExecutor가 결과 계산 완료했으므로 연출만 스킵 |
| 패스 턴 | CombatDirector 거치지 않음. 기존 FloatingLabel로 처리 |
| DOT 틱 데미지 | PlayDotTick — 포커스 없이 플래시 + 팝업만 |
| 사망의 문턱 진입 | 데미지 팝업 후 "사망의 문턱!" 추가 팝업 |

---

## 파일 목록

| 파일 | 상태 | 설명 |
|------|------|------|
| `Scripts/Combat/CombatDirector.cs` | NEW | 연출 시퀀스 총괄 |
| `Scripts/VFX/CombatFeedback.cs` | NEW | 히트스탑, 플래시 |
| `Scripts/VFX/DamagePopup.cs` | NEW | World Space 텍스트 팝업 |
| `Scripts/VFX/DamagePopupPool.cs` | NEW | 팝업 오브젝트 풀 |
| `Scripts/Combat/UnitAnimBridge.cs` | NEW | Animation Event 브릿지 |
| `Scripts/Combat/CombatFieldView.cs` | MODIFY | UnitView struct, 접근 메서드 추가 |
| `Scripts/Combat/CombatStateMachine.cs` | MODIFY | CombatDirector yield return 연동 |
| `Scripts/Data/NikkeData.cs` | MODIFY | m_CombatAnimator 필드 추가 |
| `Scripts/Data/EnemyData.cs` | MODIFY | m_CombatAnimator 필드 추가 |
