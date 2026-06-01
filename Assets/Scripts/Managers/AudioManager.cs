using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

public class AudioManager : Singleton<AudioManager>
{
    private const string k_BgmKey = "BgmVolume";
    private const string k_SfxKey = "SfxVolume";

    [SerializeField] private AudioMixer m_Mixer;
    [SerializeField] private AudioSource m_BgmSource;
    [SerializeField] private AudioSource[] m_SfxPool; // Inspector에서 8개 정도

    private int m_SfxIndex;
    private Coroutine m_FadeRoutine;

    protected override void Awake()
    {
        base.Awake();
        float v = GetBgmVolume();
        ApplyVolume(k_BgmKey, v);
        v = GetSfxVolume();
        ApplyVolume(k_SfxKey, v);

    }

    public void PlayBgm(SoundData sound, float fadeSec = 1f)
    {
        if (sound == null) return;
        if (m_BgmSource.clip == sound.Clip && m_BgmSource.isPlaying) return;
        if (m_FadeRoutine != null)
        {
            StopCoroutine(m_FadeRoutine);
            m_FadeRoutine = null;
        }
        m_FadeRoutine = StartCoroutine(FadeBgm(sound, fadeSec));
    }
    public void StopBgm(float fadeSec = 1f)
    {
        if (m_FadeRoutine != null)
        {
            StopCoroutine(m_FadeRoutine);
            m_FadeRoutine = null;
        }

        // sound = null → FadeBgm 내부에서 페이드 아웃 후 Stop만 하고 yield break
        m_FadeRoutine = StartCoroutine(FadeBgm(null, fadeSec));
    }
    public void PlaySfx(SoundData sound)
    {
        if (sound == null || sound.Clip == null) return;
        AudioSource source = m_SfxPool[m_SfxIndex];
        source.clip = sound.Clip;
        source.volume = sound.Volume;

        source.Play();
        m_SfxIndex = (m_SfxIndex + 1) % m_SfxPool.Length;
    }
    public void SetBgmVolume(float v)
    {
        ApplyVolume(k_BgmKey, v);
        PlayerPrefs.SetFloat(k_BgmKey, v);
    }
    public void SetSfxVolume(float v)
    {
        ApplyVolume(k_SfxKey, v);
        PlayerPrefs.SetFloat(k_SfxKey, v);
    }
    public float GetBgmVolume()
    {
        return PlayerPrefs.GetFloat(k_BgmKey, 1f);
    }
    public float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat(k_SfxKey, 1f);
    }
    private void ApplyVolume(string param, float v)
    {
        float dB = 0f;
        if (v > 0)
            dB = Mathf.Log10(v) * 20f;
        else dB = -80f;
        m_Mixer.SetFloat(param, dB);
            
    }
    private IEnumerator FadeBgm(SoundData sound, float fadeSec)
    {
        // 페이드 아웃
        float startVol = m_BgmSource.volume;
        float elapsed = 0f;
        float half = fadeSec * 0.5f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            m_BgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / half);
            yield return null;
        }

        m_BgmSource.volume = 0f;
        m_BgmSource.Stop();

        // StopBgm 경우 (sound == null) 여기서 종료
        if (sound == null)
        {
            m_FadeRoutine = null;
            yield break;
        }

        // 클립 교체 후 재생 시작
        m_BgmSource.clip = sound.Clip;
        m_BgmSource.loop = sound.Loop;
        m_BgmSource.volume = 0f;
        m_BgmSource.Play();

        // 페이드 인
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            m_BgmSource.volume = Mathf.Lerp(0f, sound.Volume, elapsed / half);
            yield return null;
        }

        m_BgmSource.volume = sound.Volume;
        m_FadeRoutine = null;
    }


}
