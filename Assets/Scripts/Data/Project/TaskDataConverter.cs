using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCV_Module.Data.Project
{
    /// <summary>
    /// 任务数据转换器 —— 在不同表示形式间转换 TaskData。
    /// 支持 ProjectClip JSON 配置的导入导出。
    /// </summary>
    public static class TaskDataConverter
    {
        /// <summary>将 JSON 字符串解析为 TaskDataBase</summary>
        public static TaskDataBase FromJson(string json, TaskType taskType)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return taskType switch
                {
                    TaskType.Purpose => JsonConvert.DeserializeObject<TaskPurposeData>(json),
                    TaskType.Equipment => JsonConvert.DeserializeObject<TaskEquipmentData>(json),
                    TaskType.Principle => JsonConvert.DeserializeObject<TaskPrincipleData>(json),
                    TaskType.LineConnection => JsonConvert.DeserializeObject<TaskLineConnectionData>(json),
                    TaskType.Training => JsonConvert.DeserializeObject<TaskTrainingData>(json),
                    TaskType.Test => JsonConvert.DeserializeObject<TaskTestData>(json),
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[TaskDataConverter] JSON 反序列化失败 [{taskType}]: {ex.Message}");
                return null;
            }
        }

        /// <summary>将 TaskDataBase 序列化为 JSON 字符串</summary>
        public static string ToJson(TaskDataBase data)
        {
            if (data == null) return null;

            try
            {
                return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[TaskDataConverter] JSON 序列化失败 [{data.TaskType}]: {ex.Message}");
                return null;
            }
        }

        /// <summary>深度克隆 TaskDataBase</summary>
        public static TData Clone<TData>(TData source) where TData : TaskDataBase
        {
            if (source == null) return null;

            string json = ToJson(source);
            return json != null ? FromJson(json, source.TaskType) as TData : null;
        }

        /// <summary>类型安全转换</summary>
        public static TData SafeCast<TData>(TaskDataBase data) where TData : TaskDataBase
        {
            return data as TData;
        }

        /// <summary>从 JObject 提取 TaskData（用于 JSON 配置解析）</summary>
        public static TaskDataBase FromJObject(JObject obj, TaskType taskType)
        {
            if (obj == null) return null;
            return FromJson(obj.ToString(), taskType);
        }
    }
}
