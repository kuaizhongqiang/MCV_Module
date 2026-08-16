using System.Collections.Generic;
using MCV_Module.Models.Project;
using MCV_Module.Models.User;
using NUnit.Framework;

namespace MCV_Module.Tests
{
    /// <summary>
    /// UserData.ResultData.GetTaskScores 测试 —— 验证按 ProjectClip 任务过滤成绩（曾因变量遮蔽恒返回空）。
    /// </summary>
    public class UserDataTests
    {
        [Test]
        public void GetTaskScores_FiltersByClipTasks()
        {
            var rd = new ResultData();
            rd.taskScores = new List<TaskScore>
            {
                new TaskScore { taskId = "p_purpose", points = 10 },
                new TaskScore { taskId = "p_training", points = 8 },
                new TaskScore { taskId = "other_clip", points = 5 },
            };

            var clip = new ProjectClip("p", "测试实验");
            var result = rd.GetTaskScores(clip);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Exists(s => s.taskId == "p_purpose"));
            Assert.IsTrue(result.Exists(s => s.taskId == "p_training"));
            Assert.IsFalse(result.Exists(s => s.taskId == "other_clip"));
        }

        [Test]
        public void GetTaskScores_NullClip_ReturnsEmpty()
        {
            var rd = new ResultData();
            rd.taskScores = new List<TaskScore>
            {
                new TaskScore { taskId = "p_purpose", points = 10 },
            };

            var result = rd.GetTaskScores(null);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetTaskScores_NoMatch_ReturnsEmpty()
        {
            var rd = new ResultData();
            rd.taskScores = new List<TaskScore>
            {
                new TaskScore { taskId = "zzz", points = 10 },
            };

            var clip = new ProjectClip("p", "测试实验");
            var result = rd.GetTaskScores(clip);

            Assert.AreEqual(0, result.Count);
        }
    }
}
