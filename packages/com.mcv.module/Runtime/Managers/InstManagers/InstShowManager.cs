using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Managers.InstManagers
{
    /// <summary>仅展示型实例管理器：实例只用于观看（姿态/尺寸由展示脚本自实现），不参与步骤交互。</summary>
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

        /// <summary>仅展示型按需加载：packageKeys 可留空（不预加载、不报错）；配了则照旧预加载并建池。</summary>
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

        // WHY: 未激活物体上的组件不会被 Awake/Start，管理器不会初始化；首次打开才触发 Awake→Start→DelayInit 预加载建池，关闭只走 OnDisable，包与池全部保留
        /// <summary>展示根（本管理器的宿主对象，如 ShowObjParent）的显隐；不存在管理器时只记日志并返回 false。</summary>
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
