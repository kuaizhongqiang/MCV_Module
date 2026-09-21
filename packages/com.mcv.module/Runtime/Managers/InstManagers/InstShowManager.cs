using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Managers.InstManagers
{
    /// <summary>
    /// 仅展示型实例管理器 —— 取出的实例只用于观看（姿态/尺寸由展示脚本自己实现），不参与步骤交互。
    ///
    /// 对象池、包加载、实例归属全部由 <see cref="InstManagerBase"/> 提供，本类只负责：
    ///   ① 静态单例（场景唯一，不做 DontDestroyOnLoad，随 1_Content 场景销毁）；
    ///   ② 声明「非可操控」（<c>isControlled = false</c>）；
    ///   ③ 静态便捷入口 <see cref="Show"/> / <see cref="Hide"/>（免去每次写 Instance）。
    ///
    /// 典型用法（模型展示）：
    /// <code>
    /// var model = InstShowManager.Show(prefabKey);                 // 命中池：零 Instantiate
    /// if (model != null) model.transform.SetParent(showParent, false);
    /// ...
    /// InstShowManager.Hide(model);                                 // 归还等待复用
    /// </code>
    /// </summary>
    public class InstShowManager : InstManagerBase
    {
        static InstShowManager s_Instance;

        /// <summary>场景中的唯一实例；未挂载时返回 null（不自动创建 GameObject）。</summary>
        public static InstShowManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var found = FindObjectsByType<InstShowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for (int i = 0; i < found.Length; i++)
                    {
                        if (found[i] == null) continue;
                        s_Instance = found[i];
                        break;
                    }
                }
                return s_Instance;
            }
            set { s_Instance = value; }
        }

        /// <summary>单例是否已存在（退出阶段请用这个判断，避免触发查找）。</summary>
        public static bool Exists => s_Instance != null;

        /// <summary>
        /// 把「释放本管理器对象池」注册到 core 的内容包卸载注入点上。
        ///
        /// 为什么由 module 自注册：<c>GlobalAssetsMgr</c> 属 core 包、本类属 module 包，
        /// core 直接引用 module 会形成反向依赖（包化形态编译失败）。
        /// <c>BeforeSceneLoad</c> 注册，早于任何内容包装卸。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterPackageReleaseHook()
        {
            GlobalAssetsMgr.PackageReleasing -= OnPackageReleasing;
            GlobalAssetsMgr.PackageReleasing += OnPackageReleasing;
        }

        /// <summary>内容包卸载时释放本管理器的池（池持有 bundle 内预制体引用，必须先于卸 bundle）。</summary>
        static void OnPackageReleasing(string packageId, bool destroyInstances)
        {
            var mgr = Instance;                 // 场景里没有实例时静默跳过（那也就不存在池）
            if (mgr != null) mgr.ReleasePackage(packageId, destroyInstances);
        }

        /// <summary>
        /// 仅展示型按需加载：packageKeys 可以留空（不预加载、不报错），
        /// 简介页的器件模型就是这样用的 —— 8 个器件随时切换，只加载当前那一个包。
        /// 若在 Inspector 配了 packageKeys，则照旧预加载并建池（Spawn 可同步命中）。
        /// </summary>
        protected override bool AllowEmptyPackageKeys => true;

        #region 生命周期
        protected override void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Log.Warning($"[InstShowManager] 场景中存在重复实例，已销毁：{name}");
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            isControlled = false;      // 仅展示：不参与步骤交互
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (s_Instance == this) s_Instance = null;
        }
        #endregion

        #region 静态便捷入口
        /// <summary>取一个仅展示实例（包未加载时返回 null；需要异步兜底请用 <c>Instance.SpawnAsync</c>）。</summary>
        public static GameObject Show(string packageId, Transform parent = null)
        {
            var mgr = Instance;
            if (mgr == null)
            {
                Log.Error($"[InstShowManager] 场景中不存在 InstShowManager 实例，Show({packageId}) 被跳过");
                return null;
            }
            return mgr.Spawn(packageId, parent);
        }

        /// <summary>归还仅展示实例（等价于 Despawn，仅语义更贴合展示场景）。</summary>
        public static bool Hide(GameObject instance)
        {
            var mgr = Instance;
            if (mgr == null) return false;
            return mgr.Despawn(instance);
        }

        /// <summary>
        /// 展示根（本管理器的宿主对象，如 ShowObjParent）的显隐 —— 供「宿主默认关闭、由业务按需打开」的用法调用。
        ///
        /// 为什么需要它：宿主默认关闭可以省掉其下渲染相机的开销，但 Unity 不会给**未激活**物体上的组件调
        /// Awake / Start，管理器因此不会初始化。业务在需要展示前调本方法打开：首次打开会同步触发
        /// Awake → Start(→DelayInit)，开始按 packageKeys 预加载并建池；
        /// 关闭只走 OnDisable，已加载的包 / 预制体缓存 / 对象池全部保留，再次打开无需重新加载。
        /// </summary>
        /// <returns>场景中存在管理器并完成操作返回 true；不存在返回 false（只记日志，不自动创建）</returns>
        public static bool SetShowRootActive(bool active)
        {
            var mgr = Instance;
            if (mgr == null)
            {
                Log.Error($"[InstShowManager] 场景中不存在 InstShowManager 实例，SetShowRootActive({active}) 被跳过");
                return false;
            }

            if (mgr.gameObject.activeSelf != active) mgr.gameObject.SetActive(active);
            return true;
        }
        #endregion
    }
}
