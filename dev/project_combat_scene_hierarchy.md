---
name: CombatScene Hierarchy
description: CombatScene의 전체 Hierarchy 구조 — UI 연결 작업 시 오브젝트 위치 참조용
type: project
---

# CombatScene Hierarchy

```
CombatScene
├── MainCamera
├── CombatStateMachine (empty) → CombatStateMachine.cs
├── BG (empty)
│   └── background → Sprite Renderer
├── CombatField (empty)
│   ├── Nikkes (empty)
│   │   ├── Slot0 → Transform
│   │   ├── Slot1 → Transform
│   │   ├── Slot2 → Transform
│   │   └── Slot3 → Transform
│   └── Enemies (empty)
│       ├── Slot0 → Transform
│       ├── Slot1 → Transform
│       ├── Slot2 → Transform
│       └── Slot3 → Transform
├── Canvas
│   └── CombatHUD (empty) → CombatHUD.cs
│       ├── Nikkes (empty)
│       │   └── NikkeSlot_0~3 (prefab)
│       │       ├── UnitName (TMP)
│       │       ├── Hp bar (Slider)
│       │       ├── Ebla bar (empty)
│       │       │   └── Image 0~9 (총 10개)
│       │       ├── NikkeSelect (Button)
│       │       ├── Highlight (Image)
│       │       ├── EnemyTargetHighlight (Image)
│       │       └── TurnTickers
│       │           └── TurnTicker Image × 3
│       ├── Enemies (empty)
│       │   ├── EnemySlot_0~3 (prefab)
│       │   │   ├── Hp bar (Slider)
│       │   │   ├── UnitName (TMP)
│       │   │   ├── EnemySelect (Button)
│       │   │   ├── TurnTickers
│       │   │   │   └── TurnTicker Image × 2
│       │   │   └── Highlight (Image)
│       │   └── LargeEnemySlot_0~2 (prefab)
│       │       ├── Hp bar (Slider)
│       │       ├── UnitName (TMP)
│       │       ├── EnemySelect (Button)
│       │       ├── TurnTickers
│       │       │   └── TurnTicker Image × 3
│       │       └── Highlight (Image)
│       ├── TurnOrderPanel (empty)
│       │   ├── TMP text (전체 유닛 턴 순서 출력용)
│       ├── AnnouncePanel (empty) → Animator
│       │   ├── Image
│       │   └── TMP text (적 스킬명 / 턴 넘김 표시)
│       ├── BottomUI
│       │   ├── panel_transition (Image)
│       │   ├── panel_nikke (Image)
│       │   ├── panel_skill (Image)
│       │   ├── panel_side1 (Image)
│       │   └── panel_side2 (Image)
│       │   └── panel_inventory (Image)
│       ├── SkillSelectPanel → SkillSelectPanel.cs
│       │   ├── SkillBtn_0~3 (Button) → TMP text
│       │   ├── PassBtn (Button)
│       │   └── MoveBtn (Button)
│       ├── NikkeInfoPanel → NikkeInfoPanel.cs
│       │   ├── Portrait&Identity (empty)
│       │   │   ├── panel_portrait (Image)
│       │   │   ├── Name (TMP)
│       │   │   └── Class (TMP)
│       │   ├── Skills (empty)
│       │   │   ├── icon_0~3 (Image)
│       │   │   ├── icon_move (Image)
│       │   │   └── icon_pass (Image)
│       │   ├── Status (empty)
│       │   │   ├── HP (TMP)
│       │   │   └── Ebla (TMP)
│       │   └── Stat (empty)
│       │       ├── Acc (TMP)
│       │       ├── Crit (TMP)
│       │       ├── Dmg (TMP)
│       │       ├── Dodge (TMP)
│       │       └── Prot (TMP)
│       ├── TargetSelectPanel → TargetSelectPanel.cs
│       │   └── Cancel (Button)
│       ├── SelectBar (empty)
│       │   ├── image (size-1 전용)
│       │   ├── image (size-2 전용)
│       ├── WarningUI (empty)
│       │   └── Image
│       └── RoundUI (empty)
│           ├── roundbg1 (Image)
│           ├── round_effect (Image) → RoundAnimEvent.cs + Animator
│           ├── roundbg2 (Image)
│           └── roundtext (TMP text)
└── EventSystem
```

## 주요 연결 관계
- `CombatHUD.cs` → Canvas/CombatHUD에 부착
- `RoundAnimEvent.cs` → round_effect에 부착, m_CombatHUD 필드에 CombatHUD 오브젝트 연결
- Animation Event 패턴: 다른 GameObject의 메서드 호출이 필요할 때 브릿지 스크립트를 해당 Animator GameObject에 부착

## Animation Event 브릿지 패턴
Animator가 붙은 GameObject와 실제 로직 스크립트가 다른 GameObject에 있을 때:
1. Animator GameObject에 브릿지 스크립트 부착
2. 브릿지 스크립트에 `[SerializeField]`로 로직 스크립트 참조
3. Animation Event → 브릿지 메서드 → 로직 메서드 포워딩
