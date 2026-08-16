using System.Text.RegularExpressions;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MCV_Module.Tests
{
    /// <summary>
    /// TaskDataConverter 数据层测试 —— 纯逻辑，无场景依赖（EditMode）。
    /// </summary>
    public class TaskDataConverterTests
    {
        [TestCase(TaskType.Purpose)]
        [TestCase(TaskType.Equipment)]
        [TestCase(TaskType.Principle)]
        [TestCase(TaskType.LineConnection)]
        [TestCase(TaskType.Training)]
        [TestCase(TaskType.Test)]
        public void FromJson_ToJson_RoundTrip(TaskType type)
        {
            var data = CreateTask(type);
            Assert.IsNotNull(data);

            var json = TaskDataConverter.ToJson(data);
            Assert.IsNotNull(json);

            var back = TaskDataConverter.FromJson(json, type);
            Assert.IsNotNull(back);
            Assert.AreEqual(type, back.TaskType);
            Assert.AreEqual(data.id, back.id);
            Assert.AreEqual(data.displayName, back.displayName);
        }

        [Test]
        public void FromJson_InvalidJson_ReturnsNull()
        {
            // 负路径用例：TaskDataConverter 会按设计 LogError 后返回 null——声明该错误为预期，避免 Console 噪音
            LogAssert.Expect(LogType.Error, new Regex(@"\[TaskDataConverter\] JSON 反序列化失败"));
            Assert.IsNull(TaskDataConverter.FromJson("not json {{", TaskType.Purpose));
        }

        [Test]
        public void FromJson_Empty_ReturnsNull()
        {
            Assert.IsNull(TaskDataConverter.FromJson(null, TaskType.Purpose));
            Assert.IsNull(TaskDataConverter.FromJson("", TaskType.Purpose));
        }

        [Test]
        public void ToJson_Null_ReturnsNull()
        {
            Assert.IsNull(TaskDataConverter.ToJson(null));
        }

        [Test]
        public void Clone_ProducesEqualIndependentData()
        {
            var source = new TaskTrainingData("t1");
            var clone = TaskDataConverter.Clone(source);

            Assert.IsNotNull(clone);
            Assert.AreNotSame(source, clone);
            Assert.AreEqual(source.id, clone.id);
            Assert.AreEqual(source.displayName, clone.displayName);
            Assert.AreEqual(source.TaskType, clone.TaskType);
        }

        static TaskDataBase CreateTask(TaskType type) => type switch
        {
            TaskType.Purpose => new TaskPurposeData("p"),
            TaskType.Equipment => new TaskEquipmentData("e"),
            TaskType.Principle => new TaskPrincipleData("pr"),
            TaskType.LineConnection => new TaskLineConnectionData("l"),
            TaskType.Training => new TaskTrainingData("t"),
            TaskType.Test => new TaskTestData("te"),
            _ => null,
        };
    }
}
