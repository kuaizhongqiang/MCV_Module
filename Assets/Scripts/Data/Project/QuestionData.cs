using System;
using System.Collections.Generic;

namespace MCV_Module.Data.Project
{
    [Serializable]
    public class QuestionData
    {
        public string questionId;
        public string questionText;
        public QuestionType questionType = QuestionType.SingleChoice;
        public List<QuestionItem> options = new List<QuestionItem>();
    }

    [Serializable]
    public class QuestionClip
    {
        public List<QuestionData> questions = new List<QuestionData>();
        public float passScore = 60f;
    }

    [Serializable]
    public class QuestionItem
    {
        public string itemText;
        public bool isCorrect;
    }

    public enum QuestionType
    {
        None,
        SingleChoice,
        MultipleChoice,
        TrueFalse,
        FillInBlank,
    }
}
