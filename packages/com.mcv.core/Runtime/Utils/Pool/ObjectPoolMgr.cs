using System.Collections.Generic;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Utils.Pool
{
    /// <summary>
    /// 对象池注册表：按 key（通常是包配置 id）管理多个 <see cref="GameObjectPool"/>。
    ///
    /// 定位：**普通工具类，不是全局管理器** —— 不继承 <c>SingletonGlobalMgr</c>、不参与 Setup 启动链。
    /// 谁需要谁持有：<c>InstManagerBase</c> 每人持有一个（生命周期随管理器），
    /// <c>GlobalAssetsMgr</c> 持有一个全局共享的（供非 Inst* 场合直接取用）。
    ///
    /// 层级：Root（池容器根）→ 每个 key 一个子容器 → 该 key 的闲置实例。
    /// 取用实例时把它挂到业务父节点下，归还时挂回子容器并失活。
    /// </summary>
    public sealed class ObjectPoolMgr
    {
        readonly Dictionary<string, GameObjectPool> m_Pools = new Dictionary<string, GameObjectPool>();
        readonly Transform m_Root;
        readonly bool m_OwnsRoot;

        /// <summary>池容器根节点（每个 key 会在其下建一个子容器）。</summary>
        public Transform Root => m_Root;

        /// <summary>已创建的池数量。</summary>
        public int PoolCount => m_Pools.Count;

        /// <summary>全部池的 key。</summary>
        public IEnumerable<string> Keys => m_Pools.Keys;

        /// <param name="root">池容器根；为空时自建一个（<see cref="Dispose"/> 会一并销毁）。</param>
        public ObjectPoolMgr(Transform root = null)
        {
            if (root != null)
            {
                m_Root = root;
                m_OwnsRoot = false;
            }
            else
            {
                var go = new GameObject("__ObjectPools");
                m_Root = go.transform;
                m_OwnsRoot = true;
            }
        }

        #region 池管理
        /// <summary>取池；不存在返回 null。</summary>
        public GameObjectPool Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            m_Pools.TryGetValue(key, out var pool);
            return pool;
        }

        /// <summary>池是否已创建。</summary>
        public bool Contains(string key) => Get(key) != null;

        /// <summary>
        /// 建池（已存在则直接返回，不覆盖、不重复预热）。
        /// </summary>
        /// <param name="key">池的键（通常是包配置 id）</param>
        /// <param name="prefab">池管理的预制体</param>
        /// <param name="maxIdleCount">闲置上限，&lt;=0 不限</param>
        /// <param name="preload">建池后立即预热的实例数</param>
        /// <param name="instantiate">实例创建工厂（可为空，默认 Object.Instantiate）</param>
        public GameObjectPool CreatePool(string key, GameObject prefab, int maxIdleCount = 0,
                                        int preload = 0, PoolInstantiateFunc instantiate = null)
        {
            if (string.IsNullOrEmpty(key) || prefab == null) return null;
            if (m_Pools.TryGetValue(key, out var exists)) return exists;

            var container = new GameObject(GameObjectPool.Sanitize(key));
            container.transform.SetParent(m_Root, false);

            var pool = new GameObjectPool(key, prefab, container.transform, maxIdleCount, instantiate);
            m_Pools[key] = pool;

            if (preload > 0) pool.Preload(preload);
            return pool;
        }

        /// <summary>销毁指定池（含其实例与容器节点）。</summary>
        public bool DestroyPool(string key, bool destroyInstances = true)
        {
            if (!m_Pools.TryGetValue(key, out var pool)) return false;

            m_Pools.Remove(key);
            var root = pool.Root;
            pool.Destroy(destroyInstances);                     // 池自建的根由池自己销毁
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);   // 本注册表建的容器由这里销毁
            return true;
        }

        /// <summary>销毁全部池（保留容器根，之后仍可继续建池）。返回销毁的池数量。</summary>
        public int DestroyAllPools(bool destroyInstances = true)
        {
            if (m_Pools.Count == 0) return 0;

            var keys = new List<string>(m_Pools.Keys);
            for (int i = 0; i < keys.Count; i++) DestroyPool(keys[i], destroyInstances);
            return keys.Count;
        }

        /// <summary>释放本注册表：销毁全部池，并销毁自建的容器根（自建时）。</summary>
        public void Dispose(bool destroyInstances = true)
        {
            DestroyAllPools(destroyInstances);

            if (m_OwnsRoot && m_Root != null) UnityEngine.Object.Destroy(m_Root.gameObject);
        }
        #endregion

        #region 取用 / 归还
        /// <summary>取一个实例（parent 为空时挂在池容器下，便于之后统一回收）。</summary>
        public GameObject Spawn(string key, Transform parent = null)
        {
            var pool = Get(key);
            if (pool == null)
            {
                Log.Error($"[ObjectPoolMgr] 池不存在：{key}（请先 CreatePool，或走 GlobalAssetsMgr / InstManager 的 SpawnAsync）");
                return null;
            }
            return pool.Spawn(parent != null ? parent : pool.Root);
        }

        /// <summary>取一个实例并摆位。</summary>
        public GameObject Spawn(string key, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            var pool = Get(key);
            if (pool == null)
            {
                Log.Error($"[ObjectPoolMgr] 池不存在：{key}（请先 CreatePool，或走 GlobalAssetsMgr / InstManager 的 SpawnAsync）");
                return null;
            }
            return pool.Spawn(parent != null ? parent : pool.Root, localPosition, localRotation);
        }

        /// <summary>归还实例（按实例上的 <see cref="PooledObject"/> 标记反查所属池）。</summary>
        public bool Despawn(GameObject instance)
        {
            if (instance == null) return false;

            var marker = instance.GetComponent<PooledObject>();
            if (marker == null || marker.Owner == null)
            {
                Log.Warning($"[ObjectPoolMgr] {instance.name} 不是池内对象，归还已忽略");
                return false;
            }
            return marker.Owner.Despawn(instance);
        }

        /// <summary>归还指定池的全部活跃实例（保留池）。</summary>
        public int DespawnAll(string key)
        {
            var pool = Get(key);
            return pool != null ? pool.DespawnAll() : 0;
        }

        /// <summary>归还全部池的活跃实例（保留池）。</summary>
        public int DespawnAll()
        {
            int count = 0;
            foreach (var kvp in m_Pools) count += kvp.Value.DespawnAll();
            return count;
        }
        #endregion

        #region 查询
        /// <summary>指定池的活跃实例数；池不存在返回 0。</summary>
        public int ActiveCount(string key)
        {
            var pool = Get(key);
            return pool != null ? pool.ActiveCount : 0;
        }

        /// <summary>指定池的闲置实例数；池不存在返回 0。</summary>
        public int IdleCount(string key)
        {
            var pool = Get(key);
            return pool != null ? pool.IdleCount : 0;
        }

        /// <summary>活跃实例总数（所有池）。</summary>
        public int ActiveCount()
        {
            int count = 0;
            foreach (var kvp in m_Pools) count += kvp.Value.ActiveCount;
            return count;
        }
        #endregion
    }
}
