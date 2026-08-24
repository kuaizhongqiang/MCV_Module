using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Models.User;
using MCV_Module.Objects.Interactives;
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

    // ── 登录事件 ──────────────────────────────────────────────

    /// <summary>
    /// 登录通过事件数据（白名单验证通过后由 LoginController 发布。
    /// 「登录成功后的执行」暂为空：后续业务在此订阅做场景切换等处理）。
    /// </summary>
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
        }
    }

    /// <summary>
    /// 任务类型变化事件数据（用户切换任务时发布，驱动 UI Canvas 任务面板重建）。
    /// 订阅方：GlobalUIMgr（重建面板）、TaskListController（同步当前任务状态）。
    /// </summary>
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

    // ── 全局交互事件（统一事件驱动）──────────────────────────────

    /// <summary>全局交互类型</summary>
    public enum GlobalInteractionType
    {
        Enter, Exit, Down, Up, Click, ClickRight, ClickDouble, Move
    }

    /// <summary>
    /// 全局交互事件数据（GlobalInteractiveMgr 统一发布；
    /// 元件 Mo* 事件由管理器直接派发；连线状态机、步骤条件等全局逻辑订阅本事件处理）。
    /// 同步分发（Publish 返回后无人持有），经对象池 Get/Release 复用，降低每帧分配。
    /// </summary>
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

    // ── 步骤/进程事件（与元件/步骤载荷相关的事件已随 module 包拆分，见 CoreEventModule.cs）──

    /// <summary>全部进程/步骤执行完成事件</summary>
    public class AllStepsCompletedEvent
    {
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

    /// <summary>
    /// 打开对话框请求事件（业务/步骤/交互系统发布，DialogController 订阅并显示）。
    /// 订阅方：DialogController。
    /// 结构：标题 + 文字 + 两个按钮（确认/取消），按钮可按 ShowConfirm/ShowCancel 决定显隐。
    /// </summary>
    public class DialogRequestEvent
    {
        /// <summary>对话框标题</summary>
        public string Title;
        /// <summary>正文内容</summary>
        public string Content;
        /// <summary>确认按钮文案（默认「确认」）</summary>
        public string ConfirmLabel;
        /// <summary>取消按钮文案（默认「取消」）</summary>
        public string CancelLabel;
        /// <summary>是否显示确认按钮（false 时仅文字无按钮）</summary>
        public bool ShowConfirm;
        /// <summary>是否显示取消按钮（false 时隐藏取消按钮）</summary>
        public bool ShowCancel;

        public DialogRequestEvent(string title, string content,
            bool showConfirm = true, bool showCancel = true,
            string confirmLabel = "确认", string cancelLabel = "取消")
        {
            Title = title;
            Content = content;
            ShowConfirm = showConfirm;
            ShowCancel = showCancel;
            ConfirmLabel = confirmLabel;
            CancelLabel = cancelLabel;
        }
    }

    /// <summary>
    /// 对话框结果事件（用户操作后由 DialogController 发布，业务系统订阅）。
    /// Confirmed 为 true 表示点击了确认按钮，false 表示点击了取消按钮。
    /// </summary>
    public class DialogResultEvent
    {
        /// <summary>本次结果对应的请求标题（用于区分并发对话框）</summary>
        public string Title;
        /// <summary>是否点击确认（取消为 false）</summary>
        public bool Confirmed;

        public DialogResultEvent(string title, bool confirmed)
        {
            Title = title;
            Confirmed = confirmed;
        }
    }
}
