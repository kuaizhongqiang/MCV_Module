using System.Collections.Generic;
using System.Text;
using MCV_Module.Models;
using MCV_Module.Utils;
using MCV_Module.Models.User;
using MCV_Module.Utils;

namespace MCV_Module.UI.Tools
{
    // WHY: 行名常量必须与预制体 LabelTextPrefab 实例名逐字一致（面板按名字取行、不看顺序），改名不同步会立刻报多了一行 / 少了一行。
    /// <summary>成绩预览面板的展示数据：文案已格式化好，View 直接塞进对应行，不做任何加工。</summary>
    public class ResultSummitViewData
    {
        // WHY: 行名常量与预制体实例名一一对应，改名需同步，否则面板按名取不到行。
        public const string RowNum = "NumContent";
        public const string RowClass = "ClassContent";
        public const string RowName = "NameContent";
        public const string RowSoftware = "SoftwareContent";
        public const string RowProject = "ProjectContent";
        public const string RowPlatform = "PlateformIndexContent";
        public const string RowCompletion = "FinishTypeContent";
        public const string RowScore = "ScoresContent";
        public const string RowStartTime = "StartTimeContent";
        public const string RowEndTime = "FinishTimeContent";
        public const string RowDuration = "DurationContent";

        readonly Dictionary<string, string> labels = new Dictionary<string, string>();
        readonly Dictionary<string, string> values = new Dictionary<string, string>();

        /// <summary>「实验步骤记录」区域的多行文本。</summary>
        public string RecordText { get; set; } = string.Empty;

        /// <summary>登记一行（行名 → 标签文案 + 数值文案）。</summary>
        public void Set(string rowName, string label, string value)
        {
            if (string.IsNullOrEmpty(rowName)) return;
            labels[rowName] = label ?? string.Empty;
            values[rowName] = value ?? string.Empty;
        }

        /// <summary>取该行的标签文案；未登记返回 null（面板据此报"预制体多了一行"）。</summary>
        public string GetLabel(string rowName)
        {
            return !string.IsNullOrEmpty(rowName) && labels.TryGetValue(rowName, out string v) ? v : null;
        }

        /// <summary>取该行的数值文案；未登记返回 null。</summary>
        public string GetValue(string rowName)
        {
            return !string.IsNullOrEmpty(rowName) && values.TryGetValue(rowName, out string v) ? v : null;
        }

        /// <summary>已登记的行数。</summary>
        public int RowCount => labels.Count;
    }

    // WHY: 标签文案、时间与用时格式、记录拼接都集中在本类；把格式化散到 Panel/Controller 会让展示文案失去唯一改动点。
    /// <summary>成绩预览文案格式化：字段映射、标签文案、时间与用时格式的唯一改动点。</summary>
    public static class ScoreRecordFormatter
    {
        /// <summary>开始/结束时间格式。</summary>
        const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
        /// <summary>无记录时的占位文案。</summary>
        const string EmptyRecordText = "暂无记录";

        /// <summary>把成绩档案 + 软件项目名装配成面板展示数据（data 为已结算档案，projectName 取 SystemData.projectInfo.projectName）。</summary>
        public static ResultSummitViewData Build(ScoreData data, string projectName)
        {
            var view = new ResultSummitViewData();
            if (data == null) return view;

            StudentInfo student = data.student;

            view.Set(ResultSummitViewData.RowNum, "学  号", student != null ? student.studentNum : string.Empty);
            view.Set(ResultSummitViewData.RowClass, "班  级", student != null ? student.className : string.Empty);
            view.Set(ResultSummitViewData.RowName, "姓  名", student != null ? student.studentName : string.Empty);
            view.Set(ResultSummitViewData.RowSoftware, "软件名称", data.softwareName);
            view.Set(ResultSummitViewData.RowProject, "项目名称", projectName);
            view.Set(ResultSummitViewData.RowPlatform, "接入平台编号", data.platformCode);
            view.Set(ResultSummitViewData.RowCompletion, "完成情况", FormatCompletion(data.completion));
            view.Set(ResultSummitViewData.RowScore, "成绩（百分制）", FormatScore(data.totalScore));
            view.Set(ResultSummitViewData.RowStartTime, "开始时间", FormatTime(data.startTime));
            view.Set(ResultSummitViewData.RowEndTime, "结束时间", FormatTime(data.endTime));
            view.Set(ResultSummitViewData.RowDuration, "用时（秒）", FormatDuration(data.TotalDuration));

            view.RecordText = FormatRecord(data);
            return view;
        }

        // ── 单值格式 ────────────────────────────────────────────

        /// <summary>完成情况：已完成 / 未完成。</summary>
        public static string FormatCompletion(CompletionStatus status)
        {
            return status == CompletionStatus.Completed ? "已完成" : "未完成";
        }

        /// <summary>分数：去掉无意义的小数尾巴（5 → "5"，6.67 → "6.67"）。</summary>
        public static string FormatScore(float value)
        {
            return value.ToString("0.##");
        }

        /// <summary>日期时间：yyyy-MM-dd HH:mm:ss。</summary>
        public static string FormatTime(System.DateTime time)
        {
            return time.ToString(TimeFormat);
        }

        /// <summary>用时：00:12:30（会自然进位到 &gt;24 小时，如 25:00:00）。</summary>
        public static string FormatDuration(System.TimeSpan span)
        {
            return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
        }

        // ── 实验步骤记录（拼接方式改动点）────────────────────────

        /// <summary>「实验步骤记录」整段文本：一个器件一行，行内列出该器件的计分单元（如"接触器：结构 5/5，测量 5/5"）。</summary>
        public static string FormatRecord(ScoreData data)
        {
            if (data == null || data.clipScores == null || data.clipScores.Count == 0) return EmptyRecordText;

            var sb = new StringBuilder();
            for (int i = 0; i < data.clipScores.Count; i++)
            {
                string line = FormatClip(data.clipScores[i]);
                if (string.IsNullOrEmpty(line)) continue;   // 该器件没有计分单元 → 不列

                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line);
            }
            return sb.Length > 0 ? sb.ToString() : EmptyRecordText;
        }

        /// <summary>单个器件一行；该器件没有任何计分单元时返回 null（调用方跳过）。</summary>
        public static string FormatClip(ClipScore clip)
        {
            if (clip == null || clip.taskScores == null) return null;

            var sb = new StringBuilder();
            for (int i = 0; i < clip.taskScores.Count; i++)
            {
                TaskScore task = clip.taskScores[i];
                if (task == null || !ScoreCalculator.IsScoredTask(task.taskType)) continue;

                if (sb.Length > 0) sb.Append('，');
                sb.Append(FormatUnit(task));
            }
            if (sb.Length == 0) return null;

            return $"{(string.IsNullOrEmpty(Localized.Name(clip)) ? clip.id : Localized.Name(clip))}：{sb}";
        }

        /// <summary>单个计分单元：完成 → 「步骤名 得分/满分」，未完成 → 「步骤名 未完成」。</summary>
        public static string FormatUnit(TaskScore task)
        {
            if (task == null) return string.Empty;

            string name = string.IsNullOrEmpty(task.displayName) ? task.taskType.ToString() : task.displayName;
            return task.isCompleted
                ? $"{name} {FormatScore(task.score)}/{FormatScore(task.fullScore)}"
                : $"{name} 未完成";
        }
    }
}
