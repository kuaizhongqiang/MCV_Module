using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Managers.InstManagers
{
    /// <summary>
    /// 可操控型实例管理器（**占位实现**）。
    ///
    /// ⚠ 范围说明：本类的完整实现（LOW 侧 22.2KB 的"简化步骤系统"）**不在本次升级范围内**
    /// —— 决策 F-4 / §9 已定「步骤体系只留一套」，走 CUR 的 <c>StepManager</c>，不引入第二套步骤体系。
    /// 本类保留为占位，仅对齐 <see cref="InstManagerBase"/> 的新基类形态（Awake/OnDestroy 覆写）。
    /// </summary>
    public class InstControlledManager : InstManagerBase
    {
        static InstControlledManager s_Instance;

        /// <summary>场景中的唯一实例；未挂载时返回 null（不自动创建 GameObject）。</summary>
        public static InstControlledManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var found = FindObjectsByType<InstControlledManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        #region 生命周期
        protected override void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Log.Warning($"[InstControlledManager] 场景中存在重复实例，已销毁：{name}");
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            isControlled = true;       // 可操控型：参与步骤交互
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (s_Instance == this) s_Instance = null;
        }
        #endregion
    }
}
