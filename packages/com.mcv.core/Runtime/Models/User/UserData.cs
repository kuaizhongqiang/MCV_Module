
using System;
using System.Collections.Generic;
using MCV_Module.Models.Project;
using Newtonsoft.Json;

namespace MCV_Module.Models.User
{
    // 一、用户身份

    /// <summary>用户身份信息（登录态）：来源为平台注入或本地登录面板（LoginPanel → LoginController → GlobalDataMgr.SetUserData）；游客（Unknow）三项均空，成绩档案落 Anonymous.json。</summary>
    [Serializable]
    public class UserData : DataBase
    {
        /// <summary>姓名（本地登录时同时作为登录账号）。</summary>
        public string userName;
        /// <summary>学号。平台登录时由平台下发，本地登录时由用户填写；游客为空。成绩文件名取此值。</summary>
        public string indentyNum;
        /// <summary>班级。来源同学号。</summary>
        public string className;
        public string password;
        public UserType userType = UserType.Unknow;
        /// <summary>本次登录时间。</summary>
        public DateTime loginTime;
        /// <summary>上次登录时间（本次登录时由 GlobalDataMgr.SetUserData 回填）。</summary>
        public DateTime lastLoginTime;

        public UserData()
        {
            id = "UserData";
            displayName = "默认用户";
            description = "默认用户的数据组装";
            userName = string.Empty;
            indentyNum = string.Empty;
            password = string.Empty;
            className = string.Empty;
            userType = UserType.Unknow;
            loginTime = DateTime.Now;
            lastLoginTime = DateTime.Now;
        }

        public UserData(string userName, string indentyNum, string password, string className, UserType userType)
        {
            id = "UserData";
            displayName = userName;
            description = "登录用户的数据组装";
            this.userName = userName ?? string.Empty;
            this.indentyNum = indentyNum ?? string.Empty;
            this.password = password ?? string.Empty;
            this.className = className ?? string.Empty;
            this.userType = userType;
            loginTime = DateTime.Now;
            lastLoginTime = DateTime.Now;
        }

        /// <summary>是否游客（未登录 / 平台未注入身份）。</summary>
        public bool IsGuest => userType == UserType.Unknow;

        /// <summary>是否有学号（决定成绩档案用真实学号还是 <c>Anonymous</c>）。</summary>
        public bool HasStudentNum => !string.IsNullOrEmpty(indentyNum);

        /// <summary>成绩档案文件名（不含扩展名）：有学号取学号，否则 <c>Anonymous</c>。</summary>
        public string ScoreFileName => HasStudentNum ? indentyNum : "Anonymous";

        /// <summary>成绩档案中记录的姓名（空则回落「游客」）。</summary>
        public string ScoreDisplayName => string.IsNullOrEmpty(userName) ? "游客" : userName;

        /// <summary>转成成绩档案里的身份快照（不含密码）。</summary>
        public StudentInfo ToStudentInfo()
        {
            return new StudentInfo
            {
                studentNum = indentyNum ?? string.Empty,
                studentName = userName ?? string.Empty,
                className = className ?? string.Empty,
                userType = userType,
            };
        }
    }

    // WHY: 刻意不复用 UserData——成绩文件会落到本机磁盘/上传平台，不能携带 password。
    /// <summary>成绩档案中的身份快照（学号/姓名/班级/用户类型）。</summary>
    [Serializable]
    public class StudentInfo
    {
        /// <summary>学号（游客为空）。</summary>
        public string studentNum = string.Empty;
        /// <summary>姓名（游客为空）。</summary>
        public string studentName = string.Empty;
        /// <summary>班级（游客为空）。</summary>
        public string className = string.Empty;
        /// <summary>用户类型（学生 / 教师 / 游客）。</summary>
        public UserType userType = UserType.Unknow;
    }

    // 二、成绩档案（一名学生一份，落盘 StreamingAssets/Score/{学号|Anonymous}.json）

    // WHY: 文件按学号命名并覆盖更新——项目得分按最新一轮覆盖、用时多次运行累加；总分/完成情况由 Recalculate 统一重算，不手工维护。
    /// <summary>成绩档案根：本地成绩 JSON 与后续上传的载荷结构，身份字段由 id（学号或 Anonymous）与 displayName（姓名）承载。</summary>
    [Serializable]
    public class ScoreData : DataBase
    {
        // ── 身份 ──
        /// <summary>身份快照（学号 / 姓名 / 班级）。</summary>
        public StudentInfo student = new StudentInfo();

        // ── 软件与平台 ──
        /// <summary>软件名称（平台跳转时下发，否则取 SystemData.projectInfo.projectName）。</summary>
        public string softwareName = string.Empty;
        /// <summary>接入平台编号（平台获取，本地运行为空）。</summary>
        public string platformCode = string.Empty;

        // ── 成绩 ──
        /// <summary>完成情况：所有实训项目都完成后为 <see cref="CompletionStatus.Completed"/>。</summary>
        public CompletionStatus completion = CompletionStatus.Unfinished;
        /// <summary>成绩（百分制）：已完成实训项目得分之和，未完成的项目计 0 分。</summary>
        public float totalScore;

        // ── 时间 ──
        /// <summary>开始时间：首次建立本档案的时间，后续运行**不覆盖**。</summary>
        public DateTime startTime = DateTime.Now;
        /// <summary>结束时间：最近一次点开成绩预览的时间。</summary>
        public DateTime endTime = DateTime.Now;
        /// <summary>用时（秒）：多次运行的**累积**时长。</summary>
        public double totalSeconds;
        /// <summary>实训次数（累计用时的配套计数）。</summary>
        public int runCount;

        // ── 过程记录 ──
        /// <summary>实验步骤记录：每个实训项目一条（含步骤级明细）。</summary>
        public List<ClipScore> clipScores = new List<ClipScore>();

        public ScoreData() { }

        /// <summary>按当前用户建一份新档案（id = 学号或 Anonymous，displayName = 姓名）。</summary>
        public ScoreData(UserData user, string softwareName, string platformCode = null)
        {
            id = user != null ? user.ScoreFileName : "Anonymous";
            displayName = user != null ? user.ScoreDisplayName : "游客";
            description = "实训成绩档案";
            student = user != null ? user.ToStudentInfo() : new StudentInfo();
            this.softwareName = softwareName ?? string.Empty;
            this.platformCode = platformCode ?? string.Empty;
            startTime = DateTime.Now;
            endTime = startTime;
        }

        /// <summary>补空身份（保存前按当前用户补齐 id/displayName/student/softwareName）；只补空值，已有值不动。</summary>
        public void EnsureIdentity(UserData user, string softwareName)
        {
            if (string.IsNullOrEmpty(id)) id = user != null ? user.ScoreFileName : "Anonymous";
            if (string.IsNullOrEmpty(displayName)) displayName = user != null ? user.ScoreDisplayName : "游客";
            if (string.IsNullOrEmpty(description)) description = "实训成绩档案";

            StudentInfo from = user != null ? user.ToStudentInfo() : null;
            if (student == null) student = from ?? new StudentInfo();
            else if (from != null)
            {
                if (string.IsNullOrEmpty(student.studentNum)) student.studentNum = from.studentNum;
                if (string.IsNullOrEmpty(student.studentName)) student.studentName = from.studentName;
                if (string.IsNullOrEmpty(student.className)) student.className = from.className;
                if (student.userType == UserType.Unknow) student.userType = from.userType;
            }

            if (string.IsNullOrEmpty(this.softwareName)) this.softwareName = softwareName ?? string.Empty;
        }

        /// <summary>档案文件名（不含扩展名）。**派生值，不进 JSON**。</summary>
        [JsonIgnore]
        public string FileName => string.IsNullOrEmpty(id) ? "Anonymous" : id;

        /// <summary>累计用时的可读形式（时:分:秒）。**派生值，不进 JSON**。</summary>
        [JsonIgnore]
        public TimeSpan TotalDuration => TimeSpan.FromSeconds(totalSeconds);

        // ── 查询 ──

        /// <summary>取指定实训项目的成绩记录；没有则返回 null。</summary>
        public ClipScore GetClipScore(string clipId)
        {
            if (string.IsNullOrEmpty(clipId)) return null;
            for (int i = 0; i < clipScores.Count; i++)
            {
                if (clipScores[i] != null && clipScores[i].id == clipId) return clipScores[i];
            }
            return null;
        }

        /// <summary>取指定实训项目的成绩记录；没有则新建并挂入档案。</summary>
        public ClipScore EnsureClipScore(string clipId, string clipName, float fullScore)
        {
            if (string.IsNullOrEmpty(clipId)) return null;

            ClipScore clip = GetClipScore(clipId);
            if (clip == null)
            {
                clip = new ClipScore { id = clipId, displayName = clipName ?? string.Empty };
                clipScores.Add(clip);
            }
            else if (!string.IsNullOrEmpty(clipName))
            {
                clip.displayName = clipName;   // 名称以最新配置为准（配置可改名）
            }
            if (fullScore > 0f) clip.fullScore = fullScore;
            return clip;
        }

        // ── 写入口径 ──

        /// <summary>合并一次运行：累计用时 + 次数并刷新结束时间（开始时间保留首次）；由「返回菜单 → 预览成绩」调用。</summary>
        public void AccumulateRun(TimeSpan runDuration)
        {
            double seconds = runDuration.TotalSeconds;
            if (seconds > 0d) totalSeconds += seconds;
            runCount++;
            endTime = DateTime.Now;
        }

        /// <summary>重算器件小计、总分与完成情况，转发给 ScoreCalculator.Apply（核算口径只此一处）。</summary>
        public void Recalculate(int expectedUnitCount = -1)
        {
            ScoreCalculator.Apply(this, expectedUnitCount);
        }
    }

    // WHY: 刻意只存 id + 名称、不内嵌 ProjectClip，成绩文件不该带上题库/步骤/资源包配置。
    // WHY: score/isCompleted/fullScore 是只读派生值，由 ScoreCalculator.Apply 算出，业务侧不要直接写。
    /// <summary>单个实训项目的成绩记录（成绩表「实验步骤记录」的一条，即一个器件的小计）；id = ProjectClip.id，displayName = 项目名称。</summary>
    [Serializable]
    public class ClipScore : DataBase
    {
        /// <summary>该项目满分 = 其下计分单元满分之和（由核算派生）。</summary>
        public float fullScore;
        /// <summary>该项目得分 = 其下**已完成**计分单元得分之和（由核算派生）。</summary>
        public float score;
        /// <summary>该项目是否完成 = 其下所有计分单元都完成（由核算派生）。</summary>
        public bool isCompleted;
        /// <summary>步骤级明细（**计分单元**都在这里）。</summary>
        public List<TaskScore> taskScores = new List<TaskScore>();

        /// <summary>得分率（满分 &lt;= 0 时返回 0）。</summary>
        public float ScoreRate => fullScore > 0f ? score / fullScore : 0f;

        /// <summary>取指定步骤的成绩记录；没有则返回 null。</summary>
        public TaskScore GetTaskScore(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return null;
            for (int i = 0; i < taskScores.Count; i++)
            {
                if (taskScores[i] != null && taskScores[i].id == taskId) return taskScores[i];
            }
            return null;
        }

        /// <summary>取指定步骤的成绩记录；没有则新建。单价（<paramref name="fullScore"/>）由调用方从 <see cref="ScoreCalculator"/> 取。</summary>
        public TaskScore EnsureTaskScore(string taskId, string taskName, TaskType taskType, float fullScore)
        {
            if (string.IsNullOrEmpty(taskId)) return null;

            TaskScore task = GetTaskScore(taskId);
            if (task == null)
            {
                task = new TaskScore { id = taskId, taskType = taskType };
                taskScores.Add(task);
            }
            if (!string.IsNullOrEmpty(taskName)) task.displayName = taskName;
            task.taskType = taskType;
            if (fullScore > 0f) task.fullScore = fullScore;
            return task;
        }
    }

    /// <summary>单个计分单元的成绩记录（= 某器件的结构/某器件的测量/考核）；id = TaskDataBase.id，displayName = 步骤名称。</summary>
    [Serializable]
    public class TaskScore : DataBase
    {
        /// <summary>步骤类型（只对 Structure / Inspection / Exam 计分，见 <see cref="ScoreCalculator.IsScoredTask"/>）。</summary>
        public TaskType taskType = TaskType.None;
        /// <summary>该单元满分 = 类别单价（由 <see cref="ScoreCalculator.GetUnitFullScore"/> 算出）。</summary>
        public float fullScore;
        /// <summary>该单元得分：同一学生多次实训按**最新一轮**覆盖。</summary>
        public float score;
        /// <summary>该单元是否完成（未完成则总分计 0）。</summary>
        public bool isCompleted;
        /// <summary>该单元累计实训次数。</summary>
        public int attempts;

        /// <summary>得分率（满分 &lt;= 0 时返回 0）。</summary>
        public float ScoreRate => fullScore > 0f ? score / fullScore : 0f;

        /// <summary>上报一轮得分：按**最新一轮**覆盖，完成标志只增不减，次数 +1。</summary>
        public void Submit(float score, bool completed)
        {
            this.score = score;
            if (completed) isCompleted = true;
            attempts++;
        }
    }
}
