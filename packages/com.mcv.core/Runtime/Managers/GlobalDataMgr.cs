using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Models.System;
using MCV_Module.Models.User;
using MCV_Module.Singleton;
using MCV_Module.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace MCV_Module.Managers
{
    /// <summary>全局数据持有者：读各 JSON，并作为当前片段/任务类型/项目状态/画质/语言的唯一读写口径。</summary>
    public class GlobalDataMgr : SingletonGlobalMgr<GlobalDataMgr>
    {
        #region 参数
        [SerializeField, Header("系统数据")] SystemData systemData = new SystemData();
        [SerializeField, Header("目录数据")] MenuData menuData = new MenuData();
        [SerializeField, Header("内容数据")] ProjectData projectData = new ProjectData();
        [SerializeField, Header("用户数据")] UserData userData = new UserData();
        [SerializeField, Header("语言数据")] LanguageData languageData = new LanguageData();
        [SerializeField, Header("问题数据")] QuestionData questionData = new QuestionData();
        [SerializeField, Header("步骤内容数据")] StepContentData stepContentData = new StepContentData();
        [SerializeField, Header("成绩数据")] ScoreData scoreData = new ScoreData();

        /// <summary>本轮实训的开始时刻（仅内存，用于累计用时）。</summary>
        [NonSerialized] DateTime runStartTime = DateTime.Now;

        public SystemData SystemData { get => systemData; set => systemData = value; }
        public MenuData MenuData { get => menuData; set => menuData = value; }
        public ProjectData ProjectData { get => projectData; set => projectData = value; }
        public UserData UserData { get => userData; set => userData = value; }
        public LanguageData LanguageData { get => languageData; set => languageData = value; }
        public QuestionData QuestionData { get => questionData; set => questionData = value; }
        public StepContentData StepContentData { get => stepContentData; set => stepContentData = value; }
        /// <summary>当前成绩档案（运行态，落盘由 PreviewScore 完成）。</summary>
        public ScoreData ScoreData { get => scoreData; set => scoreData = value; }
        #endregion

        #region 语言（取文案的唯一实现，别处只调这里）
        /// <summary>key → 条目 的运行态索引（懒建；LanguageData 实例一换即重建）。</summary>
        Dictionary<string, LanguageClip> clipIndex;
        /// <summary>上面那份索引是按哪个 LanguageData 实例建的（引用比较，用来判失效）。</summary>
        LanguageData clipIndexSource;

        // WHY: 条目只有几十条时线性查找并不慢，但取词在文本节点上是**每帧级**调用（Refresh / 动态文本），
        // 且条目数只增不减 —— 用字典把 O(n) 降到 O(1)，代价是一份引用表。
        /// <summary>按 id（= LanguageClip.id）取语言条目；数据未就绪或不存在返回 false。</summary>
        public bool TryGetClip(string key, out LanguageClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(key)) return false;

            List<LanguageClip> clips = languageData?.languageClips;
            if (clips == null) return false;

            if (clipIndex == null || !ReferenceEquals(clipIndexSource, languageData)) RebuildClipIndex(clips);
            return clipIndex.TryGetValue(key, out clip);
        }

        /// <summary>重建 key 索引（同 id 重复时**首个**胜出，与原来的线性查找口径一致）。</summary>
        void RebuildClipIndex(List<LanguageClip> clips)
        {
            clipIndex = new Dictionary<string, LanguageClip>(clips.Count);
            for (int i = 0; i < clips.Count; i++)
            {
                LanguageClip c = clips[i];
                if (c == null || string.IsNullOrEmpty(c.id)) continue;
                if (!clipIndex.ContainsKey(c.id)) clipIndex[c.id] = c;
            }
            clipIndexSource = languageData;
        }

        // WHY: 界面语言的真源是设置类数据 SystemData.languageType（与画质同处），冷启动读一次、不做运行期热切；LanguageData 只管文案表。
        /// <summary>当前界面语言（SystemData 未就绪时按中文）。</summary>
        public LanguageType GetLanguageType()
        {
            return systemData != null ? systemData.languageType : LanguageType.Chinese;
        }

        /// <summary>按当前语言取文案；回退链：当前语言 → clips[0]（中文原文）→ null（调用方决定兜底，永不抛）。</summary>
        public string PickClipText(LanguageClip clip)
        {
            if (clip?.clips == null || clip.clips.Length == 0) return null;
            int index = (int)GetLanguageType();
            if (index >= 0 && index < clip.clips.Length && !string.IsNullOrEmpty(clip.clips[index]))
                return clip.clips[index];
            if (!string.IsNullOrEmpty(clip.clips[0])) return clip.clips[0];
            return null;
        }

        // WHY: 与 SetRenderQuality 同一走法（改数据 → SaveSystemData 落盘），但不做任何广播/刷新——语言是冷启动口径，改完下次启动生效。
        /// <summary>写入界面语言（唯一写入口：改 SystemData.languageType → 落盘 SystemData.json）。</summary>
        public static void SetLanguage(LanguageType type)
        {
            if (!Exists || Instance == null) return;
            if (Instance.SystemData == null) Instance.SystemData = new SystemData();
            Instance.SystemData.languageType = type;

            SaveSystemData();
        }
        #endregion

        #region 数据就绪（就绪门）
        // WHY: 本管理器的 DelayInit 是**异步**读 JSON 的，而 TextComponent.Awake 是同步执行的 ⇒ 早起的组件读到的是字段初始值。
        // 用它定型形态/语言等于把组件永久钉死成兜底值，故由这里在就绪后**广播一次**，组件订阅后补装（见 TextComponent.Awake）。
        /// <summary>数据就绪事件（<see cref="DelayInit"/> 末尾广播一次）。订阅方须自行处理"订阅时已就绪"的情形。</summary>
        public static event Action Ready;

        /// <summary>数据是否已就绪（DelayInit 完成）。未就绪时**不得**用兜底值定型文本形态 / 语言。</summary>
        public static bool IsReady => Exists && Instance != null && Instance.IsInit;

        /// <summary>广播就绪；逐个 try/catch 隔离，一个订阅方抛异常不影响其余订阅方与后续流程。</summary>
        static void RaiseReady()
        {
            Action handlers = Ready;
            if (handlers == null) return;

            Delegate[] list = handlers.GetInvocationList();
            for (int i = 0; i < list.Length; i++)
            {
                try
                {
                    ((Action)list[i])();
                }
                catch (Exception e)
                {
                    Log.Error($"[GlobalDataMgr] 就绪事件订阅方抛异常：{e.Message}");
                }
            }
        }
        #endregion

        #region 生命周期
        protected GlobalDataMgr() { }

        protected override IEnumerator DelayInit()
        {
            // WHY: 必须异步读（WebGL 无本地文件系统，不能用 File.ReadAllText）
            bool loaded = false;

            yield return JsonReaderWriter.ReadAsync<SystemData>("SystemData", (data, ok) =>
            {
                if (ok) SystemData = data;
                loaded = true;
            });
            yield return new WaitUntil(() => loaded);

            // WHY: 启动一律按 SystemData.json 档位应用（该值即本期默认档，改后写回 JSON 下次生效）
            ApplyRenderQuality(GetRenderQuality());

            // WHY: 语言数据必须加载，否则 WriteJson 以默认空值覆盖 JSON，Clip 会被清掉
            bool langLoaded = false;
            yield return JsonReaderWriter.ReadAsync<LanguageData>("LanguageData", (data, ok) =>
            {
                if (ok) LanguageData = data;
                langLoaded = true;
            });
            yield return new WaitUntil(() => langLoaded);

            // WHY: 目录数据须在 AI 预热前加载（预热要读【当前目录结构】）
            bool menuLoaded = false;
            yield return JsonReaderWriter.ReadAsync<MenuData>("MenuData", (data, ok) =>
            {
                if (ok && data != null) MenuData = data;
                menuLoaded = true;
            });
            yield return new WaitUntil(() => menuLoaded);

            // WHY: 内容数据须在 AI 预热前加载（预热要读【当前学习内容】）
            bool projectLoaded = false;
            yield return JsonReaderWriter.ReadAsync<ProjectData>("ProjectData", (data, ok) =>
            {
                if (ok && data != null) ProjectData = data;
                projectLoaded = true;
            });
            yield return new WaitUntil(() => projectLoaded);

            bool questionLoaded = false;
            yield return JsonReaderWriter.ReadAsync<QuestionData>("QuestionData", (data, ok) =>
            {
                if (ok && data != null) QuestionData = data;
                questionLoaded = true;
            });

            bool stepContentLoaded = false;
            yield return JsonReaderWriter.ReadAsync<StepContentData>("StepContentData", (data, ok) =>
            {
                if (ok && data != null) StepContentData = data;
                stepContentLoaded = true;
            });


            // WHY: 必须置 isInit=true，原实现漏置导致 Setup 启动链等待 15s 超时
            isInit = true;
            // WHY: 就绪门 —— 广播放在 isInit 置位**之后**，订阅方在回调里读 IsReady / GetTextType 才拿得到真值。
            RaiseReady();
            yield break;
        }
        #endregion

        #region 静态方法
        public static ProjectClip GetProjectClip()
        {
            return GlobalDataMgr.Instance.ProjectData.currentClip;
        }

        public static ProjectClip GetProjectClip(string clipId)
        {
            return GlobalDataMgr.Instance.ProjectData.clips.Find(clip => clip.id == clipId);
        }

        // WHY: 其他模块要知道当前任务就订阅 TaskTypeChangeEventData 或调本方法，不要各处自持一份
        /// <summary>读取当前任务类型（全项目唯一源；写入只走 SetCurrentTaskType）。</summary>
        public static TaskType GetCurrentTaskType()
        {
            if (!Exists || Instance == null || Instance.ProjectData == null) return TaskType.None;
            return Instance.ProjectData.currentTaskType;
        }

        /// <summary>写入当前任务类型（唯一写入口，业务侧不要直接改 ProjectData.currentTaskType）。</summary>
        public static void SetCurrentTaskType(TaskType type)
        {
            if (!Exists || Instance == null || Instance.ProjectData == null) return;
            Instance.ProjectData.currentTaskType = type;
        }

        /// <summary>当前片段在 ProjectData.clips 中的序号（0 基，未选中或找不到返回 -1）。</summary>
        public static int GetProjectClipIndex()
        {
            if (!Exists || Instance == null || Instance.ProjectData == null) return -1;
            ProjectClip clip = Instance.ProjectData.currentClip;
            if (clip == null) return -1;
            return Instance.ProjectData.clips.IndexOf(clip);
        }

        /// <summary>写入当前项目片段（唯一写入口；内容页与 GetTaskData 都读此值）。</summary>
        public static void SetCurrentClip(ProjectClip clip)
        {
            if (!Exists || Instance == null || Instance.ProjectData == null) return;
            Instance.ProjectData.currentClip = clip;
        }

        /// <summary>读取当前项目状态（所处页面口径）。</summary>
        public static ProjectState GetProjectState()
        {
            if (!Exists || Instance == null || Instance.ProjectData == null) return ProjectState.Start;
            return Instance.ProjectData.projectState;
        }

        /// <summary>写入当前项目状态（唯一写入口；须在 Canvas 重建前写好，面板 Awake 即读）。</summary>
        public static void SetProjectState(ProjectState state)
        {
            if (!Exists || Instance == null || Instance.ProjectData == null) return;
            Instance.ProjectData.projectState = state;
        }

        public static TaskDataBase GetTaskData(TaskType type)
        {
            ProjectClip clip = GetProjectClip();
            if (clip != null)
            {
                return clip.GetTaskData(type);
            }
            return null;
        }

        public static TaskDataBase GetTaskData(string clipId, TaskType type)
        {
            ProjectClip clip = GetProjectClip(clipId);
            if (clip != null)
            {
                return clip.GetTaskData(type);
            }
            return null;
        }

        #region 登录
        /// <summary>登录白名单验证（暂空：直接返回 true，待接入校验）。</summary>
        public static bool VerifyLogin(string userName, string password, UserType type)
        {
            // WHY: 白名单校验暂空，直接返回 true（待接入账号密码/白名单）
            return true;
        }

        /// <summary>写入登录用户数据（覆盖 UserData 并记录登录时间；学号/班级沿用现值）。</summary>
        public static void SetUserData(string userName, string password, UserType type)
        {
            SetUserData(userName, null, null, password, type);
        }

        /// <summary>写入登录用户数据（覆盖 UserData 并记录登录时间；学号为空则沿用现值）。</summary>
        public static void SetUserData(string userName, string indentyNum, string className, string password, UserType type)
        {
            UserData data = GlobalDataMgr.Instance.UserData;
            if (data == null)
            {
                data = new UserData();
                GlobalDataMgr.Instance.UserData = data;
            }

            // WHY: 必须先把现值写入 lastLoginTime 再更新 loginTime（首次为 0001-01-01 表示无历史）
            data.lastLoginTime = data.loginTime;
            data.loginTime = System.DateTime.Now;

            data.userName = userName ?? string.Empty;
            data.userType = type;
            data.password = password ?? string.Empty;
            if (!string.IsNullOrEmpty(indentyNum)) data.indentyNum = indentyNum;
            if (!string.IsNullOrEmpty(className)) data.className = className;
        }
        #endregion

        #region 渲染质量
        /// <summary>当前画面质量档（唯一源 SystemData.renderQuality；数据未就绪返回 High）。</summary>
        public static RenderQualityLevel GetRenderQuality()
        {
            if (!Exists || Instance == null || Instance.SystemData == null || Instance.SystemData.renderQuality == null)
                return RenderQualityLevel.High;
            return Instance.SystemData.renderQuality.renderQuality;
        }

        /// <summary>画面质量是否已设置过（首次启动为 false；须在 SystemData 加载后读，否则误判）。</summary>
        public static bool IsRenderQualitySetted()
        {
            if (!Exists || Instance == null || Instance.SystemData == null || Instance.SystemData.renderQuality == null)
                return false;
            return Instance.SystemData.renderQuality.qualitySetted;
        }

        /// <summary>把档位应用到 QualitySettings（只改运行时画质，不写数据，可重复调用）。</summary>
        public static void ApplyRenderQuality(RenderQualityLevel level)
        {
            if (QualitySettings.names.Length == 0)
            {
                Log.Warning("[GlobalDataMgr] 工程没有任何画质档（QualitySettings 为空），画面质量设置被忽略");
                return;
            }

            int index = Mathf.Clamp((int)level, 0, QualitySettings.names.Length - 1);
            // WHY: 同档不重复设置，SetQualityLevel 会重载渲染管线资产（applyExpensiveChanges）
            if (QualitySettings.GetQualityLevel() == index) return;
            QualitySettings.SetQualityLevel(index, true);
            Log.Info($"[GlobalDataMgr] 画面质量已应用：{level}（QualitySettings 档位 {index}）");
        }

        // WHY: 面板创建/显示时绝不能调，否则「没选就标记已设置」，设置面板从此再也不弹
        /// <summary>写入画面质量（唯一写入口：应用画质 → 改数据 → 落盘；只可在用户点选后调）。</summary>
        public static void SetRenderQuality(RenderQualityLevel level)
        {
            ApplyRenderQuality(level);

            if (!Exists || Instance == null || Instance.SystemData == null) return;
            if (Instance.SystemData.renderQuality == null) Instance.SystemData.renderQuality = new RenderQuality();
            Instance.SystemData.renderQuality.renderQuality = level;
            Instance.SystemData.renderQuality.qualitySetted = true;

            SaveSystemData();
        }

        /// <summary>把 SystemData 写回 StreamingAssets/Data/SystemData.json（WebGL 只留内存）。</summary>
        public static void SaveSystemData()
        {
            if (!Exists || Instance == null || Instance.SystemData == null) return;
            if (!JsonReaderWriter.WriteRuntime("SystemData", Instance.SystemData)) return;
            Log.Info("[GlobalDataMgr] SystemData 已落盘：StreamingAssets/Data/SystemData.json");
        }
        #endregion

        #region 文本形态
        // WHY: 形态是设置类数据（与语言/画质同处 SystemData），冷启动读一次即定；不做运行期热切。
        /// <summary>当前文本形态（SystemData 未就绪时按 Legacy）。</summary>
        public static TextType GetTextType()
        {
            if (!Exists || Instance == null || Instance.SystemData == null) return TextType.Legacy;
            return Instance.SystemData.textType;
        }
        #endregion

        #region 成绩
        /// <summary>成绩档案目录 StreamingAssets/Score（WebGL 不可写，成绩只留内存）。</summary>
        public static string ScoreDirectory => Path.Combine(Application.streamingAssetsPath, "Score");

        /// <summary>成绩档案文件路径：{学号|Anonymous}.json。</summary>
        public static string GetScorePath(string fileName)
        {
            return Path.Combine(ScoreDirectory, (string.IsNullOrEmpty(fileName) ? "Anonymous" : fileName) + ".json");
        }

        /// <summary>开始一轮实训：读回已有档案接续并记录用时起点（重复调用不清成绩）。</summary>
        public static void BeginScoreSession()
        {
            GlobalDataMgr self = Instance;
            if (self == null) return;

            ScoreData archive = LoadScoreData(self.UserData);
            if (archive == null) archive = NewArchive(self);
            self.ScoreData = archive;
            self.runStartTime = DateTime.Now;
        }

        /// <summary>预览成绩：结算用时 → 重算总分与完成情况 → 落盘 → 返回档案（供预览面板展示）。</summary>
        public static ScoreData PreviewScore(int expectedUnitCount = -1)
        {
            GlobalDataMgr self = Instance;
            if (self == null) return null;

            ScoreData archive = EnsureArchive(self);
            archive.AccumulateRun(DateTime.Now - self.runStartTime);
            self.runStartTime = DateTime.Now;   // WHY: 下一轮从此刻续算，避免同一次用时被重复累计

            if (expectedUnitCount <= 0) expectedUnitCount = ScoreCalculator.CountEnabledUnits(self.ProjectData);
            archive.Recalculate(expectedUnitCount);

            SaveScoreData(archive);
            return archive;
        }

        // WHY: 单价按类别均分、调用方不要自己算分；非计分类型（简介/原理/旧实验线）直接返回 null
        /// <summary>上报一个计分单元的成绩（单价由 ScoreCalculator 算，同单元按最新一轮覆盖）。</summary>
        public static TaskScore ReportScoredUnit(string clipId, string clipName, string taskId, string taskName,
            TaskType taskType, bool completed, int correctCount = -1)
        {
            GlobalDataMgr self = Instance;
            if (self == null || !ScoreCalculator.IsScoredTask(taskType)) return null;

            ScoreData archive = EnsureArchive(self);

            // WHY: 单价按类别均分，分母取「启用数」，尾差落在最后一个已记录单元
            int enabledCount = ScoreCalculator.CountEnabledUnits(self.ProjectData, taskType);
            if (enabledCount <= 0)
            {
                // WHY: 配置缺失时退回基准单元数（结构 8 / 测量 6 / 考核 1），避免单价被算成整类满分
                enabledCount = ScoreCalculator.GetDefaultUnitCount(taskType);
                Log.Warning($"[GlobalDataMgr] ProjectData 未给出 {taskType} 的启用单元数，暂按基准 {enabledCount} 个计价");
            }
            if (enabledCount <= 0) return null;

            float unitFullScore = ScoreCalculator.GetUnitFullScore(taskType, enabledCount, CountRecordedUnits(archive, taskType));
            float score = correctCount >= 0
                ? ScoreCalculator.ScoreExam(correctCount, unitFullScore)
                : ScoreCalculator.ScoreUnit(completed, unitFullScore);

            ClipScore clip = archive.EnsureClipScore(clipId, clipName, 0f);
            if (clip == null) return null;

            TaskScore task = clip.EnsureTaskScore(taskId, taskName, taskType, unitFullScore);
            if (task == null) return null;

            task.Submit(score, completed);
            ScoreCalculator.Apply(archive);   // WHY: 只即时刷新器件小计与总分，完成情况留到 PreviewScore 定
            return task;
        }

        /// <summary>成绩档案中某类别已记录的计分单元数（用作单价尾差的下标）。</summary>
        static int CountRecordedUnits(ScoreData archive, TaskType taskType)
        {
            if (archive == null) return 0;

            int count = 0;
            for (int i = 0; i < archive.clipScores.Count; i++)
            {
                ClipScore clip = archive.clipScores[i];
                if (clip == null) continue;

                for (int j = 0; j < clip.taskScores.Count; j++)
                {
                    TaskScore task = clip.taskScores[j];
                    if (task != null && task.taskType == taskType) count++;
                }
            }
            return count;
        }

        /// <summary>清空内存中的成绩档案并重新起算用时（换用户 / 重开一场时调用；**不删除**磁盘上的档案文件）。</summary>
        public static void ResetScore()
        {
            GlobalDataMgr self = Instance;
            if (self == null) return;

            self.ScoreData = NewArchive(self);
            self.runStartTime = DateTime.Now;
        }

        /// <summary>读取成绩档案：按学号（缺省 Anonymous）读 StreamingAssets/Score，无文件返回 null。</summary>
        public static ScoreData LoadScoreData(UserData user)
        {
            string fileName = user != null ? user.ScoreFileName : "Anonymous";
#if !UNITY_WEBGL
            try
            {
                string path = GetScorePath(fileName);
                if (File.Exists(path))
                {
                    ScoreData archive = JsonConvert.DeserializeObject<ScoreData>(File.ReadAllText(path));
                    if (archive != null) return archive;
                }
            }
            catch (Exception e)
            {
                Log.Error($"[GlobalDataMgr] 成绩读取失败：{e.Message}");
            }
#endif
            return null;
        }

        /// <summary>把成绩档案写入 StreamingAssets/Score/{档案名}.json（整体覆盖；WebGL 只留内存）。</summary>
        public static void SaveScoreData(ScoreData archive)
        {
            if (archive == null) return;
#if !UNITY_WEBGL
            try
            {
                if (!Directory.Exists(ScoreDirectory)) Directory.CreateDirectory(ScoreDirectory);
                string path = GetScorePath(archive.FileName);
                File.WriteAllText(path, JsonConvert.SerializeObject(archive, Formatting.Indented));
                Log.Info($"[GlobalDataMgr] 成绩已保存：{path}");
            }
            catch (Exception e)
            {
                Log.Error($"[GlobalDataMgr] 成绩保存失败：{e.Message}");
            }
#else
            Log.Warning("[GlobalDataMgr] WebGL 无本地文件系统，成绩仅保留在内存中");
#endif
        }

        // WHY: 非新建路径也必须补空身份——scoreData 是序列化字段，构造值会被反序列化覆盖成空，不补则 JSON 里 id/softwareName/student 全空
        /// <summary>取当前成绩档案（为空则新建；非新建路径也要补一次空身份）。</summary>
        static ScoreData EnsureArchive(GlobalDataMgr self)
        {
            if (self.scoreData == null) self.scoreData = NewArchive(self);
            else self.scoreData.EnsureIdentity(self.UserData, GetProjectName(self));
            return self.scoreData;
        }

        /// <summary>按当前用户与软件信息新建一份空档案。</summary>
        static ScoreData NewArchive(GlobalDataMgr self)
        {
            return new ScoreData(self.UserData, GetProjectName(self));
        }

        /// <summary>软件名称 / 项目名称；数据未就绪返回空串。</summary>
        static string GetProjectName(GlobalDataMgr self)
        {
            return self.SystemData != null && self.SystemData.projectInfo != null
                ? self.SystemData.projectInfo.projectName
                : string.Empty;
        }
        #endregion

        #region 菜单数据
        /// <summary>获取目录数据。</summary>
        public static MenuData GetMenuData()
        {
            return GlobalDataMgr.Instance.MenuData;
        }

        /// <summary>软件名称 / 项目名称（SystemData.projectInfo）；数据未就绪返回空串。</summary>
        public static string GetProjectName()
        {
            return !Exists || Instance == null ? string.Empty : GetProjectName(Instance);
        }

        /// <summary>获取根菜单列表。</summary>
        public static List<MenuClip> GetRootMenus()
        {
            MenuData menuData = GetMenuData();
            return menuData != null ? menuData.GetRootClips() : new List<MenuClip>();
        }

        /// <summary>获取指定菜单的子菜单列表。</summary>
        public static List<MenuClip> GetChildMenus(MenuClip parent)
        {
            MenuData menuData = GetMenuData();
            return menuData != null && parent != null
                ? menuData.GetChildClips(parent)
                : new List<MenuClip>();
        }
        #endregion

        #endregion

        #region 私有方法
#if UNITY_EDITOR
        // WHY: 同步 IO 仅限 Editor，故整段包在 #if UNITY_EDITOR 内
        void WriteJson()
        {
            JsonReaderWriter.Write<SystemData>("SystemData", SystemData, null);
            JsonReaderWriter.Write<ProjectData>("ProjectData", ProjectData, null);
            JsonReaderWriter.Write<UserData>("UserData", UserData, null);
            JsonReaderWriter.Write<LanguageData>("LanguageData", LanguageData, null);
        }
#endif
        #endregion
    }
}
