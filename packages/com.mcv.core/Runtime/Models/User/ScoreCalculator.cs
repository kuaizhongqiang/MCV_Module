
using System;
using System.Collections.Generic;
using MCV_Module.Models.Project;

namespace MCV_Module.Models.User
{
    // WHY: 给分口径=结构/测量二值（走完整套步骤拿满单价，半途退出计 0，不给部分分），考核按答对题数累加；缺素材单元 taskActive=false 后在本类别内重新均分，满分恒为 100。
    /// <summary>成绩核算规则（纯计算类，无 MonoBehaviour/生命周期/文件与 Unity API，可单独测试）：满分 100 = 结构 40 + 测量 30 + 考核 30（8 选 5）。</summary>
    public static class ScoreCalculator
    {
        #region 规则常量

        /// <summary>总满分。</summary>
        public const float FullScore = 100f;
        /// <summary>结构类满分。</summary>
        public const float StructureCategoryFullScore = 40f;
        /// <summary>测量类满分。</summary>
        public const float MeasureCategoryFullScore = 30f;
        /// <summary>考核类满分。</summary>
        public const float ExamCategoryFullScore = 30f;
        /// <summary>考核题数（题库 8 题随机抽 5）。</summary>
        public const int ExamQuestionCount = 5;
        /// <summary>基准结构单元数（8 个器件）—— 配置不可用时的兜底分母。</summary>
        public const int DefaultStructureUnitCount = 8;
        /// <summary>基准测量单元数（6 个器件有测量）—— 配置不可用时的兜底分母。</summary>
        public const int DefaultMeasureUnitCount = 6;
        /// <summary>基准考核单元数（1 条考核）—— 配置不可用时的兜底分母。</summary>
        public const int DefaultExamUnitCount = 1;
        /// <summary>分数保留小数位（类内均分可能除不尽，最后一项吃尾差）。</summary>
        public const int ScoreDecimals = 2;
        /// <summary>浮点比较容差（<see cref="IsUnitFull"/> 判"拿满"用）。</summary>
        public const float ScoreEpsilon = 0.001f;

        #endregion

        #region 分类

        /// <summary>该任务类型是否计入成绩：Structure / Inspection（测量）/ Exam（考核）计分，其余不计分。</summary>
        public static bool IsScoredTask(TaskType type)
        {
            return type == TaskType.Structure || type == TaskType.Inspection || type == TaskType.Exam;
        }

        /// <summary>该类别的分值上限（满分 100 的拆分）；不计分类型返回 0。</summary>
        public static float GetCategoryFullScore(TaskType type)
        {
            switch (type)
            {
                case TaskType.Structure: return StructureCategoryFullScore;
                case TaskType.Inspection: return MeasureCategoryFullScore;
                case TaskType.Exam: return ExamCategoryFullScore;
                default: return 0f;
            }
        }

        /// <summary>统计 <paramref name="projectData"/> 中某类别**启用**的计分单元数（<c>TaskActive == true</c> 的任务）。</summary>
        public static int CountEnabledUnits(ProjectData projectData, TaskType type)
        {
            if (projectData == null || !IsScoredTask(type)) return 0;

            int count = 0;
            for (int i = 0; i < projectData.clips.Count; i++)
            {
                ProjectClip clip = projectData.clips[i];
                if (clip == null) continue;

                List<TaskDataBase> tasks = clip.Tasks;
                for (int j = 0; j < tasks.Count; j++)
                {
                    TaskDataBase task = tasks[j];
                    if (task != null && task.TaskActive && task.TaskType == type) count++;
                }
            }
            return count;
        }

        /// <summary>统计全部启用的计分单元数（结构 + 测量 + 考核）。</summary>
        public static int CountEnabledUnits(ProjectData projectData)
        {
            return CountEnabledUnits(projectData, TaskType.Structure)
                 + CountEnabledUnits(projectData, TaskType.Inspection)
                 + CountEnabledUnits(projectData, TaskType.Exam);
        }

        /// <summary>该类别的基准单元数（8/6/1）；CountEnabledUnits 返回 0（配置缺失/未加载）时用作兜底分母，避免单价被算成"整个类别满分"。</summary>
        public static int GetDefaultUnitCount(TaskType type)
        {
            switch (type)
            {
                case TaskType.Structure: return DefaultStructureUnitCount;
                case TaskType.Inspection: return DefaultMeasureUnitCount;
                case TaskType.Exam: return DefaultExamUnitCount;
                default: return 0;
            }
        }

        #endregion

        #region 单价

        /// <summary>计分单元单价 = 类别分值 ÷ 启用单元数（取 ScoreDecimals 位）；类别内最后一个单元吃掉取整尾差，保证同类之和恰为类别满分。</summary>
        public static float GetUnitFullScore(TaskType type, int enabledUnitCount, int unitIndex = 0)
        {
            float category = GetCategoryFullScore(type);
            if (category <= 0f || enabledUnitCount <= 0 || unitIndex < 0) return 0f;

            float unit = Round(category / enabledUnitCount);
            if (unitIndex >= enabledUnitCount - 1) unit = Round(category - unit * (enabledUnitCount - 1));
            return unit;
        }

        /// <summary>考核每题分值 = 考核单元单价 ÷ 题数（默认 5 题）；题数传 &lt;= 0 返回 0。</summary>
        public static float GetExamQuestionFullScore(float examUnitFullScore, int questionCount = ExamQuestionCount)
        {
            if (questionCount <= 0) return 0f;
            return Round(examUnitFullScore / questionCount);
        }

        /// <summary>校验：结构 + 测量 + 考核各类别的满分合计（默认 8 结构 / 6 测量 / 5 题考核时 = 100）。</summary>
        public static float SumCategoryFullScore(int structureUnitCount, int measureUnitCount, int examQuestionCount = -1)
        {
            float total = 0f;
            if (structureUnitCount > 0) total += StructureCategoryFullScore;
            if (measureUnitCount > 0) total += MeasureCategoryFullScore;
            if (examQuestionCount != 0) total += ExamCategoryFullScore;
            return total;
        }

        #endregion

        #region 计分单元打分

        /// <summary>结构 / 测量打分：二值 —— 完成拿满单价，否则 0 分（不给部分分）。</summary>
        public static float ScoreUnit(bool completed, float unitFullScore)
        {
            return completed ? Round(unitFullScore) : 0f;
        }

        /// <summary>考核打分：按正确题数累加（每对一题得一份单价，答对题数上限为题数）。</summary>
        public static float ScoreExam(int correctCount, float examUnitFullScore = ExamCategoryFullScore, int questionCount = ExamQuestionCount)
        {
            if (questionCount <= 0 || correctCount <= 0) return 0f;
            if (correctCount > questionCount) correctCount = questionCount;
            return Round(examUnitFullScore * correctCount / questionCount);
        }

        #endregion

        #region 汇总与核算

        // WHY: 以 TaskScore.isCompleted 为闸门——未完成的单元即使残留分数也一律计 0，堵住「完成=满分」口径被绕过。
        /// <summary>单个器件的得分小计 = 其下所有已完成计分单元得分之和。</summary>
        public static float SumClipScore(ClipScore clip)
        {
            if (clip == null) return 0f;

            float sum = 0f;
            for (int i = 0; i < clip.taskScores.Count; i++)
            {
                TaskScore task = clip.taskScores[i];
                if (task != null && IsScoredTask(task.taskType) && task.isCompleted) sum += task.score;
            }
            return Round(sum);
        }

        /// <summary>总分 = 所有已完成的计分单元得分之和（<b>唯一口径</b>，不读 <see cref="ClipScore.score"/>）。</summary>
        public static float SumTotalScore(ScoreData data)
        {
            if (data == null) return 0f;

            float sum = 0f;
            for (int i = 0; i < data.clipScores.Count; i++)
            {
                sum += SumClipScore(data.clipScores[i]);
            }
            return Round(sum);
        }

        /// <summary>单个器件的满分小计 = 其下所有计分单元的单价之和。</summary>
        public static float SumClipFullScore(ClipScore clip)
        {
            if (clip == null) return 0f;

            float sum = 0f;
            for (int i = 0; i < clip.taskScores.Count; i++)
            {
                TaskScore task = clip.taskScores[i];
                if (task != null && IsScoredTask(task.taskType)) sum += task.fullScore;
            }
            return Round(sum);
        }

        /// <summary>单个计分单元是否拿满：已完成、单价有效、且得分不低于单价（考核答错题即不算满）。</summary>
        public static bool IsUnitFull(TaskScore task)
        {
            if (task == null || !IsScoredTask(task.taskType)) return false;
            if (!task.isCompleted) return false;
            if (task.fullScore <= 0f) return false;                       // 单价缺失视为未满，避免误判满分
            return task.score >= task.fullScore - ScoreEpsilon;
        }

        // WHY: 单价是"类别满分÷启用数"取 2 位小数，浮点累加/配置漂移会算出 99.99，故全做完做对时必须直接给 FullScore 而不参与累加。
        // WHY: 必须传 expectedUnitCount——数量未知时无法确认"没漏做"，一律返回 false，否则中途上报会被误判成满分。
        /// <summary>满分短路判断：所有计分单元是否「全部完成且全部正确」。</summary>
        public static bool IsPerfect(ScoreData data, int expectedUnitCount)
        {
            if (data == null || expectedUnitCount <= 0) return false;

            int scored = 0;
            for (int i = 0; i < data.clipScores.Count; i++)
            {
                ClipScore clip = data.clipScores[i];
                if (clip == null) continue;

                for (int j = 0; j < clip.taskScores.Count; j++)
                {
                    TaskScore task = clip.taskScores[j];
                    if (task == null || !IsScoredTask(task.taskType)) continue;

                    scored++;
                    if (!IsUnitFull(task)) return false;   // 有一个没做完或没做对 → 不是满分
                }
            }

            return scored >= expectedUnitCount;            // 数量对得上才认"全部做完"
        }

        /// <summary>已完成的计分单元数。</summary>
        public static int CountCompletedUnits(ScoreData data)
        {
            if (data == null) return 0;

            int count = 0;
            for (int i = 0; i < data.clipScores.Count; i++)
            {
                ClipScore clip = data.clipScores[i];
                if (clip == null) continue;

                for (int j = 0; j < clip.taskScores.Count; j++)
                {
                    TaskScore task = clip.taskScores[j];
                    if (task != null && IsScoredTask(task.taskType) && task.isCompleted) count++;
                }
            }
            return count;
        }

        /// <summary>完成情况：已完成的计分单元数达到 expectedUnitCount 即为 Completed。</summary>
        public static CompletionStatus EvaluateCompletion(ScoreData data, int expectedUnitCount)
        {
            if (expectedUnitCount <= 0) return CompletionStatus.Unfinished;
            return CountCompletedUnits(data) >= expectedUnitCount
                ? CompletionStatus.Completed
                : CompletionStatus.Unfinished;
        }

        // WHY: 唯一核算入口——器件小计/完成标志由本方法派生，业务侧不要另行写入。
        /// <summary>重算器件小计、总分与完成情况；总分口径：全部完成且正确 → FullScore，否则按已完成单元累加。</summary>
        public static void Apply(ScoreData data, int expectedUnitCount = -1)
        {
            if (data == null) return;

            for (int i = 0; i < data.clipScores.Count; i++)
            {
                ClipScore clip = data.clipScores[i];
                if (clip == null) continue;

                clip.fullScore = SumClipFullScore(clip);
                clip.score = SumClipScore(clip);
                clip.isCompleted = IsClipCompleted(clip);
            }

            data.totalScore = IsPerfect(data, expectedUnitCount) ? FullScore : SumTotalScore(data);
            if (expectedUnitCount > 0) data.completion = EvaluateCompletion(data, expectedUnitCount);
        }

        /// <summary>器件是否完成 = 该器件下所有**计分**单元都完成（无计分单元的器件返回 false）。</summary>
        public static bool IsClipCompleted(ClipScore clip)
        {
            if (clip == null) return false;

            bool hasScoredUnit = false;
            for (int i = 0; i < clip.taskScores.Count; i++)
            {
                TaskScore task = clip.taskScores[i];
                if (task == null || !IsScoredTask(task.taskType)) continue;

                hasScoredUnit = true;
                if (!task.isCompleted) return false;
            }
            return hasScoredUnit;
        }

        /// <summary>按 <see cref="ScoreDecimals"/> 位四舍五入（避免浮点尾巴）。</summary>
        public static float Round(float value)
        {
            return (float)Math.Round(value, ScoreDecimals, MidpointRounding.AwayFromZero);
        }

        #endregion
    }
}
