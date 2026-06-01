using UnityEngine;

[CreateAssetMenu(fileName = "New Sound", menuName = "LastKingdom/Sound Data")]
public class SoundData : ScriptableObject
{
    [SerializeField] private AudioClip m_Clip;
    [SerializeField] private float m_Volume = 1f;
    [SerializeField] private bool m_Loop;

    public AudioClip Clip => m_Clip;
    public float Volume => m_Volume;
    public bool Loop => m_Loop;
}
