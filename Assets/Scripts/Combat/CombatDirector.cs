using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class CombatDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatFeedback m_Feedback;
    [SerializeField] private DamagePopupPool m_PopupPool;

    [Header("Focus")]
    [SerializeField] private float m_FocusScale = 1.5f;
    [SerializeField] private float m_FocusInDuration = 0.3f;
    [SerializeField] private float m_FocusOutDuration = 0.2f;
    [SerializeField] private float m_UnfocusedAlpha = 0.3f;


    [Header("Timing")]
    [SerializeField] private float m_PostHitDelay = 0.2f;
    [SerializeField] private float m_PopupDuration = 0.6f;
    [SerializeField] private float m_StatusPopupDelay = 0.3f;
    [SerializeField] private float m_PostSequenceDelay = 0.2f;
    [SerializeField] private float m_FallbackHitDelay = 0.3f;

    // 색상 상수
    private static readonly Color COLOR_DAMAGE = Color.darkRed;
    private static readonly Color COLOR_CRIT = Color.orange;
    private static readonly Color COLOR_HEAL = new Color(0.53f, 0.75f, 0.26f);
    private static readonly Color COLOR_MISS = Color.gray;
    private static readonly Color COLOR_BLEED = new Color(0.75f, 0.7f, 0.7f);
    private static readonly Color COLOR_POISON = new Color(0.73f, 0.75f, 0.25f);
    private static readonly Color COLOR_STUN = new Color(0.8f, 0.64f, 0.32f);
    private static readonly Color COLOR_DEBUFF = new Color(0.75f, 0.38f, 0.06f);
    private static readonly Color COLOR_RESIST = Color.gray;

    // Animation Event 동기화 플래그
    private bool m_HitFrameReceived;
    private bool m_AttackEndReceived;

    // 포커스 복원용 캐싱
    private Dictionary<CombatUnit, Vector3> m_OriginalScales;
    private Dictionary<CombatUnit, float> m_OriginalAlphas;
    private List<CombatUnit> m_AllLivingBuffer;
    private List<CombatUnit> m_FocusBuffer;

    private void Awake()
    {
        m_OriginalScales = new Dictionary<CombatUnit, Vector3>();
        m_OriginalAlphas = new Dictionary<CombatUnit, float>();
        m_AllLivingBuffer = new List<CombatUnit>();
        m_FocusBuffer = new List<CombatUnit>();
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

        // 2. Focus In
        yield return FocusIn();

        // 3. 공격 모션
        CombatFieldView.UnitView userView = m_FieldView.GetView(user);
        if (userView.Animator != null)
        {
            userView.AnimBridge.SetCallbacks(OnHitFrame, OnAttackEnd);
            m_HitFrameReceived = false;
            m_AttackEndReceived = false;
            userView.Animator.SetTrigger("Attack");
            while (!m_HitFrameReceived) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(m_FallbackHitDelay);
        }

        // 4. 히트 처리
        if (result.TargetResults != null && result.TargetResults.Length > 0)
        {
            if (targets.Count > 1)
                ProcessHitBatch(result.TargetResults, skill);
            else
                ProcessSingleHit(result.TargetResults[0], skill);
        }
        yield return new WaitForSecondsRealtime(m_PopupDuration);

        // 5. 상태이상 팝업
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
                        m_PopupPool.Spawn(v.Renderer.transform.position, tr.AppliedEffects[j].EffectName, GetEffectColor(tr.AppliedEffects[j].EffectType));
                        yield return new WaitForSecondsRealtime(m_StatusPopupDelay);
                    }
                }
                if (tr.ResistedEffects != null)
                {
                    for (int j = 0; j < tr.ResistedEffects.Length; ++j)
                    {
                        m_PopupPool.Spawn(v.Renderer.transform.position, "저항!", COLOR_RESIST);
                        yield return new WaitForSecondsRealtime(m_StatusPopupDelay);
                    }
                }
            }
        }

        // 6 사망처리
        if (result.TargetResults != null)
        {
            for (int i = 0; i < result.TargetResults.Length; ++i)
            {
                TargetResult tr = result.TargetResults[i];
                if (tr.ResultState == UnitState.Dead || tr.ResultState == UnitState.Corpse)
                {
                    CombatFieldView.UnitView v = m_FieldView.GetView(tr.Target);
                    if (v.Animator != null)
                        v.Animator.SetTrigger("Death");
                }
            }
        }

        // 7. OnAttackEnd 대기
        if (userView.Animator != null)
            while (!m_AttackEndReceived) yield return null;

        // 8.콜백 정리
        if (userView.AnimBridge != null)
            userView.AnimBridge.ClearCallbacks();

        // 9. FocusOut
        yield return FocusOut();

        // 10. 후딜레이
        yield return new WaitForSecondsRealtime(m_PostSequenceDelay);


    }

    // 코루틴
    private IEnumerator FocusIn()
    {
        // 1. 살아있는 유닛 전체 가져오기
        m_FieldView.GetAllLivingUnits(m_AllLivingBuffer);

        // 2. 원래 스케일 / 알파 캐싱
        m_OriginalScales.Clear();
        m_OriginalAlphas.Clear();

        foreach (CombatUnit unit in m_AllLivingBuffer)
        {
            CombatFieldView.UnitView view = m_FieldView.GetView(unit);
            m_OriginalScales[unit] = view.Renderer.transform.localScale;
            m_OriginalAlphas[unit] = view.Renderer.color.a;
        }

        // 3. lerp
        float elapsed = 0f;
        while (elapsed < m_FocusInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_FocusInDuration;
            foreach (CombatUnit unit in m_AllLivingBuffer)
            {
                CombatFieldView.UnitView view = m_FieldView.GetView(unit);
                if (m_FocusBuffer.Contains(unit))
                {
                    // 스케일 확대
                    view.Renderer.transform.localScale = Vector3.Lerp(m_OriginalScales[unit], m_OriginalScales[unit] * m_FocusScale, t);
                }
                else
                {
                    // 알파 감소
                    Color c = view.Renderer.color;
                    c.a = Mathf.Lerp(m_OriginalAlphas[unit], m_UnfocusedAlpha, t);
                    view.Renderer.color = c;
                }
            }
            yield return null;
        }
    }

    private IEnumerator FocusOut()
    {
        float elapsed = 0f;
        while (elapsed < m_FocusOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_FocusOutDuration;
            foreach (CombatUnit unit in m_AllLivingBuffer)
            {
                CombatFieldView.UnitView view = m_FieldView.GetView(unit);
                if (m_FocusBuffer.Contains(unit))
                {
                    // 스케일 감소
                    view.Renderer.transform.localScale = Vector3.Lerp(m_OriginalScales[unit] * m_FocusScale, m_OriginalScales[unit], t);
                }
                else
                {
                    // 알파 증가
                    Color c = view.Renderer.color;
                    c.a = Mathf.Lerp(m_UnfocusedAlpha, m_OriginalAlphas[unit], t);
                    view.Renderer.color = c;
                }
            }
            yield return null;
        }
    }

    private void ProcessSingleHit(TargetResult targetResult, SkillData skill)
    {
        CombatFieldView.UnitView targetView = m_FieldView.GetView(targetResult.Target);
        if (targetResult.IsHit)
        {
            if (targetView.Animator != null)
                targetView.Animator.SetTrigger("Hit");
            m_Feedback.PlayFlash(targetView.Renderer);
            m_Feedback.PlayHitStop(targetResult.IsCrit);
            SpawnDamagePopup(targetResult.Target, targetResult, skill);
        }
        else
        {
            m_PopupPool.Spawn(targetView.Renderer.transform.position, "회피", COLOR_MISS);
        }
    }
    private void ProcessHitBatch(TargetResult[] results, SkillData skill)
    {
        bool anyCrit = false;
        foreach (TargetResult result in results)
        {
            CombatFieldView.UnitView v = m_FieldView.GetView(result.Target);
            if (result.IsHit)
            {
                if (v.Animator != null)
                    v.Animator.SetTrigger("Hit");
                m_Feedback.PlayFlash(v.Renderer);
                if (result.IsCrit)
                    anyCrit = true;
                SpawnDamagePopup(result.Target, result, skill);
            }
            else
            {
                m_PopupPool.Spawn(v.Renderer.transform.position, "회피", COLOR_MISS);
            }
        }
        m_Feedback.PlayHitStop(anyCrit);

    }
    private void SpawnDamagePopup(CombatUnit target, TargetResult result, SkillData skill)
    {
        CombatFieldView.UnitView view = m_FieldView.GetView(target);
        Vector3 pos = view.Renderer.transform.position;

        if (skill.MaxHeal > 0)
        {
            m_PopupPool.Spawn(pos, result.HealAmount.ToString(), COLOR_HEAL);
        }
        else if (result.IsHit)
        {
            string text = result.IsCrit ? result.DamageDealt + "!" : result.DamageDealt.ToString();
            Color color = result.IsCrit ? COLOR_CRIT : COLOR_DAMAGE;
            float scale = result.IsCrit ? 1.3f : 1f;
            m_PopupPool.Spawn(pos, text, color, scale);
        }
        else
            m_PopupPool.Spawn(pos, "회피", COLOR_MISS);

        if (result.PreviousState == UnitState.Alive && result.ResultState == UnitState.DeathsDoor)
            m_PopupPool.Spawn(pos, "죽음의 일격!", COLOR_CRIT);
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
        m_Feedback.PlayFlash(view.Renderer);
        m_PopupPool.Spawn(view.Renderer.transform.position, damage.ToString(), GetEffectColor(type));
        yield return new WaitForSecondsRealtime(m_PopupDuration);
    }

    private void OnHitFrame()
    {
        m_HitFrameReceived = true;
    }
    private void OnAttackEnd()
    {
        m_AttackEndReceived = true;
    }
}
