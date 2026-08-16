using System;
using UnityEngine;

namespace MCV_Module.Models
{
    [Serializable]
    public enum PlayMode
    {
        Debug,
        Release,
    }

    #region UI
    [Serializable]
    public enum ComponentType
    {
        [InspectorName("文字")]
        Text,
        [InspectorName("视频")]
        Video, 
    }
    [Serializable]
    public enum TextType
    {
        [InspectorName("传统")]
        Legacy,
        [InspectorName("Tmp")]
        TextMeshPro,
    }
    [Serializable]
    public enum VideoType
    {
        [InspectorName("传统")]
        Legacy,
        [InspectorName("AvPro")]
        AvPro,
    }
    [Serializable]
    public enum OverrideAlignment
    {
        UpperLeft,
        UpperCenter,
        UpperRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        LowerLeft,
        LowerCenter,
        LowerRight,
        Justified,
        Auto,
    }
    #endregion

    #region Audio
    [Serializable]
    public enum AudioSouceType
    {
        BGM,
        Speaker,
        Effect,
    }

    [Serializable]
    public enum AudioEffectType
    {
        Click,
        Dragging,
        Success,
        Fail,
        Hover,
        None,
    }
    #endregion

    #region Scene
    [Serializable]
    public enum SceneState
    {
        [InspectorName("初始化")]
        Setup,
        [InspectorName("开始")]
        Start,
        [InspectorName("登录")]
        Login,
        [InspectorName("菜单")]
        Menu,
        [InspectorName("UI")]
        UI,
        [InspectorName("漫游")]
        Roaming,
    }

    #endregion

    #region User
    [Serializable]
    public enum UserType
    {
        [InspectorName("未知")]
        Unknow,
        [InspectorName("学生")]
        Student,
        [InspectorName("教师")]
        Teacher,
        [InspectorName("管理员")]
        Admin
    }
    #endregion
    #region Element
    [Serializable]
    public enum ElementType
    {
        [InspectorName("空类型")]
        None,
        [InspectorName("电阻")]
        Resistor,
        [InspectorName("电容")]
        Capacitor,
        [InspectorName("电感")]
        Inductor,
        [InspectorName("热继电器")]
        Thermistor,
        [InspectorName("熔断器")]
        Fuse,
        [InspectorName("接触器")]
        Contactor,
        [InspectorName("按钮开关")]
        ButtonSwitch,
        [InspectorName("旋钮开关")]
        KnobSwitch,
        [InspectorName("滑块开关")]
        SliderSwitch,
        [InspectorName("电源")]
        Power,
        [InspectorName("断路器")]
        Breaker,
        [InspectorName("继电器")]
        Relay,
        [InspectorName("时间继电器")]
        TimerRelay,
        [InspectorName("电动机")]
        Motor,
        [InspectorName("点")]
        Point,
        [InspectorName("线")]
        Line,
    }
    [Serializable]
    public enum ElementPointNameType
    {        
        [InspectorName("空类型")]
        None,
        [InspectorName("1")]
        One,
        [InspectorName("2")]
        Two,
        [InspectorName("3")]
        Three,
        [InspectorName("4")]
        Four,
        [InspectorName("5")]
        Five,
        [InspectorName("6")]
        Six,
        [InspectorName("7")]
        Seven,
        [InspectorName("8")]
        Eight,
        [InspectorName("1L1")]
        Input1,
        [InspectorName("3L2")]
        Input2,
        [InspectorName("5L3")]
        Input3,
        [InspectorName("2T1")]
        Output1,
        [InspectorName("4T2")]
        Output2,
        [InspectorName("6T3")]
        Output3,
        [InspectorName("13NO")]
        NO_In_1,
        [InspectorName("15NO")]
        NO_In_2,
        [InspectorName("17NO")]
        NO_In_3,
        [InspectorName("14NO")]
        NO_Out_1,
        [InspectorName("16NO")]
        NO_Out_2,
        [InspectorName("18NO")]
        NO_Out_3,
        [InspectorName("21NC")]
        NC_In_1,
        [InspectorName("23NC")]
        NC_In_2,
        [InspectorName("25NC")]
        NC_In_3,
        [InspectorName("22NC")]
        NC_Out_1,
        [InspectorName("24NC")]
        NC_Out_2,
        [InspectorName("26NC")]
        NC_Out_3,
        [InspectorName("A1")]
        A1,
        [InspectorName("A2")]
        A2,
        [InspectorName("U1")]
        U1,
        [InspectorName("V1")]
        V1,
        [InspectorName("W1")]
        W1,
        [InspectorName("U2")]
        U2,
        [InspectorName("V2")]
        V2,
        [InspectorName("W2")]
        W2,
        [InspectorName("接地")]
        PE, 
        [InspectorName("95")]
        NinetyFive,
        [InspectorName("96")]
        NinetySix,
    }
    #endregion

    #region Task
    [Serializable]
    public enum TaskType
    {
        [InspectorName("空类型")]
        None,
        [InspectorName("任务目的")]
        Purpose,
        [InspectorName("实验仪器")]
        Equipment,
        [InspectorName("实验原理")]
        Principle,
        [InspectorName("电路连接")]
        LineConnection,
        [InspectorName("仿真实验")]
        Training,
        [InspectorName("小测验")]
        Test,

    }
    [Serializable]
    public enum QuestionType
    {
        None,
        SingleChoice,
        MultipleChoice,
        TrueFalse,
        FillInBlank,
    }
    
    [Serializable]
    public enum ConditionType
    {        
        [InspectorName("默认无操作")]
        Default,     // 默认无操作
        [InspectorName("点击交互")]
        Click,       // 点击交互
        [InspectorName("拖拽交互")]
        Drag,        // 拖拽交互
        [InspectorName("工具交互")]
        Tool,        // 工具使用
        [InspectorName("UI 交互")]
        UI,          // UI 交互
        [InspectorName("答题")]
        Question,    // 答题
        [InspectorName("连线配对")]
        LineConnect, // 连线配对
        [InspectorName("完成")]
        Finish,      // 完成/结束
    }
    [Serializable]
    public enum StepStutus
    {
        [InspectorName("准备")]
        Ready,
        [InspectorName("等待")]
        Waiting,
        [InspectorName("完成")]
        Complete,
    }
    #endregion

    #region Animation
    [Serializable]
    public enum ObjAxis
    {
        [InspectorName("X轴")]
        X,
        [InspectorName("Y轴")]
        Y,
        [InspectorName("Z轴")]
        Z,
    }
    #endregion

    #region Language
    [Serializable]
    public enum LanguageType
    {
        [InspectorName("简体中文")]
        Chinese,
        [InspectorName("English")]
        English,
    }
    #endregion
}