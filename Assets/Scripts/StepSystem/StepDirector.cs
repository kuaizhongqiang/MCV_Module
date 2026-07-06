using System;
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Data.Project;
using MCV_Module.Event;
using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>
    /// 步骤执行器 —— 三级驱动引擎。
    /// 生命周期：Processing → Step → Condition，逐级推进。
    /// 每步三阶段：Prepare → Waiting → Complete。
    /// </summary>
    public class StepDirector : MonoBehaviour
    {
        [SerializeField] private List<ProcessingData> _processings = new List<ProcessingData>();

        private int _currentProcessingIndex;
        private int _currentStepIndex;
        private StepLifecycle _currentLifecycle = StepLifecycle.Idle;
        private Coroutine _executionCoroutine;

        public ProcessingData CurrentProcessing =>
            _processings.Count > _currentProcessingIndex ? _processings[_currentProcessingIndex] : null;
        public StepData CurrentStep =>
            CurrentProcessing?.steps.Count > _currentStepIndex ? CurrentProcessing.steps[_currentStepIndex] : null;
        public StepLifecycle CurrentLifecycle => _currentLifecycle;
        public bool IsRunning => _currentLifecycle != StepLifecycle.Idle;

        /// <summary>注册到 GlobalStepSystemMgr</summary>
        private void Start()
        {
            GlobalStepSystemMgr.Instance.RegisterDirector(this);
        }

        /// <summary>开始执行全部工序</summary>
        public void StartExecution()
        {
            if (_executionCoroutine != null) StopCoroutine(_executionCoroutine);
            _executionCoroutine = StartCoroutine(ExecuteAll());
        }

        /// <summary>跳转到指定步骤（P0S0 快进）</summary>
        public void JumpToStep(int processingIndex, int stepIndex)
        {
            if (_executionCoroutine != null) StopCoroutine(_executionCoroutine);

            _currentProcessingIndex = Mathf.Clamp(processingIndex, 0, _processings.Count - 1);
            _currentStepIndex = Mathf.Clamp(stepIndex, 0, CurrentProcessing?.steps.Count - 1 ?? 0);
            _currentLifecycle = StepLifecycle.Idle;

            _executionCoroutine = StartCoroutine(ExecuteAll());
        }

        /// <summary>标记当前步骤完成（由 Condition 触发）</summary>
        public void CompleteCurrentStep()
        {
            if (_currentLifecycle == StepLifecycle.Waiting)
                _currentLifecycle = StepLifecycle.Complete;
        }

        private IEnumerator ExecuteAll()
        {
            for (int p = _currentProcessingIndex; p < _processings.Count; p++)
            {
                _currentProcessingIndex = p;
                EventBus<ProcessChangedEvent>.Publish(new ProcessChangedEvent(_processings[p].processingId));

                var steps = _processings[p].steps;
                for (int s = (p == _currentProcessingIndex ? _currentStepIndex : 0); s < steps.Count; s++)
                {
                    _currentStepIndex = s;
                    yield return ExecuteStep(steps[s]);
                }
                _currentStepIndex = 0;
            }

            EventBus<AllStepsCompletedEvent>.Publish(new AllStepsCompletedEvent());
            _currentLifecycle = StepLifecycle.Idle;
        }

        private IEnumerator ExecuteStep(StepData step)
        {
            // Phase 1: Prepare
            _currentLifecycle = StepLifecycle.Prepare;
            EventBus<StepPreparedEvent>.Publish(new StepPreparedEvent(step.stepId));
            yield return null;

            // Phase 2: Waiting
            _currentLifecycle = StepLifecycle.Waiting;
            EventBus<StepWaitingEvent>.Publish(new StepWaitingEvent(step.stepId));

            if (step.executeType == StepExecuteType.Auto)
            {
                // 自动执行：直接完成
                yield return null;
            }
            else
            {
                // 等待交互：由 Condition 触发 CompleteCurrentStep
                float elapsed = 0f;
                while (_currentLifecycle == StepLifecycle.Waiting)
                {
                    if (step.timeoutSeconds > 0 && elapsed >= step.timeoutSeconds)
                        break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // Phase 3: Complete
            _currentLifecycle = StepLifecycle.Complete;
            EventBus<StepCompletedEvent>.Publish(new StepCompletedEvent(step.stepId));
        }

        private void OnDestroy()
        {
            if (GlobalStepSystemMgr.Exists)
                GlobalStepSystemMgr.Instance.UnregisterDirector(this);
        }
    }

    public enum StepLifecycle
    {
        Idle,
        Prepare,
        Waiting,
        Complete,
    }
}
