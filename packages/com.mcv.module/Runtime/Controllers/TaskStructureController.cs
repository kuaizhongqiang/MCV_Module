// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-14）—— 请按需补充业务代码
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Managers.InstManagers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Objects.Interactives.TaskObj;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: 资源由 GlobalAssetsMgr 按 currentClip 统一装卸，且面板 OnViewBound 必早于加载完成 —— 未就绪要记 pendingClipId，等 ClipReadyEvent 补一次
    // WHY: 结构动画直接改零件 transform、Animation.Stop() 不复位姿态，模型必须每次重新实例化、收口即销毁（不走对象池）
    /// <summary>结构面板调度：按当前 ProjectClip 的 taskStructureData.prefabKey 装配三维分解模型，并驱动其上的 InstControlledManager 执行播放。</summary>
    public class TaskStructureController : ControllerBase<MCV_Module.UI.Panels.TaskStructurePanel>
    {
        // WHY: 结构展示根的挂点不再走场景序列化引用（控制器已不挂场景）——按场景内唯一的父级路径查找：
        //      1_Content 里 ControlObjParent/ObjParent 是结构模型的挂点，ShowObjParent/ObjParent 是简介展示根，
        //      两者同级且同名子物体，**只按 "ObjParent" 找会命中相机那一支**，必须带父级限定。
        const string ObjParentPath = "ControlObjParent/ObjParent";
        const string ControlObjParentName = "ControlObjParent";

        // WHY: 行为参数（自动来回播速度），不是视图表现参数 —— 留在控制器；控制器已无序列化入口，故为常量
        const float AutoPingPongSpeed = 2f;

        /// <summary>解析出的结构模型挂点缓存（场景对象只在 1_Content 就绪后找一次）。</summary>
        Transform m_InitObjParent;

        // WHY: 收口路径（AbandonModel / SetShowRootActive）会被 GlobalUIMgr 的初始状态事件在 1_Content 加载前调到，
        //      那时场景里的 ControlObjParent 根本不存在——这不是配置错误，只是还没加载；故收口路径走静默解析，
        //      只有真正要用挂点装配模型的 BindAndPlay 才要求解析成功并打 Error。
        /// <summary>按路径解析结构模型挂点；找不到只返回 null、不写日志。解析失败不缓存失败结果，下次调用会再试（1_Content 可能还没加载完）。</summary>
        Transform TryResolveObjParent()
        {
            if (m_InitObjParent != null) return m_InitObjParent;

            var root = GameObject.Find(ControlObjParentName);
            if (root == null) return null;

            m_InitObjParent = root.transform.Find("ObjParent");
            return m_InitObjParent;
        }

        /// <summary>结构模型实例的父节点（1_Content 场景的 ControlObjParent/ObjParent）；装配需要它，解析失败即报错。</summary>
        Transform InitObjParent
        {
            get
            {
                var parent = TryResolveObjParent();
                if (parent == null)
                    Log.Error($"[TaskStructureController] 场景中找不到 {ObjParentPath}，结构模型无法上屏（检查 1_Content 的 {ControlObjParentName}）");
                return parent;
            }
        }

        /// <summary>待补装配的 clip（包未就绪时记下，收到 ClipReadyEvent 再装配）</summary>
        string pendingClipId;

        /// <summary>当前结构模型实例（切走 / 退出内容页时归还）</summary>
        GameObject structureModel;

        /// <summary>结构模型实例上的简化步骤系统（点击 + 播动画）</summary>
        InstControlledManager currentObjMgr;

        /// <summary>本次已起播的 clip id —— 用来认领 <c>StructInteractiveCompletedEvent</c>，只处理自己这一轮</summary>
        string playingClipId;

        #region 生命周期
        public override void OnInit()
        {
            base.OnInit();      // 先注册：面板生命周期按 1:1 名字约定找 Controller，不能因配置缺失被跳过

            // WHY: 挂点不在这里解析 —— OnInit 跑在 0_Setup 阶段（1_Content 尚未加载），那时场景里还没有 ObjParent；
            //      改由 InitObjParent 首次访问时按路径懒解析（OnViewBound / 装配时），解析不到各调用点自己打日志。

            // Controller 由 GlobalControllerMgr 常驻持有 → 一次订阅即可（EventBus 内部去重）
            EventBus<ClipReadyEvent>.Subscribe(OnClipReady);
            EventBus<TaskTypeChangeEventData>.Subscribe(OnTaskTypeChanged);
            EventBus<SceneStateChangeEventData>.Subscribe(OnSceneStateChanged);
            EventBus<StructInteractiveCompletedEvent>.Subscribe(OnStructStepsCompleted);
            EventBus<GlobalInteractionEventData>.Subscribe(OnGlobalInteraction);
        }

        public override void OnDispose()
        {
            EventBus<ClipReadyEvent>.Unsubscribe(OnClipReady);
            EventBus<TaskTypeChangeEventData>.Unsubscribe(OnTaskTypeChanged);
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChanged);
            EventBus<StructInteractiveCompletedEvent>.Unsubscribe(OnStructStepsCompleted);
            EventBus<GlobalInteractionEventData>.Unsubscribe(OnGlobalInteraction);
            AbandonModel();      // 控制器随 1_Content 销毁时，内容包（含池与实例）已被卸掉，不必也不能再归还
            base.OnDispose();
        }

        public override void OnViewBound()
        {
            ProjectClip clip = GlobalDataMgr.GetProjectClip();
            if (clip == null)
            {
                Log.Warning("[TaskStructureController] 当前没有 ProjectClip，结构面板跳过播放");
                return;
            }

            if (GlobalAssetsMgr.IsClipReady(clip.id))
            {
                pendingClipId = null;
                BindAndPlay(clip);
            }
            else
            {
                // 包还在加载（遮罩盖着）→ 等 ClipReadyEvent 补一次，不要在这里同步取（只能拿到空）
                pendingClipId = clip.id;
                Log.Info($"[TaskStructureController] {clip.id} 内容包未就绪，等 ClipReadyEvent 后播放");
            }
        }
        #endregion

        #region 事件
        /// <summary>包就绪：补一次装配并起播（只认自己等的那一个 clip）。</summary>
        void OnClipReady(ClipReadyEvent e)
        {
            if (e == null || pendingClipId == null || e.ClipId != pendingClipId) return;

            pendingClipId = null;

            ProjectClip clip = GlobalDataMgr.GetProjectClip();
            if (clip == null || clip.id != e.ClipId || View == null) return;   // 已切走 / 面板已重建销毁

            BindAndPlay(clip);
        }

        /// <summary>切到别的任务类型：停播并归还模型。</summary>
        void OnTaskTypeChanged(TaskTypeChangeEventData e)
        {
            if (e == null || e.TaskType == TaskType.Structure) return;
            ReleaseModel();
        }

        // WHY: 离开内容页时 GlobalAssetsMgr 会卸包并连池销毁实例，再归还只会命中已销毁的池；且「返回菜单/进漫游」只发状态事件，漏了这里动画会一直在播
        /// <summary>离开内容页（回菜单 / 进漫游）：停播并放开引用、不归还池，实例随内容包卸载一并销毁。</summary>
        void OnSceneStateChanged(SceneStateChangeEventData e)
        {
            if (e == null || e.State == SceneState.UI) return;

            AbandonModel();
        }

        /// <summary>手动段全部点完（步骤系统发的完成事件）→ 转入自动来回播。</summary>
        void OnStructStepsCompleted(StructInteractiveCompletedEvent e)
        {
            if (e == null || currentObjMgr == null) return;              // 已收口，本轮不算数
            if (e.clip == null || e.clip.id != playingClipId) return;    // 不是本轮（别的器件 / 别的来源）

            Log.Info($"[TaskStructureController] {playingClipId} 手动段完成，转入自动来回播放");

            ReportStructureScore(e.clip);   // 结构计分单元上报（手动段走完 = 完成）

            // WHY: 自动播会收回全部零件的碰撞体、此后不再有悬停事件；鼠标不动时 GlobalInteractiveMgr 会跳过射线检测，浮动框必须在这里手动收掉
            if (View != null) View.CloseTips();

            PlayAutoPingPong(-AutoPingPongSpeed);
        }

        // WHY: 判定复用 InstControlledManager 的手动完成事件（它刻意不在自动来回播时发），"手动点过每个零件"就是唯一完成口径
        /// <summary>上报结构计分单元：二值口径（手动段全部点完 = 完成拿满单价，半途退出 0 分）。</summary>
        static void ReportStructureScore(ProjectClip clip)
        {
            if (clip == null) return;

            var data = GlobalDataMgr.GetTaskData(clip.id, TaskType.Structure) as TaskStructureData;
            if (data == null)
            {
                Log.Warning($"[TaskStructureController] {clip.id} 没有结构任务数据，结构成绩无法上报");
                return;
            }

            var task = GlobalDataMgr.ReportScoredUnit(clip.id, clip.displayName, data.id, data.displayName,
                TaskType.Structure, completed: true);

            if (task == null)
            {
                Log.Warning($"[TaskStructureController] {clip.displayName} 的结构成绩上报被忽略（该任务类型不计分？）");
                return;
            }

            Log.Info($"[TaskStructureController] {clip.displayName}·{data.displayName} 完成，已上报计分单元：{task.score:0.##}/{task.fullScore:0.##} 分");
        }

        // WHY: View 不认场景对象，悬停判定必须收在控制器；只认 StructureTaskObj 且开了 HoverOrTips 的零件（HoverOnly 档位保证已完成零件仍可悬停）
        /// <summary>悬停驱动浮动提示框：移入结构零件 → 打开浮动框并写入其 structureName；移出 → 关闭。</summary>
        void OnGlobalInteraction(GlobalInteractionEventData e)
        {
            // 没在播（收口后）或面板已销毁：不处理，免得漫游 / 别的页面里的悬停也在结构面板上弹框
            if (e == null || currentObjMgr == null || View == null) return;
            if (e.Type != GlobalInteractionType.Enter && e.Type != GlobalInteractionType.Exit) return;

            var obj = e.Target as StructureTaskObj;
            if (obj == null || !obj.HoverOrTips) return;

            if (e.Type == GlobalInteractionType.Enter) View.ShowTips(obj.StructureName);
            else View.CloseTips();
        }
        #endregion

        #region 装配与播放
        void BindAndPlay(ProjectClip clip)
        {
            ReleaseModel(false);      // 先销毁上一次的实例：展示根下面马上还要用，不来回关

            if (InitObjParent == null) return;      // InitObjParent 自身已打日志（场景里缺 ControlObjParent/ObjParent）

            var data = GlobalDataMgr.GetTaskData(TaskType.Structure) as TaskStructureData;
            if (data == null || string.IsNullOrEmpty(data.prefabKey))
            {
                Log.Warning($"[TaskStructureController] {clip.id} 未配置结构模型（taskStructureData.prefabKey），跳过播放");
                return;
            }

            string key = data.prefabKey;
            GameObject prefab = GlobalAssetsMgr.GetPrefabByPackageId(key);
            if (prefab == null)
            {
                Log.Warning($"[TaskStructureController] {clip.id} 结构模型未加载：{key}");
                return;
            }

            // WHY: 展示根必须先打开再实例化 —— 模型在未激活层级下 Awake/Start 会被推迟、InstControlledManager 不注册、随后的 StartSteps 会打空（其下挂着结构页专用渲染相机，故平时默认关）
            SetShowRootActive(true);

            // WHY: 每次进结构页都实例化全新对象（不走对象池）—— 结构动画直接改 transform，Animation.Stop() 不复位姿态，重新实例化是最可靠的重置；实例经 GlobalAddressableMgr 登记包归属，卸包时连带销毁
            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null)
            {
                Log.Error($"[TaskStructureController] GlobalAddressableMgr 未就绪，{clip.id} 结构模型无法实例化");
                return;
            }

            GameObject instance = GlobalAddressableMgr.Instance.InstantiatePrefab(prefab, key, InitObjParent);
            if (instance == null)
            {
                Log.Error($"[TaskStructureController] {clip.id} 结构模型实例化失败：{key}");
                return;
            }

            structureModel = instance;
            currentObjMgr = instance.GetComponentInChildren<InstControlledManager>(true);
            if (currentObjMgr == null)
            {
                Log.Error($"[TaskStructureController] {key} 上没有 InstControlledManager，无法执行播放");
                return;
            }

            playingClipId = clip.id;
            currentObjMgr.StartSteps(clip);
            Log.Info($"[TaskStructureController] {clip.id} 结构模型装配完成并起播（手动段）：{key}");
        }
        #endregion

        #region 自动来回播（pingpong）
        // WHY: 必须从负（倒放）开始 —— 手动段刚把零件逐个"拆开"，倒放整段正好"原路装回"，从正开始会连出两轮正放（视觉跳一下）
        /// <summary>自动来回播：正负交替、无限循环（负 = 第 N→0 步且每段倒放；正 = 第 0→N 步且每段正放），每轮在回调里续下一轮。</summary>
        void PlayAutoPingPong(float speed)
        {
            if (currentObjMgr == null || currentObjMgr.StepCount == 0) return;   // 没东西可播就别起链（否则空转）

            currentObjMgr.PlayAuto(speed, () =>
            {
                if (currentObjMgr == null) return;      // 中途收口了：不再续下一轮
                PlayAutoPingPong(-speed);
            });
        }
        #endregion

        #region 收口
        /// <summary>停播并销毁模型实例（结构模型不池化，重新实例化才是全新状态；closeRoot = 是否顺带关掉展示根）。</summary>
        void ReleaseModel(bool closeRoot = true)
        {
            StopPlayback();

            if (structureModel != null)
            {
                // 交给 GlobalAddressableMgr 销毁：同时注销它「实例 → 包配置 id」的登记，避免登记表残留空槽
                if (GlobalAddressableMgr.Exists && GlobalAddressableMgr.Instance != null)
                    GlobalAddressableMgr.Instance.ReleaseInstance(structureModel);
                else
                    Object.Destroy(structureModel);

                structureModel = null;
            }

            if (closeRoot) SetShowRootActive(false);
        }

        /// <summary>只停播（收回全部点击 + 停动画），不动实例归属；顺手作废本轮的完成事件认领。</summary>
        void StopPlayback()
        {
            if (View != null) View.CloseTips();      // 浮动框跟着一起收，避免残留在屏幕上
            if (currentObjMgr != null) currentObjMgr.StopSteps();
            currentObjMgr = null;
            playingClipId = null;      // 停掉之后到达的完成事件不再属于本轮，避免又拉起一轮 pingpong
        }

        /// <summary>停播并放开引用、不归还池，最后关掉展示根（用于实例会被卸包统一销毁的场合：离开内容页 / 控制器自身销毁）。</summary>
        void AbandonModel()
        {
            StopPlayback();
            structureModel = null;
            SetShowRootActive(false);
        }

        /// <summary>结构展示根的显隐（InitObjParent.parent）：进结构页装配前打开、收口即关（默认关闭，其下挂着结构页专用渲染相机）。</summary>
        void SetShowRootActive(bool active)
        {
            // WHY: 走静默解析 —— 本方法会被 GlobalUIMgr 的初始状态事件在 1_Content 加载前调到，那时挂点还不存在，
            //      打 Error 会误报成配置错误；真正要装配的 BindAndPlay 走 InitObjParent（解析失败会报错）。
            Transform parent = TryResolveObjParent();
            if (parent == null) return;

            Transform root = parent.parent;
            if (root == null)
            {
                Log.Warning($"[TaskStructureController] {ObjParentPath} 没有父对象，无法控制结构展示根（模型可能不上屏）");
                return;
            }

            if (root.gameObject.activeSelf != active) root.gameObject.SetActive(active);
        }
        #endregion
    }
}
