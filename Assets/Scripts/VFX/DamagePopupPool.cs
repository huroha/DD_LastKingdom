using UnityEngine;
using System.Collections.Generic;

public class DamagePopupPool : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private DamagePopup m_DamagePrefab;
    [SerializeField] private DamagePopup m_EffectPrefab;

    [Header("Offsets")]
    [SerializeField] private Vector3 m_NikkeDamageOffset;
    [SerializeField] private Vector3 m_EnemyDamageOffset;
    [SerializeField] private Vector3 m_EffectOffset;

    [Header("Pool Size")]
    [SerializeField] private int m_DamageInitialSize = 8;
    [SerializeField] private int m_EffectInitialSize = 8;

    private List<DamagePopup> m_DamagePool;
    private List<DamagePopup> m_EffectPool;

    private void Awake()
    {
        m_DamagePool = new List<DamagePopup>();
        m_EffectPool = new List<DamagePopup>();

        for (int i = 0; i < m_DamageInitialSize; ++i)
        {
            DamagePopup popup = Instantiate(m_DamagePrefab, transform);
            popup.gameObject.SetActive(false);
            m_DamagePool.Add(popup);
        }
        for (int i = 0; i < m_EffectInitialSize; ++i)
        {
            DamagePopup popup = Instantiate(m_EffectPrefab, transform);
            popup.gameObject.SetActive(false);
            m_EffectPool.Add(popup);
        }
    }
    public void SpawnDamage(Vector3 pos, string text, Color color, bool isNikke, float scale = 1f)
    {
        Vector3 offset = isNikke ? m_NikkeDamageOffset : m_EnemyDamageOffset;
        DamagePopup popup = GetFromPool(m_DamagePool, m_DamagePrefab);
        popup.Show(pos, offset, text, color, scale);
    }

    public void SpawnEffect(Vector3 pos, string text, Color color)
    {
        DamagePopup popup = GetFromPool(m_EffectPool, m_EffectPrefab);
        popup.Show(pos, m_EffectOffset, text, color);
    }

    private DamagePopup GetFromPool(List<DamagePopup> pool, DamagePopup prefab)
    {
        for (int i = 0; i < pool.Count; ++i)
        {
            if (!pool[i].gameObject.activeSelf)
                return pool[i];
        }
        DamagePopup newPopup = Instantiate(prefab, transform);
        pool.Add(newPopup);
        return newPopup;
    }

}
