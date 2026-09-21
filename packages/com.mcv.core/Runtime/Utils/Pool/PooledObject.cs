using UnityEngine;

namespace MCV_Module.Utils.Pool
{
    /// <summary>
    /// 池内实例的归属标记（由 <see cref="GameObjectPool"/> 自动挂在实例根节点上）。
    ///
    /// 作用：拿到一个 GameObject 就能反查它属于哪个池、哪个 key，避免「还错池」「重复归还」。
    /// 注意：由池自动维护，**不要手工添加/删除**；实例被销毁时标记会自行摘除所属关系。
    /// </summary>
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
