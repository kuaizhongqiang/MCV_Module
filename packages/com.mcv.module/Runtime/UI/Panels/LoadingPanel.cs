using System.Collections;
using MCV_Module.Utils;
using MCV_Module.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>
    /// 加载遮挡面板：加载 AA 包 / 切换场景时用于遮挡屏幕。
    /// 由 LoadingController 订阅 SceneLoadingEvent / SceneLoadedEvent 控制显示、进度与隐藏。
    /// </summary>
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

        Coroutine m_BreathCoroutine;

        protected override void Awake()
        {
            base.Awake();
            if (bgImage == null || titleText == null || contentText == null || progressText == null || progressSlider == null)
            {
                Log.Error($"[LoadingPanel] 缺少必要组件", this);
                return;
            }
            StartBreath();
        }   

        public void Init(Texture2D bgTexture, string title, string content)
        {
            // bgTexture 判空：避免 Awake 中 Init(null,...) 时对 null 解引用
            if (bgTexture != null)
            {
                bgImage.sprite = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), Vector2.zero);
            }
            titleText.text = title;
            contentText.text = content;
            SetProgress(0f);
            StartBreath();
        }

        public void SetProgress(float progress)
        {
            progressText.text = string.Format("{0:0.00}%", progress * 100) + " 加载中...";
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

        /// <summary>
        /// 呼吸协程：让进度文字透明度按 breathCurve 循环起伏。
        /// lifeCycle 为一次完整呼吸的时长：在一个周期内将采样坐标从曲线起点归一化推进到终点
        /// （t = 周期内进度，恒在 [0,1]，与曲线定义域一致），从而真正实现循环呼吸。
        /// 每帧 yield 等待，仅面板激活时运行，销毁时随 OnDestroy 停止。
        /// </summary>
        IEnumerator BreathEffectCoroutine()
        {
            if (breathCurve == null || progressText == null)
            {
                m_BreathCoroutine = null;
                yield break;
            }

            float cycle = lifeCycle > 0f ? lifeCycle : 1f;
            while (true)
            {
                // 归一化到 [0,1]：lifeCycle 走完 = 曲线完整走一遍 = 一次呼吸
                float t = Mathf.Repeat(Time.time, cycle) / cycle;
                float value = Mathf.Clamp01(breathCurve.Evaluate(t));

                Color textColor = progressText.color;
                progressText.color = new Color(textColor.r, textColor.g, textColor.b, value);

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
