using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatFeedback m_Feedback;
    [SerializeField] private DamagePopupPool m_PopupPool;
    [SerializeField] private CombatHUD m_CombatHUD;

    [Header("Focus")]
    [SerializeField] private float m_FocusScale = 1.5f;
    [SerializeField] private float m_FocusOutDuration = 0.2f;
    [SerializeField] private float m_NikkeFocusLayoutScale = 0.5f;
    [SerializeField] private float m_EnemyFocusLayoutScale = 0.5f;
    [SerializeField] private float m_EnemyFocusMinXMargin = 0f;

    [Header("Focus Points")]
    [SerializeField] private Transform m_NikkeFocusPoint;
    [SerializeField] private Transform m_EnemyFocusPoint;

    [Header("Camera")]
    [SerializeField] private Camera m_Camera;
    [SerializeField] private float m_FocusOrthoSize;

    [Header("Blur")]
    [SerializeField] private FocusBlurController m_BlurController;
    [SerializeField] private float m_BlurStrength = 3f;
    [SerializeField] private int m_FocusSortingOrder = 10;
    private int m_FocusLayer;
    private int m_DefaultLayer;

    [Header("Drift")]
    [SerializeField] private float m_DriftSpeed = 0.5f;

    [Header("Timing")]
    [SerializeField] private float m_PopupDuration = 0.6f;
    [SerializeField] private float m_StatusPopupDelay = 0.3f;
    [SerializeField] private float m_PostSequenceDelay = 0.2f;

    // 색상 상수
    private static readonly Color COLOR_DAMAGE = new Color(0.75f, 0.07f, 0.07f);
    private static readonly Color COLOR_CRIT = new Color(0.9f, 0.53f, 0.1f);
    private static readonly Color COLOR_HEAL = new Color(0.53f, 0.75f, 0.26f);
    private static readonly Color COLOR_MISS = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color COLOR_BLEED = new Color(0.75f, 0.07f, 0.07f);
    private static readonly Color COLOR_POISON = new Color(0.73f, 0.75f, 0.25f);
    private static readonly Color COLOR_STUN = new Color(0.8f, 0.64f, 0.32f);
    private static readonly Color COLOR_DEBUFF = new Color(0.75f, 0.38f, 0.06f);
    private static readonly Color COLOR_RESIST = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color COLOR_DEATHDOOR = new Color(0.6f, 0f, 0f);

    // 팝업 텍스트
    private const string TRIGGER_ATTACK = "Attack";
    private const string TRIGGER_HIT = "Hit";
    private const string TRIGGER_DEATH = "Death";

    private const string POPUP_DODGE = "회피";
    private const string POPUP_RESIST = "저항";
    private const string POPUP_CRIT = "치명타!\n";
    private const string POPUP_DEATHS_DOOR = "죽음의 문턱!";


    // Animation Event 동기화 플래그
    private bool m_AttackEndReceived;

    // 포커스 복원용 캐싱
    private Dictionary<CombatUnit, Vector3> m_OriginalScales;
    private List<CombatUnit> m_AllLivingBuffer;
    private HashSet<CombatUnit> m_FocusBuffer;
    private List<CombatUnit> m_NikkeFocusBuffer;
    private List<CombatUnit> m_EnemyFocusBuffer;

    private Dictionary<CombatUnit, Vector3> m_OriginalPositions;
    private Dictionary<CombatUnit, Vector3> m_DriftedPositions; // FocusOut lerp 용
    private CombatUnit m_FocusUser;
    private float m_OriginalOrthoSize;
    private Dictionary<CombatUnit, int> m_OriginalSortingOrders;


    private Coroutine m_DriftCoroutine;


    // GC 방지
    private Dictionary<CombatUnit, CombatFieldView.UnitView> m_ViewCache;

    private void Awake()
    {
        m_OriginalScales = new Dictionary<CombatUnit, Vector3>();
        m_AllLivingBuffer = new List<CombatUnit>();
        m_FocusBuffer = new HashSet<CombatUnit>();
        m_ViewCache = new Dictionary<CombatUnit, CombatFieldView.UnitView>();
        m_OriginalSortingOrders = new Dictionary<CombatUnit, int>();
        m_OriginalPositions = new Dictionary<CombatUnit, Vector3>();
        m_DriftedPositions = new Dictionary<CombatUnit, Vector3>();
        m_NikkeFocusBuffer = new List<CombatUnit>();
        m_EnemyFocusBuffer = new List<CombatUnit>();

        m_FocusLayer = LayerMask.NameToLayer("FocusForeground");
        m_DefaultLayer = LayerMask.NameToLayer("Default");
    }
    public Coroutine PlaySkillSequence(CombatUnit user, SkillData skill, List<CombatUnit> targets, SkillResult result)
    {
        return StartCoroutine(SkillSequence(user, skill, targets, result));
    }
    private IEnumerator SkillSequence(CombatUnit user, SkillData skill, List<CombatUnit> targets, SkillResult result)
    {
        // 1. 포커스 대상 구성
        m_FocusBuffer.Clear();
        m_FocusBuffer.Add(user);
        for (int i = 0; i < targets.Count; ++i)
        {
            if (targets[i] != user)
                m_FocusBuffer.Add(targets[i]);
        }

        m_FocusUser = user;
        m_FieldView.SetFocusLock(true);
        // 2. Focus In
        yield return FocusIn();
        StartDrift(user,skill);

        // 3. 공격 모션
        if (result.TargetResults != null && result.TargetResults.Length > 0)
        {
            if (targets.Count > 1)
                ProcessHitBatch(result.TargetResults, skill);
            else
                ProcessSingleHit(result.TargetResults[0], skill);
        }

        CombatFieldView.UnitView userView = m_FieldView.GetView(user);
        if (userView.Animator != null)
        {
            userView.AnimBridge.SetCallbacks(null, OnAttackEnd);
            m_AttackEndReceived = false;
            userView.Animator.SetTrigger(TRIGGER_ATTACK);
        }

        yield return new WaitForSecondsRealtime(m_PopupDuration);

        if (userView.Animator != null)
            while (!m_AttackEndReceived) yield return null;

        StopDrift();





        // 8.콜백 정리
        if (userView.AnimBridge != null)
            userView.AnimBridge.ClearCallbacks();

        // FocusOut
        yield return FocusOut();
        m_FieldView.SetFocusLock(false);
        m_FieldView.MoveAllToCurrentSlots();

        // 상태이상 팝업
        if (result.TargetResults != null)
        {
            for (int i = 0; i < result.TargetResults.Length; ++i)
            {
                TargetResult tr = result.TargetResults[i];
                CombatFieldView.UnitView v = m_FieldView.GetView(tr.Target);
                if (tr.AppliedEffects != null)
                {
                    for (int j = 0; j < tr.AppliedEffects.Length; ++j)
                    {
                        m_PopupPool.SpawnEffect(v.Renderer.transform.position, tr.AppliedEffects[j].EffectName,GetEffectColor(tr.AppliedEffects[j].EffectType)); 
                        yield return new WaitForSecondsRealtime(m_StatusPopupDelay);
                    }
                }
                if (tr.ResistedEffects != null)
                {
                    for (int j = 0; j < tr.ResistedEffects.Length; ++j)
                    {
                        m_PopupPool.SpawnEffect(v.Renderer.transform.position, POPUP_RESIST, COLOR_RESIST);
                        yield return new WaitForSecondsRealtime(m_StatusPopupDelay);
                    }
                }
            }
        }
        if(result.TargetResults != null)
        {
            for (int i = 0; i < result.TargetResults.Length; ++i)
            {
                TargetResult tr = result.TargetResults[i];
                if (tr.PreviousState == UnitState.Alive && tr.ResultState == UnitState.DeathsDoor)
                {
                    CombatFieldView.UnitView v = m_FieldView.GetView(tr.Target);
                    m_PopupPool.SpawnEffect(v.Renderer.transform.position, POPUP_DEATHS_DOOR, COLOR_DEATHDOOR);
                    yield return new WaitForSecondsRealtime(m_StatusPopupDelay);
                }
            }
        }


        // 10. 후딜레이
        yield return new WaitForSecondsRealtime(m_PostSequenceDelay);


    }

    // 코루틴
    private IEnumerator FocusIn()
    {
        m_CombatHUD.SetHpBarsVisible(false);
        m_FieldView.GetAllLivingUnits(m_AllLivingBuffer);

        m_OriginalScales.Clear();
        m_OriginalPositions.Clear();
        m_OriginalSortingOrders.Clear();
        m_ViewCache.Clear();

        foreach (CombatUnit unit in m_AllLivingBuffer)
        {
            CombatFieldView.UnitView view = m_FieldView.GetView(unit);
            m_ViewCache[unit] = view;
            m_OriginalScales[unit] = view.Renderer.transform.localScale;
            m_OriginalPositions[unit] = view.Renderer.transform.position;
            m_OriginalSortingOrders[unit] = view.Renderer.sortingOrder;
        }

        m_NikkeFocusBuffer.Clear();
        m_EnemyFocusBuffer.Clear();
        foreach (CombatUnit unit in m_FocusBuffer)
        {
            if (unit.UnitType == CombatUnitType.Nikke)
                m_NikkeFocusBuffer.Add(unit);
            else
                m_EnemyFocusBuffer.Add(unit);
        }
        Vector3 screenCenter = (m_NikkeFocusPoint.position + m_EnemyFocusPoint.position) * 0.5f;
        Vector3 nikkeForward = (m_EnemyFocusPoint.position - m_NikkeFocusPoint.position).normalized;

        bool allSameTeam = m_NikkeFocusBuffer.Count == 0 || m_EnemyFocusBuffer.Count == 0;

        if (allSameTeam)
        {
            List<CombatUnit> units = m_NikkeFocusBuffer.Count > 0 ? m_NikkeFocusBuffer : m_EnemyFocusBuffer;
            AssignFocusPositions(units, screenCenter, units == m_NikkeFocusBuffer ? m_NikkeFocusLayoutScale :
            m_EnemyFocusLayoutScale);
        }
        else
        {
            AssignFocusPositions(m_NikkeFocusBuffer, m_NikkeFocusPoint.position, m_NikkeFocusLayoutScale);
            AssignFocusPositions(m_EnemyFocusBuffer, m_EnemyFocusPoint.position, m_EnemyFocusLayoutScale, screenCenter.x +
            m_EnemyFocusMinXMargin);
        }

        m_BlurController.SetBlurStrength(m_BlurStrength);
        m_OriginalOrthoSize = m_Camera.orthographicSize;
        m_Camera.orthographicSize = m_FocusOrthoSize;

        yield break;
    }
    private void AssignFocusPositions(List<CombatUnit> units, Vector3 focusCenter, float layoutScale,float minX = float.MinValue)
    {
        Vector3 layoutCenter = Vector3.zero;
        for (int i = 0; i < units.Count; ++i)
            layoutCenter += m_FieldView.GetSlotPosition(units[i]);
        layoutCenter /= units.Count;

        float xShift = 0f;
        if (minX > float.MinValue)
        {
            for (int i = 0; i < units.Count; ++i)
            {
                Vector3 slotOffset = m_FieldView.GetSlotPosition(units[i]) - layoutCenter;
                float candidateX = focusCenter.x + slotOffset.x * layoutScale;
                xShift = Mathf.Max(xShift, minX - candidateX);
            }
        }

        for (int i = 0; i < units.Count; ++i)
        {
            CombatUnit unit = units[i];
            CombatFieldView.UnitView view = m_ViewCache[unit];
            Vector3 slotOffset = m_FieldView.GetSlotPosition(unit) - layoutCenter;
            Vector3 pos = focusCenter + slotOffset * layoutScale;
            pos.x += xShift;
            view.Renderer.transform.position = pos;
            view.Renderer.transform.localScale = m_OriginalScales[unit] * m_FocusScale;
            view.Renderer.sortingOrder = m_FocusSortingOrder;
            view.Renderer.gameObject.layer = m_FocusLayer;
        }
    }


    private IEnumerator FocusOut()
    {
        m_DriftedPositions.Clear();
        foreach (CombatUnit unit in m_AllLivingBuffer)
            m_DriftedPositions[unit] = m_ViewCache[unit].Renderer.transform.position;

        float elapsed = 0f;
        while (elapsed < m_FocusOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_FocusOutDuration;

            foreach (CombatUnit unit in m_AllLivingBuffer)
            {
                if (m_FocusBuffer.Contains(unit))
                {
                    CombatFieldView.UnitView view = m_ViewCache[unit];
                    view.Renderer.transform.localScale = Vector3.Lerp(
                        m_OriginalScales[unit] * m_FocusScale,
                        m_OriginalScales[unit],
                        t);
                    Vector3 targetPos = (unit == m_FocusUser)
                        ? m_FieldView.GetSlotPosition(unit)
                        : m_OriginalPositions[unit];
                    view.Renderer.transform.position = Vector3.Lerp(
                        m_DriftedPositions[unit],
                        targetPos,
                        t);


                }
            }

            m_Camera.orthographicSize = Mathf.Lerp(m_FocusOrthoSize, m_OriginalOrthoSize, t);

            yield return null;
        }

        foreach (CombatUnit unit in m_AllLivingBuffer)
        {
            if (m_FocusBuffer.Contains(unit))
            {
                m_ViewCache[unit].Renderer.sortingOrder = m_OriginalSortingOrders[unit];
                m_ViewCache[unit].Renderer.gameObject.layer = m_DefaultLayer;
            }
        }
        m_BlurController.SetBlurStrength(0f);
        m_CombatHUD.SetHpBarsVisible(true);
    }

    private void ProcessSingleHit(TargetResult targetResult, SkillData skill)
    {
        ProcessOneTarget(targetResult, skill);
        m_Feedback.PlayHitStop(targetResult.IsCrit);
    }
    private void ProcessHitBatch(TargetResult[] results, SkillData skill)
    {
        bool anyCrit = false;
        for (int i = 0; i < results.Length; ++i)
        {
            ProcessOneTarget(results[i], skill);
            if (results[i].IsHit && results[i].IsCrit) anyCrit = true;
        }
        m_Feedback.PlayHitStop(anyCrit);
    }
    private void SpawnDamagePopup(CombatUnit target, TargetResult result, SkillData skill)
    {
        CombatFieldView.UnitView view = m_FieldView.GetView(target);
        Vector3 pos = view.Renderer.transform.position;
        bool isNikke = target.UnitType == CombatUnitType.Nikke;

        if (skill.MaxHeal > 0)
        {
            m_PopupPool.SpawnDamage(pos, result.HealAmount.ToString(), COLOR_HEAL, isNikke);
        }
        else
        {
            string text = result.IsCrit ? POPUP_CRIT + result.DamageDealt : result.DamageDealt.ToString();
            Color color = result.IsCrit ? COLOR_CRIT : COLOR_DAMAGE;
            float scale = result.IsCrit ? 1.15f : 1f;
            m_PopupPool.SpawnDamage(pos, text, color, isNikke, scale);
        }
    }
    private Color GetEffectColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed:
                return COLOR_BLEED;
            case StatusEffectType.Poison:
                return COLOR_POISON;
            case StatusEffectType.Stun:
                return COLOR_STUN;
            default:
                return COLOR_DEBUFF;
        }
    }

    public Coroutine PlayDotTick(CombatUnit unit, int damage, StatusEffectType type)
    {
        return StartCoroutine(DotTickRoutine(unit, damage, type));
    }
    private IEnumerator DotTickRoutine(CombatUnit unit, int damage, StatusEffectType type)
    {
        CombatFieldView.UnitView view = m_FieldView.GetView(unit);
        if (view.Renderer == null)
            yield break;
        m_PopupPool.SpawnEffect(view.Renderer.transform.position, damage.ToString(), GetEffectColor(type));
        yield return new WaitForSecondsRealtime(m_PopupDuration);
    }

    private void OnAttackEnd()
    {
        m_AttackEndReceived = true;
    }

    private void ProcessOneTarget(TargetResult result, SkillData skill)
    {
        CombatFieldView.UnitView targetView = m_FieldView.GetView(result.Target);
        if (result.IsHit)
        {
            if (targetView.Animator != null)
            {
                targetView.Animator.SetTrigger(TRIGGER_HIT);
                if (result.ResultState == UnitState.Dead || result.ResultState == UnitState.Corpse)
                    targetView.Animator.SetTrigger(TRIGGER_DEATH);
            }
            SpawnDamagePopup(result.Target, result, skill);
        }
        else
            m_PopupPool.SpawnEffect(targetView.Renderer.transform.position, POPUP_DODGE, COLOR_MISS);
    }

    private void StartDrift(CombatUnit user, SkillData skill)
    {
        m_DriftCoroutine = StartCoroutine(DriftRoutine(user, skill));
    }

    private void StopDrift()
    {
        if(m_DriftCoroutine != null)
            StopCoroutine(m_DriftCoroutine);
        m_DriftCoroutine=null;
    }

    private IEnumerator DriftRoutine(CombatUnit user, SkillData skill)
    {
        Vector3 nikkeForward = (m_EnemyFocusPoint.position - m_NikkeFocusPoint.position).normalized;
        Vector3 enemyForward = (m_NikkeFocusPoint.position - m_EnemyFocusPoint.position).normalized;

        Vector3 userForward = user.UnitType == CombatUnitType.Nikke ? nikkeForward : enemyForward;
        int nonUserCount = m_FocusBuffer.Count - 1;

        CombatFieldView.UnitView userView = m_FieldView.GetView(user);
        float centerX = m_Camera.transform.position.x;

        Vector3 userDir;
        if (skill.SkillType == SkillType.Melee)
            userDir = userForward;
        else if (skill.IsAllyTargeting)
            userDir = (userView.Renderer.transform.position.x >= centerX)
                ? Vector3.right : Vector3.left;
        else
            userDir = -userForward;

        // 아군 타겟팅: 초기 위치 기반 방향 스냅샷
        Dictionary<CombatUnit, Vector3> allyTargetDirs = null;
        if (skill.IsAllyTargeting)
        {
            allyTargetDirs = new Dictionary<CombatUnit, Vector3>();
            foreach (CombatUnit t in m_FocusBuffer)
            {
                if (t == user) continue;
                CombatFieldView.UnitView tv = m_FieldView.GetView(t);
                if (tv.Renderer == null) continue;
                allyTargetDirs[t] = (tv.Renderer.transform.position.x >= centerX)
                    ? Vector3.right : Vector3.left;
            }
        }
        while (true)
        {
            if (userView.Renderer != null)
                userView.Renderer.transform.position += userDir * m_DriftSpeed * Time.deltaTime;

            foreach (CombatUnit target in m_FocusBuffer)
            {
                if (target == user) continue;
                CombatFieldView.UnitView targetView = m_FieldView.GetView(target);
                if (targetView.Renderer == null) continue;

                Vector3 targetForward = target.UnitType == CombatUnitType.Nikke ? nikkeForward : enemyForward;
                Vector3 targetDir;

                if (skill.IsAllyTargeting)
                    targetDir = allyTargetDirs[target];
                else if (target.UnitType == user.UnitType)
                    targetDir = (target.SlotIndex < 2) ? targetForward : -targetForward;
                else
                    targetDir = -targetForward;

                targetView.Renderer.transform.position += targetDir * m_DriftSpeed * Time.deltaTime;
            }
            yield return null;
        }
    }


}
