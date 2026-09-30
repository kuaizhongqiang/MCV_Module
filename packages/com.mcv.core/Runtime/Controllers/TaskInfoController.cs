// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-14）—— 请按需补充业务代码
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.InputController.FocusRotationController;
using MCV_Module.Managers;
using MCV_Module.Managers.InstManagers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.UI.Panels;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: 资源不由本类加载（内容包由 GlobalAssetsMgr 进内容页时统一装卸），OnViewBound 必早于加载完成 —— 未就绪要记 pendingClipId 等 ClipReadyEvent 补装配（BundlePipeline §6）
    // WHY: 展示根收口两条路径缺一不可：内容页内换步骤走 TaskTypeChangeEventData，离开内容页走 SceneStateChangeEventData；后者不能靠前者兜底，否则 ShowObjParent 一直开着、渲染相机持续出图
    /// <summary>简介面板调度：按当前 ProjectClip 的 taskInfoData 装配「简介图集 + 文案 + 器件模型」，开面板即把展示相机复位。</summary>
    public class TaskInfoController : ControllerBase<TaskInfoPanel>
    {
        /// <summary>待补装配的 clip（包未就绪时记下，收到 ClipReadyEvent 再装配）</summary>
        string pendingClipId;

        /// <summary>本次装配用到的展示管理器（其宿主对象 = 展示根 ShowObjParent）</summary>
        InstShowManager showObjMgr;

        /// <summary>当前展示中的简介模型实例（切器件 / 退出简介任务时归还）</summary>
        GameObject showModel;

        #region 生命周期
        public override void OnInit()
        {
            base.OnInit();
            // Controller 由 GlobalControllerMgr 常驻持有 → 一次订阅即可（EventBus 内部去重）
            EventBus<ClipReadyEvent>.Subscribe(OnClipReady);
            EventBus<TaskTypeChangeEventData>.Subscribe(OnTaskTypeChanged);
            EventBus<SceneStateChangeEventData>.Subscribe(OnSceneStateChanged);
        }

        public override void OnDispose()
        {
            EventBus<ClipReadyEvent>.Unsubscribe(OnClipReady);
            EventBus<TaskTypeChangeEventData>.Unsubscribe(OnTaskTypeChanged);
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChanged);
            ReturnModel();
            base.OnDispose();
        }

        public override void OnViewBound()
        {
            // WHY: 必须放在所有 return 之前 —— 包未就绪时 OnViewBound 会提前 return，复位不能跟着被跳过
            ResetCameraPose();

            ProjectClip clip = GlobalDataMgr.GetProjectClip();
            if (clip == null)
            {
                Log.Warning("[TaskInfoController] 当前没有 ProjectClip，简介面板跳过装配");
                return;
            }

            if (GlobalAssetsMgr.IsClipReady(clip.id))
            {
                pendingClipId = null;
                BindAll(clip);
            }
            else
            {
                // 包还在加载（遮罩盖着）→ 等 ClipReadyEvent 补装配，不要在这里同步取（只能拿到空）
                pendingClipId = clip.id;
                Log.Info($"[TaskInfoController] {clip.id} 内容包未就绪，等 ClipReadyEvent 补装配");
            }
        }

        // WHY: 必须用瞬移版 ResetPos()，不要换成 ResetPosSmooth()（那是 resetDuration 秒缓动、有过渡）
        /// <summary>相机瞬间复位到初始位姿：FocusRotationControl.ResetPos() 打断在途协程、直接设位姿与初始 FOV，无惯性。</summary>
        void ResetCameraPose()
        {
            var focus = GlobalInputMgr.GetController<FocusRotationControl>();
            if (focus == null)
            {
                Log.Warning("[TaskInfoController] 未找到 FocusRotationControl（1_Content 的 ColorCam），本次跳过相机复位");
                return;
            }

            focus.ResetPos();
        }

        /// <summary>包就绪：补一次装配（只认自己等的那一个 clip）。</summary>
        void OnClipReady(ClipReadyEvent e)
        {
            if (e == null || pendingClipId == null || e.ClipId != pendingClipId) return;

            pendingClipId = null;

            ProjectClip clip = GlobalDataMgr.GetProjectClip();
            if (clip == null || clip.id != e.ClipId || View == null) return;   // 面板已切走 / 已重建销毁

            BindAll(clip);
        }

        /// <summary>切到别的任务类型：把模型还回池并收起展示根（其下的渲染相机随之停摆）。</summary>
        void OnTaskTypeChanged(TaskTypeChangeEventData e)
        {
            if (e == null || e.TaskType == TaskType.Info) return;
            HideShowObj();
        }

        // WHY: TaskTypeChangeEventData 只在内容页内换步骤时发，回菜单/进漫游都不发 —— 不在这里兜底 ShowObjParent 会一直开着、渲染相机持续白出图
        /// <summary>离开内容页（回菜单 / 进漫游）：收起展示根、归还模型。</summary>
        void OnSceneStateChanged(SceneStateChangeEventData e)
        {
            if (e == null || e.State == SceneState.UI) return;

            Log.Info($"[TaskInfoController] 离开内容页（{e.State}），收起展示根");
            HideShowObj();
        }
        #endregion

        #region 装配
        void BindAll(ProjectClip clip)
        {
            var data = GlobalDataMgr.GetTaskData(TaskType.Info) as TaskInfoData;
            BindPictureSet(clip, data, GlobalDataMgr.GetProjectClipIndex());
            BindShowObj(clip, data);
        }

        /// <summary>图集 + 文案：包已加载，全部走同步取用。</summary>
        void BindPictureSet(ProjectClip clip, TaskInfoData data, int textIndex)
        {
            if (View == null) return;

            List<Sprite> sprites = null;
            if (data != null && data.images != null && data.images.Count > 0)
            {
                sprites = GlobalAssetsMgr.GetSpritesByPackageIds(data.images);
                if (sprites.Count != data.images.Count)
                {
                    Log.Warning($"[TaskInfoController] {clip.id} 简介图集不完整：" +
                                $"{sprites.Count}/{data.images.Count} 张（检查包内资源与 JSON 是否对齐）");
                }
            }
            else
            {
                Log.Warning($"[TaskInfoController] {clip.id} 未配置简介图集（taskInfoData.images），只显示文案");
            }

            View.Init(sprites, textIndex);
        }

        /// <summary>器件模型：包已加载 → InstShowManager 池化取用（SpawnAsync 命中资源缓存即同步回调）。</summary>
        void BindShowObj(ProjectClip clip, TaskInfoData data)
        {
            ReturnModel();

            if (data == null || string.IsNullOrEmpty(data.prefabKey))
            {
                Log.Warning($"[TaskInfoController] {clip.id} 未配置简介模型（taskInfoData.prefabKey），跳过物体渲染");
                return;
            }

            if (GlobalAssetsMgr.GetPrefabByPackageId(data.prefabKey) == null)
            {
                Log.Warning($"[TaskInfoController] {clip.id} 简介模型未加载：{data.prefabKey}");
                return;
            }

            if (InstShowManager.Instance == null)
            {
                Log.Error("[TaskInfoController] 场景中没有 InstShowManager，简介模型无法装配");
                return;
            }

            showObjMgr = InstShowManager.Instance;
            InstShowManager.SetShowRootActive(true);     // 展示根默认关闭（其下挂着渲染相机），用到才开

            string key = data.prefabKey;
            showObjMgr.SpawnAsync(key, null,               // parent 传 null → 用管理器的 objParent（ObjParent）
                model =>
                {
                    if (model == null) return;
                    showModel = model;
                    Log.Info($"[TaskInfoController] {clip.id} 简介模型装配完成：{key}");
                },
                error => Log.Error($"[TaskInfoController] {clip.id} 简介模型取用失败：{error}"));
        }
        #endregion

        #region 收口
        /// <summary>归还当前模型实例（保留展示根与已加载的包，只把实例还回池）。</summary>
        void ReturnModel()
        {
            if (showModel == null) return;

            if (showObjMgr != null && !showObjMgr.Despawn(showModel))
                Log.Warning($"[TaskInfoController] 简介模型归还失败：{showModel.name}");

            showModel = null;
        }

        /// <summary>收起展示根：先归还模型，再关闭 ShowObjParent（渲染相机随之停止出图）。</summary>
        void HideShowObj()
        {
            ReturnModel();

            if (showObjMgr != null)
            {
                InstShowManager.SetShowRootActive(false);
                showObjMgr = null;
            }
        }
        #endregion
    }
}
