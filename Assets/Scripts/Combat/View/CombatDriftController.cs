using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatDriftController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float m_DriftSpeed;

    [Header("References")]
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatFocusController m_FocusController;

    private Coroutine m_DriftCoroutine;


    public void StartDrift(CombatUnit user, BaseSkillData skill)
    {
        m_DriftCoroutine = StartCoroutine(DriftRoutine(user, skill));
    }
    public void StopDrift()
    {
        if (m_DriftCoroutine != null)
            StopCoroutine(m_DriftCoroutine);
        m_DriftCoroutine = null;
    }

    // 헬퍼
    private IEnumerator DriftRoutine(CombatUnit user, BaseSkillData skill)
    {
        Vector3 nikkeForward = (m_FocusController.EnemyFocusPoint.position - m_FocusController.NikkeFocusPoint.position).normalized;
        Vector3 enemyForward = (m_FocusController.NikkeFocusPoint.position - m_FocusController.EnemyFocusPoint.position).normalized;

        Vector3 userForward = user.UnitType == CombatUnitType.Nikke ? nikkeForward : enemyForward;

        CombatFieldView.UnitView userView = m_FieldView.GetView(user);
        float centerX = m_FocusController.FocusCamera.transform.position.x;

        Vector3 userDir;
        if (skill.IsAllyTargeting)
            userDir = (userView.Renderer.transform.position.x >= centerX) ? Vector3.right : Vector3.left;
        else if (skill.SkillType == SkillType.Melee)
            userDir = userForward;
        else
            userDir = -userForward;

        int snapCount = 0;
        CombatFieldView.UnitView[] snapViews = new CombatFieldView.UnitView[m_FocusController.FocusBuffer.Count];
        Vector3[] snapDirs = new Vector3[snapViews.Length];

        foreach (CombatUnit target in m_FocusController.FocusBuffer)
        {
            if (target == user) continue;
            CombatFieldView.UnitView tv = m_FieldView.GetView(target);
            if (tv.Renderer == null) continue;

            Vector3 dir;
            if (skill.IsAllyTargeting)
            {
                dir = (tv.Renderer.transform.position.x >= centerX) ? Vector3.right : Vector3.left;
            }
            else
            {
                Vector3 targetForward = target.UnitType == CombatUnitType.Nikke ? nikkeForward : enemyForward;
                if (target.UnitType == user.UnitType)
                    dir = (target.SlotIndex < 2) ? targetForward : -targetForward;
                else
                    dir = -targetForward;
            }

            snapViews[snapCount] = tv;
            snapDirs[snapCount] = dir;
            ++snapCount;
        }

        while (true)
        {
            if (userView.Renderer != null)
                userView.Renderer.transform.position += userDir * m_DriftSpeed * Time.deltaTime;

            for (int i = 0; i < snapCount; ++i)
            {
                if (snapViews[i].Renderer != null)
                    snapViews[i].Renderer.transform.position += snapDirs[i] * m_DriftSpeed * Time.deltaTime;
            }
            yield return null;
        }
    }
}
