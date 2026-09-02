using System;
using System.Collections.Generic;
using MCV_Module.Managers;
using MCV_Module.Models.Project;
using MCV_Module.UI;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>RecordPanel 面板</summary>
    [RequireController(typeof(MCV_Module.Controllers.RecordController))]
    public class RecordPanel : PanelBase
    {
        [SerializeField] Transform RecordListLabelParent;
        [SerializeField] Color normalColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] Color selectedColor = new Color(1, 1, 1);
        [SerializeField] Transform RecordContentParent;
        [SerializeField] Button backBtn;
        static readonly string[] RecordListLabelPaths = { "UI/RecordListLabelOne", "UI/RecordListLabelTwo", "UI/RecordListLabelThree" };
        static readonly string RecordContentLinePath = "UI/RecordContentLine";
        string currentLabel;
        List<RecordContentLineStruct> recordContentLineStructs = new List<RecordContentLineStruct>();
        List<RecordListLabelStruct> recordListLabelStructs = new List<RecordListLabelStruct>();
        List<RecordContentLineClass> recordContentLineClasses = new List<RecordContentLineClass>();

        public event Action OnRececordPanelClosed;

        #region 生命周期
        protected override void Awake()
        {            
            base.Awake();
            if (RecordListLabelParent == null || RecordContentParent == null || backBtn == null)
            {
                Log.Error($"[RecordPanel] 缺少必要组件", this);
                return;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="labelContent"></param>
        /// <remarks> 每次打开panel执行 </remarks>
        public void Init(string labelContent,List<RecordContentLineStruct> recordContentLineStructs)
        {
            recordListLabelStructs.Clear();
            recordContentLineClasses.Clear();
            recordContentLineStructs.Clear();

            this.recordContentLineStructs = recordContentLineStructs;
            currentLabel = labelContent;

            UpdateLabels();
            UpdateContent();
        }
        #endregion

        #region 工具方法
        /// <summary>标签支持的最大层级（RecordListLabelPaths 仅 3 级：0~2）</summary>
        const int MaxLabelLevel = 2;

        void UpdateLabels()
        {
            ClearChildren(RecordListLabelParent);

            MenuData menuData = GlobalDataMgr.GetMenuData();
            if (menuData == null) return;

            var rootClips = menuData.GetRootClips();
            for (int i = 0; i < rootClips.Count; i++)
            {
                CreateLabelRecursive(menuData, rootClips[i], (i + 1).ToString(), 0, null);
            }
        }

        /// <summary>
        /// 深度优先按层级创建标签，编号形如 1 / 1-1 / 1-1-1 / 1-1-2 / 1-2
        /// </summary>
        /// <param name="menuData"> 菜单数据源 </param>
        /// <param name="clip"> 当前菜单 </param>
        /// <param name="labelPath"> 编号路径，如 "1-1" </param>
        /// <param name="level"> 层级，0 起 </param>
        /// <param name="parentStruct"> 父级标签结构 </param>
        void CreateLabelRecursive(MenuData menuData, MenuClip clip, string labelPath, int level, RecordListLabelStruct parentStruct)
        {
            if (clip == null) return;
            if (level > MaxLabelLevel)
            {
                Log.Warning($"RecordPanel: 菜单层级超过 {MaxLabelLevel + 1} 级，已截断：{labelPath}");
                return;
            }

            RecordListLabelStruct labelStruct = CreateListLabelPrefab(level, labelPath, parentStruct);
            if (labelStruct != null) recordListLabelStructs.Add(labelStruct);

            var children = menuData.GetChildClips(clip);
            for (int i = 0; i < children.Count; i++)
            {
                CreateLabelRecursive(menuData, children[i], labelPath + "-" + (i + 1), level + 1, labelStruct);
            }
        }

        void UpdateContent()
        {
            ClearChildren(RecordContentParent);
            for (int i = 0; i < recordContentLineStructs.Count; i++)
            {
                RecordContentLineStruct recordContentLineStruct = recordContentLineStructs[i];
                RecordContentLineClass recordContentLineClass = CreateLinePrefab(recordContentLineStruct);
                recordContentLineClasses.Add(recordContentLineClass);
            }
        }

        RecordListLabelStruct CreateListLabelPrefab(int level,string labelContent,RecordListLabelStruct parentStruct = null)
        {
            GameObject prefab = Resources.Load<GameObject>(RecordListLabelPaths[level]);
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, RecordListLabelParent);
            go.name = labelContent;
            return new RecordListLabelStruct(labelContent, go.transform, normalColor, selectedColor, parentStruct);
        }

        RecordContentLineClass CreateLinePrefab(RecordContentLineStruct recordContentLineStruct)
        {
            GameObject prefab = Resources.Load<GameObject>(RecordContentLinePath);
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, RecordContentParent);
            go.name = $"RecordContentLine_{recordContentLineStruct.index}";
            return new RecordContentLineClass(recordContentLineStruct, go.transform);
        }
        #endregion
    }

    /// <summary>
    /// 记录列表标签结构
    /// </summary>
    public class RecordListLabelStruct
    {
        public string labelContent;
        public Transform structTransform;
        public bool selected = false;
        Text label;
        Color normalColor;
        Color selectedColor;
        RecordListLabelStruct parentStruct;

        public RecordListLabelStruct(string labelContent, Transform structTransform, 
            Color normalColor, Color selectedColor, RecordListLabelStruct parentStruct)
        {
            this.labelContent = labelContent;
            this.structTransform = structTransform;
            this.normalColor = normalColor;
            this.selectedColor = selectedColor;
            this.parentStruct = parentStruct;
            GetLabel();
            if (label != null) SetLabelContent(labelContent);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            this.selected = selected;
            Color color = selected ? selectedColor : normalColor;
            label.color = color;
        }

        void SetLabelContent(string labelContent)
        {
            label.text = labelContent;
        }

        void GetLabel()
        {
            label = structTransform.GetChild(0).GetComponent<Text>();
        }
    }
    /// <summary>
    /// 记录内容行结构类
    /// </summary>
    public class RecordContentLineClass
    {
        public RecordContentLineStruct recordContentLineStruct;
        public Transform structTransform;

        Text indexText;
        Text stepNameText;
        Text executeStandardText;
        Text recordResultText;

        public RecordContentLineClass(RecordContentLineStruct recordContentLineStruct, Transform structTransform)
        {
            this.recordContentLineStruct = recordContentLineStruct;
            this.structTransform = structTransform;
            GetText();

            SetTextContent(recordContentLineStruct.index, recordContentLineStruct.stepName, recordContentLineStruct.executeStandard, recordContentLineStruct.recordResult);
        }

        void SetTextContent(string index, string stepName, string executeStandard, string recordResult)
        {
            if (indexText != null)indexText.text = index;
            if (stepNameText != null)stepNameText.text = stepName;
            if (executeStandardText != null)executeStandardText.text = executeStandard;
            if (recordResultText != null)recordResultText.text = recordResult;
        }

        void GetText()
        {
            indexText = structTransform.GetChild(0).GetChild(0).GetComponent<Text>();
            stepNameText = structTransform.GetChild(1).GetChild(0).GetComponent<Text>();
            executeStandardText = structTransform.GetChild(2).GetChild(0).GetComponent<Text>();
            recordResultText = structTransform.GetChild(3).GetChild(0).GetComponent<Text>();
        }

    }
    /// <summary>
    /// 记录内容行结构
    /// </summary>
    public struct RecordContentLineStruct
    {
        public string index;
        public string stepName;
        public string executeStandard;
        public string recordResult;
    }
}
