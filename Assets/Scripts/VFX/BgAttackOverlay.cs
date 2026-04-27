using UnityEngine;
using System.Collections;
public class BgAttackOverlay : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private SpriteRenderer m_Renderer;
    [SerializeField] private Sprite m_NormalSprite;
    [SerializeField] private Sprite m_CritSprite;


    [Header("Flip")]
    [SerializeField] private bool m_NikkeTargetFlipX;
    [SerializeField] private bool m_EnemyTargetFlipX;

    [Header("Timing")]
    [SerializeField] private float m_FadeInDuration = 0.1f;
    [SerializeField] private float m_HoldDuration = 0.3f;
    [SerializeField] private float m_FadeOutDuration = 0.2f;

    [Header("Scale")]
    [SerializeField] private float m_Scale = 1f;

    private Coroutine m_Coroutine;

    public void Show(bool isCrit, bool targetIsNikke)
    {
        m_Renderer.sprite = isCrit ? m_CritSprite : m_NormalSprite;
        m_Renderer.flipX = targetIsNikke ? m_NikkeTargetFlipX : m_EnemyTargetFlipX;
        transform.localScale = Vector3.one * m_Scale;
        m_Renderer.sortingOrder = 20;
        if(targetIsNikke)
            transform.localPosition = new Vector3(-2.8f, 1.3f, 0f);
        else
            transform.localPosition = new Vector3(2.8f, 1.3f, 0f);
        CoroutineHelper.Restart(this, ref m_Coroutine, PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        yield return CoroutineHelper.FadeAlpha(m_Renderer, 0f, 1f, m_FadeInDuration);

        yield return new WaitForSeconds(m_HoldDuration);

        yield return CoroutineHelper.FadeAlpha(m_Renderer, 1f, 0f, m_FadeOutDuration);
    }
    private void OnDisable()
    {
        if(m_Coroutine != null)
            StopCoroutine(m_Coroutine);
        m_Coroutine = null;
        m_Renderer.color = new Color(1f, 1f, 1f, 0f);
    }

}
