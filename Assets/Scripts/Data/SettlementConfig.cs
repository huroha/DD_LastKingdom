using UnityEngine;

[CreateAssetMenu(fileName = "SettlementConfig", menuName = "LastKingdom/Settlement Config")]
public class SettlementConfig : ScriptableObject
{
    [Header("Relic 단가 (index  = (int)RelicType")]
    [SerializeField] private int[] m_RelicCredits = new int[4];

    [Header("SupplyItem")]
    [SerializeField, Range(0f, 1f)] private float m_SupplySellRatio = 0.1f;

    public float SupplySellRatio => m_SupplySellRatio;
    public int RelicCredit(RelicType type)
    {
        int i = (int)type;
        if (m_RelicCredits == null || i < 0 || i >= m_RelicCredits.Length) return 0;
        return m_RelicCredits[i];
    }
}
