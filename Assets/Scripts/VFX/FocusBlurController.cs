using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FocusBlurController : MonoBehaviour
{
    [SerializeField] private FullScreenPassRendererFeature m_BlurFeature;
    [SerializeField] private Material m_BlurMaterial;

    private static readonly int BlurStrengthId = Shader.PropertyToID("_BlurStrength");

    public void SetBlurStrength(float strength)
    {
        m_BlurFeature.SetActive(strength > 0f);
        m_BlurMaterial.SetFloat(BlurStrengthId, strength);
    }
}
