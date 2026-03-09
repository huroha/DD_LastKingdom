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

        Destroy(m_UnitViews[e.Unit].gameObject);
        m_UnitViews.Remove(e.Unit);

        m_MoveCoroutines.Remove(e.Unit);
    }

    private SpriteRenderer CreateUnitView(CombatUnit unit, Vector3 pos)
    {
        GameObject go = new GameObject(unit.UnitName);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

        if(unit.UnitType == CombatUnitType.Nikke)
        {
            sr.sprite = unit.NikkeData.PortraitSprite;
        }
        else if(unit.UnitType == CombatUnitType.Enemy)
        {
            sr.sprite = unit.EnemyData.Sprite;
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

}
