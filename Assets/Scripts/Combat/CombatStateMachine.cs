using UnityEngine;
using System.Collections;
using System.Collections.Generic;



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



    // 전투 시스템
    private TurnManager     m_TurnManager;
    private PositionSystem  m_PositionSystem;
    private SkillExecutor   m_SkillExecutor;
    private EnemyAI         m_EnemyAI;

    // FSM 상태
    private CombatState     m_CurrentState;
    private CombatUnit      m_ActiveUnit;
    private SkillData       m_SelectedSkill;
    private CombatUnit      m_SelectedTarget;

    // 플레이어 입력 플래그
    private bool m_SkillConfirmed;
    private bool m_TargetConfirmed;

    // 외부 읽기용 프로퍼티
    public CombatState CurrentState => m_CurrentState;
    public CombatUnit ActiveUnit => m_ActiveUnit;
    public PositionSystem PositionSystem => m_PositionSystem;

    public IReadOnlyList<CombatUnit> TurnOrder => m_TurnManager?.TurnOrder;

    // UI 이벤트
    public delegate void StateChangeHandler(CombatState newState);
    public event StateChangeHandler OnStateChanged;

    private void Start()
    {
        if(null != m_TestNikkes && m_TestNikkes.Length > 0)
        {
            StartTestBattle();
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
        for(int i=0; i<m_TestNikkes.Length; ++i)
        {
            NikkeData data = m_TestNikkes[i];
            if (data == null)
                continue;
            nikkes.Add(new CombatUnit(data, i, data.BaseStats.maxHp, 0, null));
        }

        // 적 순환하면서 데이터 채우기
        List<CombatUnit> enemies = new List<CombatUnit>();
        for (int i=0; i<m_TestEnemies.Length; ++i)
        {
            EnemyData data = m_TestEnemies[i];
            if (data == null)
                continue;
            enemies.Add(new CombatUnit(data, i));
        }

        // 채운 데이터를 넘겨준다.
        StartBattle(nikkes, enemies);
    }
    public void StartBattle(List<CombatUnit> nikkes, List<CombatUnit> enemies)
    {
        // 시스템 인스턴스 생성
        m_PositionSystem = new PositionSystem();
        m_TurnManager = new TurnManager();
        m_SkillExecutor = new SkillExecutor(m_PositionSystem);
        m_EnemyAI = new EnemyAI(m_PositionSystem, m_SkillExecutor);

        m_PositionSystem.Initialize(nikkes, enemies);

        List<CombatUnit> allUnits = new List<CombatUnit>();
        allUnits.AddRange(nikkes);
        allUnits.AddRange(enemies);

        m_TurnManager.Initialize(allUnits);

        EventBus.Publish(new BattleStartedEvent(nikkes, enemies));

        StartCoroutine(RunBattle());
    }



    // 코루틴
    private IEnumerator RunBattle()
    {
        SetState(CombatState.BattleStart);
            yield return null;

        while(true)
        {
            SetState(CombatState.TurnStart);
            m_ActiveUnit = m_TurnManager.StartNextTurn();
            if (m_ActiveUnit == null)
                break;
            if (m_ActiveUnit.UnitType == CombatUnitType.Nikke)
                yield return StartCoroutine(HandlePlayerTurn());
            else
                yield return StartCoroutine(HandleEnemyTurn());

            SetState(CombatState.TurnEnd);
            m_TurnManager.EndCurrentTurn();
            yield return null;

            SetState(CombatState.CheckBattleEnd);
            if (m_PositionSystem.GetAllUnits(CombatUnitType.Enemy).Count == 0)
            {
                ApplyPostBattleEbla();
                SetState(CombatState.Victory);
                EventBus.Publish(new BattleEndedEvent(true));
                yield break;
            }
            if (m_PositionSystem.GetAllUnits(CombatUnitType.Nikke).Count == 0)
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

        while (!turnHandled)
        {
            SetState(CombatState.PlayerSelectSkill);
            m_SkillConfirmed = false;
            m_SelectedSkill = null;
            m_SkillSelectPanel.Show(m_ActiveUnit, OnSkillSelected, OnSkillPass);

            while (!m_SkillConfirmed)
                yield return null;

            // 패스 선택 시
            if (m_SelectedSkill == null)
            {
                turnHandled = true;
                continue;
            }

            // 타겟 선택 불필요한 스킬
            if (!NeedsTargetSelection(m_SelectedSkill))
            {
                turnHandled = true;
                continue;
            }

            // 타겟 선택
            SetState(CombatState.PlayerSelectTarget);
            m_TargetConfirmed = false;
            m_SelectedTarget = null;
            List<CombatUnit> validTargets = m_PositionSystem.GetValidTargets(m_ActiveUnit, m_SelectedSkill);
            m_TargetSelectPanel.Show(validTargets, OnTargetSelected, OnTargetCancel);

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
        if(!action.IsPass)
        {
            SkillResult result = m_SkillExecutor.Execute(m_ActiveUnit, action.Skill, action.Target);
            EventBus.Publish(new SkillExecutedEvent(result));
            ProcessDeadUnits(result);
        }

        yield return new WaitForSeconds(m_EnemyActionDelay);

    }

    // 헬퍼
    private bool NeedsTargetSelection(SkillData skill)
    {
        if (skill.TargetType == TargetType.EnemySingle || skill.TargetType == TargetType.AllySingle)
            return true;
        return false;
    }
    private void ProcessDeadUnits(SkillResult result)
    {
        if (result.TargetResults == null)
            return;
        for (int i =0; i<result.TargetResults.Length; ++i)
        {
            CombatUnit target = result.TargetResults[i].Target;
            if(target != null)
            {
                if(target.State == UnitState.Dead)
                {
                    m_PositionSystem.RemoveUnit(target);
                    EventBus.Publish(new UnitDiedEvent(target));
                }
            }
        }
    }
    private void SetState(CombatState newState)
    { 
        m_CurrentState = newState;
        if (OnStateChanged != null)
            OnStateChanged(newState);
        // OnstateChanged?.Invoke(newState)와 동일함.
    }

    private void ApplyPostBattleEbla()
    {
        int roundCheck = m_TurnManager.RoundNumber;
        if (m_EblaFreeRounds > roundCheck)
            return;
        int total = 0;
        for(int i=m_EblaFreeRounds +1; i<=roundCheck; ++i)
        {
            total += i * m_EblaRoundMultiplier;
        }

        List<CombatUnit> nikkes = m_PositionSystem.GetAllUnits(CombatUnitType.Nikke);
        for(int i=0; i<nikkes.Count; ++i)
        {
            nikkes[i].AddEbla(total);
        }

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


}
