# HpBarAnimator 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** HP바에 데미지(오렌지)/힐(초록) 고스트 애니메이션 연출 추가

**Architecture:** `HpBarAnimator` MonoBehaviour가 Slider + GhostFill Image를 소유하고 코루틴으로 제어. CombatHUD는 `Slider[]` 대신 `HpBarAnimator[]`를 참조.

**Tech Stack:** Unity 6 URP 2D, UGUI Slider/Image, C# 코루틴

---

## 파일 맵

| 파일 | 변경 유형 | 내용 |
|------|----------|------|
| `Assets/Scripts/Combat/UI/HpBarAnimator.cs` | **새로 작성** | 고스트 애니메이션 컴포넌트 |
| `Assets/Prefabs/UI/EnemySlot_0.prefab` | **에디터 수정** | GhostFill Image 추가 + HpBarAnimator 연결 |
| `Assets/Prefabs/UI/LargeEnemySlot_0.prefab` | **에디터 수정** | 동일 |
| `Assets/Prefabs/UI/NikkeSlot_0.prefab` | **에디터 수정** | 동일 |
| `Assets/Scripts/Combat/UI/CombatHUD.cs` | **수정** | Slider[] → HpBarAnimator[], RefreshHpBar 수정 |

---

## Task 1: HpBarAnimator 컴포넌트 작성

**Files:**
- Create: `Assets/Scripts/Combat/UI/HpBarAnimator.cs`

### 뼈대

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HpBarAnimator : MonoBehaviour
{
    [SerializeField] private Slider m_MainSlider;
    [SerializeField] private Image m_GhostFill;

    [Header("Timing")]
    [SerializeField] private float m_HoldDuration = 0.4f;
    [SerializeField] private float m_DrainDuration = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color m_DamageColor;   // 오렌지: (1, 0.5, 0, 1)
    [SerializeField] private Color m_HealColor;     // 초록: (0.2, 0.9, 0.2, 1)

    private Coroutine m_ActiveCoroutine;

    public void InitHp(int currentHp, int maxHp) { ... }
    public void SetHp(int currentHp, int maxHp) { ... }

    private IEnumerator AnimateRoutine(float ghostStart, float targetValue, bool isDamage) { ... }
}
```

### 각 메서드 구현 내용

**`InitHp(int currentHp, int maxHp)`**
- 진행 중 코루틴이 있으면 `StopCoroutine` 후 null 처리
- `float value = (float)currentHp / maxHp`
- `m_MainSlider.value = value`
- `m_GhostFill.fillAmount = value`
- `m_GhostFill.gameObject.SetActive(false)`

**`SetHp(int currentHp, int maxHp)`**
- `float newValue = (float)currentHp / maxHp`
- `newValue == m_MainSlider.value`이면 early return
- `ghostStart`: 코루틴이 진행 중이면 `m_GhostFill.fillAmount`, 아니면 `m_MainSlider.value`
- 진행 중 코루틴이 있으면 `StopCoroutine`
- `isDamage = newValue < m_MainSlider.value`
- `m_ActiveCoroutine = StartCoroutine(AnimateRoutine(ghostStart, newValue, isDamage))`

**`AnimateRoutine(float ghostStart, float targetValue, bool isDamage)`**
1. `m_MainSlider.value = targetValue` (즉시)
2. `m_GhostFill.fillAmount = ghostStart`
3. `m_GhostFill.color = isDamage ? m_DamageColor : m_HealColor`
4. `m_GhostFill.gameObject.SetActive(true)`
5. `yield return new WaitForSeconds(m_HoldDuration)`
6. `elapsed = 0f`인 while 루프: `elapsed < m_DrainDuration` 동안  
   - `elapsed += Time.deltaTime`  
   - `m_GhostFill.fillAmount = Mathf.Lerp(ghostStart, targetValue, elapsed / m_DrainDuration)`  
   - `yield return null`
7. `m_GhostFill.fillAmount = targetValue`
8. `m_GhostFill.gameObject.SetActive(false)`
9. `m_ActiveCoroutine = null`

- [ ] `HpBarAnimator.cs` 파일 작성 (Unity Editor에서 직접 생성)
- [ ] Unity에서 컴파일 오류 없는지 확인

---

## Task 2: 프리팹 3종에 GhostFill Image 추가 (에디터 작업)

**Files:**
- Modify: `Assets/Prefabs/UI/EnemySlot_0.prefab`
- Modify: `Assets/Prefabs/UI/LargeEnemySlot_0.prefab`
- Modify: `Assets/Prefabs/UI/NikkeSlot_0.prefab`

각 프리팹에서 동일한 작업을 수행한다.

- [ ] **EnemySlot_0 프리팹 열기**

  1. `Hp Bar` GameObject 선택
  2. `Fill Area` 자식 확인 → 그 안의 `Fill` Image 확인
  3. `Fill Area` 안에 새 Image GameObject 추가: 이름 `GhostFill`
  4. **`GhostFill`을 `Fill`보다 위(앞)에 배치** (Hierarchy에서 위쪽 = 먼저 렌더링 = 뒤에 보임)
  5. `GhostFill`의 RectTransform을 `Fill`과 동일하게 설정:
     - Anchor: Stretch-Stretch (min 0,0 / max 1,1)
     - Left/Right/Top/Bottom: `Fill`과 동일한 값으로 맞춤
  6. `GhostFill` Image 컴포넌트 설정:
     - Image Type: **Filled**
     - Fill Method: **Horizontal**
     - Fill Origin: **Left**
     - Fill Amount: 1 (임시)
     - Raycast Target: **Off**
  7. `Hp Bar` GameObject에 `HpBarAnimator` 컴포넌트 추가
  8. Inspector에서 연결:
     - `m_MainSlider` → `Hp Bar`의 Slider 컴포넌트
     - `m_GhostFill` → 방금 만든 `GhostFill` Image
     - `m_DamageColor`: R=1, G=0.5, B=0, A=1 (오렌지)
     - `m_HealColor`: R=0.2, G=0.9, B=0.2, A=1 (초록)
  9. 프리팹 저장

- [ ] **LargeEnemySlot_0 프리팹** — 동일한 작업 반복
- [ ] **NikkeSlot_0 프리팹** — 동일한 작업 반복

---

## Task 3: CombatHUD — 필드 타입 교체

**Files:**
- Modify: `Assets/Scripts/Combat/UI/CombatHUD.cs:40,58,63`

- [ ] **Slider[] → HpBarAnimator[] 교체** (3곳)

  ```csharp
  // Before (line 40)
  [SerializeField] private Slider[] m_NikkeHpBars;

  // After
  [SerializeField] private HpBarAnimator[] m_NikkeHpBars;
  ```

  ```csharp
  // Before (line 58)
  [SerializeField] private Slider[] m_EnemyHpBars;

  // After
  [SerializeField] private HpBarAnimator[] m_EnemyHpBars;
  ```

  ```csharp
  // Before (line 63)
  [SerializeField] private Slider[] m_LargeEnemyHpBars;

  // After
  [SerializeField] private HpBarAnimator[] m_LargeEnemyHpBars;
  ```

- [ ] Unity 컴파일 오류 확인 (아직 `RefreshHpBar`에서 `.value` 참조가 남아있어 오류 발생 — Task 4에서 수정)

---

## Task 4: CombatHUD — RefreshHpBar 수정

**Files:**
- Modify: `Assets/Scripts/Combat/UI/CombatHUD.cs:326-341`

- [ ] **`RefreshHpBar` 메서드 수정**

  ```csharp
  // Before
  private void RefreshHpBar(CombatUnit unit)
  {
      int index = unit.SlotIndex;
      if (unit.UnitType == CombatUnitType.Nikke)
      {
          m_NikkeHpBars[index].value = (float)unit.CurrentHp / unit.MaxHp;
          UpdateEblaBar(index, unit.Ebla);
      }
      else
      {
          Slider bar = unit.SlotSize == 2 ? m_LargeEnemyHpBars[unit.SlotIndex] : m_EnemyHpBars[unit.SlotIndex];
          if (unit.State == UnitState.Corpse)
              bar.value = (float)unit.CurrentHp / Mathf.Max(unit.EnemyData.CorpseHp, 1);
          else
              bar.value = (float)unit.CurrentHp / unit.MaxHp;
      }
      RefreshStatusIcons(unit);
  }

  // After
  private void RefreshHpBar(CombatUnit unit)
  {
      int index = unit.SlotIndex;
      if (unit.UnitType == CombatUnitType.Nikke)
      {
          m_NikkeHpBars[index].SetHp(unit.CurrentHp, unit.MaxHp);
          UpdateEblaBar(index, unit.Ebla);
      }
      else
      {
          HpBarAnimator bar = unit.SlotSize == 2 ? m_LargeEnemyHpBars[unit.SlotIndex] : m_EnemyHpBars[unit.SlotIndex];
          if (unit.State == UnitState.Corpse)
              bar.SetHp(unit.CurrentHp, Mathf.Max(unit.EnemyData.CorpseHp, 1));
          else
              bar.SetHp(unit.CurrentHp, unit.MaxHp);
      }
      RefreshStatusIcons(unit);
  }
  ```

- [ ] Unity 컴파일 오류 없는지 확인

---

## Task 5: CombatHUD — BattleStarted 초기화 시 InitHp 사용

**Files:**
- Modify: `Assets/Scripts/Combat/UI/CombatHUD.cs:193-282`

`OnBattleStarted`에서 `RefreshHpBar`를 호출하는 시점은 전투 시작이므로
애니메이션 없이 즉시 값을 세팅해야 한다. `RefreshHpBar`가 `SetHp`를 쓰면
초기화 시에도 고스트 애니메이션이 발생할 수 있다. 별도 초기화 경로를 분리한다.

- [ ] **`InitHpBar(CombatUnit unit)` 메서드 추가**

  ```csharp
  private void InitHpBar(CombatUnit unit)
  {
      int index = unit.SlotIndex;
      if (unit.UnitType == CombatUnitType.Nikke)
      {
          m_NikkeHpBars[index].InitHp(unit.CurrentHp, unit.MaxHp);
          UpdateEblaBar(index, unit.Ebla);
      }
      else
      {
          HpBarAnimator bar = unit.SlotSize == 2 ? m_LargeEnemyHpBars[unit.SlotIndex] : m_EnemyHpBars[unit.SlotIndex];
          if (unit.State == UnitState.Corpse)
              bar.InitHp(unit.CurrentHp, Mathf.Max(unit.EnemyData.CorpseHp, 1));
          else
              bar.InitHp(unit.CurrentHp, unit.MaxHp);
      }
      RefreshStatusIcons(unit);
  }
  ```

- [ ] **`OnBattleStarted`에서 `RefreshHpBar` 호출 → `InitHpBar`로 교체** (2곳)

  - line 232: `RefreshHpBar(e.Nikkes[i])` → `InitHpBar(e.Nikkes[i])`
  - line 259: `RefreshHpBar(enemy)` → `InitHpBar(enemy)`

- [ ] Unity 컴파일 오류 없는지 확인

---

## Task 6: Inspector 연결 및 동작 확인

**Files:**
- Modify: CombatScene.unity (Inspector 재연결)

타입이 `Slider[]` → `HpBarAnimator[]`로 변경되었으므로
CombatHUD Inspector의 기존 참조가 끊어진다.

- [ ] **CombatScene 열기 → CombatHUD 오브젝트 선택**
- [ ] Inspector에서 `m_NikkeHpBars`, `m_EnemyHpBars`, `m_LargeEnemyHpBars` 배열 재연결
  - 각 슬롯의 "Hp Bar" GameObject를 드래그 (HpBarAnimator 컴포넌트가 있는 오브젝트)
- [ ] Play Mode 진입 → 전투 시작
- [ ] 적 또는 니케에게 스킬 사용 → HP바 오렌지 고스트 확인
- [ ] (힐 스킬이 있다면) 힐 사용 → 초록 고스트 확인
- [ ] 연속 피격 시 고스트가 현재 위치에서 이어지는지 확인

---

## 체크리스트 요약

- [ ] Task 1: HpBarAnimator.cs 작성
- [ ] Task 2: 프리팹 3종 에디터 수정
- [ ] Task 3: CombatHUD 필드 타입 교체
- [ ] Task 4: RefreshHpBar 수정
- [ ] Task 5: InitHpBar 추가 및 OnBattleStarted 수정
- [ ] Task 6: Inspector 재연결 및 동작 확인
