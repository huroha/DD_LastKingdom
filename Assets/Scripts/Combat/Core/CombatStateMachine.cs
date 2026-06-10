using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum SurpriseType
{
    None,
    PlayerSurprise,
    EnemySurprise
}


public class CombatStateMachine : MonoBehaviour
{
    [Header("Test Setup")]
    [SerializeField] private NikkeData[] m_TestNikkes;
    [SerializeField] private EnemyData[] m_TestEnemies;

    [Header("Settings")]
    [SerializeField] private float m_EnemyActionDelay = 0.5f;

    [Header("UI References")]
    [SerializeField] private SkillSelectPanel m_SkillSelectPanel;
    [SerializeField] private TargetSelectPanel m_TargetSelectPanel;
    [SerializeField] private CombatDirector m_CombatDirector;
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatHUD m_CombatHUD;
    [SerializeField] private CombatHaloController m_HaloController;

    [SerializeField] private int m_EblaFreeRounds = 4;
    [SerializeField] private int m_EblaRoundMultiplier = 1;

    [Header("Ebla System")]
    [SerializeField] private AfflictionTypeData[] m_AfflictionTypes;
    [SerializeField] private VirtueTypeData[] m_VirtueTypes;
    [Range(0f, 1f)]
    [SerializeField] private float m_VirtueChance = 0.25f;
    [SerializeField] private StatusEffectData m_DeathsDoorDebuff;
    [SerializeField] private StatusEffectData m_DeathsDoorRecovery;

    [Header("Status Effects")]
    [SerializeField] private StatusEffectData m_StunResistBuff;

    [Header("Turn")]
    [SerializeField] private float m_BetweenTurnDelay = 0.3f;
    private WaitForSeconds m_WaitBetweenTurn;


    // 패널티 에블라 수치
    private const int PASS_EBLA_PENALTY = 5;
    private const int ALLY_DEATH_EBLA = 20;

    // 전투 시스템
    private TurnManager m_TurnManager;
    private PositionSystem m_PositionSystem;
    private SkillExecutor m_SkillExecutor;
    private EnemyAI m_EnemyAI;
    private EblaSystem m_EblaSystem;
    private StatusEffectManager m_StatusEffectManager;

    // halo 관련
    private int m_EblaSnapshotCount;
    private WaitForSeconds m_WaitEblaHalo;
    // FSM 상태
    private CombatState m_CurrentState;
    private CombatUnit m_ActiveUnit;
    private SkillData m_SelectedSkill;
    private CombatUnit m_SelectedTarget;
    private bool m_BattleEnded;

    // 커멘트 관련
    private PlayerCommand m_PendingCmd;
    private bool m_HasCmd;

    // 리스트 버퍼들
    private List<CombatUnit> m_UnitBuffer = new List<CombatUnit>();
    private int[] m_EblaSnapshot = new int[4];
    private CombatUnit[] m_EblaSnapshotUnits = new CombatUnit[4];
    [SerializeField] private float m_EblaHaloWaitDuration = 1.3f;
    private List<CombatUnit> m_CorpseBuffer = new List<CombatUnit>();
    private List<CombatUnit> m_ValidTargetBuffer = new List<CombatUnit>();
    private List<CombatUnit> m_MoveTargetBuffer = new List<CombatUnit>();
    private List<CombatUnit> m_IsValidTargetBuffer = new List<CombatUnit>();

    private List<CombatUnit> m_TargetExtractBuffer = new List<CombatUnit>(4);
    private List<EnemyData> m_DefeatedEnemies = new List<EnemyData>();
    private List<CombatUnit> m_EnemyTargetBuffer = new List<CombatUnit>(4);

    // 외부 읽기용 프로퍼티
    public CombatState CurrentState => m_CurrentState;
    public CombatUnit ActiveUnit => m_ActiveUnit;
    public PositionSystem PositionSystem => m_PositionSystem;
    public SkillData SelectedSkill => m_SelectedSkill;

    public IReadOnlyList<CombatUnit> TurnOrder => m_TurnManager?.TurnOrder;
    public int CurrentTurnIndex => m_TurnManager?.CurrentTurnIndex ?? 0;

    // UI 이벤트
    public delegate void StateChangeHandler(CombatState newState);
    public event StateChangeHandler OnStateChanged;

    private void Start()
    {
        Application.targetFrameRate = 60;
        if(ExpeditionManager.Instance.IsActive)
            StartFromExpedition();
        else if (m_TestNikkes != null && m_TestNikkes.Length > 0)
            StartTestBattle();
    }

    private void Awake()
    {
        m_WaitBetweenTurn = new WaitForSeconds(m_BetweenTurnDelay);
        m_WaitEblaHalo = new WaitForSeconds(m_EblaHaloWaitDuration);
    }

    // 이벤트 구독
    private void OnEnable()
    {
        EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
        EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
        EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
    }
    // 이벤트 함수
    private void OnRoundEnded(RoundEndedEvent e)
    {
        TickCorpseTimers();
    }
    private void OnBattleEnded(BattleEndedEvent e)
    {
        if (!ExpeditionManager.Instance.IsActive) return;
        if (e.IsVictory)
        {
            ExpeditionManager.Instance.RecordBattleWon();   // 원정 유지, 전투 승수만 기록
        }
        else
        {
            ExpeditionManager.Instance.SetOutcome(ExpeditionOutcome.Wiped);
            GameManager.Instance.ChangeState(GameState.Settlement);
        }
    }

    public bool ValidateSkill(CombatUnit unit, SkillData skill)
    {
        return m_SkillExecutor.ValidateSkill(unit, skill);
    }

    // 전투 시작
    private void StartTestBattle()
    {
        List<CombatUnit> nikkes = new List<CombatUnit>();

        // 니케 순환하면서 데이터 채우기
        for (int i = 0; i < m_TestNikkes.Length; ++i)
        {
            NikkeData data = m_TestNikkes[i];
            if (data == null)
                continue;
            NikkeInstance instance = new NikkeInstance(data);
            nikkes.Add(new CombatUnit(instance, i, instance.GetEffectiveBaseStats().maxHp, 10, null));
        }

        List<CombatUnit> enemies = BuildEnemyUnits(m_TestEnemies);
        StartBattle(nikkes, enemies);
    }
    private void StartFromExpedition()
    {
        IReadOnlyList<NikkeInstance> party = ExpeditionManager.Instance.Party;
        List<CombatUnit> nikkes = new List<CombatUnit>();
        for (int i=0; i< party.Count; ++i)
        {
            NikkeInstance inst = party[i];
            if (inst == null) continue;
            nikkes.Add(new CombatUnit(inst, i, inst.GetEffectiveBaseStats().maxHp, 0, null));
        }

        List<CombatUnit> enemies = BuildEnemyUnits(ExpeditionManager.Instance.Encounter.Enemies);
        StartBattle(nikkes, enemies);
    }

    public void StartBattle(List<CombatUnit> nikkes, List<CombatUnit> enemies, SurpriseType surprise = SurpriseType.None)
    {
        // 시스템 인스턴스 생성
        m_PositionSystem = new PositionSystem();
        m_TurnManager = new TurnManager();
        m_EblaSystem = new EblaSystem(m_AfflictionTypes, m_VirtueTypes, m_VirtueChance);
        m_SkillExecutor = new SkillExecutor(m_PositionSystem, m_EblaSystem, m_DeathsDoorDebuff, m_DeathsDoorRecovery);
        m_EnemyAI = new EnemyAI(m_PositionSystem, m_SkillExecutor);
        m_StatusEffectManager = new StatusEffectManager(m_StunResistBuff);
        m_DefeatedEnemies.Clear();

        m_PositionSystem.Initialize(nikkes, enemies);
        CombatUnit front = m_PositionSystem.GetUnit(CombatUnitType.Nikke, 0);
        if (front != null)
            m_SkillSelectPanel.Preview(front);

        List<CombatUnit> allUnits = new List<CombatUnit>();
        allUnits.AddRange(nikkes);
        allUnits.AddRange(enemies);

        m_TurnManager.Initialize(allUnits);

        EventBus.Publish(new BattleStartedEvent(nikkes, enemies));

        // TODO Phase 2: surprise 타입에 따라 기습 처리
        // SurpriseType.PlayerSurprise → 적 전체 1라운드 스킵
        // SurpriseType.EnemySurprise → 아군 위치 셔플 + 1라운드 스킵

        StartCoroutine(RunBattle());
    }



    // 코루틴
    private IEnumerator RunBattle()
    {
        SetState(CombatState.BattleStart);
        yield return null;

        while (true)
        {
            SetState(CombatState.TurnStart);
            m_ActiveUnit = m_TurnManager.StartNextTurn();

            if (m_ActiveUnit == null)
                break;
            if (m_CombatHUD != null)
                while (m_CombatHUD.IsTickerAnimating)
                    yield return null;

            if (m_ActiveUnit.Protecting != null)
            {
                m_ActiveUnit.Protecting.DecrementGuardTurns();
                if (m_ActiveUnit.Protecting.GuardTurnsRemaining <= 0)
                {
                    m_ActiveUnit.Protecting.SetGuardedBy(null, 0);
                    m_ActiveUnit.SetProtecting(null);
                }
            }
            m_ActiveUnit.TickSkillCooldowns();
            // Dot 틱
            List<DotTickResult> dotResults = m_StatusEffectManager.ApplyDotDamage(m_ActiveUnit);
            yield return StartCoroutine(ProcessDotResultsRoutine(dotResults));
            m_StatusEffectManager.DecrementDotEffects(m_ActiveUnit);
            if (m_CombatHUD != null && m_ActiveUnit.IsAlive)
                m_CombatHUD.RefreshUnit(m_ActiveUnit);

            EventBus.Publish(new TurnStartedEvent(m_ActiveUnit));
            // Dot 사망으로 사망 시 턴 스킵
            if (!m_ActiveUnit.IsAlive)
            {
                SetState(CombatState.TurnEnd);
                m_StatusEffectManager.ProcessTurnEnd(m_ActiveUnit);
                m_TurnManager.EndCurrentTurn();
                yield return m_WaitBetweenTurn;

                yield return StartCoroutine(CheckBattleEnd());
                if (m_BattleEnded) yield break;
                continue;
            }

            // 스턴 체크
            if (m_ActiveUnit.IsStunned)
            {
                m_StatusEffectManager.RemoveStun(m_ActiveUnit);
                yield return m_CombatDirector.PlayStunRecovery(m_ActiveUnit, m_StunResistBuff);
                SetState(CombatState.TurnEnd);
                m_StatusEffectManager.ProcessTurnEnd(m_ActiveUnit);
                m_CombatHUD.RefreshUnit(m_ActiveUnit);
                m_TurnManager.EndCurrentTurn();
                yield return m_WaitBetweenTurn;
                continue;
            }

            if (m_ActiveUnit.UnitType == CombatUnitType.Nikke)
                yield return StartCoroutine(HandlePlayerTurn());
            else
                yield return StartCoroutine(HandleEnemyTurn());

            m_StatusEffectManager.ProcessTurnEnd(m_ActiveUnit);
            m_CombatHUD.RefreshUnit(m_ActiveUnit);
            SetState(CombatState.TurnEnd);
            m_TurnManager.EndCurrentTurn();
            yield return m_WaitBetweenTurn;

            // 이동 애니메이션 완료 대기
            if (m_FieldView != null)
                while (m_FieldView.IsMoving)
                    yield return null;

            yield return StartCoroutine(CheckBattleEnd());
            if (m_BattleEnded) yield break;

        }
    }
    private IEnumerator CheckBattleEnd()
    {
        SetState(CombatState.CheckBattleEnd);
        m_BattleEnded = false;

        m_PositionSystem.GetAllUnits(CombatUnitType.Enemy, m_UnitBuffer);
        bool anyThreat = false;
        for (int i = 0; i < m_UnitBuffer.Count; ++i)
        {
            EnemyData data = m_UnitBuffer[i].EnemyData;
            if (data == null || !data.IsStructure)   // 구조물이 아닌 적이 하나라도 살아있으면 위협
            {
                anyThreat = true;
                break;
            }
        }
        if (!anyThreat)
        {
            ApplyPostBattleEbla();
            yield return StartCoroutine(FlushPendingResolutions());
            SetState(CombatState.Victory);
            CombatResult combatResult = LootRoller.Roll(m_DefeatedEnemies);
            EventBus.Publish(new BattleEndedEvent(true, combatResult));
            m_BattleEnded = true;
            yield break;
        }

        m_PositionSystem.GetAllUnits(CombatUnitType.Nikke, m_UnitBuffer);
        if (m_UnitBuffer.Count == 0)
        {
            SetState(CombatState.Defeat);
            EventBus.Publish(new BattleEndedEvent(false, null));
            m_BattleEnded = true;
        }
    }
    private IEnumerator HandlePlayerTurn()
    {
        bool turnHandled = false;
        while (!turnHandled)
        {
            SetState(CombatState.PlayerSelectSkill);
            m_SelectedSkill = null;
            m_SkillSelectPanel.Show(m_ActiveUnit, OnSkillSelected, OnSkillPass, OnMoveRequested);
            yield return WaitForCommand();

            switch (m_PendingCmd.Kind)
            {
                case PlayerCommandKind.RequestMove:
                    GetValidMoveTargets(m_MoveTargetBuffer);
                    if (m_MoveTargetBuffer.Count == 0)
                        continue;
                    SetState(CombatState.PlayerSelectMoveTarget);
                    m_SelectedTarget = null;
                    m_TargetSelectPanel.Show(m_MoveTargetBuffer, null, OnMoveTargetSelected, OnMoveCancel);
                    yield return WaitForCommand();

                    if (m_PendingCmd.Kind == PlayerCommandKind.SelectTarget)
                        turnHandled = true;
                    // Cancel 이면 break -> while 처음으로 스킬 재선택
                    break;

                case PlayerCommandKind.Pass:
                    yield return m_CombatHUD.ShowPassLabel(m_ActiveUnit);
                    SnapshotNikkeEbla();
                    if (m_EblaSystem.ModifyEbla(m_ActiveUnit, PASS_EBLA_PENALTY))
                    {
                        m_PositionSystem.RemoveUnit(m_ActiveUnit);
                        EventBus.Publish(new UnitDiedEvent(m_ActiveUnit));
                    }
                    yield return StartCoroutine(FlushEblaHalos());
                    yield return StartCoroutine(FlushPendingResolutions());
                    turnHandled = true;
                    break;

                case PlayerCommandKind.SelectSkill:
                    m_SelectedSkill = m_PendingCmd.Skill;
                    SetState(CombatState.PlayerSelectTarget);
                    m_SelectedTarget = null;
                    m_PositionSystem.GetValidTargets(m_ActiveUnit, m_SelectedSkill, m_ValidTargetBuffer);
                    if ((m_SelectedSkill.IsGuard || m_SelectedSkill.IsForceGuard) && m_SelectedSkill.TargetType == TargetType.AllySingle)
                        m_ValidTargetBuffer.Remove(m_ActiveUnit);
                    m_SkillSelectPanel.Hide();
                    m_TargetSelectPanel.Show(m_ValidTargetBuffer, m_SelectedSkill, OnTargetSelected, OnTargetCancel);
                    yield return WaitForCommand();

                    if (m_PendingCmd.Kind == PlayerCommandKind.SelectTarget)
                    {
                        m_SelectedTarget = m_PendingCmd.Target;
                        turnHandled = true;
                    }
                    // Cancel 나면 break-> while 처음으로 스킬 재선택
                    break;
            }
        }

        // 패스가 아닐 경우에 실행
        if (m_SelectedSkill != null)
        {
            SetState(CombatState.ExecuteSkill);
            SnapshotNikkeEbla();
            int skillLevel = m_ActiveUnit.NikkeInstance?.GetSkillLevel(m_SelectedSkill) ?? 1;
            SkillResult result = m_SkillExecutor.Execute(m_ActiveUnit, m_SelectedSkill, skillLevel, m_SelectedTarget);
            if (m_CombatHUD != null)
                m_CombatHUD.SnapNikkeHpBarsToSlots();
            List<CombatUnit> targets = ExtractTargets(result);
            yield return m_CombatDirector.PlaySkillSequence(m_ActiveUnit, m_SelectedSkill, targets, result);
            yield return StartCoroutine(FlushEblaHalos());
            yield return StartCoroutine(FlushPendingResolutions());
            EventBus.Publish(new SkillExecutedEvent(result));
            ProcessDeadUnits(result);
        }
    }
    private IEnumerator HandleEnemyTurn()
    {
        SetState(CombatState.EnemyDecide);
        yield return new WaitForSeconds(m_EnemyActionDelay);

        EnemyAction action = m_EnemyAI.DecideAction(m_ActiveUnit);

        if (!action.IsPass)
        {
            if (m_CombatHUD != null)
            {
                m_SkillExecutor.ResolveTargets(m_ActiveUnit, action.Skill, action.Target, m_EnemyTargetBuffer);
                m_CombatHUD.ShowEnemyTargetHighlights(m_EnemyTargetBuffer);
                yield return m_CombatHUD.PlayEnemySkillAnnounce(action.Skill.SkillName);
            }

            SetState(CombatState.ExecuteSkill);
            SnapshotNikkeEbla();
            SkillResult result = m_SkillExecutor.ExecuteEnemy(m_ActiveUnit, action.Skill, action.Target);
            if (m_CombatHUD != null)
                m_CombatHUD.SnapNikkeHpBarsToSlots();
            List<CombatUnit> targets = ExtractTargets(result);
            yield return m_CombatDirector.PlaySkillSequence(m_ActiveUnit, action.Skill, targets, result);
            yield return StartCoroutine(FlushEblaHalos());
            yield return StartCoroutine(FlushPendingResolutions());
            EventBus.Publish(new SkillExecutedEvent(result));
            ProcessDeadUnits(result);
        }
        else
        {
            if (m_CombatHUD != null)
                m_CombatHUD.ShowPassLabel(m_ActiveUnit);
        }

        if (m_CombatHUD != null)
            m_CombatHUD.HideEnemyTargetHighlights();

    }

    // 헬퍼

    private void TickCorpseTimers()
    {
        m_PositionSystem.GetCorpses(CombatUnitType.Enemy, m_CorpseBuffer);
        for (int i = 0; i < m_CorpseBuffer.Count; ++i)
        {
            m_CorpseBuffer[i].TickCorpseTimer();
            if (m_CorpseBuffer[i].CorpseTimer <= 0)
            {
                m_CorpseBuffer[i].Kill();
                m_PositionSystem.RemoveUnit(m_CorpseBuffer[i]);
                EventBus.Publish(new UnitDiedEvent(m_CorpseBuffer[i]));
            }
        }
    }

    public AttackPreview PreviewAttack(CombatUnit target)
    {
        int skillLevel = m_ActiveUnit.NikkeInstance?.GetSkillLevel(m_SelectedSkill) ?? 1;
        return m_SkillExecutor.PreviewAttack(m_ActiveUnit, m_SelectedSkill, skillLevel, target);
    }

    // 죽은 유닛 이벤트 발생
    private void ProcessDeadUnits(SkillResult result)
    {
        if (result.TargetResults == null)
            return;
        for (int i = 0; i < result.TargetResults.Length; ++i)
        {
            CombatUnit target = result.TargetResults[i].Target;
            if (target != null)
            {
                if (target.State == UnitState.Dead)
                {
                    m_PositionSystem.RemoveUnit(target);
                    EventBus.Publish(new UnitDiedEvent(target));
                    UnitState prev = result.TargetResults[i].PreviousState;
                    if (target.UnitType == CombatUnitType.Nikke)
                        ApplyAllyDeathEbla();
                    else
                        TryRecordDefeat(target, prev);
                }
                else if (target.State == UnitState.Corpse && result.TargetResults[i].PreviousState == UnitState.Alive)
                {
                    TryRecordDefeat(target, UnitState.Alive);
                    EventBus.Publish(new UnitDiedEvent(target));
                }
            }
        }
    }
    private void ApplyAllyDeathEbla()
    {
        ApplyEblaToAllNikkes(ALLY_DEATH_EBLA);
    }
    private void ApplyEblaToAllNikkes(int amount)
    {
        m_PositionSystem.GetAllUnits(CombatUnitType.Nikke, m_UnitBuffer);
        for (int i = m_UnitBuffer.Count - 1; i >= 0; --i)                            //RemoveUnit 호출 시 리스트가 변경되므로 뒤에서부터 순회해야 안전
        {
            if (m_EblaSystem.ModifyEbla(m_UnitBuffer[i], amount))
            {
                m_PositionSystem.RemoveUnit(m_UnitBuffer[i]);
                EventBus.Publish(new UnitDiedEvent(m_UnitBuffer[i]));
            }
        }
    }
    // 전투 후반부 에블라 패널티
    private void ApplyPostBattleEbla()
    {
        int roundCheck = m_TurnManager.RoundNumber;
        if (m_EblaFreeRounds > roundCheck)
            return;
        int total = 0;
        total = (roundCheck - m_EblaFreeRounds) * m_EblaRoundMultiplier * 5;
        ApplyEblaToAllNikkes(total);
    }

    // State 설정
    private void SetState(CombatState newState)
    {
        m_CurrentState = newState;
        if (OnStateChanged != null)
            OnStateChanged(newState);
        // OnstateChanged?.Invoke(newState)와 동일함.
        //Debug.Log($"[FSM] {newState}");

    }
    private void Submit(PlayerCommandKind kind, SkillData skill = null, CombatUnit target = null)
    {
        m_PendingCmd = new PlayerCommand {  Kind = kind, Skill = skill, Target = target };
        m_HasCmd = true;
    }
    private IEnumerator WaitForCommand()
    {
        while (!m_HasCmd)
            yield return null;
        m_HasCmd = false;
    }
    private void OnSkillSelected(SkillData skill)
    {
        Submit(PlayerCommandKind.SelectSkill, skill: skill);
    }
    private void OnSkillPass()
    {
        Submit(PlayerCommandKind.Pass);
    }
    private void OnTargetSelected(CombatUnit target)
    {
        Submit(PlayerCommandKind.SelectTarget, target: target);
    }
    private void OnTargetCancel()
    {
        Submit(PlayerCommandKind.Cancel);
    }
    private void OnMoveCancel()
    {
        Submit(PlayerCommandKind.Cancel);
    }
    private void OnMoveRequested()
    {
        Submit(PlayerCommandKind.RequestMove);
    }

    private void OnMoveTargetSelected(CombatUnit target)
    {
        int steps = target.SlotIndex - m_ActiveUnit.SlotIndex;
        m_PositionSystem.Move(m_ActiveUnit, steps);
        EventBus.Publish(new UnitMovedEvent(m_ActiveUnit, target));
        Submit(PlayerCommandKind.SelectTarget, target: target);
    }

    private void GetValidMoveTargets(List<CombatUnit> result)
    {
        result.Clear();
        int currentSlot = m_ActiveUnit.SlotIndex;
        int range = m_ActiveUnit.CurrentStats.moveRange;

        for (int i = 1; i <= range; ++i)
        {
            CombatUnit forward = m_PositionSystem.GetUnit(CombatUnitType.Nikke, currentSlot - i);
            CombatUnit backward = m_PositionSystem.GetUnit(CombatUnitType.Nikke, currentSlot + i);

            if (forward != null)
                result.Add(forward);
            if (backward != null)
                result.Add(backward);
        }
    }
    public bool IsValidTarget(CombatUnit target)
    {
        if (m_ActiveUnit == null || m_SelectedSkill == null)
            return false;
        m_PositionSystem.GetValidTargets(m_ActiveUnit, m_SelectedSkill, m_IsValidTargetBuffer);
        return m_IsValidTargetBuffer.Contains(target);
    }
    private IEnumerator ProcessDotResultsRoutine(List<DotTickResult> results)
    {
        for (int i = 0; i < results.Count; ++i)
        {
            DotTickResult tr = results[i];

            if (tr.PreviousState == UnitState.Alive && tr.ResultState == UnitState.DeathsDoor)
            {
                m_EblaSystem.ModifyEbla(tr.Unit, CombatUnit.DEATHS_DOOR_EBLA);
                tr.Unit.AddEffect(new ActiveStatusEffect(m_DeathsDoorDebuff));
                tr.Unit.RecalculateStats();
            }
            if (tr.ResultState == UnitState.Dead)
            {
                TryRecordDefeat(tr.Unit, tr.PreviousState);
                m_PositionSystem.RemoveUnit(tr.Unit);
                if (tr.Unit.UnitType == CombatUnitType.Nikke)
                    ApplyAllyDeathEbla();
            }

            yield return m_CombatDirector.PlayDotTick(tr.Unit, tr.Damage, tr.Effect.EffectType, tr.PreviousState, tr.ResultState);

            // DeathVFX 완료 후 view 정리
            if (tr.ResultState == UnitState.Dead)
                EventBus.Publish(new UnitDiedEvent(tr.Unit));
        }
        yield return StartCoroutine(FlushPendingResolutions());
    }

    private List<CombatUnit> ExtractTargets(SkillResult result)
    {
        m_TargetExtractBuffer.Clear();
        if (result.TargetResults == null)
            return m_TargetExtractBuffer;
        for(int i=0; i<result.TargetResults.Length; ++i)
            m_TargetExtractBuffer.Add(result.TargetResults[i].Target);
        return m_TargetExtractBuffer;
    }

    private void SnapshotNikkeEbla()
    {
        m_PositionSystem.GetAllUnits(CombatUnitType.Nikke, m_UnitBuffer);
        for (int i = 0; i < m_UnitBuffer.Count; ++i)
        {
            m_EblaSnapshotUnits[i] = m_UnitBuffer[i];
            m_EblaSnapshot[i] = m_UnitBuffer[i].Ebla;
        }
        m_EblaSnapshotCount = m_UnitBuffer.Count;
    }

    private IEnumerator FlushEblaHalos()
    {
        bool anyChanged = false;
        int count = Mathf.Min(m_EblaSnapshot.Length, m_EblaSnapshotCount);
        for (int i = 0; i < count; ++i)
        {
            CombatUnit unit = m_EblaSnapshotUnits[i];
            if (!unit.IsAlive) continue;
            int delta = unit.Ebla - m_EblaSnapshot[i];
            if (delta == 0) continue;
            anyChanged = true;
            m_HaloController.PopupEblaHalo(unit, delta);
        }
        if (anyChanged)
            yield return m_WaitEblaHalo;
    }

    private IEnumerator FlushPendingResolutions()
    {
        if (m_EblaSystem.PendingCount == 0)
            yield break;
        IReadOnlyList<PendingEblaResolution> pendings = m_EblaSystem.DrainPending();
        for (int i=0; i <pendings.Count; ++i)
        {
            PendingEblaResolution p = pendings[i];
            if (!p.Unit.IsAlive)
                continue;
            if (m_CombatHUD != null)
                yield return m_CombatHUD.PlayNarration(p);

            m_EblaSystem.ApplyResolutionEffect(p);

            if (m_CombatHUD != null)
                m_CombatHUD.RefreshUnit(p.Unit);
        }
    }
    private void TryRecordDefeat(CombatUnit unit, UnitState previousState)
    {
        if (unit.UnitType != CombatUnitType.Enemy) return;
        if (previousState == UnitState.Corpse) return;
        m_DefeatedEnemies.Add(unit.EnemyData);
    }

    private List<CombatUnit> BuildEnemyUnits(IReadOnlyList<EnemyData> enemies)
    {
        List<CombatUnit> units = new List<CombatUnit>();
        int slotIndex = 0;
        for (int i = 0; i < enemies.Count; ++i)
        {
            EnemyData data = enemies[i];
            if (data == null) continue;
            CombatUnit unit = new CombatUnit(data, slotIndex);
            units.Add(unit);
            slotIndex += unit.SlotSize;
        }
        return units;
    }
    
}
