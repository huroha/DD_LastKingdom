using UnityEngine;



public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour      // where T 는 앞으로 T자리에는 Monobehaviour를 상속받은 유니티컴포넌트만 올것이라고 확정
{
    private static T m_instance;
    private static bool m_isQuitting;

    public static T Instance
    {
        get
        {
            if (m_isQuitting)
                return null;

            if (m_instance == null)
            {
                //씬에 해당 컴포넌트 <T>가 있는지 조사. 없어야 생성한다.
                m_instance = FindFirstObjectByType<T>();
                if (m_instance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name);
                    m_instance = go.AddComponent<T>();
                }
            }
            return m_instance;
        }
    }

    protected virtual void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // DontDestroyOnLoad는 루트 오브젝트에만 적용된다.
        // 자식으로 배치된 경우 자동으로 루트로 이동하고 경고를 출력한다.
        if (transform.parent != null)
        {
            Debug.LogWarning(
                $"[Singleton] {typeof(T).Name} is not a root GameObject. " +
                $"Detaching from parent '{transform.parent.name}' to apply DontDestroyOnLoad correctly.",
                gameObject
            );
            transform.SetParent(null);
        }

        m_instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        // 앱 종료 외의 시점에 매니저가 파괴되는 것은 설계 위반이다.
        if (!m_isQuitting)
        {
            Debug.LogError(
                $"[Singleton] {typeof(T).Name} was destroyed unexpectedly. " +
                $"Singleton managers must persist until application quit.",
                gameObject
            );
        }

        // 중복 인스턴스의 Destroy 시에는 _instance를 건드리지 않는다.
        if (m_instance == this as T)
            m_instance = null;
    }

    protected virtual void OnApplicationQuit()
    {
        m_isQuitting = true;
    }
}

