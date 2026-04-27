using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName ="New Animator Effect", menuName = "LastKingdom/Combat Effect/Animator")]
public class AnimatorCombatEffect : CombatEffectData
{
    [Header("Prefab")]
    [SerializeField] private GameObject m_Prefab;
    [SerializeField] private float m_LifeTime = 1f;

    private WaitForSeconds m_WaitLifeTime;

    public GameObject Prefab => m_Prefab;

    public override Coroutine Play(MonoBehaviour runner, GameObject instance, Transform parent, int sortingOrder, bool flipX)
    {
        instance.SetActive(true);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = LocalOffset;
        SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
            if (parentSr != null)
                sr.sortingLayerID = parentSr.sortingLayerID;
            sr.sortingOrder = sortingOrder;
            sr.flipX = flipX;
        }
        SetLayerRecursively(instance, parent.gameObject.layer);
        return runner.StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (m_WaitLifeTime == null) m_WaitLifeTime = new WaitForSeconds(m_LifeTime);
        yield return m_WaitLifeTime;
    }
    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; ++i)
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
    }
}
