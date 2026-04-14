# HpBarAnimator 설계 명세

**날짜**: 2026-04-08  
**범위**: 전투 HP바 데미지/힐 애니메이션 연출

---

## 개요

현재 HP바는 `slider.value`를 즉시 변경해 피격/힐 감각이 없다.  
GhostFill Image를 추가하고 `HpBarAnimator` MonoBehaviour로 제어해  
데미지는 오렌지 고스트가 줄어들고, 힐은 초록 고스트가 채워지는 연출을 구현한다.

---

## 프리팹 구조 변경

대상 프리팹: `EnemySlot_0`, `LargeEnemySlot_0`, `NikkeSlot_0` (3종)

```
Hp Bar (Slider + HpBarAnimator)        ← HpBarAnimator 컴포넌트 추가
├── Background (Image)
├── Fill Area
│   ├── GhostFill (Image)              ← 새로 추가 (Fill보다 먼저 → 뒤에 렌더)
│   └── Fill (Image)                   ← 기존 메인 바
└── ...
```

**GhostFill Image 설정:**
- Image Type: Filled
- Fill Method: Horizontal
- Fill Origin: Left
- Raycast Target: Off
- 초기 색상/알파: 임의 (코드에서 런타임 설정)
- RectTransform: Fill과 동일한 앵커/크기

---

## HpBarAnimator 컴포넌트

**위치**: `Assets/Scripts/Combat/UI/HpBarAnimator.cs`  
**부착 대상**: "Hp Bar" GameObject (Slider와 동일 오브젝트)

### 직렬화 필드

| 필드 | 타입 | 설명 |
|------|------|------|
| `m_MainSlider` | `Slider` | 기존 HP 슬라이더 |
| `m_GhostFill` | `Image` | GhostFill Image |
| `m_HoldDuration` | `float` | 고스트 유지 시간 (기본 0.4초) |
| `m_DrainDuration` | `float` | 고스트 이동 시간 (기본 0.5초) |
| `m_DamageColor` | `Color` | 오렌지 (1, 0.5, 0, 1) |
| `m_HealColor` | `Color` | 초록 (0.2, 0.9, 0.2, 1) |

### private 상태

| 필드 | 타입 | 설명 |
|------|------|------|
| `m_ActiveCoroutine` | `Coroutine` | 현재 진행 중인 코루틴 |

### Public API

```csharp
void InitHp(int currentHp, int maxHp)
```
- 애니메이션 없이 즉시 설정 (전투 시작 초기화용)
- 진행 중 코루틴 중단
- MainSlider.value, GhostFill.fillAmount 모두 즉시 newValue로 설정
- GhostFill 비활성화

```csharp
void SetHp(int currentHp, int maxHp)
```
- 애니메이션 포함 갱신
- 값이 변하지 않으면 early return
- 연속 호출 시 현재 GhostFill.fillAmount에서 이어서 시작

### AnimateRoutine 코루틴 흐름

```
ghostStart = 진행 중 코루틴 있으면 GhostFill.fillAmount, 없으면 MainSlider.value
targetValue = newHp / maxHp
isDamage = targetValue < MainSlider.value

① MainSlider.value = targetValue (즉시)
② GhostFill.fillAmount = ghostStart
③ GhostFill.color = isDamage ? DamageColor : HealColor
④ GhostFill 활성화
⑤ WaitForSeconds(HoldDuration)
⑥ Lerp: GhostFill.fillAmount → ghostStart에서 targetValue로 (DrainDuration 동안)
⑦ GhostFill 비활성화, m_ActiveCoroutine = null
```

**데미지** (targetValue < ghostStart): 고스트가 높은 값→낮은 값으로 줄어듦  
**힐** (targetValue > ghostStart): 고스트가 낮은 값→높은 값으로 커짐 (메인 바를 따라잡는 느낌)

---

## CombatHUD 변경

### 필드 교체

```csharp
// Before
[SerializeField] private Slider[] m_NikkeHpBars;
[SerializeField] private Slider[] m_EnemyHpBars;
[SerializeField] private Slider[] m_LargeEnemyHpBars;

// After
[SerializeField] private HpBarAnimator[] m_NikkeHpBars;
[SerializeField] private HpBarAnimator[] m_EnemyHpBars;
[SerializeField] private HpBarAnimator[] m_LargeEnemyHpBars;
```

### RefreshHpBar 변경

```csharp
// Before
m_NikkeHpBars[index].value = (float)unit.CurrentHp / unit.MaxHp;
bar.value = (float)unit.CurrentHp / unit.MaxHp;

// After
m_NikkeHpBars[index].SetHp(unit.CurrentHp, unit.MaxHp);
bar.SetHp(unit.CurrentHp, unit.MaxHp);
```

### InitializeUnits 변경

전투 시작 시 `SetHp` → `InitHp` 호출 (슬롯 초기화 위치에서)

### gameObject.SetActive 호출

변경 없음. `HpBarAnimator`가 Slider와 같은 GameObject에 있으므로 그대로 동작.

---

## 미해결 사항

없음.
