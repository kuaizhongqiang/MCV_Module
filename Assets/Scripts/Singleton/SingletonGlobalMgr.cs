using UnityEngine;

/// <summary>
/// 全局管理器泛型单例基类
///
/// 职责：
///   - 全局唯一单例（线程安全）
///   - DontDestroyOnLoad 跨场景持留
///   - 延迟初始化（通过 SingletonBase.DelayInit）
///
/// 用法：
///   public class MyManager : SingletonGlobalMgr&lt;MyManager&gt;
///   {
///       protected override IEnumerator DelayInit()
///       {
///           // 初始化逻辑
///           yield break;
///       }
///   }
///
///   // 访问
///   MyManager.Instance.DoSomething();
///
/// 注意：
///   - FindObjectsByType&lt;T&gt; 使用具体类型 T 查找，不会混淆不同子类
///   - 每个具体类型独立持有自己的静态实例，互不干扰
/// </summary>
public abstract class SingletonGlobalMgr<T> : SingletonBase where T : SingletonGlobalMgr<T>
{
    private static T s_Instance;
    private static readonly object s_Lock = new object();
    private static bool s_AppQuitting;

    /// <summary>
    /// 全局唯一实例（线程安全，自动创建）
    /// </summary>
    public static T Instance
    {
        get
        {
            if (s_AppQuitting)
            {
                Debug.LogWarning($"[SingletonGlobalMgr] 实例 '{typeof(T)}' 已在退出时销毁。返回 null。");
                return null;
            }

            lock (s_Lock)
            {
                if (s_Instance == null)
                {
                    var found = FindObjectsByType<T>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None);

                    for (int i = 0; i < found.Length; i++)
                    {
                        if (found[i] != null)
                        {
                            s_Instance = found[i];
                            break;
                        }
                    }

                    if (s_Instance == null)
                    {
                        var go = new GameObject($"{typeof(T).Name} (Singleton)");
                        s_Instance = go.AddComponent<T>();
                    }

                    DontDestroyOnLoad(s_Instance.gameObject);
                }
            }

            return s_Instance;
        }
    }

    /// <summary>单例是否存在</summary>
    public static bool Exists => s_Instance != null;

    /// <summary>安全获取实例（退出时不触发创建）</summary>
    public static T SafeInstance => s_AppQuitting ? null : s_Instance;

    // ── 生命周期 ──────────────────────────────────────────────

    /// <summary>
    /// Awake 中注册单例，确保只有一份实例，
    /// 并设置 DontDestroyOnLoad
    /// </summary>
    protected virtual void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (s_Instance != this)
        {
            Debug.LogWarning($"[SingletonGlobalMgr] {typeof(T)} 的重复实例，已销毁。");
            Destroy(gameObject);
        }
    }

    /// <summary>应用退出时抑制 Instance 访问</summary>
    protected virtual void OnApplicationQuit()
    {
        s_AppQuitting = true;
    }

    /// <summary>自身销毁时标记退出状态</summary>
    protected virtual void OnDestroy()
    {
        if (s_Instance == this)
            s_AppQuitting = true;
    }
}
