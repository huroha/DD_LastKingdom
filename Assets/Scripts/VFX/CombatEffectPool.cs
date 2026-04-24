using UnityEngine;
using System.Collections.Generic;

public class CombatEffectPool : MonoBehaviour
{
    [SerializeField] private Transform m_WorldRoot;
    [SerializeField] private int m_InitialSpriteCount = 16;

    private Queue<GameObject> m_SpriteQueue;
    private Dictionary<int, Queue<GameObject>> m_AnimatorQueues;

    public Transform WorldRoot => m_WorldRoot;

    private void Awake()
    {
        m_SpriteQueue = new Queue<GameObject>();
        m_AnimatorQueues = new Dictionary<int, Queue<GameObject>>();
        for (int i = 0; i < m_InitialSpriteCount; ++i)
        {
            m_SpriteQueue.Enqueue(CreateSpriteGo());
        }
    }
    public GameObject Borrow(CombatEffectData effect)
    {
        if (effect is SpriteCombatEffect)
        {
            if (m_SpriteQueue.Count > 0)
                return m_SpriteQueue.Dequeue();
            else
                return CreateSpriteGo();
        }
        else if (effect is AnimatorCombatEffect animEffect)
        {
            int id = animEffect.Prefab.GetInstanceID();
            if (m_AnimatorQueues.TryGetValue(id, out Queue<GameObject> q) && q.Count > 0)
                return q.Dequeue();
            else
            {
                GameObject go = Instantiate(animEffect.Prefab, transform);
                go.SetActive(false);
                return go;
            }
        }
        return null;
    }
    public void Return(GameObject instance, CombatEffectData effect)
    {
        instance.SetActive(false);
        instance.transform.SetParent(transform, false);
        if (effect is SpriteCombatEffect)
        {
            SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
            sr.sprite = null;
            sr.color = Color.white;
            m_SpriteQueue.Enqueue(instance);
        }
        else if (effect is AnimatorCombatEffect)
        {
            int id = ((AnimatorCombatEffect)effect).Prefab.GetInstanceID();
            if (!m_AnimatorQueues.ContainsKey(id))
                m_AnimatorQueues[id] = new Queue<GameObject>();
            m_AnimatorQueues[id].Enqueue(instance);
        }
    }
    private GameObject CreateSpriteGo()
    {
        GameObject go = new GameObject("SpriteEffect");
        go.transform.SetParent(transform, false);
        go.AddComponent<SpriteRenderer>();
        go.SetActive(false);
        return go;
    }
}

