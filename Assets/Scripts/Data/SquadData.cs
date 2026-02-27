using UnityEngine;

[CreateAssetMenu(fileName = "New Squad", menuName = "LastKingdom/Squad Data")]
public class SquadData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_SquadName;
    [SerializeField] private Sprite m_SquadIcon;

    [Header("Party Display")]
    [SerializeField] private string m_PartyName;        // 조합 충족 시 표시되는 파티 명칭
    [SerializeField] private int m_RequiredCount = 2;   // 파티명 표시 최소 인원

    public string SquadName => m_SquadName;
    public Sprite SquadIcon => m_SquadIcon;
    public string PartyName => m_PartyName;
    public int RequiredCount => m_RequiredCount;
}