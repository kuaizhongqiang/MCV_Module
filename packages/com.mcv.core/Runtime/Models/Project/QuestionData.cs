using System;
using System.Collections.Generic;

namespace MCV_Module.Models.Project
{
    [Serializable]
    public class QuestionData : DataBase
    {
        public string questionText;
        public QuestionType questionType = QuestionType.SingleChoice;
        public List<QuestionItem> options = new List<QuestionItem>();

        public QuestionData()
        {
            id = "questionData";
            displayName = "问题";
            description = "这是一个问题数据";
            questionText = "这是一个问题数据的提干";
            questionType = QuestionType.SingleChoice;
            // 注意：不要在这里向 options 填充默认选项。
            // Newtonsoft 反序列化时对已初始化集合是「追加」而非「替换」，若构造函数塞默认项，
            // JSON 往返（ToJson→FromJson）后会出现「默认项 + 数据项」的重复。
        }
    }

    [Serializable]
    public class QuestionClip : DataBase
    {
        public List<QuestionData> questions = new List<QuestionData>();

        public QuestionClip()
        {
            id = "questionClip";
            displayName = "问题集";
            description = "这是一个问题集";
            // 注意：不要在这里向 questions 填充默认 QuestionData。
            // 原因同 QuestionData 构造函数的注释：JSON 往返时会追加默认项导致重复。
        }
    }

    [Serializable]
    public struct QuestionItem
    {
        public string itemText;
        public bool isCorrect;
    }


}
