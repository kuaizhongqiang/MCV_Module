using UnityEngine;

namespace MCV_Module.Utils.Pool
{
    // WHY: 由池自动添加并维护，手工增删会让 Spawn 归属反查失败；实例销毁时 OnDestroy 会摘除所属关系。
    /// <summary>池内实例的归属标记（由 GameObjectPool 自动挂到实例根节点），用于反查所属池与 key，避免还错池、重复归还。</summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        /// <summary>所属池（实例被销毁后置空）。</summary>
        public GameObjectPool Owner { get; private set; }

        /// <summary>所属池的 key（通常是包配置 id）。</summary>
        public string Key { get; private set; }

        /// <summary>出池/回池回调缓存（创建实例时扫描一次）。框架内部使用。</summary>
        internal IPoolable[] Callbacks { get; set; }

        /// <summary>绑定归属（框架内部调用）。</summary>
        internal void Bind(GameObjectPool owner, string key)
        {
            Owner = owner;
            Key = key;
        }

        /// <summary>是否归属于指定池。</summary>
        public bool BelongsTo(GameObjectPool pool)
        {
            return pool != null && Owner == pool;
        }

        void OnDestroy()
        {
            Owner = null;
            Callbacks = null;
        }
    }
}
