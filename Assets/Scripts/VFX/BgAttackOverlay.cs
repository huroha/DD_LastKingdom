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
        CoroutineHelper.Restart(this, ref m_Coroutine, PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float elapsed = 0f;
        while (elapsed < m_FadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / m_FadeInDuration;
            m_Renderer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        m_Renderer.color = new Color(1f, 1f, 1f, 1f);

        yield return new WaitForSeconds(m_HoldDuration);

        elapsed = 0f;
        while (elapsed < m_FadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / m_FadeOutDuration);
            m_Renderer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        m_Renderer.color = new Color(1f, 1f, 1f, 0f);
    }
    private void OnDisable()
    {
        if(m_Coroutine != null)
            StopCoroutine(m_Coroutine);
        m_Coroutine = null;
        m_Renderer.color = new Color(1f, 1f, 1f, 0f);
    }

}
