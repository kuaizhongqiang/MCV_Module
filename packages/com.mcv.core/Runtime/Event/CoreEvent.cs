using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Models.User;
using MCV_Module.Objects.Interactives;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Event
{
    // ── 事件数据结构 ──────────────────────────────────────────

    /// <summary>音量变化事件数据</summary>
    public class AudioVolumeEventData
    {
        public AudioSouceType SourceType;
        public float TargetVolume;

        public AudioVolumeEventData(AudioSouceType sourceType, float targetVolume)
        {
            SourceType = sourceType;
            TargetVolume = targetVolume;
        }
    }

    /// <summary>播放音效事件数据</summary>
    public class AudioPlayEffectEventData
    {
        public AudioEffectType EffectType;

        public AudioPlayEffectEventData(AudioEffectType effectType)
        {
            EffectType = effectType;
        }
    }

    /// <summary>播放音频事件数据</summary>
    public class AudioPlayEventData
    {
        public string AudioName;
        public AudioSouceType SourceType;

        public AudioPlayEventData(string audioName, AudioSouceType sourceType)
        {
            AudioName = audioName;
            SourceType = sourceType;
        }
    }

    // ── 相机事件数据结构 ────────────────────────────────────

    /// <summary>相机背景切换事件数据</summary>
    public class CameraBgChangeEventData
    {
        public bool IsSkybox;

        public CameraBgChangeEventData(bool isSkybox)
        {
            IsSkybox = isSkybox;
        }
    }

    /// <summary>相机混合切换事件数据</summary>
    public class CameraBlendChangeEventData
    {
        public bool IsCut;
        public float BlendTime;

        public CameraBlendChangeEventData(bool isCut, float blendTime = 1f)
        {
            IsCut = isCut;
            BlendTime = blendTime;
        }
    }

    // ────────────────────── 场景加载 ──────────────────────

    public class SceneLoadingEvent
    {
        public string SceneName { get; }
        public float Progress { get; set; }
        public SceneLoadingEvent(string sceneName) { SceneName = sceneName; Progress = 0f; }
    }

    public class SceneLoadedEvent
    {
        public string SceneName { get; }
        public SceneLoadedEvent(string sceneName) { SceneName = sceneName; }
    }

    // ────────────────────── 内容包（一 ProjectClip 一包） ──────────────────────

    // WHY: 面板 OnViewBound 必然早于包加载完成，同步取件只能拿到空；收到本事件才代表 GetSpriteByPackageId / GetPrefabByPackageId 能取到东西。
    /// <summary>内容包就绪事件：某个 ProjectClip 的 AB 包已全部加载完成（遮罩随即关闭）。</summary>
    public class ClipReadyEvent
    {
        /// <summary>就绪的 ProjectClip.id（形如 clip_contactor）。</summary>
        public string ClipId { get; }

        public ClipReadyEvent(string clipId) { ClipId = clipId; }
    }

    // ── 登录事件 ──────────────────────────────────────────────

    /// <summary>登录通过事件数据（白名单验证通过后由 LoginController 发布）。</summary>
    public class LoginSuccessEvent
    {
        /// <summary>登录用户信息（含用户名/用户类型/登录时间）</summary>
        public UserData User;

        public LoginSuccessEvent(UserData user)
        {
            User = user;
        }
    }

    // ── UI 状态事件 ──────────────────────────────────────────

    /// <summary>场景状态变化事件数据（驱动 Canvas 切换 / 重建）</summary>
    public class SceneStateChangeEventData
    {
        public SceneState State;

        public SceneStateChangeEventData(SceneState state)
        {
            State = state;
            Log.Info("SceneStateChangeEventData: " + state);
        }
    }

    /// <summary>任务类型变化事件数据（用户切换任务时发布，驱动 UI Canvas 任务面板重建）。</summary>
    public class TaskTypeChangeEventData
    {
        public ProjectClip Clip;
        public TaskType TaskType;

        public TaskTypeChangeEventData(ProjectClip clip, TaskType taskType)
        {
            Clip = clip;
            TaskType = taskType;
        }
    }

    /// <summary>场景切换请求事件数据（事件驱动加载 AA 场景：先加载新 → 再卸载旧）</summary>
    public class SceneSwitchRequestEvent
    {
        public string SceneName { get; }
        public SceneSwitchRequestEvent(string sceneName) { SceneName = sceneName; }
    }

    // ── 漫游房间 ────────────────────────────────────────────

    // WHY: 房间场景进内容页时会被 UnloadSwitchedScene() 卸载、HUD 随之销毁，确认结果要等用户点完按钮才回来，跳转必须发生在常驻的 MenuController（由 GlobalControllerMgr 常驻持有）上。
    /// <summary>房间项目 HUD「进入项目」请求事件（漫游房间里点展品标签时由 RoomMenuObj 发布）。</summary>
    public class RoomMenuEnterRequestEvent
    {
        /// <summary>请求进入的项目（由发布方按 ProjectClip.id 解析好后带过来，处理方不再查）</summary>
        public ProjectClip Clip { get; }

        public RoomMenuEnterRequestEvent(ProjectClip clip) { Clip = clip; }
    }

    // ── 全局交互事件（统一事件驱动）──────────────────────────────

    /// <summary>全局交互类型</summary>
    public enum GlobalInteractionType
    {
        Enter, Exit, Down, Up, Click, ClickRight, ClickDouble, Move
    }

    /// <summary>全局交互事件数据（GlobalInteractiveMgr 统一发布；经对象池 Get/Release 复用，降低每帧分配）。</summary>
    public class GlobalInteractionEventData
    {
        /// <summary>事件目标；Exit 为原悬停物体；空白点击（无目标）为 null</summary>
        public InteractiveBase Target;

        /// <summary>交互类型</summary>
        public GlobalInteractionType Type;

        /// <summary>Move 事件的鼠标位移增量</summary>
        public Vector2 Delta;

        // 对象池（同步分发，安全回收）
        private static readonly Stack<GlobalInteractionEventData> s_Pool = new Stack<GlobalInteractionEventData>(32);

        /// <summary>从池中取一个事件实例并填充（池空时新建）。</summary>
        public static GlobalInteractionEventData Get(InteractiveBase target, GlobalInteractionType type, Vector2 delta = default)
        {
            var e = s_Pool.Count > 0 ? s_Pool.Pop() : new GlobalInteractionEventData();
            e.Target = target;
            e.Type = type;
            e.Delta = delta;
            return e;
        }

        /// <summary>清空字段并归还池中（Publish 返回后调用）。</summary>
        public void Release()
        {
            Target = null;
            Delta = default;
            s_Pool.Push(this);
        }

        private GlobalInteractionEventData() { }
    }

    // ── 鼠标移动状态事件（GlobalInputMgr 统一判定并发布）──────────────

    /// <summary>鼠标移动状态变化事件数据（GlobalInputMgr 判定状态翻转时发布，只在变化时发一次、不逐帧发）。</summary>
    public class MouseMoveStateEventData
    {
        /// <summary>新状态</summary>
        public MouseMoveState State;

        /// <summary>是否静止（等价 State == MouseMoveState.Idle，便于订阅方直接判断）</summary>
        public bool IsIdle => State == MouseMoveState.Idle;

        public MouseMoveStateEventData(MouseMoveState state)
        {
            State = state;
        }
    }

    // ── 步骤/进程事件（与元件/步骤载荷相关的事件已随 module 包拆分，见 CoreEventModule.cs）──

    /// <summary>全部进程/步骤执行完成事件</summary>
    public class AllStepsCompletedEvent
    {
    }

    // WHY: EventBus<T> 的泛型约束是 where T : class，本事件必须定义为 class，不能改成 struct。
    /// <summary>简化步骤系统（InstControlledManager）跑完全部步骤的完成事件，携带发起执行的项目数据。</summary>
    public class StructInteractiveCompletedEvent
    {
        /// <summary>发起本次简化步骤的项目数据；未传入时为 null</summary>
        public ProjectClip clip;

        public StructInteractiveCompletedEvent(ProjectClip clip = null)
        {
            this.clip = clip;
        }
    }

    /// <summary>下一步请求事件（完成当前步骤，流程进入下一步）</summary>
    public class StepNextRequestEvent
    {
    }

    /// <summary>步骤跳转请求事件（当前进程内跳到指定步骤）</summary>
    public class StepJumpRequestEvent
    {
        public int StepIndex;

        public StepJumpRequestEvent(int stepIndex)
        {
            StepIndex = stepIndex;
        }
    }

    /// <summary>进程跳转请求事件（可指定目标步骤，默认 0）</summary>
    public class ProcessingJumpRequestEvent
    {
        public int ProcessingIndex;
        public int StepIndex;

        public ProcessingJumpRequestEvent(int processingIndex, int stepIndex = 0)
        {
            ProcessingIndex = processingIndex;
            StepIndex = stepIndex;
        }
    }

    // ── 对话框事件（DialogPanel / DialogController 事件驱动）──────────────

    /// <summary>打开对话框请求事件（业务/步骤/交互系统发布，DialogController 订阅并显示）。</summary>
    public class DialogRequestEvent
    {
        /// <summary>对话框身份（回传结果时原样带回，发布方按它认领；纯提示框可传 None）</summary>
        public DialogId Id;
        /// <summary>正文内容</summary>
        public string Content;
        /// <summary>确认按钮文案；null/空 = 面板按语言取默认（ui.dialog.confirm）</summary>
        public string ConfirmLabel;
        /// <summary>取消按钮文案；null/空 = 面板按语言取默认（ui.dialog.cancel）</summary>
        public string CancelLabel;
        /// <summary>是否显示确认按钮（false 时仅文字无按钮）</summary>
        public bool ShowConfirm;
        /// <summary>是否显示取消按钮（false 时隐藏取消按钮）</summary>
        public bool ShowCancel;

        public DialogRequestEvent(DialogId id, string content,
            bool showConfirm = true, bool showCancel = true,
            string confirmLabel = null, string cancelLabel = null)
        {
            Id = id;
            Content = content;
            ShowConfirm = showConfirm;
            ShowCancel = showCancel;
            ConfirmLabel = confirmLabel;
            CancelLabel = cancelLabel;
        }
    }

    /// <summary>对话框结果事件（用户操作后由 DialogController 发布，业务系统订阅）。</summary>
    public class DialogResultEvent
    {
        /// <summary>本次结果对应的对话框身份（发布方按它认领；同一 Id 只应有一个订阅方处理）</summary>
        public DialogId Id;
        /// <summary>是否点击确认（取消为 false）</summary>
        public bool Confirmed;

        public DialogResultEvent(DialogId id, bool confirmed)
        {
            Id = id;
            Confirmed = confirmed;
        }
    }

    /// <summary>应用退出请求事件（业务系统确认「退出」后发布，由常驻管理器如 GlobalSceneMgr 订阅并执行最终退出）。</summary>
    public class AppQuitEvent
    {
    }
}
