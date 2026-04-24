using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName ="New Animator Effect", menuName = "LastKingdom/Combat Effect/Animator")]
public class AnimatorCombatEffect : CombatEffectData
{
    [Header("Prefab")]
    [SerializeField] private GameObject m_Prefab;
    [SerializeField] private float m_LifeTime = 1f;

    public GameObject Prefab => m_Prefab;

    public override Coroutine Play(MonoBehaviour runner, GameObject instance, Transform parent, int sortingOrder, bool flipX)
    {
        instance.SetActive(true);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
            if (parentSr != null)
                sr.sortingLayerID = parentSr.sortingLayerID;
            sr.sortingOrder = sortingOrder;
            sr.flipX = flipX;
        }
        instance.layer = parent.gameObject.layer;
        return runner.StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        yield return new WaitForSeconds(m_LifeTime);
    }
}
