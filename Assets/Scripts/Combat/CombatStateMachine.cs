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

    [SerializeField] private int m_EblaFreeRounds = 4;
    [SerializeField] private int m_EblaRoundMultiplier = 1;

    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatHUD m_CombatHUD;

    [Header("Ebla System")]
    [SerializeField] private StatusEffectData m_AfflictionDebuff;
    [SerializeField] private StatusEffectData m_DeathsDoorDebuff;
    [SerializeField] private StatusEffectData m_DeathsDoorRecovery;

    [Header("Status Effects")]
    [SerializeField] private StatusEffectData m_StunResistBuff;


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

    // FSM 상태
    private CombatState m_CurrentState;
    private CombatUnit m_ActiveUnit;
    private SkillData m_SelectedSkill;
    private CombatUnit m_SelectedTarget;

    // 플레이어 입력 플래그
    private bool m_SkillConfirmed;
    private bool m_TargetConfirmed;
    private bool m_MoveRequested;
    private bool m_MoveConfirmed;

    // 리스트 버퍼들
    private List<CombatUnit> m_UnitBuffer = new List<CombatUnit>();
    private List<CombatUnit> m_CorpseBuffer = new List<CombatUnit>();
    private List<CombatUnit> m_ValidTargetBuffer = new List<CombatUnit>();
    private List<CombatUnit> m_MoveTargetBuffer = new List<CombatUnit>();
    private List<CombatUnit> m_IsValidTargetBuffer = new List<CombatUnit>();


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
        if (null != m_TestNikkes && m_TestNikkes.Length > 0)
        {
            StartTestBattle();
        }
    }

    // 이벤트 구독
    private void OnEnable()
    {
        EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
    }
    // 이벤트 함수
    private void OnRoundEnded(RoundEndedEvent e)
    {
        TickCorpseTimers();
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
                continue;//data.BaseStats.maxHp
            nikkes.Add(new CombatUnit(data, i, 10, 90, null));
        }

        // 적 순환하면서 데이터 채우기
        List<CombatUnit> enemies = new List<CombatUnit>();
        int slotIndex = 0;
        for (int i = 0; i < m_TestEnemies.Length; ++i)
        {
            EnemyData data = m_TestEnemies[i];
            if (data == null)
                continue;
            CombatUnit unit = new CombatUnit(data, slotIndex);
            enemies.Add(unit);
            slotIndex += unit.SlotSize;
        }

        // 채운 데이터를 넘겨준다.
        StartBattle(nikkes, enemies);
    }
    public void StartBattle(List<CombatUnit> nikkes, List<CombatUnit> enemies, SurpriseType surprise = SurpriseType.None)
    {
        // 시스템 인스턴스 생성
        m_PositionSystem = new PositionSystem();
        m_TurnManager = new TurnManager();
        m_EblaSystem = new EblaSystem(m_AfflictionDebuff);
        m_SkillExecutor = new SkillExecutor(m_PositionSystem, m_EblaSystem, m_DeathsDoorDebuff, m_DeathsDoorRecovery);
        m_EnemyAI = new EnemyAI(m_PositionSystem, m_SkillExecutor);
        m_StatusEffectManager = new StatusEffectManager(m_StunResistBuff);

        m_PositionSystem.Initialize(nikkes, enemies);

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

            // Dot 틱
            List<DotTickResult> dotResults = m_StatusEffectManager.ProcessTurnStart(m_ActiveUnit);
            ProcessDotResults(dotResults);

            EventBus.Publish(new TurnStartedEvent(m_ActiveUnit));
            // Dot 사망으로 사망 시 턴 스킵
            if (!m_ActiveUnit.IsAlive)
            {
                SetState(CombatState.TurnEnd);
                m_StatusEffectManager.ProcessTurnEnd(m_ActiveUnit);
                m_TurnManager.EndCurrentTurn();
                yield return null;
                continue;
            }

            // 스턴 체크
            if (m_ActiveUnit.IsStunned)
            {
                m_StatusEffectManager.RemoveStun(m_ActiveUnit);
                SetState(CombatState.TurnEnd);
                m_StatusEffectManager.ProcessTurnEnd(m_ActiveUnit);
                m_TurnManager.EndCurrentTurn();
                yield return null;
                continue;
            }

            //Debug.Log($"[Turn] Round {m_TurnManager.RoundNumber} — {m_ActiveUnit.UnitName} ({m_ActiveUnit.UnitType}, Slot{ m_ActiveUnit.SlotIndex}) HP: { m_ActiveUnit.CurrentHp}/{ m_ActiveUnit.MaxHp}");

            if (m_ActiveUnit.UnitType == CombatUnitType.Nikke)
                yield return StartCoroutine(HandlePlayerTurn());
            else
                yield return StartCoroutine(HandleEnemyTurn());

            m_StatusEffectManager.ProcessTurnEnd(m_ActiveUnit);
            SetState(CombatState.TurnEnd);
            m_TurnManager.EndCurrentTurn();
            yield return null;

            // 이동 애니메이션 완료 대기
            if (m_FieldView != null)
                while (m_FieldView.IsMoving)
                    yield return null;

            SetState(CombatState.CheckBattleEnd);
            m_PositionSystem.GetAllUnits(CombatUnitType.Enemy, m_UnitBuffer);
            if (m_UnitBuffer.Count == 0)
            {
                ApplyPostBattleEbla();
                SetState(CombatState.Victory);
                EventBus.Publish(new BattleEndedEvent(true));
                yield break;
            }
            m_PositionSystem.GetAllUnits(CombatUnitType.Nikke, m_UnitBuffer);
            if (m_UnitBuffer.Count == 0)
            {
                SetState(CombatState.Defeat);
                EventBus.Publish(new BattleEndedEvent(false));
                yield break;
            }

        }
    }


    private IEnumerator HandlePlayerTurn()
    {
        bool turnHandled = false;
        m_MoveRequested = false;  // 전에 사용됬던거 사용 방지
        m_MoveConfirmed = false;
        while (!turnHandled)
        {
            SetState(CombatState.PlayerSelectSkill);
            m_SkillConfirmed = false;
            m_SelectedSkill = null;
            m_SkillSelectPanel.Show(m_ActiveUnit, OnSkillSelected, OnSkillPass, OnMoveRequested);

            while (!m_SkillConfirmed)
                yield return null;

            // Move 요청된 경우
            if (m_MoveRequested)
            {
                m_MoveRequested = false;
                GetValidMoveTargets(m_MoveTargetBuffer);
                if (m_MoveTargetBuffer.Count == 0)
                    continue; // 이동 가능 슬롯 없으면 다시 스킬 선택

                SetState(CombatState.PlayerSelectMoveTarget);
                m_MoveConfirmed = false;
                m_SelectedTarget = null;
                m_TargetSelectPanel.Show(m_MoveTargetBuffer, m_SelectedSkill, OnMoveTargetSelected, OnMoveCancel);

                while (!m_MoveConfirmed)
                    yield return null;

                if (m_SelectedTarget != null)
                    turnHandled = true;
                continue;
            }


            // 패스 선택 시
            if (m_SelectedSkill == null)
            {
                if (m_EblaSystem.ModifyEbla(m_ActiveUnit, PASS_EBLA_PENALTY))
                {
                    m_PositionSystem.RemoveUnit(m_ActiveUnit);
                    EventBus.Publish(new UnitDiedEvent(m_ActiveUnit));
                }
                turnHandled = true;
                continue;
            }



            // 타겟 선택
            SetState(CombatState.PlayerSelectTarget);
            m_TargetConfirmed = false;
            m_SelectedTarget = null;
            m_PositionSystem.GetValidTargets(m_ActiveUnit, m_SelectedSkill, m_ValidTargetBuffer);
            m_TargetSelectPanel.Show(m_ValidTargetBuffer, m_SelectedSkill, OnTargetSelected, OnTargetCancel);

            while (!m_TargetConfirmed)
                yield return null;

            if (m_SelectedTarget != null)
                turnHandled = true;
            // 취소면 루프 처음으로 돌아가 스킬 재선택
        }

        // 패스가 아닐 때만 실행
        if (m_SelectedSkill != null)
        {
            SetState(CombatState.ExecuteSkill);
            SkillResult result = m_SkillExecutor.Execute(m_ActiveUnit, m_SelectedSkill, m_SelectedTarget);
            EventBus.Publish(new SkillExecutedEvent(result));
            ProcessDeadUnits(result);
            yield return null;
        }
    }
    private IEnumerator HandleEnemyTurn()
    {
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
                m_CombatHUD.ShowEnemySkillName("턴 넘김");
        }

        yield return new WaitForSeconds(m_EnemyActionDelay);
        if (m_CombatHUD != null)
        {
            m_CombatHUD.HideEnemyTargetHighlights();
            m_CombatHUD.HideEnemySkillName();
        }
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
        return m_SkillExecutor.PreviewAttack(m_ActiveUnit, m_SelectedSkill, target);
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
                    if (target.UnitType == CombatUnitType.Nikke)
                        ApplyAllyDeathEbla();
                }
                else if (target.State == UnitState.Corpse && result.TargetResults[i].PreviousState == UnitState.Alive)
                    EventBus.Publish(new UnitDiedEvent(target));
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
            if (m_EblaSystem.ModifyEbla(m_UnitBuffer[i], ALLY_DEATH_EBLA))
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
        for (int i = m_EblaFreeRounds + 1; i <= roundCheck; ++i)
        {
            total += i * m_EblaRoundMultiplier;
        }
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
    private void OnSkillSelected(SkillData skill)
    {
        m_SelectedSkill = skill;
        m_SkillConfirmed = true;
    }
    private void OnSkillPass()
    {
        m_SelectedSkill = null;
        m_SkillConfirmed = true;
    }
    private void OnTargetSelected(CombatUnit target)
    {
        m_SelectedTarget = target;
        m_TargetConfirmed = true;
    }
    private void OnTargetCancel()
    {
        m_SelectedTarget = null;
        m_TargetConfirmed = true;
    }

    private void OnMoveCancel()
    {
        m_SelectedTarget = null;
        m_MoveConfirmed = true;
    }
    private void OnMoveRequested()
    {
        m_MoveRequested = true;
        m_SkillConfirmed = true; // while(!m_SkillConfirmed) 루프 탈출용
    }

    private void OnMoveTargetSelected(CombatUnit target)
    {
        int steps = target.SlotIndex - m_ActiveUnit.SlotIndex;
        m_PositionSystem.Move(m_ActiveUnit, steps);
        EventBus.Publish(new UnitMovedEvent(m_ActiveUnit, target));
        m_SelectedTarget = target;
        m_MoveConfirmed = true;
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
    private void ProcessDotResults(List<DotTickResult> results)
    {
        for (int i = 0; i < results.Count; ++i)
        {
            CombatUnit unit = results[i].Unit;

            if (results[i].PreviousState == UnitState.Alive
                          && results[i].ResultState == UnitState.DeathsDoor)
            {
                m_EblaSystem.ModifyEbla(unit, CombatUnit.DEATHS_DOOR_EBLA);
                unit.AddEffect(new ActiveStatusEffect(m_DeathsDoorDebuff));
                unit.RecalculateStats();
            }
            if (results[i].ResultState == UnitState.Dead)
            {
                m_PositionSystem.RemoveUnit(m_ActiveUnit);
                EventBus.Publish(new UnitDiedEvent(m_ActiveUnit));
                if (m_ActiveUnit.UnitType == CombatUnitType.Nikke)
                    ApplyAllyDeathEbla();
            }
        }
    }


}
