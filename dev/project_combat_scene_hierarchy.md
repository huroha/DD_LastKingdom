---
name: CombatScene Hierarchy
description: CombatScene의 전체 Hierarchy 구조 — UI 연결 작업 시 오브젝트 위치 참조용
type: project
---

# CombatScene Hierarchy

```
CombatScene
├── MainCamera → Camera (Perspective, FOV 57)
├── CombatStateMachine (empty) → CombatStateMachine.cs
├── CombatDirector (empty) → CombatDirector.cs
├── CombatFocusController (empty) → CombatFocusController.cs
├── CombatDriftController (empty) → CombatDriftController.cs
├── CombatFieldView (empty) → CombatFieldView.cs
├── CombatFeedback (empty) → CombatFeedback.cs
├── DamagePopupPool (empty) → DamagePopupPool.cs
├── NikkeFocusPoint → Transform (Focus 연출 기준점)
├── EnemyFocusPoint → Transform (Focus 연출 기준점)
├── FocusCam → FocusBlurController.cs
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
└── Canvas
    └── CombatHUD (empty) → CombatHUD.cs
        ├── TurnTickerDisplay (empty) → CombatTurnTickerDisplay.cs
        ├── TurnBarDisplay (empty) → CombatTurnBarDisplay.cs
        │   ├── ActiveBar (Image) — size-1 ActiveTurnBar
        │   └── LargeActiveBar (Image) — size-2 ActiveTurnBar
        ├── Nikkes (empty)
        │   └── NikkeSlot_0~3 (prefab)
        │       ├── UnitName (TMP)
        │       ├── Hp bar (Slider)
        │       ├── Ebla bar (empty)
        │       │   └── Image 0~9 (총 10개)
        │       ├── NikkeSelect (Button)
        │       ├── Highlight (Image)
        │       ├── EnemyTargetHighlight (Image)
        │       ├── TurnTickers
        │       │   └── TurnTicker Image × 3
        │       └── StatusEffectIcon → StatusEffectIconDisplay.cs
        ├── Enemies (empty)
        │   ├── EnemySlot_0~3 (prefab)
        │   │   ├── Hp bar (Slider)
        │   │   ├── UnitName (TMP)
        │   │   ├── EnemySelect (Button)
        │   │   ├── TurnTickers
        │   │   │   └── TurnTicker Image × 2
        │   │   ├── Highlight (Image)
        │   │   └── StatusEffectIcon → StatusEffectIconDisplay.cs
        │   └── LargeEnemySlot_0~2 (prefab)
        │       ├── Hp bar (Slider)
        │       ├── UnitName (TMP)
        │       ├── EnemySelect (Button)
        │       ├── TurnTickers
        │       │   └── TurnTicker Image × 3
        │       ├── Highlight (Image)
        │       └── StatusEffectIcon → StatusEffectIconDisplay.cs
        ├── TurnOrderPanel (empty)
        │   └── TMP text (전체 유닛 턴 순서 출력용)
        ├── AnnouncePanel (empty) → Animator
        │   ├── Image
        │   └── TMP text (적 스킬명 / 턴 넘김 표시)
        ├── BottomUI
        │   ├── panel_transition (Image)
        │   ├── panel_nikke (Image)
        │   ├── panel_skill (Image)
        │   ├── panel_side1 (Image)
        │   ├── panel_side2 (Image)
        │   └── panel_inventory (Image)
        ├── SkillSelectPanel → SkillSelectPanel.cs
        │   ├── SkillBtn_0~3 (Button) → TMP text
        │   ├── PassBtn (Button)
        │   └── MoveBtn (Button)
        ├── NikkeInfoPanel → NikkeInfoPanel.cs
        │   ├── Portrait&Identity (empty)
        │   │   ├── panel_portrait (Image)
        │   │   ├── Name (TMP)
        │   │   └── Class (TMP)
        │   ├── Skills (empty)
        │   │   ├── icon_0~3 (Image)
        │   │   ├── icon_move (Image)
        │   │   └── icon_pass (Image)
        │   ├── Status (empty)
        │   │   ├── HP (TMP)
        │   │   └── Ebla (TMP)
        │   └── Stat (empty)
        │       ├── Acc (TMP)
        │       ├── Crit (TMP)
        │       ├── Dmg (TMP)
        │       ├── Dodge (TMP)
        │       └── Prot (TMP)
        ├── EnemyInfoPanel → EnemyInfoPanel.cs
        │   ├── Enemy_infopanel (Image)
        │   ├── basic_info (empty)
        │   └── preview_info (empty)
        ├── TargetSelectPanel → TargetSelectPanel.cs
        │   └── Cancel (Button)
        ├── SelectBar (empty)
        │   ├── image (size-1 전용)
        │   └── image (size-2 전용)
        ├── WarningUI (empty)
        │   └── Image
        ├── RoundUI (empty)
        │   ├── roundbg1 (Image)
        │   ├── round_effect (Image) → RoundAnimEvent.cs + Animator
        │   ├── roundbg2 (Image)
        │   └── roundtext (TMP text)
        ├── CombatTooltip (empty) → CombatTooltip.cs
        │   ├── bg_tooltip (Image)
        │   └── txt_tooltip (TMP)
        ├── SkillTooltip (empty) → SkillTooltip.cs
        │   ├── bg_tooltip (Image)
        │   ├── position_display → SkillPositionDisplay.cs
        │   └── txt_tooltip (TMP)
        └── PassLabel (empty) → FloatingLabel.cs
└── EventSystem
```

## 주요 연결 관계
- `CombatHUD.cs` → Canvas/CombatHUD에 부착
- `CombatTurnTickerDisplay.cs` → Canvas/CombatHUD/TurnTickerDisplay에 부착
- `CombatTurnBarDisplay.cs` → Canvas/CombatHUD/TurnBarDisplay에 부착
- `CombatFocusController.cs` → CombatFocusController 오브젝트에 부착
- `CombatDriftController.cs` → CombatDriftController 오브젝트에 부착
- `RoundAnimEvent.cs` → round_effect에 부착, m_CombatHUD 필드에 CombatHUD 오브젝트 연결

## 스크립트 폴더 구조 (Assets/Scripts/Combat/)
```
Combat/
├── Core/       CombatStateMachine, CombatState, CombatEvent, CombatUnit
├── Skill/      SkillExecutor, SkillResult, ActiveStatusEffect
├── Turn/       TurnManager, PositionSystem, StatusEffectManager
├── AI/         EnemyAI, EnemyAction
├── View/       CombatFieldView, CombatDirector, CombatFocusController,
│               CombatDriftController, CombatCameraTilt, UnitAnimBridge
└── UI/         CombatHUD, CombatTurnTickerDisplay, CombatTurnBarDisplay,
                TargetSelectPanel, SkillSelectPanel
```

## Animation Event 브릿지 패턴
Animator가 붙은 GameObject와 실제 로직 스크립트가 다른 GameObject에 있을 때:
1. Animator GameObject에 브릿지 스크립트 부착
2. 브릿지 스크립트에 `[SerializeField]`로 로직 스크립트 참조
3. Animation Event → 브릿지 메서드 → 로직 메서드 포워딩
