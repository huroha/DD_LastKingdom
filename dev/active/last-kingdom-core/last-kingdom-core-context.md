# Last Kingdom Core - Context & Decisions

## Status
- Phase: Phase 1.4 진행 중 (전투 시스템 핵심)
- Progress: 12 / 65 tasks complete (18%)
- Last Updated: 2026-03-02

## Key Files

### 프로젝트 구조 (현재 상태)
```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs          # 전역 싱글톤, 게임 상태 관리 ✅
│   │   ├── SceneFlowManager.cs     # 씬 전환 (Town↔Dungeon↔Combat) ✅
│   │   ├── AudioManager.cs         # BGM/SFX 관리
│   │   └── DataManager.cs          # SO 로딩, 세이브/로드 중재
│   ├── Data/                        # ScriptableObject 정의
│   │   ├── StatBlock.cs            # 스탯 구조체 (ResistanceBlock 포함) ✅
│   │   ├── NikkeData.cs            # 니케 캐릭터 데이터 ✅
│   │   ├── SkillData.cs            # 스킬 데이터 ✅
│   │   ├── EnemyData.cs            # 적 데이터 ✅
│   │   ├── StatusEffectData.cs     # 상태이상 데이터 ✅
│   │   ├── SquadData.cs            # 스쿼드 그룹 데이터 ✅
│   │   ├── RoomData.cs             # 방 타입/이벤트 데이터
│   │   ├── BuildingData.cs         # 건물 업그레이드 데이터
│   │   ├── TrinketData.cs          # 장신구 데이터
│   │   └── SupplyItemData.cs       # 보급품 데이터
│   ├── Combat/
│   │   ├── CombatUnit.cs           # 런타임 유닛 (Pure C#) ✅
│   │   ├── ActiveStatusEffect.cs   # 런타임 상태이상 인스턴스 (Pure C#) ✅
│   │   ├── CombatEvent.cs          # 전투 EventBus 이벤트 타입 선언 ✅
│   │   ├── TurnManager.cs          # SPD 기반 행동 순서 (Pure C#) ✅
│   │   ├── PositionSystem.cs       # 4슬롯 포지션 관리
│   │   ├── SkillExecutor.cs        # 스킬 실행, 데미지 계산
│   │   ├── StatusEffectManager.cs  # DOT/Debuff 틱 처리
│   │   └── CombatStateMachine.cs   # 전투 FSM
│   ├── Systems/
│   │   ├── EblaSystem.cs           # 에블라 수치 관리
│   │   ├── DetectionSystem.cs      # 위험발각 시스템
│   │   ├── InventorySystem.cs      # 인벤토리 관리
│   │   ├── TrinketSystem.cs        # 장신구 장착/스탯보정
│   │   ├── SaveSystem.cs           # JSON 직렬화/역직렬화
│   │   └── QuestSystem.cs          # 퀘스트 목표 추적
│   ├── Dungeon/
│   │   ├── IDungeonMapProvider.cs  # 던전 맵 제공 인터페이스
│   │   ├── ManualDungeonMapProvider.cs # 수동 맵 (SO 기반)
│   │   ├── DungeonNavigator.cs     # 이동, 방 진입
│   │   ├── CampSystem.cs           # 야영 처리
│   │   └── ExplorationEventManager.cs # 함정/골동품/이벤트
│   ├── Town/
│   │   ├── BuildingManager.cs      # 건물 업그레이드
│   │   ├── RosterManager.cs        # 니케 풀 관리
│   │   └── ShopManager.cs          # 보급품 구매
│   ├── UI/
│   │   ├── CombatHUD.cs            # 전투 UI
│   │   ├── TownUI.cs               # 마을 UI
│   │   ├── DungeonUI.cs            # 던전 UI
│   │   ├── InventoryUI.cs          # 인벤토리/장비 UI
│   │   └── NikkeInfoPanel.cs       # 니케 상세 정보
│   ├── VFX/
│   │   ├── CombatFeedback.cs       # 히트스탑/쉐이크/플래시
│   │   └── EblaVFX.cs              # 에블라 시각 연출
│   └── Utils/
│       ├── Singleton.cs            # 제네릭 싱글톤 베이스 ✅
│       ├── ObjectPool.cs           # 오브젝트 풀
│       └── EventBus.cs             # 전역 이벤트 시스템 ✅
├── ScriptableObjects/
│   ├── Nikkes/                     # 니케 SO 에셋 ✅
│   ├── Skills/                     # 스킬 SO 에셋 ✅
│   ├── Enemies/                    # 적 SO 에셋 ✅
│   ├── StatusEffects/              # 상태이상 SO 에셋 ✅
│   ├── Squads/                     # 스쿼드 SO 에셋 ✅
│   ├── Buildings/                  # 건물 SO 에셋
│   ├── Trinkets/                   # 장신구 SO 에셋
│   ├── Rooms/                      # 방 타입 SO 에셋
│   └── Supplies/                   # 보급품 SO 에셋
├── Prefabs/
│   ├── Combat/                     # 전투 유닛 프리팹
│   ├── Dungeon/                    # 던전 방/복도 프리팹
│   ├── UI/                         # UI 프리팹
│   └── VFX/                        # 이펙트 프리팹
├── Scenes/
│   ├── BootScene.unity
│   ├── TitleScene.unity
│   ├── TownScene.unity
│   ├── DungeonScene.unity
│   └── CombatScene.unity
├── Art/
│   ├── Nikkes/                     # 니케 일러스트/스프라이트
│   ├── Enemies/                    # 적 스프라이트
│   ├── Dungeon/                    # 던전 배경/타일
│   ├── Town/                       # 마을 배경/건물
│   ├── UI/                         # UI 스프라이트
│   └── VFX/                        # 이펙트 스프라이트
├── Audio/
│   ├── BGM/
│   └── SFX/
├── Animations/
│   ├── Nikkes/                     # 니케 Animator Controller + Clips
│   └── Enemies/                    # 적 Animator Controller + Clips
└── Resources/
    └── GameConfig/                 # 런타임 로드 필요한 설정
```

## Key Decisions

### 1. 렌더 파이프라인: URP 2D (2026-02-25)
- **Rationale**: 2D 프로젝트에 최적. Post Processing 내장 지원
- **Alternatives**: Built-in RP (Post Processing 별도 패키지 필요), HDRP (2D에 과함)
- **Trade-offs**: URP 2D Renderer는 3D 기능 제한적이나 본 프로젝트에선 불필요

### 2. 입력 시스템: New Input System (2026-02-25)
- **Rationale**: PC 키보드/마우스 입력 처리에 적합, Action Map 기반 입력 추상화
- **Alternatives**: Old Input Manager (간단하지만 확장성 낮음)
- **Trade-offs**: 학습 곡선이 약간 있지만 장기적 이점이 큼

### 3. UI: UGUI (Canvas 기반) (2026-02-25)
- **Rationale**: 초급자에게 직관적, 레퍼런스 풍부, Inspector에서 시각적 편집
- **Alternatives**: UI Toolkit (코드 기반, 웹 스타일 레이아웃)
- **Trade-offs**: UGUI는 Canvas 재구축 비용이 있지만 본 프로젝트 규모에서는 문제없음

### 4. 전투 씬 분리 (2026-02-25)
- **Rationale**: 디버깅 용이, 메모리 격리, 관리 쉬움
- **Alternatives**: Additive Scene Loading, 같은 씬 내 UI 오버레이
- **Trade-offs**: 씬 전환 시 로딩 발생하지만 Async + 페이드 연출로 커버

### 5. 애니메이션: Sprite Animation (FlipBook 스타일) (2026-02-25)
- **Rationale**: 개발자의 DirectX 2D 경험과 일치. PNG 에셋 준비 가능
- **Alternatives**: Spine 2D (부드러운 보간, 에셋 제작 복잡), Unity 2D Animation (리깅 필요)
- **Trade-offs**: 프레임 수 많으면 메모리 증가, Sprite Atlas로 최적화

### 6. 세이브 방식: JSON + 자동저장 (2026-02-25)
- **Rationale**: 영구 사망 게임에서 세이브 스캠 방지 필수. 핵심 시점 자동저장
- **Alternatives**: Binary Serialization (보안 좋지만 디버깅 어려움), PlayerPrefs (소규모만)
- **Trade-offs**: JSON은 사용자가 파일 수정 가능 → 암호화/체크섬 추가 고려

### 7. 에블라 수치 범위 0~200 2단계 구조 (2026-02-25)
- **확정**: 0~100 정상 구간 → 100 도달 시 Affliction(기본)/Virtue(특정 영웅) 발동 → 100~200 위험 구간 → 200 도달 시 영구 사망
- **Rationale**: 원작 DD의 스트레스 시스템과 동일한 2단계 구조. 100에서 디버프, 200에서 사망이라는 명확한 위험 곡선
- **게임플레이 효과**: 100 이후에도 계속 던전을 진행할지 퇴각할지의 리스크 판단 요소

### 8. 던전 생성: 수동 맵 우선 + 인터페이스 추상화 (2026-02-25)
- **확정**: IDungeonMapProvider 인터페이스 정의, 초기에는 ManualDungeonMapProvider(SO 기반 수동 맵)로 구현. 추후 ProceduralDungeonMapProvider로 교체 가능
- **Rationale**: 절차적 생성은 XL 사이즈 태스크이므로 MVP에서 제외. 인터페이스 추상화로 교체 비용 최소화
- **Trade-offs**: 수동 맵은 리플레이 가치가 낮지만, 핵심 루프 검증에 충분

### 9. 플랫폼: PC(Windows) 전용 (2026-02-25)
- **확정**: Android 포팅 스코프에서 제외
- **Rationale**: 3개월 1인 개발에서 멀티플랫폼은 비현실적. PC에 집중하여 품질 확보
- **비고**: URP 2D 기반이므로 향후 포팅 시 기술적 장벽은 낮음

### 10. HeroData → NikkeData 로 변경 (2026-02-27)
- **확정**: 영웅 데이터 클래스명을 세계관에 맞게 NikkeData로 변경
- **추가된 필드**: NikkeClass, Manufacturer, ElementType, SquadData, 스킬 슬롯 7개(4개 선택), StatGrowthPerLevel
- **Rationale**: 원작 니케 세계관 반영. 스킬 7개 중 4개 선택 시스템으로 커스텀 여지 확보

### 11. CombatUnit Pure C# 클래스 (2026-03-02)
- **확정**: CombatUnit은 MonoBehaviour가 아닌 Pure C# 클래스
- **Rationale**: 데이터/로직은 Pure C# 클래스 원칙. Ebla와 ActiveEffects를 별도 컴포넌트로 분리하지 않고 CombatUnit에 통합
- **기존 계획 변경**: EblaHandler.cs, StatusEffectHolder.cs 별도 컴포넌트 → CombatUnit 내 통합 필드로

### 12. UnitState 4단계 사망 판정 (2026-03-02)
- **확정**: Alive / DeathsDoor / Corpse / Dead
- **Nikke 사망**: HP 0 → DeathsDoor 진입, 추가 피해 시 deathBlowResist 판정 → 실패 시 Dead
- **Enemy 사망**: 일반 피해 HP 0 → Corpse(별도 CorpseHp 보유, 일반 스킬 타겟 가능), DOT 피해 HP 0 → 즉시 Dead
- **Corpse 제거**: 어떤 피해든 HP 0 → 즉시 Dead (deathBlow 없음)
- **Rationale**: 원작 DD의 Death's Door + 시체 시스템을 니케 세계관에 맞게 재설계

### 13. 전투 간 Nikke 상태 유지 (2026-03-02)
- **확정**: Nikke의 HP, Ebla, ActiveEffects는 전투 종료 후에도 유지
- **Rationale**: 던전 내 이동 턴마다 상태이상 소모. 충분히 이동하지 않으면 다음 전투에도 디버프 유지 → 리스크 관리 요소
- **구현**: Nikke 생성자에 currentHp, ebla, activeEffects 파라미터로 주입. Enemy는 매 전투 새 인스턴스 생성

### 15. SlotIndex 0-based (2026-03-02)
- **확정**: SlotIndex는 0-based. index 0 = 최전방
- **Rationale**: 배열 접근 시 `-1` 변환이 불필요해 off-by-one 버그 가능성 제거
- **UI 표시**: UI에서 사람이 읽는 번호가 필요할 때만 `SlotIndex + 1` 변환
- **CombatUnitTest**: 생성자 호출 시 슬롯 번호 0, 1, 2, 3으로 전달

### 14. TurnManager Non-Singleton (2026-03-02)
- **확정**: TurnManager는 Singleton이 아닌 Pure C# 클래스. Combat/ 폴더에 위치
- **Rationale**: 전투 중에만 존재하고 CombatStateMachine만 사용. DontDestroyOnLoad 불필요
- **규칙 확립**: Managers/ 폴더 = DontDestroyOnLoad 싱글톤 전용. 이름에 Manager가 붙어도 전투 전용이면 Combat/에 위치

## Component Architecture

### CombatUnit (전투 유닛) — Pure C# 클래스
```
CombatUnit (Pure C#)
├── 유닛 식별: CombatUnitType, UnitName, SlotIndex
├── SO 참조: NikkeData 또는 EnemyData
├── 런타임 상태: UnitState, IsAlive, CurrentHp, MaxHp
├── 스탯: BaseStats, CurrentStats (버프/디버프 반영)
├── 에블라: Ebla (Nikke 전용, 0~200)
└── 상태이상: ActiveEffects List<ActiveStatusEffect>
```

> 씬 시각 표현(SpriteRenderer, Animator)은 별도 MonoBehaviour에서 CombatUnit을 참조하는 구조로 Phase 3에서 연결 예정

### CombatManager (전투 관리)
```
GameObject "CombatManager"
└── CombatStateMachine.cs (MonoBehaviour) — 전투 FSM
      ├── TurnManager        (Pure C# 멤버) — 행동 순서 정렬/실행
      ├── PositionSystem     (Pure C# 멤버) — 4슬롯 x 2팀 관리
      ├── SkillExecutor      (Pure C# 멤버) — 스킬 실행 파이프라인
      └── StatusEffectManager(Pure C# 멤버) — DOT/Debuff 틱 처리
```

## ScriptableObject Data

### NikkeData.asset (구 HeroData)
```csharp
- m_NikkeName: string
- m_NikkeClass: NikkeClass (Attacker / Supporter / Defender)
- m_Manufacturer: Manufacturer (Pilgrim / Elysion / Missilis / Tetra / Abnormal)
- m_Element: ElementType (Fire / Water / Wind / Iron / Electric)
- m_Squad: SquadData
- m_CanVirtue: bool
- m_BaseStats: StatBlock
- m_MaxLevel: int
- m_ExpThresholds: int[]
- m_StatGrowthPerLevel: StatBlock
- m_Skills: SkillData[7]  (7개 중 4개 선택)
- m_PortraitSprite: Sprite
- m_VirtuePortraitSprite: Sprite
- m_OnCritSelfEffects: StatusEffectData[]
- m_OnReceiveCritSelfEffects: StatusEffectData[]
```

### SkillData.asset
```csharp
- m_SkillName: string
- m_Description: string
- m_SkillIcon: Sprite
- m_SkillType: SkillType (Melee / Ranged)
- m_RequiredState: SkillRequiredState (None / Awakened)
- m_UsablePositions: bool[4]
- m_TargetPositions: bool[4]
- m_TargetType: TargetType (EnemySingle / EnemyAll / AllySingle / AllyAll / Self)
- m_DamageMultiplier: float
- m_AccuracyMod: int
- m_CritMod: float
- m_OnHitEffects: StatusEffectData[]
```

### EnemyData.asset
```csharp
- m_EnemyName: string
- m_EnemyType: EnemyType (Normal / Elite / Boss)
- m_Element: ElementType
- m_BaseStats: StatBlock
- m_Skills: SkillData[]
- m_CorpseHp: int           # 시체 전환 시 HP
- m_DropTable: DropTable
- m_Sprite: Sprite
```

### StatusEffectData.asset
```csharp
- m_EffectName: string
- m_Icon: Sprite
- m_EffectType: StatusEffectType (Bleed / Poison / Disease / Stun / Buff / Debuff)
- m_Duration: int
- m_TickDamage: int
- m_StatModifier: StatBlock
- m_IsStackable: bool
- m_MaxStack: int
```

## Testing Notes
- **Phase 1 테스트**: CombatScene에서 전투 루프 수동 테스트
- **Phase 2 테스트**: 풀 사이클 플레이스루 (마을→던전→전투→귀환)
- **단위 테스트**: CombatUnitTest.cs로 SO → CombatUnit 인스턴스 생성 및 TakeDamage/Heal/AddEbla 검증 ✅

## Known Issues
- 아트 에셋 미확보 — 프로토타입은 플레이스홀더 사용
- 절차적 던전 생성은 후순위 — 수동 맵으로 시작, IDungeonMapProvider 인터페이스로 추후 교체 예정
- RecalculateStats()는 현재 placeholder — Phase 2 StatusEffectManager 구현 시 ActiveEffects 합산 로직 추가 필요
