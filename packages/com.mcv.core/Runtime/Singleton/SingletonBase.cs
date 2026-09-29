using System.Collections;
using UnityEngine;

// WHY: 本类只管生命周期（isInit / DelayInit），不含实例管理与 DontDestroyOnLoad；实例部分由 SingletonGlobalMgr<T> 实现，勿上移到此处。
/// <summary>单例抽象基类（非泛型）：只提供框架级生命周期接口（isInit 标记、DelayInit 抽象协程）。</summary>
namespace MCV_Module.Singleton
{
    public abstract class SingletonBase : MonoBehaviour
    {
        /// <summary>是否已完成延迟初始化</summary>
        protected bool isInit = false;
        public bool IsInit
        {
            get { return isInit; }
        }

        /// <summary>延迟初始化协程，子类必须实现各自的初始化逻辑。</summary>
        protected abstract IEnumerator DelayInit();

        /// <summary>统一在 Start 中触发延迟初始化；子类一般无需重写。</summary>
        protected virtual void Start()
        {
            if (!isInit)
                StartCoroutine(DelayInit());
        }
    }

}
