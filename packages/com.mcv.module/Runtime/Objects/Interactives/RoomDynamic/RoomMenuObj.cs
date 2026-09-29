using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models.Project;
using MCV_Module.Utils;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.Objects.Interactives.RoomDynamic
{
    // WHY: 点击只抛 RoomMenuEnterRequestEvent、不自己跳转——HUD 随房间场景销毁，二次确认与跳转必须交给常驻 MenuController
    /// <summary>漫游房间里的项目 HUD（展品标签）：悬停高亮 + 点击请求进入对应项目的内容页</summary>
    public class RoomMenuObj : InteractiveBase
    {
        [SerializeField] Color nomalColor = Color.white;
        [Tooltip("对应 ProjectClip.id（如 clip_contactor）；也可填 displayName，脚本会兜底按名匹配")]
        [SerializeField] string projectName = "ProjectName";
        [SerializeField] Text projectNameText;
        [SerializeField] Image projectICO;

        /// <summary>高亮装饰件（= 除 [0] 底图外的全部子物体），悬停时整体激活</summary>
        Transform[] m_HighlightObjs;

        /// <summary>已解析的项目；null = 还没解析出来（数据源未就绪或 projectName 配错）</summary>
        ProjectClip m_Clip;

        /// <summary>项目名文本节点上的组件（TMP 形态下认领的控件是 TMP、Legacy 已被卸载，悬停变色必须经它下发）。</summary>
        TextComponent m_ProjectNameTextComp;

        #region 生命周期
        protected override void Awake()
        {
            // 先走基类：交互注册与 Mo* 事件绑定不能省
            base.Awake();

            // WHY: 提前解析一次组件 —— 必须早于卸载：换形态在本组件 Awake 里发起，而 Destroy 到帧末才生效，此刻 GetComponent
            // 稳定可用，换形态之后再解析就会抛。TMP 形态下本组件会把该节点上的 Legacy Text **卸载**（先禁用再 Destroy；必须卸 ——
            // Unity 不允许同一个 GameObject 上存在两个 Graphic，留着 Legacy 会让 AddComponent<TextMeshProUGUI>() 被拒绝并返回 null），
            // 并把当前认领的控件换成 TMP；直写 projectNameText.color 会抛 MissingReferenceException，悬停变色只能写 ColorValue。
            if (projectNameText != null) m_ProjectNameTextComp = projectNameText.GetComponent<TextComponent>();

            CacheHighlightObjs();
            HighlightPluginInit(gameObject);

            ApplyProject();

            // WHY: 初始必须未高亮（不依赖预制体上 1~4 号显隐），否则进屋就带着高亮框
            SetHighlight(false);
        }
        #endregion

        #region 装配
        /// <summary>缓存 [1..n] 高亮装饰件：0 号是底图永不参与，只按"除底图外全部"这个约定取</summary>
        void CacheHighlightObjs()
        {
            int count = Mathf.Max(transform.childCount - 1, 0);
            m_HighlightObjs = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                m_HighlightObjs[i] = transform.GetChild(i + 1);
            }
        }

        /// <summary>解析 projectName 并刷新名称文案；解析不到保持预制体原文案，不写空串</summary>
        void ApplyProject()
        {
            m_Clip = FindClip(projectName);
            if (m_Clip == null) return;
            // WHY: 文案必须经组件写 —— 换形态后 projectNameText 成了"假 null"，静态入口 SetTextOn 会静默 no-op（项目名不显示）。
            if (m_ProjectNameTextComp != null) m_ProjectNameTextComp.SetText(Localized.Name(m_Clip));
            else TextComponent.SetTextOn(projectNameText, Localized.Name(m_Clip));
        }

        /// <summary>外部装配图标入口：RoomOneDynamicMgr 装载 RoomOne 包后调用，只交图不写文案</summary>
        public void SetIcon(Sprite icon)
        {
            if (projectICO != null && icon != null) projectICO.sprite = icon;
        }

        /// <summary>本 HUD 对应的 ProjectClip.id（= Inspector 上的 projectName）；加载方据此拼图标的包配置 id</summary>
        public string ProjectName { get { return projectName; } }

        /// <summary>projectName→ProjectClip：先按 id 精确查，再按 displayName 兜底；数据源未就绪静默返回 null</summary>
        static ProjectClip FindClip(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!GlobalDataMgr.Exists || GlobalDataMgr.Instance == null || GlobalDataMgr.Instance.ProjectData == null)
                return null;

            ProjectClip clip = GlobalDataMgr.GetProjectClip(key);
            if (clip != null) return clip;

            var clips = GlobalDataMgr.Instance.ProjectData.clips;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] != null && clips[i].displayName == key) return clips[i];
            }

            return null;
        }
        #endregion

        #region 交互
        protected override void MoEnterEvent()
        {
            SetHighlight(true);
        }

        protected override void MoExitEvent()
        {
            SetHighlight(false);
        }

        /// <summary>点击：补一次解析后把「要进哪个项目」抛给常驻控制器，本类不做跳转决策</summary>
        protected override void MoClickEvent()
        {
            if (m_Clip == null) ApplyProject();

            if (m_Clip == null)
            {
                Log.Warning($"[RoomMenuObj] projectName「{projectName}」解析不到任何项目，点击被忽略（请填 ProjectClip.id）", this);
                return;
            }

            EventBus<RoomMenuEnterRequestEvent>.Publish(new RoomMenuEnterRequestEvent(m_Clip));
        }

        /// <summary>悬停表现：装饰件整体显隐 + 项目名换色，颜色端点取 Inspector（不写死）</summary>
        void SetHighlight(bool isHighlight)
        {
            if (m_HighlightObjs != null)
            {
                for (int i = 0; i < m_HighlightObjs.Length; i++)
                {
                    if (m_HighlightObjs[i] != null) m_HighlightObjs[i].gameObject.SetActive(isHighlight);
                }
            }

            // 用序列化引用而不是 GetChild(0).GetChild(1)：层级一改就断，且省掉每次悬停的查找
            // WHY: 优先写组件的 ColorValue 而不是直写 projectNameText.color —— 换形态后该引用成了"假 null"（Legacy 已被卸载），
            // 直写会抛 MissingReferenceException；可见控件是 TMP，ColorValue 由 ApplyStyle 下发到当前认领的控件。
            // 仅节点上本就没有组件时才退回 SetColorOn 直写。
            Color target = isHighlight ? highlightColor : nomalColor;
            if (m_ProjectNameTextComp != null) m_ProjectNameTextComp.ColorValue = target;
            else TextComponent.SetColorOn(projectNameText, target);
        }
        #endregion
    }
}
