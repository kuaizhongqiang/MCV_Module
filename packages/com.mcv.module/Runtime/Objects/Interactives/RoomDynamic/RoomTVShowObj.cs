using System.Collections;
using System.Collections.Generic;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.RoomDynamic
{
    // WHY: 峰值(t>=0.5)处必须把下一张也写进 _MainTex(A 槽)——A/B 同图后回落段才稳定停在新图而不跳回旧图
    /// <summary>房间电视/展示屏的图片自动轮播（PPT 式）：_MainTex=当前画面、_SecondTex=下一张，_Lerp/_BlurLerp/_Color 走 0→1→0 脉冲</summary>
    public class RoomTVShowObj : InteractiveBase
    {
        #region 序列化参数
        [Header("轮播")]
        [Tooltip("轮播画面集合，默认按列表顺序播放")]
        [SerializeField] List<Texture2D> showPics = new List<Texture2D>();
        [Tooltip("每张画面停留时间（秒）")]
        [SerializeField] float freezeTime = 10f;
        [Tooltip("停留时长的随机幅度：0 = 恒为 freezeTime，1 = 在 ±100% 范围内随机")]
        [SerializeField, Range(0, 1)] float randomPercent = 0.5f;
        // 动画时间会切成4块 前四分之执行LerpName 之后四分之二执行BlurLerpName和 ColorName 最后执行LerpName
        [Tooltip("单次切换动画总时长（秒），内部均分四块")]
        [SerializeField] float animDuration = 2.0f;

        [Header("材质")]
        [Tooltip("目标材质在 Renderer 上的槽位下标（电视模型通常 0 = 外框、1 = 屏幕）")]
        [SerializeField] int matIndex = 1;
        [Tooltip("切换过程中屏幕压暗到的最暗颜色")]
        [SerializeField, ColorUsage(true, true)] Color minColor = new Color(0.5f, 0.5f, 0.5f);
        [Tooltip("正常显示亮度，切换结束后恢复为该颜色（HDR，可超过 1）")]
        [SerializeField, ColorUsage(true, true)] Color maxColor = new Color(1, 1, 1);
        #endregion

        #region 材质属性名
        const string MainTexName = "_MainTex";
        const string SecondTexName = "_SecondTex";
        const string LerpName = "_Lerp";
        const string BlurLerpName = "_BlurLerp";
        const string ColorName = "_Color";
        #endregion

        int curIndex = 0;
        Texture2D curTex;
        Texture2D nextTex;

        Renderer targetRenderer;
        Material targetMaterial;
        Coroutine playCoroutine;
        /// <summary>鼠标是否悬停在物体上：悬停期间暂停"每张画面停留计时"（MoEnter 暂停 / MoExit 继续）</summary>
        bool isHovering;

        protected override void Awake()
        {
            base.Awake();   // 交互注册与事件绑定不能省
            CacheTarget();

            HighlightPluginInit(gameObject);
        }

        void OnEnable()
        {
            if (targetMaterial == null) return;

            if (showPics == null || showPics.Count < 2)
            {
                Log.Warning("RoomTVShowObj：showPics 少于 2 张，轮播不启动。", this);
                return;
            }

            // 每次启用都从"当前张"重新铺一遍，避免上次被打断停在半过渡状态
            ResetToCurrent();

            // 先按"未悬停"复位：避免在悬停状态下被禁用/启用后停留计时永远不推进
            isHovering = false;

            if (playCoroutine == null)
                playCoroutine = StartCoroutine(PlayRoutine());
        }

        void OnDisable()
        {
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }
        }

        #region 初始化
        void CacheTarget()
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                Log.Warning("RoomTVShowObj：物体上没有 Renderer，轮播不可用。", this);
                return;
            }

            // materials[] 会在运行时克隆材质实例，避免直接写脏磁盘上的共享材质资产
            Material[] mats = targetRenderer.materials;
            if (matIndex < 0 || matIndex >= mats.Length)
            {
                Log.WarningFormat("RoomTVShowObj：matIndex({0}) 超出材质槽数量({1})，轮播不可用。", matIndex, mats.Length);
                return;
            }

            targetMaterial = mats[matIndex];
        }

        /// <summary>把材质参数复位到"当前张"的静止状态。</summary>
        void ResetToCurrent()
        {
            if (curIndex < 0 || curIndex >= showPics.Count) curIndex = 0;

            curTex = showPics[curIndex];
            nextTex = showPics[(curIndex + 1) % showPics.Count];

            targetMaterial.SetTexture(MainTexName, curTex);
            targetMaterial.SetTexture(SecondTexName, nextTex);
            SetLerp(0f);
            SetBlur(0f);
            SetColor(maxColor);
        }
        #endregion

        #region 轮播
        IEnumerator PlayRoutine()
        {
            while (true)
            {
                // WHY: 停留用可暂停等待而非 WaitForSeconds——鼠标移入要暂停计时、移出从剩余时间继续
                yield return WaitFreeze(GetFreezeTime());

                // 顺序播下一张（PPT 式），脉冲式切换（参数整体走 0→1→0）
                yield return PlayOneSwitch((curIndex + 1) % showPics.Count);
            }
        }

        /// <summary>可暂停的停留等待：isHovering 期间计时不推进；只作用于停留阶段，切换动画不暂停</summary>
        IEnumerator WaitFreeze(float seconds)
        {
            float remain = Mathf.Max(seconds, 0f);

            while (remain > 0f)
            {
                if (!isHovering) remain -= Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator PlayOneSwitch(int nextIndex)
        {
            float duration = Mathf.Max(animDuration, 0f);

            // 起点：A 槽 = 当前张，B 槽 = 下一张，_Lerp = 0 时画面完全由 A 体现
            nextTex = showPics[nextIndex];
            targetMaterial.SetTexture(SecondTexName, nextTex);
            SetLerp(0f);
            SetBlur(0f);
            SetColor(maxColor);

            if (duration > 0f)
            {
                float time = 0f;
                bool peakSwapped = false;

                while (time < duration)
                {
                    time += Time.deltaTime;
                    float t = Mathf.Clamp01(time / duration);

                    // 三个参数同步走同一条 0→1→0 脉冲：中点 B 全显、最模糊、最暗
                    float pulse = Pulse(t);
                    SetLerp(pulse);
                    SetBlur(pulse);
                    SetColor(Color.Lerp(maxColor, minColor, pulse));

                    // 峰值处把下一张也写进 A 槽：此后 A/B 同图，回落段画面稳定停在新图而不跳回旧图
                    if (!peakSwapped && t >= 0.5f)
                    {
                        peakSwapped = true;
                        targetMaterial.SetTexture(MainTexName, nextTex);
                    }

                    yield return null;
                }
            }

            CommitSwitch(nextIndex);
        }

        /// <summary>过渡脉冲曲线：0 → 1 → 0。想换成线性三角波就改成 1 - Mathf.Abs(2 * t - 1)。</summary>
        static float Pulse(float t)
        {
            return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        }

        /// <summary>收尾：确认新图落在 A 槽、参数复位、推进游标。</summary>
        void CommitSwitch(int nextIndex)
        {
            targetMaterial.SetTexture(MainTexName, nextTex);
            SetLerp(0f);
            SetBlur(0f);
            SetColor(maxColor);

            curIndex = nextIndex;
            curTex = nextTex;
        }

        /// <summary>停留时长：randomPercent = 0 时恒为 freezeTime，= 1 时在 freezeTime 的 ±100% 范围内随机。</summary>
        float GetFreezeTime()
        {
            if (randomPercent <= 0f) return freezeTime;

            float percent = Mathf.Clamp01(randomPercent);
            return freezeTime * (1f + UnityEngine.Random.Range(-percent, percent));
        }
        #endregion

        void SetLerp(float value) => targetMaterial.SetFloat(LerpName, value);
        void SetBlur(float value) => targetMaterial.SetFloat(BlurLerpName, value);
        void SetColor(Color value) => targetMaterial.SetColor(ColorName, value);

        #region 事件重写
        protected override void MoEnterEvent()
        {
            Highlight(true);

            // 移入：暂停停留计时（正在切换动画时也置位，动画播完后的停留不再倒计时）
            isHovering = true;
        }

        protected override void MoExitEvent()
        {
            Highlight(false);

            // 移出：从剩余时间继续计时
            isHovering = false;
        }

        protected override void MoClickEvent()
        {
            // 暂时没有 空着
        }
        #endregion
    }
}
