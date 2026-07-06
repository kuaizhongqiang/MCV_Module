using System.Collections;
using System.Collections.Generic;
using MCV_Module.Data.Project;
using MCV_Module.Event;
using UnityEngine;

namespace MCV_Module.StepSystem
{
    public class StepDirector : MonoBehaviour
    {
        [SerializeField] private List<ProcessingData> _processings = new List<ProcessingData>();

        private int _currentProcessingIndex;
        private int _currentStepIndex;
        private StepLifecycle _currentLifecycle = StepLifecycle.Idle;
        private Coroutine _executionCoroutine;
        private ConditionFactory _factory;

        public ProcessingData CurrentProcessing =>
            _processings.Count > _currentProcessingIndex ? _processings[_currentProcessingIndex] : null;
        public StepData CurrentStep =>
            CurrentProcessing?.steps.Count > _currentStepIndex ? CurrentProcessing.steps[_currentStepIndex] : null;
        public StepLifecycle CurrentLifecycle => _currentLifecycle;
        public bool IsRunning => _currentLifecycle != StepLifecycle.Idle;
        public IReadOnlyList<ProcessingData> Processings => _processings;

        private void Start()
        {
            _factory = GetComponentInChildren<ConditionFactory>();
            GlobalStepSystemMgr.Instance.RegisterDirector(this);
        }

        public void StartExecution()
        {
            if (_executionCoroutine != null) StopCoroutine(_executionCoroutine);
            _executionCoroutine = StartCoroutine(ExecuteAll());
        }

        public void JumpToStep(int processingIndex, int stepIndex)
        {
            if (_executionCoroutine != null) StopCoroutine(_executionCoroutine);
            _currentProcessingIndex = Mathf.Clamp(processingIndex, 0, _processings.Count - 1);
            _currentStepIndex = Mathf.Clamp(stepIndex, 0, CurrentProcessing?.steps.Count - 1 ?? 0);
            _currentLifecycle = StepLifecycle.Idle;
            _executionCoroutine = StartCoroutine(ExecuteAll());
        }

        /// <summary>标记当前步骤完成</summary>
        public void CompleteCurrentStep()
        {
            if (_currentLifecycle == StepLifecycle.Waiting)
                _currentLifecycle = StepLifecycle.Complete;
        }

        /// <summary>跳过当前步骤（无需等待条件满足）</summary>
        public void SkipCurrentStep()
        {
            if (_currentLifecycle == StepLifecycle.Waiting || _currentLifecycle == StepLifecycle.Prepare)
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
            // 在步骤开始时通过 ConditionFactory 创建 Condition
            if (_factory != null && step.conditions != null)
            {
                foreach (var config in step.conditions)
                    _factory.CreateCondition(config);
            }

            // Phase 1: Prepare
            _currentLifecycle = StepLifecycle.Prepare;
            EventBus<StepPreparedEvent>.Publish(new StepPreparedEvent(step.stepId));
            yield return null;

            // Phase 2: Waiting
            _currentLifecycle = StepLifecycle.Waiting;
            EventBus<StepWaitingEvent>.Publish(new StepWaitingEvent(step.stepId));

            if (step.executeType == StepExecuteType.Auto)
            {
                yield return null;
            }
            else
            {
                float elapsed = 0f;
                bool timedOut = false;
                while (_currentLifecycle == StepLifecycle.Waiting)
                {
                    if (step.timeoutSeconds > 0 && elapsed >= step.timeoutSeconds)
                    {
                        timedOut = true;
                        break;
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (timedOut)
                {
                    EventBus<StepTimeoutEvent>.Publish(new StepTimeoutEvent(step.stepId, step.timeoutSeconds));
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
