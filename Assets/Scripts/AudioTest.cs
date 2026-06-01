using UnityEngine;

public class AudioTest : MonoBehaviour
{
    [SerializeField] private SoundData m_BgmTest;
    [SerializeField] private SoundData m_SfxTest;

    private void Start()
    {
        AudioManager.Instance.PlayBgm(m_BgmTest);

    }
    private void Update()
    {

        // space 로 테스트
        if (Input.GetKeyDown(KeyCode.Space))
            AudioManager.Instance.PlaySfx(m_SfxTest);

        if (Input.GetKeyDown(KeyCode.B))
            AudioManager.Instance.StopBgm();

    }
}
