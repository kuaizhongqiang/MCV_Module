
using System;
using System.Collections.Generic;
using MCV_Module.Models.Project;

namespace MCV_Module.Models.User
{
    [Serializable]
    public class UserData : DataBase
    {
        public string userName;
        public string indentyNum;
        public string password;
        public UserType userType = UserType.Unknow;
        public DateTime loginTime;
        public DateTime lastLoginTime;
        public ResultData resultData;
    }
    [Serializable]
    public class ResultData : DataBase
    {
        public DateTime startTime;
        public DateTime endTime;
        public int score;
        public List<TaskScore> taskScores = new List<TaskScore>();

        /// <summary>
        /// 从当前成绩列表中筛选出属于指定 ProjectClip 的 TaskScore。
        /// 修复：原实现用空局部变量遮蔽了 this.taskScores，导致恒返回空列表。
        /// </summary>
        public List<TaskScore> GetTaskScores(ProjectClip clip)
        {
            var result = new List<TaskScore>();
            if (clip == null) return result;

            var clipTasks = clip.Tasks; // 只取一次，避免 getter 重复分配
            for (int i = 0; i < taskScores.Count; i++)
            {
                var score = taskScores[i];
                for (int j = 0; j < clipTasks.Count; j++)
                {
                    if (clipTasks[j].id == score.taskId)
                    {
                        result.Add(score);
                        break;
                    }
                }
            }
            return result;
        }
    }
    [Serializable]
    public struct TaskScore
    {
        public string taskId;
        public string standard;
        public int totalScore;
        public int points;
    }
}