using UnityEngine;
using System.Collections;

[System.Serializable]
public struct SpriteEntry
{
    public Sprite Sprite;
    public Vector3 Offset;      // local offset 에 더해지는 추가위치
    public float Rotation;      // z 축 회전
    public float Delay;         // pop in 시작 전 대기
    public float Scale;
}


[CreateAssetMenu(fileName = "New Sprite Effect", menuName = "LastKingdom/Combat Effect/Sprite")]
public class SpriteCombatEffect : CombatEffectData
{
    [Header("Sprites")]
    [SerializeField] private SpriteEntry[] m_Entries;
    [SerializeField] private EffectPlayMode m_PlayMode;

    [Header("Timing")]
    [SerializeField] private float m_HoldDuration = 0.2f;
    [SerializeField] private float m_FadeDuration = 0.2f;

    [Header("Scale / PopIn")]
    [SerializeField] private float m_Scale = 1f;
    [SerializeField] private float m_PopInStartMul = 0.6f;
    [SerializeField] private float m_PopInDuration = 0.1f;

    public override Coroutine Play(MonoBehaviour runner, GameObject instance, Transform parent, int sortingOrder, bool flipX)
    {
        instance.SetActive(true);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = LocalOffset;

        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
        if (parentSr != null)
            sr.sortingLayerID = parentSr.sortingLayerID;
        instance.layer = parent.gameObject.layer;
        sr.sortingOrder = sortingOrder;
        sr.flipX = flipX;
        sr.color = Color.white;
        instance.transform.localScale = Vector3.one * m_Scale;
        return runner.StartCoroutine(PlayRoutine(sr, instance, runner));
    }
    private IEnumerator PlayRoutine(SpriteRenderer sr, GameObject instance, MonoBehaviour runner)
    {
        switch (m_PlayMode)
        {
            case EffectPlayMode.Static:
                yield return StaticRoutine(sr); break;
            case EffectPlayMode.Sequential:
                yield return SequentialRoutine(sr); break;
            default:
                yield return PopInRoutine(sr, instance, runner); break;
        }
    }
    private IEnumerator StaticRoutine(SpriteRenderer sr)
    {
        sr.sprite = m_Entries[0].Sprite;
        yield return new WaitForSeconds(m_HoldDuration);
        float elapsed = 0f;
        while (elapsed < m_FadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / m_FadeDuration);
            sr.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        sr.color = Color.clear;
    }
    private IEnumerator SequentialRoutine(SpriteRenderer sr)
    {
        float interval = m_HoldDuration / m_Entries.Length;
        for (int i = 0; i < m_Entries.Length; ++i)
        {
            sr.sprite = m_Entries[i].Sprite;
            float t = 0f;
            while (t < interval)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
        float t2 = 0f;
        while (t2 < m_FadeDuration)
        {
            t2 += Time.deltaTime;
            float k = 1f - (t2 / m_FadeDuration);
            sr.color = new Color(1f, 1f, 1f, k);
            yield return null;
        }
        sr.color = Color.clear;
    }
    private IEnumerator PopInRoutine(SpriteRenderer rootSr, GameObject root, MonoBehaviour runner)
    {
        SpriteRenderer[] srs = new SpriteRenderer[m_Entries.Length];
        srs[0] = rootSr;
        root.transform.localScale = Vector3.one;
        for (int i = 1; i < m_Entries.Length; ++i)
        {
            GameObject child = new GameObject("SpriteEntry");
            child.transform.SetParent(root.transform.parent, false);
            child.layer = root.layer;
            SpriteRenderer childSr = child.AddComponent<SpriteRenderer>();
            childSr.sortingLayerID = rootSr.sortingLayerID;
            childSr.sortingOrder = rootSr.sortingOrder;
            childSr.flipX = rootSr.flipX;
            srs[i] = childSr;
        }

        float maxDelay = 0f;
        for (int i = 0; i < m_Entries.Length; ++i)
        {
            Vector3 baseOffset = LocalOffset;
            runner.StartCoroutine(PopInEntryRoutine(srs[i], m_Entries[i], baseOffset));
            if (m_Entries[i].Delay > maxDelay) maxDelay = m_Entries[i].Delay;
        }

        yield return new WaitForSeconds(maxDelay + m_PopInDuration);
        yield return new WaitForSeconds(m_HoldDuration);

        float elapsed = 0f;
        while (elapsed < m_FadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / m_FadeDuration);
            for (int i = 0; i < srs.Length; ++i)
                srs[i].color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        for (int i = 0; i < srs.Length; ++i)
            srs[i].color = Color.clear;

        for (int i = 1; i < srs.Length; ++i)
            Object.Destroy(srs[i].gameObject);
    }

    private IEnumerator PopInEntryRoutine(SpriteRenderer sr, SpriteEntry entry, Vector3 baseOffset)
    {
        if (entry.Delay > 0f)
            yield return new WaitForSeconds(entry.Delay);

        sr.sprite = entry.Sprite;
        sr.color = Color.white;
        sr.transform.localPosition = baseOffset + entry.Offset;
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, entry.Rotation);

        Vector3 startScale = Vector3.one * (m_Scale * entry.Scale * m_PopInStartMul);
        Vector3 endScale = Vector3.one * (m_Scale * entry.Scale);
        float t = 0f;
        while (t < m_PopInDuration)
        {
            t += Time.deltaTime;
            float k = t / m_PopInDuration;
            float eased = 1f - (1f - k) * (1f - k);
            sr.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            yield return null;
        }
        sr.transform.localScale = endScale;
    }
}
