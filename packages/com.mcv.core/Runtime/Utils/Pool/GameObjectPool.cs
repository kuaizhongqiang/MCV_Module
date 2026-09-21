using System;
using System.Collections.Generic;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Utils.Pool
{
    /// <summary>
    /// 池内实例的创建工厂（默认 <c>Object.Instantiate(prefab, parent)</c>）。
    ///
    /// 注入它可以让资源管线接管实例化 —— 例如 <c>GlobalAddressableMgr.InstantiatePrefab</c>
    /// 会在创建时登记「实例 → 包配置 id」，卸载资源包时就能把相关实例一起销毁。
    /// </summary>
    /// <param name="key">池的键（通常是包配置 id）</param>
    /// <param name="prefab">池管理的预制体</param>
    /// <param name="parent">创建时的父节点（池的闲置容器）</param>
    public delegate GameObject PoolInstantiateFunc(string key, GameObject prefab, Transform parent);

    /// <summary>
    /// 单个预制体的 GameObject 对象池。
    ///
    /// 语义：
    ///   - <see cref="Spawn(Transform)"/> 取用：优先复用闲置实例（零 Instantiate、零磁盘 IO），池空则新建；
    ///   - <see cref="Despawn"/> 归还：先回调 <see cref="IPoolable.OnDespawn"/>，再挂回闲置容器并失活；
    ///   - 闲置数量超过 <see cref="MaxIdleCount"/> 时归还即销毁，避免内存无限增长；
    ///   - 实例被外部 <c>Destroy</c> 后，池在下次取用时跳过空槽并新建，不会报错。
    ///
    /// 注意（Unity 池化的固有约束）：预制体若是激活状态，实例化瞬间就会跑一次 Awake/OnEnable，
    /// 之后才被失活；因此**业务启动逻辑请写在 <see cref="IPoolable.OnSpawn"/> 里**，不要依赖 OnEnable 只跑一次。
    /// </summary>
    public sealed class GameObjectPool
    {
        #region 字段与属性
        readonly Stack<GameObject> m_Idle = new Stack<GameObject>();
        readonly HashSet<GameObject> m_Active = new HashSet<GameObject>();
        readonly PoolInstantiateFunc m_Instantiate;
        readonly bool m_OwnsRoot;

        /// <summary>池的键（通常是包配置 id）；用于反查与日志。</summary>
        public string Key { get; }

        /// <summary>池管理的预制体。</summary>
        public GameObject Prefab { get; }

        /// <summary>闲置实例容器（同时也是实例的创建父节点）。</summary>
        public Transform Root { get; private set; }

        /// <summary>闲置上限；&lt;=0 表示不限。</summary>
        public int MaxIdleCount { get; set; }

        /// <summary>闲置实例数（含已被外部销毁、尚未清理的空槽）。</summary>
        public int IdleCount => m_Idle.Count;

        /// <summary>活跃实例数（读取时顺带清理被外部销毁的空槽）。</summary>
        public int ActiveCount
        {
            get { PruneDestroyed(); return m_Active.Count; }
        }

        /// <summary>池内实例总数（活跃 + 闲置）。</summary>
        public int TotalCount => ActiveCount + IdleCount;
        #endregion

        #region 构造
        /// <param name="key">池的键（通常是包配置 id）</param>
        /// <param name="prefab">池管理的预制体，不可为空</param>
        /// <param name="root">闲置容器；为空时本池自建一个（销毁池时会一并销毁）</param>
        /// <param name="maxIdleCount">闲置上限；&lt;=0 不限</param>
        /// <param name="instantiate">实例创建工厂；为空时用 <c>Object.Instantiate</c></param>
        public GameObjectPool(string key, GameObject prefab, Transform root = null,
                              int maxIdleCount = 0, PoolInstantiateFunc instantiate = null)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));

            Key = key;
            Prefab = prefab;
            MaxIdleCount = maxIdleCount;
            m_Instantiate = instantiate;

            if (root != null)
            {
                Root = root;
                m_OwnsRoot = false;
            }
            else
            {
                var go = new GameObject($"__Pool_{Sanitize(key)}");
                Root = go.transform;
                m_OwnsRoot = true;
            }
        }
        #endregion

        #region 取用 / 归还
        /// <summary>取一个实例（不复位位置：调用方按需自行摆位）。</summary>
        public GameObject Spawn(Transform parent = null)
        {
            return Spawn(parent, Vector3.zero, Quaternion.identity);
        }

        /// <summary>取一个实例并挂到 <paramref name="parent"/> 下，设置本地位置与旋转。</summary>
        public GameObject Spawn(Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            var instance = Acquire();
            if (instance == null) return null;

            var t = instance.transform;
            t.SetParent(parent != null ? parent : Root, false);
            t.localPosition = localPosition;
            t.localRotation = localRotation;

            instance.SetActive(true);
            m_Active.Add(instance);
            Notify(instance, true);
            return instance;
        }

        /// <summary>归还实例：回调 OnDespawn → 挂回闲置容器 → 失活；超出闲置上限则直接销毁。</summary>
        public bool Despawn(GameObject instance)
        {
            if (instance == null) return false;

            var marker = instance.GetComponent<PooledObject>();
            if (marker == null || marker.Owner != this)
            {
                Log.Warning($"[GameObjectPool] {instance.name} 不属于池 {Key}，归还已忽略");
                return false;
            }

            if (!m_Active.Remove(instance)) return false;   // 重复归还 / 未取用 → 忽略

            Notify(instance, false);
            instance.transform.SetParent(Root, false);
            instance.SetActive(false);

            if (MaxIdleCount > 0 && m_Idle.Count >= MaxIdleCount)
            {
                DestroyInstance(instance);                  // 超出闲置上限：归还即销毁
                return true;
            }

            m_Idle.Push(instance);
            return true;
        }

        /// <summary>归还全部活跃实例（保留池本身），返回实际归还数量。</summary>
        public int DespawnAll()
        {
            PruneDestroyed();
            if (m_Active.Count == 0) return 0;

            var actives = new List<GameObject>(m_Active);
            int count = 0;
            for (int i = 0; i < actives.Count; i++)
            {
                if (Despawn(actives[i])) count++;
            }
            return count;
        }

        /// <summary>实例是否由本池取用中。</summary>
        public bool Contains(GameObject instance)
        {
            return instance != null && m_Active.Contains(instance);
        }
        #endregion

        #region 预热 / 清理
        /// <summary>预热：确保池内实例总数不少于 <paramref name="count"/>，返回新建数量。</summary>
        public int Preload(int count)
        {
            if (count <= 0) return 0;
            PruneDestroyed();

            int created = 0;
            while (m_Idle.Count + m_Active.Count < count)
            {
                var go = CreateInstance();
                if (go == null) break;
                m_Idle.Push(go);
                created++;
            }
            return created;
        }

        /// <summary>
        /// 销毁闲置实例。返回值销毁数量。
        /// <paramref name="trimToZero"/> = true 全清；false 只清到 <see cref="MaxIdleCount"/>。
        /// </summary>
        public int ClearIdle(bool trimToZero = true)
        {
            int keep = trimToZero ? 0 : Mathf.Max(0, MaxIdleCount);
            int destroyed = 0;
            while (m_Idle.Count > keep)
            {
                var go = m_Idle.Pop();
                if (go == null) continue;
                DestroyInstance(go);
                destroyed++;
            }
            return destroyed;
        }

        /// <summary>销毁整个池（可选是否连活跃实例一起销毁）。自建的容器节点也会被销毁。</summary>
        public void Destroy(bool destroyActiveInstances = true)
        {
            ClearIdle(true);

            if (destroyActiveInstances)
            {
                var actives = new List<GameObject>(m_Active);
                for (int i = 0; i < actives.Count; i++) DestroyInstance(actives[i]);
                m_Active.Clear();
            }
            else
            {
                PruneDestroyed();
            }

            if (m_OwnsRoot && Root != null)
            {
                UnityEngine.Object.Destroy(Root.gameObject);
            }
            Root = null;
        }
        #endregion

        #region 私有实现
        /// <summary>取一个可用实例：优先弹闲置槽（跳过已被外部销毁的空槽），否则新建。</summary>
        GameObject Acquire()
        {
            while (m_Idle.Count > 0)
            {
                var go = m_Idle.Pop();
                if (go != null) return go;
            }
            return CreateInstance();
        }

        GameObject CreateInstance()
        {
            GameObject go = m_Instantiate != null
                ? m_Instantiate(Key, Prefab, Root)
                : UnityEngine.Object.Instantiate(Prefab, Root);

            if (go == null)
            {
                Log.Error($"[GameObjectPool] 实例创建失败：池 {Key}");
                return null;
            }

            go.name = Prefab.name;                          // 去掉 Instantiate 追加的 "(Clone)"
            if (go.transform.parent != Root) go.transform.SetParent(Root, false);
            go.SetActive(false);                            // 先失活，避免新建实例在池里闪一帧

            var marker = go.GetComponent<PooledObject>();
            if (marker == null) marker = go.AddComponent<PooledObject>();
            marker.Bind(this, Key);
            marker.Callbacks = go.GetComponentsInChildren<IPoolable>(true);   // 只扫一次（含未激活节点）

            return go;
        }

        void DestroyInstance(GameObject instance)
        {
            if (instance == null) return;
            m_Active.Remove(instance);
            UnityEngine.Object.Destroy(instance);
        }

        /// <summary>触发实例上的 <see cref="IPoolable"/> 回调（创建时已缓存）。</summary>
        void Notify(GameObject instance, bool isSpawn)
        {
            var marker = instance.GetComponent<PooledObject>();
            var callbacks = marker != null ? marker.Callbacks : null;
            if (callbacks == null) return;

            for (int i = 0; i < callbacks.Length; i++)
            {
                var cb = callbacks[i];
                if (!IsAlive(cb)) continue;

                if (isSpawn) cb.OnSpawn();
                else cb.OnDespawn();
            }
        }

        /// <summary>回调是否仍然可用（接口引用不走 Unity 的销毁判定，需显式转 Object 比对）。</summary>
        static bool IsAlive(IPoolable callback)
        {
            if (callback == null) return false;
            var unityObject = callback as UnityEngine.Object;
            if (unityObject != null && unityObject == null) return false;   // 组件已销毁
            return true;
        }

        void PruneDestroyed()
        {
            if (m_Active.Count == 0) return;

            List<GameObject> dead = null;
            foreach (var go in m_Active)
            {
                if (go == null)
                {
                    if (dead == null) dead = new List<GameObject>();
                    dead.Add(go);
                }
            }
            if (dead == null) return;

            for (int i = 0; i < dead.Count; i++) m_Active.Remove(dead[i]);
        }

        /// <summary>把 key 变成合法的 GameObject 名字（bundle 名里的 '/' 不能进节点名）。</summary>
        internal static string Sanitize(string key)
        {
            if (string.IsNullOrEmpty(key)) return "pool";
            return key.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
        }
        #endregion
    }
}
