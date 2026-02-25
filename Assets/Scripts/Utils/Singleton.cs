using UnityEngine;


/// <summary>
/// MonoBehaviour 기반 제네릭 싱글톤 베이스 클래스.
/// DontDestroyOnLoad 적용, 씬 전환 시 중복 인스턴스 자동 제거.
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isQuitting;

    public static T Instance
    {
        get
        {
            if (_isQuitting)
                return null;

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
                if (_instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
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

        _instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        // 앱 종료 외의 시점에 매니저가 파괴되는 것은 설계 위반이다.
        if (!_isQuitting)
        {
            Debug.LogError(
                $"[Singleton] {typeof(T).Name} was destroyed unexpectedly. " +
                $"Singleton managers must persist until application quit.",
                gameObject
            );
        }

        // 중복 인스턴스의 Destroy 시에는 _instance를 건드리지 않는다.
        if (_instance == this as T)
            _instance = null;
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}

