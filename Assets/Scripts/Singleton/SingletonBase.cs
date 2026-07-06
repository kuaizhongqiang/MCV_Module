using System.Collections;
using UnityEngine;

/// <summary>
/// 单例抽象基类（非泛型）
///
/// 职责：
///   - 只提供框架级生命周期接口：isInit 标记、DelayInit 抽象协程
///   - 不包含单例实例管理，不处理 DontDestroyOnLoad
///   - SingletonGlobalMgr&lt;T&gt; 在此之上实现完整的泛型单例模式
/// </summary>
public abstract class SingletonBase : MonoBehaviour
{
    /// <summary>是否已完成延迟初始化</summary>
    protected bool isInit = false;
    public bool IsInit
    {
        get { return isInit; }
    }

    /// <summary>
    /// 延迟初始化协程，子类必须实现各自的初始化逻辑
    /// </summary>
    protected abstract IEnumerator DelayInit();

    /// <summary>
    /// 统一在 Start 中触发延迟初始化
    /// 子类如需自定义时机可重写，但一般不需要
    /// </summary>
    protected virtual void Start()
    {
        if (!isInit)
            StartCoroutine(DelayInit());
    }
}
