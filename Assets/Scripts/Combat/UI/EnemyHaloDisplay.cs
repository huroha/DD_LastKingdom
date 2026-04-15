using UnityEngine;

public class EnemyHaloDisplay : MonoBehaviour , IStunHaloDisplay
{
    [SerializeField] private CanvasGroup m_StunHalo;


    private void Awake()
    {
        m_StunHalo.alpha = 0;
        m_StunHalo.gameObject.SetActive(false);
    }

    public void ShowStunHalo()
    {
        m_StunHalo.gameObject.SetActive(true);
        m_StunHalo.alpha = 1;
    }
    public void HideStunHalo()
    {
        m_StunHalo.alpha = 0;
        m_StunHalo.gameObject.SetActive(false);
    }
}
