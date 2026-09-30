using System.Collections;
using MCV_Module.Utils;
using MCV_Module.Controllers;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    // WHY: 显示/进度/隐藏全部由 LoadingController 订阅 SceneLoadingEvent / SceneLoadedEvent 驱动, 面板自己不决定何时出现或消失
    /// <summary>加载遮挡面板：加载 AA 包 / 切换场景时用于遮挡屏幕。</summary>
    [RequireController(typeof(LoadingController))]
    public class LoadingPanel : PanelBase
    {
        [SerializeField] Image bgImage;
        [SerializeField] Text titleText;
        [SerializeField] Text contentText;
        [SerializeField] Text progressText;
        [SerializeField] Slider progressSlider;
        [SerializeField] AnimationCurve breathCurve;
        [SerializeField] float lifeCycle = 5f;

        // WHY: 本参数决定"遮挡层最短可见时长"，属面板自身的显示表现（与呼吸曲线、生命周期同层），
        //      故自 B 分支起从 LoadingController 迁到面板；控制器只读取它，不再自带序列化字段。
        [SerializeField, Tooltip("遮挡层最短显示时长（秒）：加载再快也要撑满，避免一闪而过；真实加载更慢时不额外等待")]
        float minShowDuration = 1.2f;

        /// <summary>遮挡层最短显示时长（秒）；&lt;=0 视为不等待。</summary>
        public float MinShowDuration => Mathf.Max(0f, minShowDuration);

        Coroutine m_BreathCoroutine;
        /// <summary>进度文本节点上的组件（TMP 形态下本组件认领的控件是 TMP、Legacy 已被卸载，颜色只能经它下发）。</summary>
        TextComponent m_ProgressTextComp;
        /// <summary>标题文本节点上的组件（TMP 形态下节点上的 Legacy Text 被卸载，字段随后成"假 null"，静态入口静默 no-op）。</summary>
        TextComponent m_TitleTextComp;
        /// <summary>正文文本节点上的组件（同上）。</summary>
        TextComponent m_ContentTextComp;

        protected override void Awake()
        {
            base.Awake();

            // WHY: 提前解析一次组件 —— 必须早于卸载：换形态在本组件 Awake 里发起，而 Destroy 到帧末才生效，此刻 GetComponent
            // 稳定可用，换形态之后再解析就会抛。TMP 形态下本组件会把该节点上的 Legacy Text **卸载**（先禁用再 Destroy；必须卸 ——
            // Unity 不允许同一个 GameObject 上存在两个 Graphic，留着 Legacy 会让 AddComponent<TextMeshProUGUI>() 被拒绝并返回 null），
            // 并把当前认领的控件换成 TMP；直写 progressText.color 会抛 MissingReferenceException，呼吸只能写组件的 ColorValue。
            if (progressText != null) m_ProgressTextComp = progressText.GetComponent<TextComponent>();

            // WHY: 标题/正文同样提前解析 —— 换过形态后这两个字段成了"假 null"，那时代码里的 GetComponent 会抛、
            // 静态入口 SetTextOn 会被静默丢弃（加载标题只显示进度不显示文案）。
            if (titleText != null) m_TitleTextComp = titleText.GetComponent<TextComponent>();
            if (contentText != null) m_ContentTextComp = contentText.GetComponent<TextComponent>();

            // WHY: 组件存在即视为已配置 —— 换形态后 Legacy 被卸载、Text 字段变成 null 是正常状态，只有字段与缓存组件都为 null 才算缺配置。
            if (bgImage == null || (titleText == null && m_TitleTextComp == null) || (contentText == null && m_ContentTextComp == null)
                || (progressText == null && m_ProgressTextComp == null) || progressSlider == null)
            {
                Log.Error($"[LoadingPanel] 缺少必要组件", this);
                return;
            }
            StartBreath();
        }   

        public void Init(Texture2D bgTexture, string title, string content)
        {
            // WHY: bgTexture 判空：避免 Awake 中 Init(null,...) 时对 null 解引用
            if (bgTexture != null)
            {
                bgImage.sprite = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), Vector2.zero);
            }
            if (m_TitleTextComp != null) m_TitleTextComp.SetText(title);
            else TextComponent.SetTextOn(titleText, title);
            if (m_ContentTextComp != null) m_ContentTextComp.SetText(content);
            else TextComponent.SetTextOn(contentText, content);
            SetProgress(0f);
            StartBreath();
        }

        public void SetProgress(float progress)
        {
            string progressLabel = Lang.Get("ui.loading.progress", string.Format("{0:0.00}%", progress * 100));
            if (m_ProgressTextComp != null) m_ProgressTextComp.SetText(progressLabel);
            else TextComponent.SetTextOn(progressText, progressLabel);
            progressSlider.value = progress;
        }

        /// <summary>启动呼吸协程（已启动则忽略，避免重复）。</summary>
        void StartBreath()
        {
            if (m_BreathCoroutine != null) return;
            m_BreathCoroutine = StartCoroutine(BreathEffectCoroutine());
        }

        /// <summary>停止呼吸协程。</summary>
        void StopBreath()
        {
            if (m_BreathCoroutine == null) return;
            StopCoroutine(m_BreathCoroutine);
            m_BreathCoroutine = null;
        }

        /// <summary>呼吸协程：让进度文字透明度按 breathCurve 循环起伏（lifeCycle = 一次完整呼吸的时长）。</summary>
        IEnumerator BreathEffectCoroutine()
        {
            // WHY: 组件存在即视为已配置 —— 换形态后 Legacy 被卸载、progressText 变成 null 是正常状态；只看字段会让呼吸在这里
            // 直接 yield break（表现为 TMP 形态下进度文字的呼吸效果整个消失）。
            if (breathCurve == null || (progressText == null && m_ProgressTextComp == null))
            {
                m_BreathCoroutine = null;
                yield break;
            }

            float cycle = lifeCycle > 0f ? lifeCycle : 1f;
            while (true)
            {
                // WHY: 必须归一化成 [0,1] 的周期进度再采样曲线（lifeCycle 走完 = 曲线走一遍 = 一次呼吸）, 否则采样点落在曲线定义域外, 呼吸不会循环
                float t = Mathf.Repeat(Time.time, cycle) / cycle;
                float value = Mathf.Clamp01(breathCurve.Evaluate(t));

                // WHY: 语义是"只动 alpha、保留配置色 RGB"，所以先读回当前色再写回 —— 读写的都必须是**当前认领的控件**：
                // 换形态后 progressText 成了"假 null"（Legacy 已被卸载），直写 progressText.color 会抛 MissingReferenceException；
                // 可见控件是 TMP，ColorValue 由 ApplyStyle 下发到它，呼吸才看得到（ColorValue 的 setter 会回写组件自身的 color
                // 字段，面板实例随 Canvas 重建、不跨实例泄漏，可接受）。
                if (m_ProgressTextComp != null)
                {
                    Color current = m_ProgressTextComp.ColorValue;
                    m_ProgressTextComp.ColorValue = new Color(current.r, current.g, current.b, value);
                }
                else if (progressText != null)   // 节点上没有 TextComponent（永远不会换形态），退回直写
                {
                    Color current = progressText.color;
                    progressText.color = new Color(current.r, current.g, current.b, value);
                }

                yield return null;
            }
        }

        protected override void OnDestroy()
        {
            StopBreath();
            base.OnDestroy();
        }
    }
}
