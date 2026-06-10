using UnityEngine;

public class DungeonBgView : MonoBehaviour
{
    private enum  BgKind {  Combat, Settle }

    [SerializeField] private BgKind m_Kind;
    [SerializeField] private SpriteRenderer m_Renderer;

    private void Awake()
    {
        ExpeditionManager em = ExpeditionManager.Instance;
        if (em == null || em.Dungeon == null) return;

        DungeonData d = em.Dungeon;
        m_Renderer.sprite = (m_Kind == BgKind.Combat) ? d.CombatBg : d.SettleBg;
    }
}
