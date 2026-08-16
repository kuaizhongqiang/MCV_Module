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
            options.Add(new QuestionItem() { itemText = "选项1", isCorrect = true });
            options.Add(new QuestionItem() { itemText = "选项2", isCorrect = false });
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
            questions.Add(new QuestionData());
        }
    }

    [Serializable]
    public struct QuestionItem
    {
        public string itemText;
        public bool isCorrect;
    }


}
