using System.Collections;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Managers.InstManagers
{
    // WHY: 不走对象池 —— 表笔会被拖动、检测点会记录接触状态，复用会把上一次状态带进来，重新实例化才可靠
    // WHY: 资源不由本类加载 —— 内容包由 GlobalAssetsMgr 按 currentClip 统一装卸（规约见 Docs/design_ai/BundlePipeline.md §6）
    /// <summary>检测任务调度：按当前 ProjectClip 的 taskInspectionData.prefabKey 装配检测预制体，切走即销毁。</summary>
    public class InspectionManager : InstManagerBase
    {
        static InspectionManager s_Instance;

        /// <summary>场景中的唯一实例；未挂载时返回 null（不自动创建 GameObject）。</summary>
        public static InspectionManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var found = FindObjectsByType<InspectionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        #region 状态
        /// <summary>当前检测预制体实例。</summary>
        GameObject currentModel;

        /// <summary>包未就绪时记下待装配的 clip id（收到 ClipReadyEvent 再装配）。</summary>
        string pendingClipId;

        /// <summary>当前已装配实例对应的 clip id。</summary>
        string boundClipId;
        #endregion

        #region 对外属性
        /// <summary>当前检测预制体实例（未装配时为 null）。</summary>
        public GameObject CurrentModel => currentModel;

        /// <summary>检测预制体是否已装配。</summary>
        public bool IsModelBound => currentModel != null;

        /// <summary>当前实例对应的 clip id（未装配时为 null）。</summary>
        public string BoundClipId => boundClipId;

        /// <summary>按需加载型：不预加载 packageKeys，预制体由当前 clip 的 taskInspectionData.prefabKey 决定。</summary>
        protected override bool AllowEmptyPackageKeys => true;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Log.Warning($"[InspectionManager] 场景中存在重复实例，已销毁：{name}");
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            base.Awake();

            // 管理器常驻（1_Content）→ 一次订阅即可（EventBus 内部去重）
            EventBus<ClipReadyEvent>.Subscribe(OnClipReady);
            EventBus<TaskTypeChangeEventData>.Subscribe(OnTaskTypeChanged);
            EventBus<SceneStateChangeEventData>.Subscribe(OnSceneStateChanged);
        }

        protected override IEnumerator DelayInit()
        {
            yield return base.DelayInit();      // 按需加载型：不做预加载，只置 isInit

            // 本场景加载时当前任务就已经是"检测"的话补一次装配（包未就绪会转 pending 等 ClipReadyEvent）
            if (GlobalDataMgr.GetCurrentTaskType() == TaskType.Inspection) BindCurrentClip();
        }

        protected override void OnDestroy()
        {
            EventBus<ClipReadyEvent>.Unsubscribe(OnClipReady);
            EventBus<TaskTypeChangeEventData>.Unsubscribe(OnTaskTypeChanged);
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChanged);

            AbandonModel();     // 随 1_Content 销毁：内容包已被卸掉，实例不必也不能再 ReleaseInstance

            base.OnDestroy();
            if (s_Instance == this) s_Instance = null;
        }
        #endregion

        #region 事件
        /// <summary>包就绪：补一次装配（只认自己等的那一个 clip）。</summary>
        void OnClipReady(ClipReadyEvent e)
        {
            if (e == null || pendingClipId == null || e.ClipId != pendingClipId) return;

            pendingClipId = null;

            ProjectClip clip = GlobalDataMgr.GetProjectClip();
            if (clip == null || clip.id != e.ClipId) return;    // 已切走

            Bind(clip);
        }

        /// <summary>切到检测任务 → 装配（全新实例）；切到别的任务 → 销毁实例。</summary>
        void OnTaskTypeChanged(TaskTypeChangeEventData e)
        {
            if (e == null) return;

            if (e.TaskType == TaskType.Inspection) BindCurrentClip();
            else ReleaseModel();
        }

        // WHY: 离开内容页时 GlobalAssetsMgr 会卸包并连带销毁实例，自己再 Destroy 只会打到已失效的登记上
        /// <summary>进内容页且任务是检测 → 补一次装配；离开内容页 → 只放开引用（实例随卸包销毁）。</summary>
        void OnSceneStateChanged(SceneStateChangeEventData e)
        {
            if (e == null) return;

            if (e.State == SceneState.UI)
            {
                if (GlobalDataMgr.GetCurrentTaskType() == TaskType.Inspection) BindCurrentClip();
                return;
            }

            AbandonModel();
        }
        #endregion

        #region 装配
        /// <summary>按当前 ProjectClip 装配检测预制体（包未就绪则记下 clip，等 ClipReadyEvent 补一次）。</summary>
        public void BindCurrentClip()
        {
            ProjectClip clip = GlobalDataMgr.GetProjectClip();
            if (clip == null)
            {
                Log.Warning("[InspectionManager] 当前没有 ProjectClip，检测预制体无法装配");
                return;
            }

            if (!GlobalAssetsMgr.IsClipReady(clip.id))
            {
                pendingClipId = clip.id;
                Log.Info($"[InspectionManager] {clip.id} 内容包未就绪，等 ClipReadyEvent 后装配");
                return;
            }

            pendingClipId = null;
            Bind(clip);
        }

        /// <summary>装配：销毁上一次实例 → 取 taskInspectionData.prefabKey → 实例化到父节点。</summary>
        void Bind(ProjectClip clip)
        {
            ReleaseModel();     // 每次进来都是全新实例：先销毁上一次的（同一父节点下马上还要用）

            var data = GlobalDataMgr.GetTaskData(TaskType.Inspection) as TaskInspectionData;
            if (data == null || string.IsNullOrEmpty(data.prefabKey))
            {
                Log.Warning($"[InspectionManager] {clip.id} 未配置检测预制体（taskInspectionData.prefabKey），跳过装配");
                return;
            }

            string key = data.prefabKey;
            GameObject prefab = GlobalAssetsMgr.GetPrefabByPackageId(key);
            if (prefab == null)
            {
                Log.Warning($"[InspectionManager] {clip.id} 检测预制体未加载：{key}");
                return;
            }

            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null)
            {
                Log.Error($"[InspectionManager] GlobalAddressableMgr 未就绪，{clip.id} 检测预制体无法实例化");
                return;
            }

            // 走实例化管线（不是 Object.Instantiate）：登记「实例 → 包配置 id」，卸载内容包时连带销毁
            GameObject instance = GlobalAddressableMgr.Instance.InstantiatePrefab(prefab, key, ResolveModelParent());
            if (instance == null)
            {
                Log.Error($"[InspectionManager] {clip.id} 检测预制体实例化失败：{key}");
                return;
            }

            currentModel = instance;
            boundClipId = clip.id;
            Log.Info($"[InspectionManager] {clip.id} 检测预制体装配完成：{key}");
        }

        /// <summary>模型父节点：配了 <c>objParent</c> 用配置，否则挂到本管理器物体下。</summary>
        Transform ResolveModelParent()
        {
            return objParent != null ? objParent : transform;
        }
        #endregion

        #region 收口
        /// <summary>销毁当前检测实例（同时注销它的包归属登记）；重新装配前也会先走这里。</summary>
        public void ReleaseModel()
        {
            pendingClipId = null;
            boundClipId = null;

            if (currentModel == null) return;

            if (GlobalAddressableMgr.Exists && GlobalAddressableMgr.Instance != null)
                GlobalAddressableMgr.Instance.ReleaseInstance(currentModel);
            else
                Destroy(currentModel);

            currentModel = null;
        }

        /// <summary>只放开引用、不销毁：用于「实例会被卸包统一销毁」的场合（离开内容页、管理器自身销毁）。</summary>
        void AbandonModel()
        {
            pendingClipId = null;
            boundClipId = null;
            currentModel = null;
        }
        #endregion
    }
}
