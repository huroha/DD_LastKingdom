using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class AfflictionNarrationPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private CanvasGroup m_Root;

    [Header("Banner")]
    [SerializeField] private CanvasGroup m_BannerGroup;
    [SerializeField] private TMP_Text m_NikkeNameText;
    [SerializeField] private TMP_Text m_NarrationText;
    [SerializeField] private string m_NarrationLine = "의 의지가 시험받고 있습니다...";

    [Header("Art")]
    [SerializeField] private CanvasGroup m_ArtGroup;
    [SerializeField] private Image m_ArtImage;
    [SerializeField] private TMP_Text m_TypeNameText;


    [Header("Timing")]
    [SerializeField] private float m_FadeInDuration = 0.3f;
    [SerializeField] private float m_BannerHoldDuration = 1.0f;
    [SerializeField] private float m_TransitionDuration = 0.3f;
    [SerializeField] private float m_ArtHoldDuration = 1.2f;
    [SerializeField] private float m_FadeOutDuration = 0.4f;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        m_Root.alpha = 0;
        m_Root.interactable = false;
        m_Root.blocksRaycasts = false;
        m_BannerGroup.alpha = 0;
        m_ArtGroup.alpha = 0;
    }

    public IEnumerator Play(PendingEblaResolution pending)
    {
        IsPlaying = true;

        m_NikkeNameText.text = pending.Unit.UnitName;
        m_NarrationText.text = m_NarrationLine;

        // 타입별 분기
        if (pending.ResolutionType == EblaResolutionType.Afflicted)
        {
            m_ArtImage.sprite = pending.Unit.NikkeData.AfflictionArt;
            m_TypeNameText.text = pending.AfflictionType.DisplayName;
        }
        else
        {
            m_ArtImage.sprite = pending.Unit.NikkeData.VirtueArt;
            m_TypeNameText.text = pending.VirtueType.DisplayName;
        }

        // 초기 알파 리셋
        m_BannerGroup.alpha = 0f;
        m_ArtGroup.alpha = 0f;
        m_Root.blocksRaycasts = true;

        // 루트 + 배너 fadein
        yield return FadeCanvasGroup(m_Root, 0f, 1f, m_FadeInDuration);
        yield return FadeCanvasGroup(m_BannerGroup, 0f, 1f, m_FadeInDuration);

        // 배너 유지
        yield return new WaitForSeconds(m_BannerHoldDuration);

        // 배너 -> 아트로 cross fade
        StartCoroutine(FadeCanvasGroup(m_BannerGroup, 1f, 0f, m_TransitionDuration));
        yield return FadeCanvasGroup(m_ArtGroup, 0f, 1f, m_TransitionDuration);

        // 아트 유지
        yield return new WaitForSeconds(m_ArtHoldDuration);

        // 전체 fadeout
        yield return FadeCanvasGroup(m_Root, 1f, 0f, m_FadeOutDuration);

        // 다음 재생을 위한 리셋
        m_BannerGroup.alpha = 0f;
        m_ArtGroup.alpha = 0f;
        m_Root.blocksRaycasts = false;
        IsPlaying = false;
    }
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from , float to, float duration)
    {
        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        IsPlaying = false;
        if (m_Root != null)
        {
            m_Root.alpha = 0f;
            m_Root.blocksRaycasts = false;
        }
    }
}
