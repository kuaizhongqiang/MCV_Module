using System.Collections;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Singleton;
using MCV_Module.Steps;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Managers.Steps
{
    /// <summary>步骤导演：协程驱动全部进程/步骤（Prepare→Waiting→Complete），发布状态事件，处理延迟、下一步、跳转与跳过。</summary>
    public class StepManager : SingletonBase
    {
        #region 参数
        static StepManager instance;
        public static StepManager Instance { get => instance; set => instance = value; }

        /// <summary>步骤间延迟（秒）</summary>
        [SerializeField] float stepDelayTime = 0.5f;
        /// <summary>进程间延迟（秒）</summary>
        [SerializeField] float processingDelayTime = 0.3f;

        // WHY: 检测预制体在任务切换事件里立刻实例化，而画布重建要等上一画布淡出（约 0.3s）；当场开跑会让 Start 弹的 StepUIPanel 被 ClearPanels() 销毁，面板不显示、流程卡死。等待信号是 GlobalUIMgr.CanvasRebuildVersion，不是固定秒数
        /// <summary>启动步骤链前最多等多久让内容页画布完成重建（秒）；0 = 立即启动。</summary>
        [SerializeField, Tooltip("启动步骤链前最多等多久让画布完成重建（秒）：0 = 立即启动（旧行为）。" +
                                 "检测预制体实例化早于画布重建约 0.3 秒，不等会导致说明面板被清掉、流程卡死")]
        float canvasRebuildWaitTimeout = 1.5f;

        List<ProcessingHandler> processingHandlers = new List<ProcessingHandler>();

        int currentProcessingIndex = -1;
        int currentStepIndex = -1;
        StepHandler currentStep;
        StepLifecycle lifecycle = StepLifecycle.Idle;
        Coroutine executionCoroutine;
        /// <summary>Finish 步骤已触发全部完成（防止 ExecuteAll/快进重复推进）</summary>
        bool isFinished;

        /// <summary>当前进程</summary>
        public ProcessingHandler CurrentProcessing =>
            currentProcessingIndex >= 0 && currentProcessingIndex < processingHandlers.Count
                ? processingHandlers[currentProcessingIndex] : null;

        /// <summary>当前步骤</summary>
        public StepHandler CurrentStep => currentStep;

        /// <summary>当前执行生命周期状态</summary>
        public StepLifecycle CurrentLifecycle => lifecycle;

        /// <summary>是否正在执行</summary>
        public bool IsRunning => lifecycle != StepLifecycle.Idle;
        #endregion

        #region 生命周期
        void Awake()
        {
            instance = this;
            // 收集子 ProcessingHandler
            processingHandlers.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var ph = transform.GetChild(i).GetComponent<ProcessingHandler>();
                if (ph != null) processingHandlers.Add(ph);
            }
        }

        protected override IEnumerator DelayInit()
        {
            // 事件驱动入口（强引用，OnDestroy 必须退订）
            EventBus<StepNextRequestEvent>.Subscribe(OnStepNextRequested);
            EventBus<StepJumpRequestEvent>.Subscribe(OnStepJumpRequested);
            EventBus<ProcessingJumpRequestEvent>.Subscribe(OnProcessingJumpRequested);

            // ① 步骤初始化：初始化所有步骤的条件
            for (int i = 0; i < processingHandlers.Count; i++)
            {
                List<StepHandler> steps = processingHandlers[i].GetSteps();
                for (int j = 0; j < steps.Count; j++)
                {
                    if (steps[j].condition != null)
                        steps[j].condition.ConditionInit(steps[j]);
                }
            }

            isInit = true;

            // ② 等这一轮画布重建收口，再开始执行（见 canvasRebuildWaitTimeout 的说明）
            if (processingHandlers.Count > 0)
            {
                yield return WaitCanvasRebuild();
                StartExecution();
            }
            yield break;
        }

        /// <summary>等画布重建完成再启动；让出一帧 / 未在切换 / 超时三种收口，避免死等。</summary>
        IEnumerator WaitCanvasRebuild()
        {
            if (canvasRebuildWaitTimeout <= 0f) yield break;

            int version = GlobalUIMgr.CanvasRebuildVersion;
            float elapsed = 0f;

            yield return null;                  // ① 让出第一帧
            elapsed += Time.unscaledDeltaTime;

            // ② 只在"正在切换"时等重建；没在切换说明画布稳定，直接走
            while (GlobalUIMgr.IsSwitching
                && GlobalUIMgr.CanvasRebuildVersion == version
                && elapsed < canvasRebuildWaitTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            bool rebuilt = GlobalUIMgr.CanvasRebuildVersion != version;
            Log.Info($"{name}: 步骤链启动前等待 {elapsed * 1000f:0} ms（{(rebuilt ? "已等到画布重建完成" : "画布未在切换/超时，直接启动")}）");
        }

        void OnDestroy()
        {
            if (executionCoroutine != null) StopCoroutine(executionCoroutine);
            executionCoroutine = null;
            EventBus<StepNextRequestEvent>.Unsubscribe(OnStepNextRequested);
            EventBus<StepJumpRequestEvent>.Unsubscribe(OnStepJumpRequested);
            EventBus<ProcessingJumpRequestEvent>.Unsubscribe(OnProcessingJumpRequested);
            if (instance == this) instance = null;
        }
        #endregion

        #region 公开方法
        /// <summary>开始执行（从当前索引开始；初始从第 0 个进程第 0 步）</summary>
        public void StartExecution()
        {
            if (processingHandlers.Count == 0) return;
            if (currentProcessingIndex < 0) currentProcessingIndex = 0;
            if (currentStepIndex < 0) currentStepIndex = 0;
            if (executionCoroutine != null) StopCoroutine(executionCoroutine);
            executionCoroutine = StartCoroutine(ExecuteAll());
        }

        // WHY: 跳转必须「全员归位 + 快进前缀 + 目标正常执行」，否则动画状态与当前步骤不一致（对齐 Tuanjie）。
        /// <summary>跳转到指定进程的指定步骤。</summary>
        public void JumpToStep(int processingIndex, int stepIndex)
        {
            if (processingHandlers.Count == 0) return;
            processingIndex = Mathf.Clamp(processingIndex, 0, processingHandlers.Count - 1);
            int count = processingHandlers[processingIndex].StepCount;
            stepIndex = Mathf.Clamp(stepIndex, 0, Mathf.Max(0, count - 1));
            if (executionCoroutine != null) StopCoroutine(executionCoroutine);
            executionCoroutine = StartCoroutine(JumpTo(processingIndex, stepIndex));
        }

        /// <summary>跳转进程（默认从该进程第 0 步开始）</summary>
        public void JumpToProcessing(int processingIndex) => JumpToStep(processingIndex, 0);

        /// <summary>跳转步骤（当前进程内）</summary>
        public void JumpToStep(int stepIndex) => JumpToStep(currentProcessingIndex, stepIndex);

        /// <summary>上一步：同工序上一步 / 上一工序最后一步（走 JumpToStep，保证状态一致）</summary>
        public void PrevStep()
        {
            if (currentProcessingIndex < 0 || processingHandlers.Count == 0) return;
            if (currentStepIndex > 0)
                JumpToStep(currentProcessingIndex, currentStepIndex - 1);
            else if (currentProcessingIndex > 0)
                JumpToStep(currentProcessingIndex - 1, processingHandlers[currentProcessingIndex - 1].StepCount - 1);
        }

        /// <summary>跳转指定进程（从该进程第 0 步开始）</summary>
        public void SetProcessing(int processingIndex) => JumpToStep(processingIndex, 0);

        /// <summary>停止执行（复位到 Idle）</summary>
        public void StopExecution()
        {
            if (executionCoroutine != null) StopCoroutine(executionCoroutine);
            executionCoroutine = null;
            lifecycle = StepLifecycle.Idle;
        }

        /// <summary>下一步：强制完成当前步骤（ForceComplete 协作式打断 Waiting 协程），流程进入下一步</summary>
        public void NextStep()
        {
            if (lifecycle != StepLifecycle.Prepare && lifecycle != StepLifecycle.Waiting) return;
            if (currentStep != null && currentStep.condition != null)
                currentStep.condition.ForceComplete();
        }

        /// <summary>跳过当前步骤（无需条件满足）</summary>
        public void SkipCurrentStep() => NextStep();

        /// <summary>标记当前步骤完成（后续条件检测到满足时可调用）</summary>
        public void CompleteCurrentStep() => NextStep();
        #endregion

        #region 私有方法
        void OnStepNextRequested(StepNextRequestEvent e) => NextStep();
        void OnStepJumpRequested(StepJumpRequestEvent e) => JumpToStep(e.StepIndex);
        void OnProcessingJumpRequested(ProcessingJumpRequestEvent e) => JumpToStep(e.ProcessingIndex, e.StepIndex);

        IEnumerator ExecuteAll()
        {
            isFinished = false;
            int startProcessing = currentProcessingIndex;
            int startStep = currentStepIndex;

            for (int p = startProcessing; p < processingHandlers.Count; p++)
            {
                currentProcessingIndex = p;
                var handler = processingHandlers[p];
                EventBus<ProcessChangedEvent>.Publish(new ProcessChangedEvent(p, handler));

                // 进程间延迟（起始进程不延迟）
                if (p > startProcessing) yield return new WaitForSeconds(processingDelayTime);

                List<StepHandler> steps = handler.GetSteps();
                for (int s = (p == startProcessing ? startStep : 0); s < steps.Count; s++)
                {
                    currentStepIndex = s;
                    currentStep = steps[s];
                    yield return ExecuteStep(currentStep, p, s);
                    if (isFinished) yield break; // Finish 步骤已触发全部完成
                }
                currentStepIndex = 0;
                currentStep = null;
            }

            lifecycle = StepLifecycle.Idle;
            EventBus<AllStepsCompletedEvent>.Publish(new AllStepsCompletedEvent());
        }

        IEnumerator ExecuteStep(StepHandler step, int processingIndex, int stepIndex)
        {
            var condition = step.condition;

            if (condition == null)
            {
                // 无条件：视为立即完成
                EventBus<StepPreparedEvent>.Publish(new StepPreparedEvent(step, processingIndex, stepIndex));
                EventBus<StepWaitingEvent>.Publish(new StepWaitingEvent(step, processingIndex, stepIndex));
                EventBus<StepCompletedEvent>.Publish(new StepCompletedEvent(step, processingIndex, stepIndex));
                yield return new WaitForSeconds(stepDelayTime);

                if (IsFinishStep(step)) EndChain();
                yield break;
            }

            // ① Prepare —— 完成后发布
            lifecycle = StepLifecycle.Prepare;
            yield return condition.Prepare();
            EventBus<StepPreparedEvent>.Publish(new StepPreparedEvent(step, processingIndex, stepIndex));

            // ② Waiting —— 完成后发布
            lifecycle = StepLifecycle.Waiting;
            yield return condition.Waiting();
            EventBus<StepWaitingEvent>.Publish(new StepWaitingEvent(step, processingIndex, stepIndex));

            // ③ Complete —— 完成后发布
            lifecycle = StepLifecycle.Complete;
            yield return condition.Complete();
            EventBus<StepCompletedEvent>.Publish(new StepCompletedEvent(step, processingIndex, stepIndex));

            // WHY: Finish 步骤要先正常走完三阶段再收尾 —— 旧实现识别到 Finish 就跳过三阶段，配在 Finish 上的说明面板永远弹不出来
            if (IsFinishStep(step))
            {
                EndChain();
                yield break;
            }

            // ⑤ 步骤间延迟
            yield return new WaitForSeconds(stepDelayTime);
        }

        /// <summary>是否为终结步骤（Finish）：执行完本步即整条链结束。</summary>
        static bool IsFinishStep(StepHandler step) => step != null && step.Type == ConditionType.Finish;

        /// <summary>收尾：标记已结束并发布「全部完成」（两个循环里都有 isFinished 判断，不会补发）。</summary>
        void EndChain()
        {
            isFinished = true;
            lifecycle = StepLifecycle.Idle;
            EventBus<AllStepsCompletedEvent>.Publish(new AllStepsCompletedEvent());
        }

        /// <summary>跳转到目标：全员归位 + 前缀快进（动画跳末帧）+ 目标起正常执行并继续推进。</summary>
        IEnumerator JumpTo(int targetProcess, int targetStep)
        {
            isFinished = false;
            // 全员归位：所有条件 Reset + Prepare（显隐归位、隐藏动画、隐藏交互物/关面板/取消临时线）
            yield return PrepareAllConditions();

            for (int p = 0; p < processingHandlers.Count; p++)
            {
                currentProcessingIndex = p;
                var handler = processingHandlers[p];
                if (p > 0) yield return new WaitForSeconds(processingDelayTime); // 进程间延迟
                EventBus<ProcessChangedEvent>.Publish(new ProcessChangedEvent(p, handler));

                List<StepHandler> steps = handler.GetSteps();
                for (int s = 0; s < steps.Count; s++)
                {
                    currentStepIndex = s;
                    currentStep = steps[s];
                    bool beforeTarget = (p < targetProcess) || (p == targetProcess && s < targetStep);
                    if (beforeTarget)
                        yield return steps[s].condition.FastForward();
                    else
                        yield return ExecuteStep(steps[s], p, s);
                    if (isFinished) yield break;
                }
                if (isFinished) yield break;
            }

            lifecycle = StepLifecycle.Idle;
        }

        /// <summary>全员归位：每个条件 Reset + Prepare（跳转前置、保证状态一致）</summary>
        IEnumerator PrepareAllConditions()
        {
            for (int p = 0; p < processingHandlers.Count; p++)
            {
                var handler = processingHandlers[p];
                foreach (var step in handler.GetSteps())
                {
                    if (step.condition == null) continue;
                    step.condition.ResetCondition();
                    yield return step.condition.Prepare();
                }
            }
        }
        #endregion
    }

    /// <summary>步骤执行生命周期</summary>
    public enum StepLifecycle
    {
        Idle,
        Prepare,
        Waiting,
        Complete,
    }
}
