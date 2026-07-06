
using System;
using System.Collections.Generic;

namespace MCV_Module.Data.Project
{
    /// <summary>
    /// 题目数据 —— 测验（TaskType.Test）所用的题目定义。
    /// TODO: M2 填入完整字段 —— 参考 Tuanjie TaskData_Question
    /// </summary>
    [Serializable]
    public class QuestionData
    {
        // TODO: M2 实现 —— 题干内容 / 选项 / 正确答案
        public string questionId;
        public string questionText;

        // TODO: M2 实现 —— 选项列表
        // public List<QuestionOption> options;
    }

    /// <summary>
    /// 试题片段 —— ProjectClip 中 Test 任务携带的题目集合。
    /// </summary>
    [Serializable]
    public class QuestionClip
    {
        // TODO: M2 实现 —— 题目列表
        public List<QuestionData> questions = new List<QuestionData>();

        // TODO: M2 实现 —— 及格分数线 / 每题分值
        public float passScore = 60f;
    }

    /// <summary>
    /// 题项 —— 单个题目的选项条目。
    /// </summary>
    [Serializable]
    public class QuestionItem
    {
        // TODO: M2 实现 —— 选项文本 / 是否正确答案
        public string itemText;
        public bool isCorrect;
    }

    /// <summary>
    /// 题目类型枚举。
    /// </summary>
    public enum QuestionType
    {
        None,
        // TODO: M2 补充 —— 题目类型枚举值
        SingleChoice,
        MultipleChoice,
        TrueFalse,
        FillInBlank,
    }
}
