# CLAUDE.md - Last Kingdom Project Guide

## Project Overview
- **Game**: Last Kingdom — 니케 세계관 기반 Darkest Dungeon 모작
- **Genre**: 2D 턴제 RPG (마을 관리 + 던전 탐험 + 전투)
- **Engine**: Unity 6 (6000.x)
- **Language**: C#
- **Platform**: PC (Windows) 전용
- **Developer**: 솔로 개발

## Tech Stack
- **Render Pipeline**: URP 2D
- **UI**: UGUI (Canvas 기반)
- **Input**: New Input System (com.unity.inputsystem)
- **Serialization**: Newtonsoft JSON (com.unity.nuget.newtonsoft-json)
- **Animation**: Sprite Animation (Animator + Animation Clip, FlipBook 스타일)

## Architecture

### Design Patterns
- **Singleton**: 매니저 클래스 (GameManager, AudioManager 등) — `Singleton<T>` 베이스 클래스 사용
- **Observer**: C# event/Action 기반 이벤트 시스템. UnityEvent는 Inspector 바인딩 필요 시만
- **State Machine**: 전투 FSM, 게임 플로우 FSM
- **Strategy**: 스킬 효과 실행
- **Interface Abstraction**: IDungeonMapProvider 등 교체 가능한 시스템

### Data Architecture
- **게임 데이터**: ScriptableObject (영웅, 스킬, 적, 아이템, 건물 등)
- **런타임 상태**: Pure C# 클래스 (SO에서 복사하여 인스턴스화)
- **세이브 데이터**: JSON 직렬화 (Application.persistentDataPath)
- **SO는 읽기 전용 템플릿**, 런타임 수정은 별도 인스턴스에서

### Scene Structure
```
BootScene → TitleScene → TownScene ↔ DungeonScene ↔ CombatScene
```
- BootScene: 매니저 초기화, DontDestroyOnLoad
- 씬 전환: SceneFlowManager (Async Loading + 페이드)

### Core Game Loop
마을(왕국) → 보급품 구매 → 던전 탐험 (복도 이동/이벤트/전투) → 귀환 → 보상 정산 → 마을 업그레이드

## Folder Structure
```
Assets/
├── Scripts/
│   ├── Managers/       # 싱글톤 매니저 (GameManager, SceneFlowManager, AudioManager, DataManager)
│   ├── Data/           # ScriptableObject 정의 클래스 (.cs)
│   ├── Combat/         # 전투 시스템 (TurnManager, PositionSystem, SkillExecutor, CombatUnit, FSM)
│   ├── Systems/        # 공유 시스템 (EblaSystem, DetectionSystem, InventorySystem, SaveSystem)
│   ├── Dungeon/        # 던전 시스템 (IDungeonMapProvider, DungeonNavigator, CampSystem)
│   ├── Town/           # 마을 시스템 (BuildingManager, RosterManager, ShopManager)
│   ├── UI/             # UI 스크립트 (CombatHUD, TownUI, DungeonUI, InventoryUI)
│   ├── VFX/            # 연출 (CombatFeedback, EblaVFX)
│   └── Utils/          # 유틸리티 (Singleton<T>, ObjectPool, EventBus)
├── ScriptableObjects/  # SO 에셋 파일 (.asset)
├── Prefabs/            # 프리팹
├── Scenes/             # 씬 파일
├── Art/                # 2D 스프라이트, 배경
├── Audio/              # BGM, SFX
├── Animations/         # Animator Controller, Animation Clip
└── Resources/          # 런타임 로드 에셋
```

## Coding Conventions

### Naming
- **클래스/구조체**: PascalCase (`TurnManager`, `HeroData`)
- **public 메서드/프로퍼티**: PascalCase (`GetCurrentUnit()`, `MaxHealth`)
- **private 필드**: _camelCase (`_currentTurn`, `_heroList`)
- **SerializeField private**: _camelCase with `[SerializeField]`
- **상수/enum**: PascalCase (`MaxPartySize`, `HeroClass.Attacker`)
- **이벤트**: On + PastTense (`OnTurnEnded`, `OnHeroDied`)
- **인터페이스**: I + PascalCase (`IDungeonMapProvider`, `IDamageable`)
- **SO 에셋 파일명**: PascalCase (`Kilo_HeroData.asset`, `Slash_SkillData.asset`)

### Code Style
```csharp
// ScriptableObject 예시
[CreateAssetMenu(fileName = "New Hero", menuName = "LastKingdom/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string _heroName;
    [SerializeField] private HeroClass _heroClass;
    
    [Header("Stats")]
    [SerializeField] private StatBlock _baseStats;
    
    [Header("Skills")]
    [SerializeField] private SkillData[] _skills = new SkillData[4];
    
    // Public read-only properties
    public string HeroName => _heroName;
    public HeroClass HeroClass => _heroClass;
    public StatBlock BaseStats => _baseStats;
    public IReadOnlyList<SkillData> Skills => _skills;
}
```

### Rules
1. **MonoBehaviour는 씬 로직 전용**, 순수 데이터/로직은 Pure C# 클래스
2. **SO는 데이터 컨테이너**, 런타임 로직 최소화
3. **매 프레임 GC 할당 금지** — string 연결, LINQ, boxing 주의. 캐싱 필수
4. **public 필드 사용 금지** — `[SerializeField] private` + public property
5. **하드코딩 금지** — 매직 넘버는 SO 또는 const로 정의
6. **null 체크** — GetComponent, Find 계열 사용 시 반드시 null 체크
7. **코루틴 vs async**: 간단한 딜레이는 코루틴, 복잡한 비동기는 async/await

### Key Systems Reference

#### Ebla System (에블라)
- 범위: 0~200
- 100 도달: Affliction (기본) 또는 Virtue (canVirtue 영웅만)
- Affliction: 스탯 디버프, 부정적 행동 트리거
- Virtue: 일러스트 변경 + 특수 스킬 전용 + 버프
- 200 도달: **영구 사망** (세이브에서 완전 제거)

#### Detection System (위험발각)
- 범위: 0~100, 25 단위 4단계
- 단계 증가: 전투 턴 초과 시
- 단계 감소: 루팅, 보급품 사용
- 100 도달: 강제 던전 퇴각

#### Dungeon System
- IDungeonMapProvider 인터페이스로 추상화
- 현재: ManualDungeonMapProvider (SO 기반 수동 맵)
- 추후: ProceduralDungeonMapProvider (절차적 생성)

## Performance Targets
- 60 FPS (PC)
- 전투 루프 내 GC 0B/frame
- Draw Calls: 전투 50 이하, 던전 100 이하
- 씬 로딩: 2초 이내

## Build
- Platform: Windows (x64)
- Build Path: Builds/Windows/

## Task Management
- 계획 문서: `dev/active/[task-name]/` 디렉토리
- `[task-name]-plan.md` — 전략 계획
- `[task-name]-context.md` — 컨텍스트, 결정 사항
- `[task-name]-tasks.md` — 체크리스트