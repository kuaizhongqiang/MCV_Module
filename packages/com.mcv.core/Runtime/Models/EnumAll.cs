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

    #region Rendering
    [Serializable]
    public enum RenderQualityLevel
    {
        [InspectorName("低")]
        Low = 0,
        [InspectorName("中")]
        Medium = 1,
        [InspectorName("高")]
        High = 2,
    }
    #endregion

    #region Input
    [Serializable]
    public enum MouseMoveState
    {
        [InspectorName("移动中")]
        Moving,
        [InspectorName("已静止")]
        Idle,
    }
    #endregion

    #region UI
    [Serializable]
    public enum ComponentType
    {
        [InspectorName("文字")]
        Text,
        [InspectorName("视频")]
        Video, 
    }
    /// <summary>文本形态（设置类数据 <c>SystemData.textType</c> 的取值）：Legacy = 0 是默认值，与工程现状一致。</summary>
    [Serializable]
    public enum TextType
    {
        [InspectorName("旧版 Text")]
        Legacy = 0,
        [InspectorName("TextMeshPro")]
        TMP = 1,
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

    #region Text（TextComponent 专用）
    /// <summary>显示稳定的分层（TextComponent.OnFinished）：Write = ①定型＋②赋值；Typography = 再加 ③中文排版收敛；Layout = 再加 ④布局完成。</summary>
    [Serializable]
    public enum TextFinishLayer
    {
        Write = 0,
        Typography = 1,
        Layout = 2,
    }
    /// <summary>文本组件的装配阶段：None 未开始（就绪门前）/ Assembling 装配中（换向跨帧）/ Ready 完成 / Failed 换向两次均失败。</summary>
    [Serializable]
    public enum AssemblePhase
    {
        None = 0,
        Assembling = 1,
        Ready = 2,
        Failed = 3,
    }
    /// <summary>TextComponent 装配期间缓冲的写入类型：None / 字面量 / key / 条目（三态缺一不可）。</summary>
    [Serializable]
    public enum PendingKind
    {
        None = 0,
        Literal = 1,
        Key = 2,
        Clip = 3,
    }
    #endregion

    /// <summary>对话框身份（DialogRequestEvent / DialogResultEvent 的认领键）：只用于区分来源，不做显示，故不带 InspectorName；None = 无需认领的纯提示框。</summary>
    [Serializable]
    public enum DialogId
    {
        None,          // 未指定（纯提示框，发布方不认领结果）
        Exit,          // FunctionController「退出」
        Back,          // FunctionController「返回」
        BackToMenu,    // ContentFunctionController「返回菜单」
        QuitApp,       // MenuController「退出应用」
        EnterProject,  // MenuController「进入项目」
        SubmitScore,   // ResultSummitController「提交成绩」
        BackFromExam,  // TaskExamController「离开考核返回菜单」（菜单「小测验」入口走的也是考核面板）
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

    /// <summary>实训完成情况（成绩档案 <c>ScoreData.completion</c>）。</summary>
    [Serializable]
    public enum CompletionStatus
    {
        [InspectorName("未完成")]
        Unfinished = 0,
        [InspectorName("已完成")]
        Completed = 1,
    }
    #endregion
    
    #region Element
    [Serializable]
    public enum InspectionProbeType
    {
        [InspectorName("红表笔")]
        Red,
        [InspectorName("黑表笔")]
        Black,
    }

    /// <summary>数字万用表档位（旋钮指向的功能）：电阻类读 resistance、电压类读 voltage、电流类读 current，Off = 关机。</summary>
    [Serializable]
    public enum MultimeterGearType
    {
        [InspectorName("关机")]
        Off,
        [InspectorName("电阻")]
        Resistance,
        [InspectorName("直流电压")]
        VoltageDC,
        [InspectorName("交流电压")]
        VoltageAC,
        [InspectorName("直流电流")]
        CurrentDC,
        [InspectorName("交流电流")]
        CurrentAC,
        [InspectorName("二极管")]
        Diode,
        [InspectorName("通断")]
        Continuity,
    }

    /// <summary>检测点「端子对」类型：只决定自动配对时填的默认读数，不决定配对本身。</summary>
    [Serializable]
    public enum TerminalPairKind
    {
        [InspectorName("动合（常开）触点")]
        NoContact,

        [InspectorName("动断（常闭）/ 正常导通")]
        NcContact,

        [InspectorName("其它（阻值按实物填）")]
        Other,
    }

    /// <summary>测量时被测电器的状态：静止时动合断开（∞）、动断闭合（0Ω），已动作时相反，由元件动作部件决定。</summary>
    [Serializable]
    public enum ElementActuationState
    {
        [InspectorName("静止（未通电）")]
        Normal,

        [InspectorName("已动作（通电 / 按住试验按键）")]
        Actuated,
    }

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

    #region Interactive
    /// <summary>检测笔（<c>InspectionProbeObj</c>）的拖拽平面法线取法</summary>
    [Serializable]
    public enum MovePlaneNormal
    {
        [InspectorName("自动（取与相机视线最接近的世界轴）")]
        Auto,
        [InspectorName("屏幕平行面（法线 = 相机视线）")]
        CameraFacing,
        [InspectorName("X轴平面")]
        X,
        [InspectorName("Y轴平面")]
        Y,
        [InspectorName("Z轴平面")]
        Z,
    }

    /// <summary>元器件上「可点击部件」（<c>InspectionSwitchObj</c>：接触器试验按钮、断路器手柄…）的手势语义。</summary>
    [Serializable]
    public enum SwitchGesture
    {
        [InspectorName("按住（松手弹回）")]
        Press,
        [InspectorName("点击切换（自锁）")]
        Toggle,
        [InspectorName("拖拽跟手")]
        Drag,
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
        [InspectorName("简介")]
        Info,
        [InspectorName("结构")]
        Structure,
        [InspectorName("检测")]
        Inspection,
        [InspectorName("考核")]
        Exam,

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
    /// <summary>题目用途（题库 <c>QuestionData.json</c>）：<see cref="Exam"/> = 考核抽题池（默认），<see cref="Step"/> = 步骤答题，不进考核池。</summary>
    [Serializable]
    public enum QuestionUsage
    {
        [InspectorName("考核")]
        Exam,
        [InspectorName("步骤")]
        Step,
    }
    [Serializable]
    public enum ProjectState
    {
        [InspectorName("开始")]
        Start,
        [InspectorName("目录")]
        Menu,
        [InspectorName("漫游")]
        Roaming,
        [InspectorName("UI")]
        UI,
        [InspectorName("考核")]
        Exam,
    }
    [Serializable]
    public enum ConditionType
    {        
        [InspectorName("默认无操作")]
        Default,
        [InspectorName("点击交互")]
        Click,
        [InspectorName("拖拽交互")]
        Drag,
        [InspectorName("工具交互")]
        Tool,
        [InspectorName("UI 交互")]
        UI,
        [InspectorName("答题")]
        Question,
        [InspectorName("连线配对")]
        LineConnect,
        [InspectorName("完成")]
        Finish,
        [InspectorName("开始")]
        Start,
        [InspectorName("测量一对点")]
        MeasurePair,
        [InspectorName("调整表旋钮")]
        GearAdjust,
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
    /// <summary>步骤文字内容的类型——多态条目的判别字段，与 <c>Models/Project/StepContentBase</c> 子类一一对应。</summary>
    [Serializable]
    public enum StepContentType
    {
        [InspectorName("空类型")]
        None,
        [InspectorName("UI说明")]
        UI,
        [InspectorName("提示")]
        Tips,
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