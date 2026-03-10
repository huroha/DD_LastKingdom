using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CombatFieldView : MonoBehaviour
{
    [SerializeField] private Transform[] m_NikkeSlots;      // Inspector에서 연결
    [SerializeField] private Transform[] m_EnemySlots;
    [SerializeField] private float m_MoveDuration = 0.3f;
    [SerializeField] private float m_UnitScale = 0.3f;


    // CombatUnit -> 해당 유닛의 SpriteRenderer 맵핑
    private Dictionary<CombatUnit, SpriteRenderer> m_UnitViews;
    // 중복 코루틴 방지용
    private Dictionary<CombatUnit, Coroutine> m_MoveCoroutines;
    // 시체 관리용
    private Dictionary<CombatUnit, SpriteRenderer> m_CorpseViews;



    private void OnEnable()
    {
        EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Subscribe<UnitMovedEvent>(OnUnitMoved);
        EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);

    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Unsubscribe<UnitMovedEvent>(OnUnitMoved);
        EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
    }

    private void OnBattleStarted(BattleStartedEvent e)
    {
        if(m_UnitViews != null)
        {
            foreach (KeyValuePair<CombatUnit, SpriteRenderer> pair in m_UnitViews)
                Destroy(pair.Value.gameObject);
        }

        m_UnitViews = new Dictionary<CombatUnit, SpriteRenderer>();
        m_MoveCoroutines = new Dictionary<CombatUnit, Coroutine>();
        m_CorpseViews = new Dictionary<CombatUnit, SpriteRenderer>();

        for (int i=0; i<e.Nikkes.Count; ++i)
        {
            CombatUnit unit = e.Nikkes[i];
            Vector3 pos = GetSlotPosition(unit);
            SpriteRenderer view = CreateUnitView(unit, pos);
            m_UnitViews[unit] = view;
        }

        for (int i=0; i<e.Enemies.Count; ++i)
        {
            CombatUnit unit = e.Enemies[i];
            Vector3 pos = GetSlotPosition(unit);
            SpriteRenderer view = CreateUnitView(unit, pos);
            m_UnitViews[unit] = view;
        }
    }
    private void OnUnitMoved(UnitMovedEvent e)
    {
        MoveAllToCurrentSlots();
    }
    private void OnUnitDied(UnitDiedEvent e)
    {
        if(m_MoveCoroutines.ContainsKey(e.Unit))
            StopCoroutine(m_MoveCoroutines[e.Unit]);
        m_MoveCoroutines.Remove(e.Unit);

        // Corpse -> Dead : 시체 제거
        if(m_CorpseViews.ContainsKey(e.Unit))
        {
            Destroy(m_CorpseViews[e.Unit].gameObject);
            m_CorpseViews.Remove(e.Unit);
            return;
        }


        SpriteRenderer view = m_UnitViews[e.Unit];
        m_UnitViews.Remove(e.Unit);

        // 적이고 시체 스프라이트가 있으면 교체, 없으면 제거?
        if(e.Unit.UnitType == CombatUnitType.Enemy && e.Unit.EnemyData.CorpseSprite != null)
        {
            view.sprite = e.Unit.EnemyData.CorpseSprite;
            m_CorpseViews[e.Unit] = view;
        }
        else
            Destroy(view.gameObject);
    }

    private SpriteRenderer CreateUnitView(CombatUnit unit, Vector3 pos)
    {
        GameObject go = new GameObject(unit.UnitName);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

        if(unit.UnitType == CombatUnitType.Nikke)
        {
            sr.sprite = unit.NikkeData.CombatIdleSprite;
        }
        else if(unit.UnitType == CombatUnitType.Enemy)
        {
            sr.sprite = unit.EnemyData.Sprite;
            go.AddComponent<BoxCollider2D>();
            UnitClickHandler handler = go.AddComponent<UnitClickHandler>();
            CombatUnit captured = unit;
            handler.Initialize(() => LogEnemyInfo(captured));
        }
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * m_UnitScale;
        return sr;

    }
    private Transform[] GetSlots(CombatUnitType type)
    {
        if (type == CombatUnitType.Nikke)
            return m_NikkeSlots;
        else
            return m_EnemySlots;
    }

    private Vector3 GetSlotPosition(CombatUnit unit)
    {
        return GetSlots(unit.UnitType)[unit.SlotIndex].position;
    }

    private void MoveAllToCurrentSlots()
    {
        foreach(KeyValuePair<CombatUnit, SpriteRenderer> pair in m_UnitViews)
        {
            CombatUnit unit = pair.Key;
            SpriteRenderer view = pair.Value;

            Vector3 targetPos = GetSlotPosition(unit);
            if(m_MoveCoroutines.ContainsKey(unit))
            {
                StopCoroutine(m_MoveCoroutines[unit]);
            }
            m_MoveCoroutines[unit] = StartCoroutine(LerpToPosition(unit, targetPos));
        }
    }
    private IEnumerator LerpToPosition(CombatUnit unit, Vector3 target)
    {
        SpriteRenderer view = m_UnitViews[unit];
        Vector3 startPos = view.transform.position;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / m_MoveDuration;
            view.transform.position = Vector3.Lerp(startPos, target, t);
            yield return null;
        }
        view.transform.position = target;
        m_MoveCoroutines.Remove(unit);

    }

    private void LogEnemyInfo(CombatUnit unit)
    {
        StatBlock stats = unit.CurrentStats;
        Debug.Log($"[Enemy] {unit.UnitName} | HP:{unit.CurrentHp}/{unit.MaxHp} | " +
                  $"\nDMG:{stats.minDamage}-{stats.maxDamage} | ACC:{stats.accuracyMod} | DODGE:{stats.dodge}" +
                  $"\nDEF:{stats.defense:F0}% | SPD:{stats.speed} | State:{unit.State}");
    }

}
