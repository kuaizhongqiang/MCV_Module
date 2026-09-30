using System;
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.UI.Panels;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: 面板按需创建且不缓存（随 Canvas 重建被清），首次创建时 Start（→ View 绑定）尚未执行，故本类不依赖 View 属性、显示路径直接持有取到的面板实例；题库只读，打乱选项前必须克隆；答错保留本题可改选重试
    /// <summary>步骤答题调度（C）—— 实现 IStepQuestionPanel 契约，供步骤条件 ConditionQuestion 经 GlobalControllerMgr.Find("StepQuestionController") 调用：按 id 取题、判题、答对后抛 OnQuestionCorrect。</summary>
    public class StepQuestionController : ControllerBase<StepQuestionPanel>, IStepQuestionPanel
    {
        #region 行为参数
        // WHY: 控制器已不挂场景、没有序列化入口，原 [SerializeField] 参数降为常量；
        //       这两个都是业务行为参数（不是视图表现参数），按 Controllers/README 的口径不往面板搬。
        /// <summary>答对后停留多久再结束本步骤（秒）—— 留一点时间给学员看反馈。</summary>
        const float CorrectDelay = 0.6f;

        // WHY: 教学步骤固定按题库原顺序出题（乱序只属考核 TaskExamController，它在自己抽题时克隆并洗牌）；
        //       原 false 在场景里恒为 false，去掉开关的同时把语义写死在克隆路径上。
        #endregion

        #region 状态
        QuestionClip currentClip;       // 当前题（已克隆，可安全打乱选项）
        Coroutine finishCoroutine;      // 答对后的收尾计时
        #endregion

        /// <summary>用户答对：步骤条件订阅此事件后结束 Waiting（<see cref="IStepQuestionPanel"/> 契约）</summary>
        public event Action OnQuestionCorrect;

        #region 绑定
        public override void OnViewBound()
        {
            // 先清后加（幂等）：面板可能被创建时就绑定过，这里再订一次也只有一个回调
            if (View == null) return;
            View.OnSubmit -= OnAnswerSubmitted;
            View.OnSubmit += OnAnswerSubmitted;
        }

        public override void OnDispose()
        {
            StopFinish();
            if (View != null) View.OnSubmit -= OnAnswerSubmitted;
            base.OnDispose();
        }
        #endregion

        #region IStepQuestionPanel（只由 ConditionQuestion 调用）
        // WHY: 找不到题目 / 取不到面板时必须告警并直接抛"答对"——条件在等这个事件才能进下一步，不抛就把流程卡死
        /// <summary>按题目 id 出题并显示面板。</summary>
        public void ShowQuestion(string questionId)
        {
            StopFinish();
            currentClip = BuildClip(questionId);

            if (currentClip == null)
            {
                Log.Warning($"[StepQuestionController] 题库中找不到题目 id={questionId}（检查 StreamingAssets/Data/QuestionData.json），跳过答题步骤");
                OnQuestionCorrect?.Invoke();
                return;
            }

            var panel = ResolvePanel(true);
            if (panel == null)
            {
                Log.Warning("[StepQuestionController] 当前激活 Canvas 上取不到 StepQuestionPanel，跳过答题步骤");
                OnQuestionCorrect?.Invoke();
                return;
            }

            panel.OnSubmit -= OnAnswerSubmitted;
            panel.OnSubmit += OnAnswerSubmitted;

            // WHY: 必须先激活再出题——ShowQuestion 末尾重建布局（协程），面板未激活时那段会直接跳过，复用时选项尺寸不刷新
            panel.SetUIActive(true);
            panel.ShowQuestion(currentClip);
        }

        /// <summary>关闭答题面板（步骤条件在 Prepare / Complete / 跳转归位时调用）。</summary>
        public void ClosePanel()
        {
            StopFinish();
            currentClip = null;

            // 收起路径绝不新建面板：新建实例的 Awake 会先亮一帧，又是一次闪
            var panel = ResolvePanel(false);
            if (panel == null) return;

            panel.OnSubmit -= OnAnswerSubmitted;

            // WHY: 失活判断不能省——本控制器协程已挪到 GlobalControllerMgr，但 SetUIActive(false) 仍会驱动面板自身协程；这里由 ConditionQuestion.OnPrepare / OnCompleteHide 调用，异常会打断 Prepare 协程（两个答题步骤共用同一面板时第 2 题就会踩到）
            if (!panel.gameObject.activeInHierarchy) return;

            panel.SetUIActive(false);
        }
        #endregion

        #region 作答调度
        /// <summary>面板上报「提交了下标为 index 的选项」→ 判题 → 反馈 → 答对则收尾</summary>
        void OnAnswerSubmitted(int index)
        {
            if (currentClip == null) return;
            if (finishCoroutine != null) return;    // 已答对、正在收尾：忽略重复提交

            var options = currentClip.options;
            if (options == null || index < 0 || index >= options.Count) return;

            bool isRight = options[index].isCorrect;

            var panel = ResolvePanel(false);
            if (panel != null) panel.ShowResult(isRight);

            if (!isRight) return;   // 答错：保留当前题，可改选后再次提交

            if (panel != null) panel.SetSubmitInteractable(false);   // 收尾期间禁用提交
            StopFinish();
            finishCoroutine = Run(FinishCoroutine());
        }

        /// <summary>答对后停留 <see cref="CorrectDelay"/> 秒，再把结果抛给步骤条件</summary>
        IEnumerator FinishCoroutine()
        {
            yield return new WaitForSeconds(CorrectDelay);
            finishCoroutine = null;
            OnQuestionCorrect?.Invoke();
        }

        void StopFinish()
        {
            if (finishCoroutine == null) return;
            Halt(finishCoroutine);
            finishCoroutine = null;
        }
        #endregion

        #region 取题
        /// <summary>按题目 id 从全局题库取题并克隆（题库只读，打乱选项必须在副本上做；步骤只按 id 取、不看用途，取不到返回 null）。</summary>
        QuestionClip BuildClip(string questionId)
        {
            // 退出阶段 / 管理器未就绪时不触发创建
            if (!GlobalDataMgr.Exists || GlobalDataMgr.Instance == null) return null;

            QuestionData data = GlobalDataMgr.Instance.QuestionData;
            if (data == null || data.questions == null) return null;

            for (int i = 0; i < data.questions.Count; i++)
            {
                QuestionClip clip = data.questions[i];
                if (clip == null || clip.id != questionId) continue;
                if (clip.options == null || clip.options.Count == 0) continue;
                return Clone(clip);
            }
            return null;
        }

        QuestionClip Clone(QuestionClip source)
        {
            var clip = new QuestionClip
            {
                id = source.id,
                displayName = source.displayName,
                displayNameEn = source.displayNameEn,
                description = source.description,
                questionText = source.questionText,
                questionTextEn = source.questionTextEn,
                questionType = source.questionType,
                options = new List<QuestionItem>(source.options),
            };
            // 不打乱：教学步骤按题库原顺序出题
            return clip;
        }

        /// <summary>Fisher-Yates 洗牌</summary>
        static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        #endregion

        #region 面板
        // WHY: 收起路径必须传 false（FindPanel）——否则会"先建一个再立刻关掉"
        /// <summary>取当前激活 Canvas 上的答题面板；createIfMissing = true 时不存在就创建。</summary>
        static StepQuestionPanel ResolvePanel(bool createIfMissing)
        {
            var canvas = GlobalUIMgr.GetActiveCanvas();
            if (canvas == null) return null;

            return createIfMissing ? canvas.GetPanel<StepQuestionPanel>() : canvas.FindPanel<StepQuestionPanel>();
        }
        #endregion
    }
}
