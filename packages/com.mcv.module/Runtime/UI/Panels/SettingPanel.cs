// 由 MCV/创建/UI Panel 生成器生成（2026-09-01）—— 请按需补充业务代码
using System.Collections.Generic;
using System.Linq;
using MCV_Module.Models;
using MCV_Module.UI;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>SettingPanel 面板</summary>
    [RequireController(typeof(MCV_Module.Controllers.SettingController))]
    public class SettingPanel : PanelBase
    {
        [SerializeField] Transform content;
        [SerializeField] Button saveBtn;
        [SerializeField] Button resetBtn;
        List<SettingLabelBase> settingLabels = new ();
        static readonly string[] SettingLabelDisplayName = new string[]
        {
            "界面设置",        // title
            "窗口化",          // bool
            "屏幕分辨率",      // dropdown
            "画面设置",        // title
            "渲染质量",        // dropdown
            "抗锯齿",          // bool
            "帧率",            // slider
            "音频设置",        // title
            "总音量",          // slider
            "背景音乐",        // slider
            "旁白音量",        // slider
            "鼠标音量",        // slider
            "语言设置",        // dropdown
        };
        static readonly string[] SettingLabelContent = new string[]
        {
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
        };

        static readonly string[] SettingLabelInfo = new string[]
        {
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
        };

        #region 常量
        /// <summary>设置行索引（与 SettingLabelDisplayName 顺序一一对应）</summary>
        enum SettingRow
        {
            InterfaceTitle,   // 界面设置（标题）
            Windowed,         // 窗口化（bool）
            Resolution,       // 屏幕分辨率（dropdown）
            GraphicTitle,     // 画面设置（标题）
            RenderQuality,    // 渲染质量（dropdown）
            AntiAliasing,     // 抗锯齿（bool）
            FrameRate,        // 帧率（slider）
            AudioTitle,       // 音频设置（标题）
            MasterVolume,     // 总音量（slider）
            BgmVolume,        // 背景音乐（slider）
            NarrationVolume,  // 旁白音量（slider）
            MouseVolume,      // 鼠标音量（slider）
            Language,         // 语言设置（dropdown）
        }
        /// <summary>帧率滑条范围（prefab 默认 0~1，帧率行在代码中覆盖）</summary>
        const float FrameRateMin = 30f;
        const float FrameRateMax = 120f;

        // 窗口化（bool）两态文案：index 0 = 关闭态（全屏），index 1 = 开启态（窗口化）
        static readonly string[] WindowToggleDisplayName = new string[]
        {
            "全屏",
            "窗口化"
        };
        // 抗锯齿（bool）两态文案：index 0 = 关闭态，index 1 = 开启态
        static readonly string[] TTAOptions = new string[]
        {
            "关闭",
            "打开"
        };

        // 屏幕分辨率（dropdown）
        enum ScreenResolution
        {
            Low,            // 1366x768      低分辨率
            Medium,         // 1920x1080     标准分辨率
            High,           // 2560x1440     2.5K分辨率
            Ultra,          // 3840x2160     4K分辨率
        }
        // 屏幕分辨率显示内容（dropdown，与 ScreenResolution 枚举顺序一一对应）
        static readonly string[] ScreenResolutionDisplayName = new string[]
        {
            "低分辨率",
            "标准分辨率",
            "2.5K分辨率",
            "4K分辨率",
        };
        // 屏幕分辨率的实际值（x=宽，y=高；保存设置时经 GetResolutionValue 取用）
        static readonly Vector2[] ScreenResolutionOptions = new Vector2[]
        {
            new Vector2(1366, 768),
            new Vector2(1920, 1080),
            new Vector2(2560, 1440),
            new Vector2(3840, 2160),
        };
        // 渲染质量（dropdown）
        enum RenderQuality
        {
            Low,             // index = 0     标清质量
            Medium,          // index = 1     高清质量
            High,            // index = 2     超清质量
        }
        // 渲染质量显示内容（dropdown，与 RenderQuality 枚举顺序一一对应；保存时映射 QualitySettings.SetQualityLevel）
        static readonly string[] RenderQualityDisplayName = new string[]
        {
            "标清质量",
            "高清质量",
            "超清质量",
        };

        /// <summary>语言选项（占位）</summary>
        static readonly string[] LanguageOptions = { "简体中文", "English" };

        /// <summary>全部设置行的默认值（唯一来源：首次打开与重置共用）</summary>
        static readonly DefaultSetting defaultSetting = new DefaultSetting();
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            if (content == null || saveBtn == null || resetBtn == null)
            {
                Log.Error($"[SettingPanel] 缺少必要组件", this);
                return;
            }
            Init();
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 初始化：清空并重建全部设置行（每次打开 panel 执行）
        /// </summary>
        public void Init()
        {
            ClearChildren(content);
            settingLabels.Clear();
            CreateLabels();
            var rect = content.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        /// <summary>分辨率下拉框索引 → 实际分辨率（宽x高），供保存设置时取用</summary>
        public static Vector2 GetResolutionValue(int index)
        {
            index = Mathf.Clamp(index, 0, ScreenResolutionOptions.Length - 1);
            return ScreenResolutionOptions[index];
        }
        #endregion

        #region 工具方法
        /// <summary>按 SettingRow 顺序创建每一行设置控件</summary>
        void CreateLabels()
        {
            for (int i = 0; i < SettingLabelDisplayName.Length; i++)
            {
                string title = SettingLabelDisplayName[i];
                string label = SettingLabelContent[i];
                string info = SettingLabelInfo[i];
                SettingLabelBase settingLabel = CreateLabelByRow((SettingRow)i, title, label, info);
                if (settingLabel != null)
                {
                    settingLabels.Add(settingLabel);
                }
            }
        }

        /// <summary>按行类型实例化对应 SettingLabel 子类（默认值统一取自 defaultSetting）</summary>
        SettingLabelBase CreateLabelByRow(SettingRow row, string title, string label, string info)
        {
            switch (row)
            {
                case SettingRow.Windowed:
                    return new SettingLabelBool(title, label, info, content, defaultSetting.Windowed, WindowToggleDisplayName);
                case SettingRow.Resolution:
                    return new SettingLabelDropdown(title, label, info, ScreenResolutionDisplayName, content, (int)defaultSetting.Resolution);
                case SettingRow.RenderQuality:
                    return new SettingLabelDropdown(title, label, info, RenderQualityDisplayName, content, (int)defaultSetting.RenderQuality);
                case SettingRow.AntiAliasing:
                    return new SettingLabelBool(title, label, info, content, defaultSetting.AntiAliasing, TTAOptions);
                case SettingRow.FrameRate:
                    return new SettingLabelSlider(title, label, info, content, defaultSetting.FrameRate, FrameRateMin, FrameRateMax, true);
                case SettingRow.MasterVolume:
                    return new SettingLabelSlider(title, label, info, content, defaultSetting.MasterVolume);
                case SettingRow.BgmVolume:
                    return new SettingLabelSlider(title, label, info, content, defaultSetting.BgmVolume);
                case SettingRow.NarrationVolume:
                    return new SettingLabelSlider(title, label, info, content, defaultSetting.NarrationVolume);
                case SettingRow.MouseVolume:
                    return new SettingLabelSlider(title, label, info, content, defaultSetting.MouseVolume);
                case SettingRow.Language:
                    return new SettingLabelDropdown(title, label, info, LanguageOptions, content, defaultSetting.Language);
                default: // InterfaceTitle / GraphicTitle / AudioTitle
                    return new SettingLabelTitle(title, content);
            }
        }
        #endregion

        /// <summary>
        /// 设置面板默认值（下拉框/滑条存索引或数值；属性名与 SettingRow 一一对应）
        /// </summary>
        private class DefaultSetting
        {
            public bool Windowed { get; } = false;                                 // 关闭 = 全屏
            public ScreenResolution Resolution { get; } = ScreenResolution.Medium; // 1920x1080
            public RenderQuality RenderQuality { get; } = RenderQuality.Low;       // 标清质量
            public bool AntiAliasing { get; } = true;
            public float FrameRate { get; } = 60f;
            public float MasterVolume { get; } = 1f;
            public float BgmVolume { get; } = 1f;
            public float NarrationVolume { get; } = 1f;
            public float MouseVolume { get; } = 1f;
            public int Language { get; } = 0;                                      // LanguageOptions 索引
        }
    }

    #region 结构类
    /// <summary>
    /// 设置栏目的基类
    /// </summary>
    public abstract class SettingLabelBase
    {
        protected SettingLabelType labelType;
        protected Text titleText;
        protected GameObject obj;
        protected Transform parent;
        protected string[] prefabPaths = new string[]
        {
            "UI/SettingContent_Label",
            "UI/SettingContent_Input",
            "UI/SettingContent_Bool",
            "UI/SettingContent_Dropdown",
            "UI/SettingContent_Slider",
        };

        protected GameObject CreateLabel()
        {
            string path = prefabPaths[(int)labelType];
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            GameObject go = GameObject.Instantiate(prefab, parent);
            go.name = labelType.ToString();
            return go;
        }

        protected virtual void Init()
        {
            titleText = obj.transform.GetChild(0).GetComponent<Text>();            
        }
    }
    /// <summary>
    /// 设置栏目的标题类型
    /// </summary>
    public class SettingLabelTitle : SettingLabelBase
    {
        string labelContent;
        
        public SettingLabelTitle(string labelContent, Transform parent)
        {
            this.labelType = SettingLabelType.Title;
            this.parent = parent;
            obj = CreateLabel();            
            this.labelContent = labelContent;
            Init();
        }

        protected override void Init()
        {
            base.Init();
            titleText.text = labelContent;
        }
    }
    /// <summary>
    /// 设置栏目的输入类型
    /// </summary>
    public class SettingLabelInput : SettingLabelBase
    {
        string title;
        string label;
        string info;
        string value;
        InputField inputField;

        public SettingLabelInput(string title, string label, string info, Transform parent, string defaultValue = "")
        {
            this.labelType = SettingLabelType.Input;
            this.parent = parent;
            obj = CreateLabel();            
            this.title = title;
            this.label = label;
            this.info = info;
            this.value = defaultValue;
            Init();
        }

        /// <summary>当前输入值</summary>
        public string Value => value;

        protected override void Init()
        {
            base.Init();
            titleText.text = title;
            obj.transform.GetChild(1).GetComponent<Text>().text = label;
            obj.transform.GetChild(3).GetComponent<Text>().text = info;
            inputField = obj.transform.GetChild(2).GetComponent<InputField>();
            inputField.text = value;
            inputField.onValueChanged.AddListener(v => value = v);
        }
    }
    /// <summary>
    /// 设置栏目的布尔类型
    /// </summary>
    public class SettingLabelBool : SettingLabelBase
    {
        string title;
        string label;
        string info;
        bool value;
        string[] stateNames;   // 两态文案（[0]=关闭态，[1]=开启态）；为 null 时显示静态 label
        Toggle toggle;
        Text stateText;

        public SettingLabelBool(string title, string label, string info, Transform parent, bool defaultValue = false, string[] stateNames = null)
        {
            this.labelType = SettingLabelType.Bool;
            this.parent = parent;
            obj = CreateLabel();            
            this.title = title;
            this.label = label;
            this.info = info;
            this.value = defaultValue;
            this.stateNames = stateNames;
            Init();
        }

        /// <summary>当前开关值</summary>
        public bool Value => value;

        protected override void Init()
        {
            base.Init();
            titleText.text = title;
            stateText = obj.transform.GetChild(1).GetComponent<Text>();
            obj.transform.GetChild(3).GetComponent<Text>().text = info;
            toggle = obj.transform.GetChild(2).GetComponent<Toggle>();
            toggle.isOn = value;
            RefreshStateText();
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        void OnToggleChanged(bool isOn)
        {
            value = isOn;
            RefreshStateText();
        }

        /// <summary>刷新开关旁文案：有 stateNames 时跟随状态（index 0 = 关 / 1 = 开），否则显示静态 label</summary>
        void RefreshStateText()
        {
            if (stateNames != null && stateNames.Length > 1)
            {
                stateText.text = stateNames[value ? 1 : 0];
            }
            else
            {
                stateText.text = label;
            }
        }
    }
    /// <summary>
    /// 设置栏目的下拉框类型
    /// </summary>
    public class SettingLabelDropdown : SettingLabelBase
    {
        string title;
        string label;
        string info;
        string[] options;
        int value;
        Dropdown dropdown;

        public SettingLabelDropdown(string title, string label, string info, string[] options, Transform parent, int defaultValue = 0)
        {
            this.labelType = SettingLabelType.Dropdown;
            this.parent = parent;
            obj = CreateLabel();            
            this.title = title;
            this.label = label;
            this.info = info;
            this.options = options;
            this.value = defaultValue;
            Init();
        }

        /// <summary>当前选中索引（对应 options / 枚举索引）</summary>
        public int Value => value;

        protected override void Init()
        {
            base.Init();
            titleText.text = title;
            obj.transform.GetChild(1).GetComponent<Text>().text = label;
            obj.transform.GetChild(3).GetComponent<Text>().text = info;
            dropdown = obj.transform.GetChild(2).GetComponent<Dropdown>();
            if (options != null && options.Length > 0)
            {
                dropdown.options = options.Select(x => new Dropdown.OptionData(x)).ToList();
                dropdown.value = Mathf.Clamp(value, 0, options.Length - 1);
                dropdown.RefreshShownValue();
                dropdown.onValueChanged.AddListener(v => value = v);
            }
        }
    }
    /// <summary>
    /// 设置栏目的滑块类型
    /// </summary>
    public class SettingLabelSlider : SettingLabelBase
    {
        string title;
        string label;
        string info;
        float value;
        float minValue;
        float maxValue;
        bool wholeNumbers;
        Slider slider;
        Text valueText;

        public SettingLabelSlider(string title, string label, string info, Transform parent, float defaultValue = 0,
            float minValue = 0f, float maxValue = 1f, bool wholeNumbers = false)
        {
            this.labelType = SettingLabelType.Slider;
            this.parent = parent;
            obj = CreateLabel();            
            this.title = title;
            this.label = label;
            this.info = info;
            this.value = defaultValue;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.wholeNumbers = wholeNumbers;
            Init();
        }

        /// <summary>当前滑条值</summary>
        public float Value => value;

        protected override void Init()
        {
            base.Init();
            titleText.text = title;
            obj.transform.GetChild(1).GetComponent<Text>().text = label;
            obj.transform.GetChild(2).GetComponent<Text>().text = info;
            slider = obj.transform.GetChild(3).GetComponent<Slider>();
            slider.wholeNumbers = wholeNumbers;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = Mathf.Clamp(value, minValue, maxValue);
            valueText = slider.transform.GetChild(3).GetComponent<Text>();
            RefreshValueText();
            slider.onValueChanged.AddListener(v =>
            {
                value = v;
                RefreshValueText();
            });
        }

        /// <summary>数值文案：整数滑条显示整数，其余保留最多两位小数</summary>
        void RefreshValueText()
        {
            valueText.text = wholeNumbers ? ((int)value).ToString() : value.ToString("0.##");
        }
    }
    #endregion

}
