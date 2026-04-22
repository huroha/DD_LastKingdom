using UnityEngine;

public class HaloRotator : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float m_RotationSpeed = 15f;

    private Quaternion m_InitialRotation;

    private void Awake()
    {
        m_InitialRotation = transform.localRotation;
    }
    private void OnEnable()
    {
        transform.localRotation = m_InitialRotation;
    }
    private void Update()
    {
        float spin = m_RotationSpeed * Time.deltaTime;
        transform.Rotate(0f,0f, spin);
    }
}
