# Enemy Hover Info Panel — Design Spec
Date: 2026-03-18

## Overview
적 유닛에 마우스를 올리면 정보 패널이 활성화되고, 벗어나면 숨겨진다.
`PlayerSelectTarget` 상태에서 hover 시 선택된 스킬 기준 전투 예측 섹션도 함께 표시한다.

---

## 컴포넌트 구조

### 신규
- `UnitHoverHandler` — OnMouseEnter/OnMouseExit 전용 컴포넌트
- `AttackPreview` — 전투 예측 데이터 struct (SkillExecutor 파일 내 정의)
- `EnemyInfoPanel` — 패널 데이터 바인딩 MonoBehaviour

### 수정
- `CombatFieldView` — CreateUnitView에서 UnitHoverHandler 추가 및 콜백 등록
- `CombatHUD` — EnemyInfoPanel 레퍼런스, ShowEnemyInfo / HideEnemyInfo 추가
- `CombatStateMachine` — SelectedSkill public 프로퍼티 노출
- `SkillExecutor` — PreviewAttack() 메서드 추가

---

## 데이터 흐름

```
OnMouseEnter
  → CombatHUD.ShowEnemyInfo(unit)
      → EnemyInfoPanel.Populate(unit)           // 기본 섹션
      → if state == PlayerSelectTarget
            SkillExecutor.PreviewAttack(activeUnit, selectedSkill, target)
            → EnemyInfoPanel.PopulatePreview(preview)  // 조준 섹션 활성화

OnMouseExit
  → CombatHUD.HideEnemyInfo()
      → EnemyInfoPanel.Hide()
```

---

## UnitHoverHandler

```csharp
delegate void HoverHandler()
private HoverHandler m_OnEnter
private HoverHandler m_OnExit

void Initialize(HoverHandler onEnter, HoverHandler onExit)
void OnMouseEnter()   // m_OnEnter 호출
void OnMouseExit()    // m_OnExit 호출
```

---

## AttackPreview (struct)

```csharp
public struct AttackPreview
{
    public float HitChance;    // attacker.accuracyMod + skill.AccuracyMod - target.dodge
    public float CritChance;   // attacker.critChance + skill.CritMod
    public int   MinDamage;    // (int)(attacker.minDamage * skill.DamageMultiplier * (1 - defense/100))
    public int   MaxDamage;    // (int)(attacker.maxDamage * skill.DamageMultiplier * (1 - defense/100))
}
```

---

## SkillExecutor

```csharp
public AttackPreview PreviewAttack(CombatUnit attacker, SkillData skill, CombatUnit target)
```
- 기존 RollHit / RollCrit / CalcDamage 로직에서 난수 제거한 순수 계산
- target.State == UnitState.Corpse 이면 dodge, defense = 0 적용 (기존 Execute와 동일 규칙)

---

## CombatStateMachine

```csharp
public SkillData SelectedSkill => m_SelectedSkill;
```

---

## EnemyInfoPanel

### 기본 섹션 (항상 표시)
- 적 이름
- HP (현재 / 전체)
- EnemyType (Normal / Elite / Boss)
- 속도 (speed), 회피 (dodge)
- 저항: stun / move / poison / disease / bleed / debuff (6항목)
- 보유 기술명 목록 (SkillData.SkillName)

### 조준 섹션 (PlayerSelectTarget 상태에서만 활성화)
- 명중률 %
- 치명타 확률 %
- 예상 데미지 (MinDamage ~ MaxDamage)

### 메서드
```csharp
void Populate(CombatUnit unit)
void PopulatePreview(AttackPreview preview)
void ShowPreviewSection()
void HidePreviewSection()
void Hide()
```

---

## CombatHUD

```csharp
[SerializeField] private EnemyInfoPanel m_EnemyInfoPanel;

public void ShowEnemyInfo(CombatUnit unit)
    // Populate(unit) 호출
    // state == PlayerSelectTarget 이면
    //   PreviewAttack 계산 후 PopulatePreview + ShowPreviewSection
    // else HidePreviewSection
    // 패널 SetActive(true)

public void HideEnemyInfo()
    // 패널 SetActive(false)
```

---

## CombatFieldView

`CreateUnitView` 내부, BoxCollider2D 추가 직후:
```csharp
UnitHoverHandler hoverHandler = go.AddComponent<UnitHoverHandler>();
hoverHandler.Initialize(
    () => m_CombatHUD.ShowEnemyInfo(captured),
    () => m_CombatHUD.HideEnemyInfo()
);
```
- Nikke에는 hover 패널 불필요 → Enemy 유닛만 등록
- CombatFieldView가 CombatHUD 레퍼런스를 가지거나, 콜백을 외부에서 주입받는 방식 중 택1
  (현재 CombatHUD는 CombatStateMachine이 가지고 있으므로 CombatFieldView에 SerializeField로 추가)

---

## 엣지 케이스 처리

### 유닛 사망 시 패널 고착
- `CombatHUD`가 `m_HoveredUnit` 필드로 현재 hover 중인 유닛 추적
- `UnitDiedEvent` 구독 → 죽은 유닛이 `m_HoveredUnit`이면 `HideEnemyInfo()` 강제 호출

### 상태 전환 시 preview 섹션 잔존
- 시나리오: 마우스가 적 위에 있는 상태에서 스킬 취소 → `PlayerSelectTarget` 종료
  → `OnMouseExit` 미발생 → preview 섹션이 그대로 남음
- 해결: `CombatHUD`가 `CombatStateMachine.OnStateChanged` 구독
  → `PlayerSelectTarget`이 아닌 상태로 전환 시 `HidePreviewSection()` 호출

## CombatStateMachine 추가 노출
```csharp
public SkillData SelectedSkill => m_SelectedSkill;
public SkillExecutor SkillExecutor => m_SkillExecutor;
```
CombatHUD는 기존 m_CombatStateMachine 레퍼런스를 통해 접근

## AttackPreview 계산 기준
기존 CalcDamage와 동일하게 2단계 int 캐스팅 적용 (소수점 절사 오차 일치)
```
int rawMin = (int)(attacker.minDamage * skill.DamageMultiplier);
int rawMax = (int)(attacker.maxDamage * skill.DamageMultiplier);
MinDamage  = (int)(rawMin * (1f - target.defense / 100f))
MaxDamage  = (int)(rawMax * (1f - target.defense / 100f))
HitChance  = attacker.accuracyMod + skill.AccuracyMod - target.dodge
CritChance = attacker.critChance + skill.CritMod
```
- 크리티컬은 별도 % 표시. 데미지 범위는 논크리티컬 기준
- target.State == Corpse 이면 dodge = 0, defense = 0 적용

## OnStateChanged 구독 방식
`CombatStateMachine.OnStateChanged`는 C# delegate event (EventBus 아님).
CombatHUD에서 `m_CombatStateMachine.OnStateChanged` 를 직접 구독/해제:
- `OnEnable()` 에서 `+=` 구독
- `OnDisable()` 에서 `-=` 해제
- 기존 EventBus 구독 쌍과 동일한 위치에 작성
