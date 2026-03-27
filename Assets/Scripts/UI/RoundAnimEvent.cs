
using UnityEngine;

public class RoundAnimEvent : MonoBehaviour
{
    [SerializeField] private CombatHUD m_CombatHUD;
    public void ApplyRoundText()
    {
        if (m_CombatHUD == null) return;
        m_CombatHUD.ApplyRoundText();
    }

}
