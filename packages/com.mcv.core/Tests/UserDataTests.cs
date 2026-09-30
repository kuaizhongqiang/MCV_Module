using MCV_Module.Models.User;
using NUnit.Framework;

namespace MCV_Module.Tests
{
    /// <summary>
    /// 成绩档案按 id 查询测试 —— 覆盖 ScoreData.GetClipScore 与 ClipScore.GetTaskScore。
    /// 旧 ResultData.GetTaskScores(ProjectClip) 已随成绩模型重写移除（现为"档案 → 项目 → 步骤"三层按 id 取）。
    /// </summary>
    public class UserDataTests
    {
        [Test]
        public void GetClipScore_ReturnsRecordById()
        {
            var data = new ScoreData();
            var clip = new ClipScore { id = "p", displayName = "测试实验" };
            data.clipScores.Add(clip);

            Assert.AreSame(clip, data.GetClipScore("p"));
            Assert.IsNull(data.GetClipScore("other_clip"));
            Assert.IsNull(data.GetClipScore(null));
            Assert.IsNull(data.GetClipScore(string.Empty));
        }

        [Test]
        public void GetTaskScore_ReturnsRecordById()
        {
            var clip = new ClipScore();
            var task = new TaskScore { id = "p_purpose", score = 10f };
            clip.taskScores.Add(task);
            clip.taskScores.Add(new TaskScore { id = "p_training", score = 8f });

            Assert.AreSame(task, clip.GetTaskScore("p_purpose"));
            Assert.AreEqual(8f, clip.GetTaskScore("p_training").score);
            Assert.IsNull(clip.GetTaskScore("other_clip"));
            Assert.IsNull(clip.GetTaskScore(null));
        }
    }
}
