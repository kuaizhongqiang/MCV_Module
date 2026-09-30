using System.Collections;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: 全部决策与流程都在控制器，面板只上报「选了哪个下标」、不判对错（业务判断不要写在面板里）
    /// <summary>考核控制器：抽题（usage=Exam）、题干与选项双乱序、判题、按 QuestionInterval 推进、题尽收尾。</summary>
    public class TaskExamController : ControllerBase<MCV_Module.UI.Panels.TaskExamPanel>
    {
        // WHY: 结果按 DialogId 认领 —— Controller 常驻、订阅跨页残留，本 Id 与其它发布方（Exit / Back / BackToMenu / QuitApp / EnterProject / SubmitScore）必须互不相同，
        //      否则同一结果会被多个控制器同时认领、重复发状态事件（BackToMenu 已被 ContentFunctionController 占）
        /// <summary>「离开考核」确认框身份（DialogResultEvent 按它认领结果）。</summary>
        const DialogId BackDialogId = DialogId.BackFromExam;

        #region 行为参数
        // WHY: 控制器已不挂场景、没有序列化入口，原 [SerializeField] 参数降为常量；
        //       抽题数与出题间隔都是业务规则，按 Controllers/README 的口径不往面板搬。
        /// <summary>每次考核抽题数量（题库 8 题随机抽 5 题）。</summary>
        const int QuestionCount = 5;

        /// <summary>答对之后间隔多久出下一题（秒）。</summary>
        const float QuestionInterval = 2f;
        #endregion

        #region 私有字段
        readonly List<QuestionClip> clips = new List<QuestionClip>();   // 本次考核题目（业务状态放控制器，不放面板）
        int currentIndex = -1;                                          // 当前题号
        Coroutine nextQuestionCoroutine;                                // 答对后的出题计时
        #endregion

        #region 绑定
        // WHY: Controller 常驻不销毁，常驻订阅放 OnInit（只跑一次）；OnViewBound 每次重建都重跑，订阅写在那里会随面板重建反复挂
        /// <summary>常驻订阅对话框结果（DialogResultEvent）；先清后加防重复订阅。</summary>
        public override void OnInit()
        {
            base.OnInit();
            EventBus<DialogResultEvent>.Unsubscribe(OnDialogResult);
            EventBus<DialogResultEvent>.Subscribe(OnDialogResult);
        }

        public override void OnViewBound()
        {
            View.OnSubmit -= OnAnswerSubmitted;   // 先清后加：面板随 Canvas 重建，每次绑定都要重订
            View.OnSubmit += OnAnswerSubmitted;
            View.OnBackClick -= OnBackClick;
            View.OnBackClick += OnBackClick;

            // 版权开关与外壳面板同源：面板不自己读数据
            View.SetCopyright(GlobalUIMgr.IfCopyright, GlobalUIMgr.IfCompany);

            clips.Clear();
            clips.AddRange(DrawQuestions(QuestionCount));
            if (clips.Count == 0)
            {
                Log.Warning("[TaskExamController] 题库为空，考核无法开始（检查 StreamingAssets/Data/QuestionData.json）");
                return;
            }

            Log.Info($"[TaskExamController] 本次考核抽题 {clips.Count} 道");
            currentIndex = 0;
            View.ShowQuestion(clips[currentIndex]);
        }

        public override void OnDispose()
        {
            StopNextQuestion();
            if (View != null)
            {
                View.OnSubmit -= OnAnswerSubmitted;
                View.OnBackClick -= OnBackClick;
            }

            // 常驻订阅必须退订（Controller 常驻，仅随应用退出 / 场景卸载销毁）
            EventBus<DialogResultEvent>.Unsubscribe(OnDialogResult);
            base.OnDispose();
        }

        // WHY: 只发 DialogRequestEvent —— 由 DialogEventDispatcher 统一定位显示（激活 Canvas → GetPanel<DialogPanel>），本类不自己找面板 / 不自己 SetActive
        /// <summary>返回入口：先弹确认框，不直接换页（半途退出即 0 分，必须二次确认）。</summary>
        void OnBackClick()
        {
            EventBus<DialogRequestEvent>.Publish(
                new DialogRequestEvent(BackDialogId, BuildBackMessage(),
                    showConfirm: true, showCancel: true));
        }

        // WHY: 数据源未就绪时安全降级为通用文案，避免空引用（与 ContentFunctionController.BuildBackMessage 同口径）
        /// <summary>确认文案：带上当前项目名，让用户明确「要离开的是哪个模块」。</summary>
        static string BuildBackMessage()
        {
            string projectName = null;

            if (GlobalDataMgr.Exists && GlobalDataMgr.Instance != null && GlobalDataMgr.Instance.ProjectData != null)
            {
                projectName = GlobalDataMgr.GetProjectClip()?.displayName;
            }

            return string.IsNullOrEmpty(projectName)
                ? "确定要返回菜单界面吗？"
                : $"确定要离开《{projectName}》返回菜单界面吗？";
        }

        // WHY: 只认领本控制器的确认框（按 DialogId 区分），取消 / 其它来源一律忽略；必须 Confirmed 才停计时并换页
        /// <summary>对话框结果：确认则停止待出题计时并回菜单页。</summary>
        void OnDialogResult(DialogResultEvent result)
        {
            if (result == null || !result.Confirmed) return;
            if (result.Id != BackDialogId) return;

            StopNextQuestion();
            ReturnToMenu();
        }
        #endregion

        #region 作答调度
        /// <summary>面板上报「提交了下标为 index 的选项」→ 判题 → 反馈 → 推进流程</summary>
        void OnAnswerSubmitted(int index)
        {
            if (currentIndex < 0 || currentIndex >= clips.Count) return;
            if (nextQuestionCoroutine != null) return;    // 已答对、正在等出题：忽略重复提交

            QuestionClip clip = clips[currentIndex];
            if (clip.options == null || index < 0 || index >= clip.options.Count) return;

            bool isRight = clip.options[index].isCorrect;
            View.ShowResult(isRight);
            if (!isRight) return;   // 答错：保留当前题，可改选后再次提交（能答完即算完成，计分见 ReportExamScore）

            View.SetSubmitInteractable(false);            // 等待出题期间禁用提交
            StopNextQuestion();
            nextQuestionCoroutine = Run(NextQuestionCoroutine());
        }

        /// <summary>答对后间隔 <see cref="QuestionInterval"/> 出下一题；题目用尽则收尾</summary>
        IEnumerator NextQuestionCoroutine()
        {
            yield return new WaitForSeconds(QuestionInterval);
            nextQuestionCoroutine = null;

            currentIndex++;
            if (currentIndex < clips.Count) View.ShowQuestion(clips[currentIndex]);
            else FinishExam();
        }

        void StopNextQuestion()
        {
            if (nextQuestionCoroutine == null) return;
            Halt(nextQuestionCoroutine);
            nextQuestionCoroutine = null;
        }

        /// <summary>全部题目作答完成：上报考核计分单元 -> 返回菜单（结束表现交给面板，目前空实现）。</summary>
        void FinishExam()
        {
            ReportExamScore();
            View.ShowFinish();
            ReturnToMenu();
        }

        // WHY: 答错可改选重试（答错不推进)，"能答完"等价"全对"，按答对题数折算恒等于满分 —— 只能二值口径
        /// <summary>上报考核计分单元：completed=true 二值口径，单价由 GlobalDataMgr.ReportScoredUnit 按类别均分算（调用方不要自己算分）；半途退出即 0 分。</summary>
        void ReportExamScore()
        {
            var clip = GlobalDataMgr.GetProjectClip();
            if (clip == null)
            {
                Log.Warning("[TaskExamController] 没有当前 ProjectClip，考核成绩无法上报");
                return;
            }

            var data = GlobalDataMgr.GetTaskData(TaskType.Exam) as TaskExamData;
            if (data == null)
            {
                Log.Warning($"[TaskExamController] {clip.id} 没有考核任务数据，考核成绩无法上报");
                return;
            }

            var task = GlobalDataMgr.ReportScoredUnit(clip.id, clip.displayName, data.id, data.displayName,
                TaskType.Exam, completed: true);

            if (task == null)
            {
                Log.Warning($"[TaskExamController] {clip.displayName} 的考核成绩上报被忽略（该任务类型不计分？）");
                return;
            }

            Log.Info($"[TaskExamController] 考核完成（{clips.Count} 题），已上报计分单元：{task.score:0.##}/{task.fullScore:0.##} 分");
        }

        // WHY: 换 Canvas 归 GlobalUIMgr，这里只发状态事件、绝不自己 SetActive Canvas
        /// <summary>回菜单：只发 SceneStateChangeEventData(Menu)，由 GlobalUIMgr 统一换 Canvas（同 ContentFunctionController 的返回入口）。</summary>
        static void ReturnToMenu()
        {
            Log.Info("[TaskExamController] 考核结束，返回菜单页");
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.Menu));
        }
        #endregion

        #region 抽题
        // WHY: 题库是只读数据（见 Models/README.md），必须克隆后再洗牌，原地打乱会污染全局题库
        /// <summary>从题库随机抽 count 题，并打乱每题选项顺序（返回克隆）。</summary>
        List<QuestionClip> DrawQuestions(int count)
        {
            var result = new List<QuestionClip>();
            var bank = TakeBank();
            if (count <= 0 || bank.Count == 0) return result;

            Shuffle(bank);
            int take = Mathf.Min(count, bank.Count);
            for (int i = 0; i < take; i++)
            {
                result.Add(CloneWithShuffledOptions(bank[i]));
            }
            return result;
        }

        /// <summary>取题库：跳过无效题（无选项）</summary>
        static List<QuestionClip> TakeBank()
        {
            var bank = new List<QuestionClip>();
            // 退出阶段 / 管理器未就绪时不触发创建
            if (!GlobalDataMgr.Exists || GlobalDataMgr.Instance == null) return bank;

            QuestionData data = GlobalDataMgr.Instance.QuestionData;
            if (data == null || data.questions == null) return bank;
            for (int i = 0; i < data.questions.Count; i++)
            {
                QuestionClip clip = data.questions[i];
                if (clip == null || clip.options == null || clip.options.Count == 0) continue;
                if (clip.usage != QuestionUsage.Exam) continue;   // 步骤题（usage=Step）不进考核抽题池
                bank.Add(clip);
            }
            return bank;
        }

        /// <summary>克隆题目并打乱其选项顺序（不修改题库原对象）</summary>
        static QuestionClip CloneWithShuffledOptions(QuestionClip source)
        {
            QuestionClip clip = new QuestionClip
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
            Shuffle(clip.options);
            return clip;
        }

        /// <summary>Fisher-Yates 洗牌</summary>
        static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        #endregion
    }
}
