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

    private WaitForSeconds m_WaitHold;
    private WaitForSeconds m_WaitPopInTotal;    // maxDelay + m_PopinDuration
    private WaitForSeconds[] m_WaitDelays;    // entry별 delay


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
        return runner.StartCoroutine(PlayRoutine(sr, instance, runner));
    }
    private IEnumerator PlayRoutine(SpriteRenderer sr, GameObject instance, MonoBehaviour runner)
    {
        sr.transform.localScale = m_PlayMode == EffectPlayMode.PopIn ? Vector3.one : Vector3.one * m_Scale;
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
        if (m_WaitHold == null) m_WaitHold = new WaitForSeconds(m_HoldDuration);
        yield return m_WaitHold;

        yield return CoroutineHelper.FadeAlpha(sr, 1f, 0f, m_FadeDuration);
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
        yield return CoroutineHelper.FadeAlpha(sr, 1f, 0f, m_FadeDuration);
    }
    private IEnumerator PopInRoutine(SpriteRenderer rootSr, GameObject root, MonoBehaviour runner)
    {
        SpriteSlotCache cache = root.GetComponent<SpriteSlotCache>();
        if (cache == null) cache = root.AddComponent<SpriteSlotCache>();
        cache.Setup(rootSr, m_Entries.Length,runner);

        if (m_WaitDelays == null || m_WaitDelays.Length != m_Entries.Length)
        {
            m_WaitDelays = new WaitForSeconds[m_Entries.Length];
            for (int i = 0; i < m_Entries.Length; ++i)
                m_WaitDelays[i] = new WaitForSeconds(m_Entries[i].Delay);
        }

        float maxDelay = 0f;
        for (int i = 0; i < m_Entries.Length; ++i)
        {
            Vector3 baseOffset = LocalOffset;
            Coroutine co = runner.StartCoroutine(PopInEntryRoutine(cache.GetSlot(i), m_Entries[i], baseOffset, i));
            cache.StoreEntry(i, co);
            if (m_Entries[i].Delay > maxDelay) maxDelay = m_Entries[i].Delay;
        }

        if (m_WaitPopInTotal == null) m_WaitPopInTotal = new WaitForSeconds(maxDelay + m_PopInDuration);
        yield return m_WaitPopInTotal;
        if (m_WaitHold == null) m_WaitHold = new WaitForSeconds(m_HoldDuration);
        yield return m_WaitHold;

        yield return CoroutineHelper.FadeAlpha(cache.Slots, 1f, 0f, m_FadeDuration);
        
        for (int i = 0; i < m_Entries.Length; ++i)
            cache.GetSlot(i).color = Color.clear;
    }

    private IEnumerator PopInEntryRoutine(SpriteRenderer sr, SpriteEntry entry, Vector3 baseOffset, int index)
    {
        if (entry.Delay > 0f)
            yield return m_WaitDelays[index];

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
            float eased = CoroutineHelper.OutQuad(k);
            sr.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            yield return null;
        }
        sr.transform.localScale = endScale;
    }
    private void OnValidate()
    {
        if (m_Entries == null) return;
        for (int i=0; i< m_Entries.Length; ++i)
        {
            if (m_Entries[i].Scale == 0f)
                Debug.LogWarning($"{name}: Entries[{i}].Scale = 0 - sprite가 보이지않음");
        }
    }
}
