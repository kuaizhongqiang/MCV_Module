using System;
using System.Collections.Generic;

namespace MCV_Module.Models.Project
{
    [Serializable]
    public class QuestionData : DataBase
    {
        public List<QuestionClip> questions = new List<QuestionClip>();

        public QuestionData()
        {
            id = "questionData";
            displayName = "问题集";
            description = "这是一个问题集";
            // WHY: 不要向 questions 填默认项，否则 JSON 往返（ToJson→FromJson）会追加默认项导致重复。
        }
    }

    [Serializable]
    public class QuestionClip : DataBase
    {    
        public string questionText;
        public string questionTextEn;                      // 英文列（空 = 回退中文）
        public QuestionType questionType = QuestionType.SingleChoice;
        public List<QuestionItem> options = new List<QuestionItem>();

        /// <summary>题目用途：Exam（默认，考核抽题池）/ Step（步骤答题，按 id 取用）；两者同库不同池。</summary>
        public QuestionUsage usage = QuestionUsage.Exam;

        public QuestionClip()
        {
            id = "questionData";
            displayName = "问题";
            description = "这是一个问题数据";
            questionText = "这是一个问题数据的提干";
            questionType = QuestionType.SingleChoice;
            // WHY: 不要向 options 填默认项——Newtonsoft 对已初始化集合是「追加」而非「替换」，会导致 JSON 往返后出现「默认项 + 数据项」重复。
        }
    }

    [Serializable]
    public struct QuestionItem
    {
        public string itemText;
        public string itemTextEn;                          // 英文列（空 = 回退中文）
        public bool isCorrect;
    }


}
