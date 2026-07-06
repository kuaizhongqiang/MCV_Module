
using System;
using System.Collections.Generic;
using MCV_Module.Data.Project;

namespace MCV_Module.Data.User
{
    [Serializable]
    public class UserData : DataBase
    {
        public string userName;
        public string indentyNum;
        public string password;
        public UserType userType = UserType.None;
        public DateTime loginTime;
        public DateTime lastLoginTime;
        public ResultData resultData;
    }
    [Serializable]
    public enum UserType
    {
        None,
        Student,
        Teacher,
        Admin
    }
    [Serializable]
    public class ResultData : DataBase
    {
        public DateTime startTime;
        public DateTime endTime;
        public int score;
        public List<TaskScore> taskScores = new List<TaskScore>();

        public List<TaskScore> GetTaskScores(ProjectClip clip)
        {
            List<TaskScore> taskScores = new List<TaskScore>();
            List<string> ids = new List<string>();
            for (int i = 0; i < clip.Tasks.Count; i++)
            {
                var taskId = clip.Tasks[i].id;
                ids.Add(taskId);
            }
            for (int i = 0; i < taskScores.Count; i++)
            {
                if (ids.Contains(taskScores[i].taskId))
                {
                    taskScores.Add(taskScores[i]);
                }
            }
            return taskScores;
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