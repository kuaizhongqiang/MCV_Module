using System;
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models.Project;
using MCV_Module.Objects.Interactives;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Managers.InstManagers
{
    // WHY: 只支持点击与播动画两种能力，无 NextStep / 跳转 / 打断；clickObj 与 controlAnim 全在 Inspector 手挂，不加载资源、不走对象池
    /// <summary>轻量步骤执行器：线性跑一遍「点击 + 播动画」，供不接入 StepManager 的演示场景（挂在结构模型预制体上）。</summary>
    public class InstControlledManager : MonoBehaviour
    {
        #region 参数
        [Tooltip("承载全部步骤动画的 Legacy Animation 组件（clip 必须是非循环动画，否则等播完会卡住）")]
        [SerializeField] Animation controlAnim;

        [Tooltip("步骤列表：按列表顺序执行；每个元素 = 一个点击对象 + 一段动画 + 延迟")]
        [SerializeField] List<SimpleStepStruct> simpleSteps = new List<SimpleStepStruct>();

        static InstControlledManager s_Instance;
        #endregion

        #region 运行时状态
        /// <summary>当前步骤下标（-1 = 未开始 / 已结束）</summary>
        int currentIndex = -1;
        /// <summary>执行协程句柄</summary>
        Coroutine executionCoroutine;
        /// <summary>Waiting 阶段的交互事件委托（退订用）</summary>
        Action<GlobalInteractionEventData> interactionHandler;
        /// <summary>当前步骤是否已被点击</summary>
        bool waitClicked;
        /// <summary>是否正在执行</summary>
        bool isRunning;
        /// <summary>本次执行携带的项目数据（完成事件回传）</summary>
        ProjectClip clip;
        #endregion

        #region 单例
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
        #endregion

        #region 对外属性
        /// <summary>是否正在执行</summary>
        public bool IsRunning => isRunning;

        /// <summary>当前步骤下标（-1 = 未开始 / 已结束）</summary>
        public int CurrentIndex => currentIndex;

        /// <summary>步骤总数</summary>
        public int StepCount => simpleSteps != null ? simpleSteps.Count : 0;
        #endregion

        #region 生命周期
        void Awake()
        {
            // WHY: 刻意不做「发现重复就销毁后来的」——本组件挂在结构模型预制体根上，Destroy 要等帧末生效，同帧重新实例化时会误杀新模型
            s_Instance = this;

            if (simpleSteps == null) simpleSteps = new List<SimpleStepStruct>();
        }

        void OnDestroy()
        {
            StopSteps();
            if (s_Instance == this) s_Instance = null;
        }
        #endregion

        #region 公开 API
        /// <summary>从第 0 步开始执行（可重复调用，会先复位）：全部 clickObj 先收回，再由各步骤逐个放开。</summary>
        public void StartSteps(ProjectClip clip = null)
        {
            this.clip = clip;

            StopSteps();                                  // 先复位（含收回全部交互、停动画）

            if (StepCount == 0)
            {
                Log.Warning("[InstControlledManager] simpleSteps 为空，无步骤可执行，直接发布完成事件");
                PublishCompleted();
                return;
            }

            isRunning = true;
            executionCoroutine = StartCoroutine(ExecuteAll());
        }

        // WHY: 刻意不发布 StructInteractiveCompletedEvent —— 那是「手动流程跑完一次」的计分信号，来回播会反复被当成又完成一遍
        /// <summary>自动播放：依次播完每步 clip，不等点击、不执行 delay（与手动流程是两套独立入口）。</summary>
        public void PlayAuto(float speed, Action onComplete = null)
        {
            // WHY: 自动播期间全部零件收回 Disabled（连碰撞体一起关）—— 零件在动，谁挡谁不好说，干脆全关
            StopSteps();

            if (StepCount == 0)
            {
                Log.Warning("[InstControlledManager] simpleSteps 为空，自动播放无可播内容");
                onComplete?.Invoke();
                return;
            }

            isRunning = true;
            executionCoroutine = StartCoroutine(PlayAutoAll(speed, onComplete));
        }

        /// <summary>停止执行并复位（全部 clickObj 收回 <see cref="StepObjState.Disabled"/>、停止动画、下标归 -1）。</summary>
        public void StopSteps()
        {
            if (executionCoroutine != null) StopCoroutine(executionCoroutine);
            executionCoroutine = null;
            isRunning = false;

            UnsubscribeInteraction();                     // 正常流程已在 Waiting 尾部退订，这里防御性兜底
            waitClicked = false;

            SetAllInteractable(StepObjState.Disabled);
            if (controlAnim != null) controlAnim.Stop();
            currentIndex = -1;
        }
        #endregion

        #region 执行
        IEnumerator ExecuteAll()
        {
            for (int i = 0; i < simpleSteps.Count; i++)
            {
                currentIndex = i;
                yield return ExecuteStep(simpleSteps[i]);
            }

            currentIndex = -1;
            isRunning = false;
            executionCoroutine = null;
            PublishCompleted();
        }

        /// <summary>自动播放遍历：speed > 0 走 0→N 逐段拆开，speed < 0 走 N→0 逐段倒放（才是原路返回）。</summary>
        IEnumerator PlayAutoAll(float speed, Action onComplete)
        {
            // WHY: 必须先让出一帧，保证协程不会在 StartCoroutine 调用栈内同步跑完（空 clip + pingpong 回调会无限递归爆栈）
            yield return null;

            speed = NormalizeSpeed(speed);      // 0 归一为 1，保证下面的方向判定与 clip 播放速度一致
            if (speed > 0f)
            {
                for (int i = 0; i < simpleSteps.Count; i++)
                {
                    currentIndex = i;
                    yield return PlayClipAndWait(simpleSteps[i], speed);
                }
            }
            else
            {
                for (int i = simpleSteps.Count - 1; i >= 0; i--)
                {
                    currentIndex = i;
                    yield return PlayClipAndWait(simpleSteps[i], speed);
                }
            }

            currentIndex = -1;
            isRunning = false;
            executionCoroutine = null;   // **先清句柄再回调**：回调里通常会立刻发起下一轮，StopSteps 才不会误停
            onComplete?.Invoke();
        }

        /// <summary>单步生命周期：Prepare → Waiting → Complete → 步骤延迟（对齐既有步骤系统）。</summary>
        IEnumerator ExecuteStep(SimpleStepStruct step)
        {
            yield return StepPrepare(step);
            yield return StepWaiting(step);
            yield return StepComplete(step);

            if (step.delay > 0f) yield return new WaitForSeconds(step.delay);
        }

        /// <summary>阶段①准备：放开 clickObj 交互 + 动画归位到起始帧。</summary>
        IEnumerator StepPrepare(SimpleStepStruct step)
        {
            Log.Verbose($"[InstControlledManager] 第 {currentIndex + 1}/{StepCount} 步开始（index={step.index}）");

            SetInteractable(step.clickObj, StepObjState.Clickable);

            SampleStartFrame(step, NormalizeSpeed(step.animSpeed));
            yield break;
        }

        /// <summary>阶段②等待：clickObj 非空等点击，为空则直接返回（进入 Complete 播动画）。</summary>
        IEnumerator StepWaiting(SimpleStepStruct step)
        {
            if (step.clickObj == null) yield break;

            waitClicked = false;
            interactionHandler = OnInteraction;
            EventBus<GlobalInteractionEventData>.Subscribe(interactionHandler);

            while (!waitClicked) yield return null;

            UnsubscribeInteraction();

            // WHY: 点击后转入 HoverOnly（保住悬停提示框）；「不能点击」由步骤系统保证——只认当前 Waiting 步骤的 clickObj
            SetInteractable(step.clickObj, StepObjState.HoverOnly);
        }

        /// <summary>阶段③完成：按步骤的 animSpeed 播放 clip 并等播完（负值 = 倒放）。</summary>
        IEnumerator StepComplete(SimpleStepStruct step)
        {
            yield return PlayClipAndWait(step, NormalizeSpeed(step.animSpeed));
        }

        /// <summary>播放一个步骤的 clip 并等它播完；<paramref name="speed"/> 可正可负（负 = 倒放，从末帧起步）。</summary>
        IEnumerator PlayClipAndWait(SimpleStepStruct step, float speed)
        {
            if (controlAnim == null || step.clip == null) yield break;

            speed = NormalizeSpeed(speed);
            controlAnim.gameObject.SetActive(true);
            controlAnim.clip = step.clip;

            AnimationState state = controlAnim[step.clip.name];
            if (state != null)
            {
                state.speed = speed;
                state.normalizedTime = speed < 0f ? 1f : 0f;   // 倒放必须从末帧起步
            }

            controlAnim.Play();
            yield return null;                                 // 等一帧让 Play 生效，避免 isPlaying 尚未翻转就判完
            while (!IsClipFinished(state, speed)) yield return null;
        }
        #endregion

        #region 私有方法
        /// <summary>点击判定：只认当前步骤 clickObj 身上发出的 Click。</summary>
        void OnInteraction(GlobalInteractionEventData data)
        {
            if (data == null || data.Type != GlobalInteractionType.Click || data.Target == null) return;
            if (currentIndex < 0 || currentIndex >= StepCount) return;
            if (simpleSteps[currentIndex].clickObj != data.Target) return;

            waitClicked = true;
        }

        /// <summary>动画归位到起始帧并停在那一帧（Play + Sample + Stop，与 StepHandler 的控帧约定一致）。</summary>
        void SampleStartFrame(SimpleStepStruct step, float speed)
        {
            if (controlAnim == null || step.clip == null) return;

            controlAnim.gameObject.SetActive(true);
            controlAnim.clip = step.clip;

            AnimationState state = controlAnim[step.clip.name];
            if (state != null)
            {
                state.speed = speed;
                state.normalizedTime = speed < 0f ? 1f : 0f;   // 倒放定格末帧
            }

            controlAnim.Play();
            controlAnim.Sample();
            controlAnim.Stop();
        }

        // WHY: 倒放在 WrapMode.Once 下不一定及时把 isPlaying 翻假，只靠它会在末尾干等；顺带把误配循环动画从「永远卡住」降级为「播一遍就下一步」
        /// <summary>片段是否播完：先看 isPlaying，再用 normalizedTime 兜边界（正放 =1，倒放 =0）。</summary>
        bool IsClipFinished(AnimationState state, float speed)
        {
            if (controlAnim == null || !controlAnim.isPlaying) return true;
            if (state == null) return false;

            float t = state.normalizedTime;
            return speed < 0f ? t <= 0f : t >= 1f;
        }

        /// <summary>规范化播放速度：0 视为 1（否则动画永远播不完，步骤会卡死）。</summary>
        static float NormalizeSpeed(float speed)
        {
            return Mathf.Approximately(speed, 0f) ? 1f : speed;
        }

        /// <summary>单个 clickObj 的交互档位。</summary>
        enum StepObjState
        {
            /// <summary>未轮到：彻底关掉（<c>IsInteractable=false</c> + 碰撞体关）—— 射线直接穿过去，不挡后面的目标。</summary>
            Disabled,

            /// <summary>轮到这一步：可点击。</summary>
            Clickable,

            /// <summary>已完成：仍可悬停（提示框跟手）但不再接受点击；两个开关都保持打开。</summary>
            HoverOnly,
        }

        void SetAllInteractable(StepObjState state)
        {
            if (simpleSteps == null) return;
            for (int i = 0; i < simpleSteps.Count; i++)
                SetInteractable(simpleSteps[i].clickObj, state);
        }

        // WHY: GlobalInteractiveMgr 取最前面的碰撞体再判 IsInteractable，只关标志会让前方零件吃掉射线（点不到后方目标）；HoverOnly 必须保留碰撞体，否则连悬停都没有
        /// <summary>切换交互档位：必须同时切 IsInteractable 与身上所有 Collider.enabled。</summary>
        static void SetInteractable(InteractiveBase obj, StepObjState state)
        {
            if (obj == null) return;

            bool on = state != StepObjState.Disabled;      // HoverOnly 也要留碰撞体
            obj.IsInteractable = on;

            var colliders = obj.GetComponents<Collider>();   // 一个物体可能挂多个碰撞体，全部一起切
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = on;

            // 没有碰撞体 = 悬停/点击都收不到，属配置问题，打开时提示一次
            if (on && colliders.Length == 0)
                Log.Warning($"[InstControlledManager] {obj.name} 上没有 Collider，悬停与点击都收不到");
        }

        void UnsubscribeInteraction()
        {
            if (interactionHandler == null) return;
            EventBus<GlobalInteractionEventData>.Unsubscribe(interactionHandler);
            interactionHandler = null;
        }

        void PublishCompleted()
        {
            EventBus<StructInteractiveCompletedEvent>.Publish(new StructInteractiveCompletedEvent(clip));
        }
        #endregion

        #region 步骤数据
        // WHY: 「点击 + 动画」的组合规则 —— clickObj 非空则等点击后播动画，为空则不等点击、直接播动画。
        /// <summary>简化步骤条目 —— 一步的全部配置。</summary>
        [Serializable]
        public struct SimpleStepStruct
        {
            [Tooltip("作者标注的步骤序号（仅用于日志排查；执行顺序以列表顺序为准）")]
            public int index;

            [Tooltip("本步骤要播放的动画片段；为空则本步骤只有点击（点击后直接进入延迟）")]
            public AnimationClip clip;

            [Tooltip("本步骤要求被点击的交互物体；为空则不等点击、直接播动画")]
            public InteractiveBase clickObj;

            [Tooltip("动画播完后的延迟秒数（<=0 = 不延迟）")]
            public float delay;

            [Tooltip("动画播放速度；负值 = 倒放，0 会被当作 1")]
            public float animSpeed;
        }
        #endregion
    }
}
