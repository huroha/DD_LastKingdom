
using UnityEngine;

public class RoundAnimEvent : MonoBehaviour
{
    [SerializeField] private CombatHUD m_CombatHUD;
    public void ApplyRoundText() =>m_CombatHUD.ApplyRoundText();
}
