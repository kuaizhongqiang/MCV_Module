using System;
using System.Collections;
using MCV_Module.Controllers;
using MCV_Module.Event;
using MCV_Module.UI.Tools;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>实验原理面板（View）—— 只负责视频播放与交互，播哪段视频由 TaskPrincipleController 决定，面板自身不读数据。</summary>
    [RequireController(typeof(TaskPrincipleController))]
    public class TaskPrinciplePanel : TaskPanelBase
    {
        #region 序列化字段
        [Header("播放控制")]
        [SerializeField] Slider processingSlider;
        [SerializeField] Button playBtn;

        [Header("视频目录")]
        [Tooltip("StreamingAssets 下的视频子目录，全路径 = StreamingAssets/{videoDir}/{videoName}.mp4")]
        [SerializeField] string videoDir = "Video";
        #endregion
        const float ScaleMin = 0.5f;
        /// <summary>鼠标静止后执行缩放收起的等待时长（秒）</summary>
        const float ControlIdleHideDelay = 3f;

        IVideoPlayer player;
        Coroutine progressCoroutine;    // 播放期间的进度同步协程（面板不写 Update 生命周期）
        Coroutine ScaleChangeCoroutine; // 缩放动画（由 OnMouseMoveStateChanged 驱动）
        Coroutine controlHideCoroutine; // 鼠标静止后的等待计时（到点才执行 OnMouseMoveStateChanged）
        bool isPlaying;
        bool m_Seeking;
        bool m_Ready;
        string currentVideoName = "";

        /// <summary>播放/暂停状态变化（true = 播放中）；播放结束自动暂停也会触发。</summary>
        public event Action<bool> OnPlayStateChanged;

        /// <summary>当前视频名（未装载为空串）。</summary>
        public string CurrentVideoName => currentVideoName;
        /// <summary>视频是否正在播放。</summary>
        public bool IsPlaying => isPlaying;

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            if (playBtn == null)
            {
                Log.Error("[TaskPrinciplePanel] 缺少必要组件（playBtn）", this);
                return;
            }

            // 画面输出（RawImage / RenderTexture / VideoPlayer 的 renderMode）由编辑器侧配置
            player = VideoTool.CreatePlayer(gameObject);
            SetupVideoInput();

            // 鼠标静止 / 恢复移动：GlobalInputMgr 统一判定后发布（面板随 Canvas 重建，每次绑定重订）
            EventBus<MouseMoveStateEventData>.Subscribe(OnMouseMoveStateEvent);

            SetButtonICO(false);
            m_Ready = true;
        }

        protected override void OnDestroy()
        {
            if (playBtn != null) playBtn.onClick.RemoveListener(ToggleVideo);

            EventBus<MouseMoveStateEventData>.Unsubscribe(OnMouseMoveStateEvent);
            StopControlHide();
            StopProgressSync();

            if (player != null)
            {
                player.Stop();
                player = null;
            }

            base.OnDestroy();
        }

        /// <summary>视频交互：播放按钮 + 进度条拖动探针（进度条缺省时只保留按钮）。</summary>
        void SetupVideoInput()
        {
            playBtn.onClick.AddListener(ToggleVideo);

            if (processingSlider == null) return;

            SliderDragProbe probe = processingSlider.GetComponent<SliderDragProbe>();
            if (probe == null) probe = processingSlider.gameObject.AddComponent<SliderDragProbe>();
            probe.OnDragStateChanged -= OnSliderDragStateChanged;
            probe.OnDragStateChanged += OnSliderDragStateChanged;
        }
        #endregion

        #region 对外播放接口（由 Controller 调用）
        // WHY: videoName 不带扩展名（如 "contactor_principle_01"）；预加载完成后才按 autoPlay 决定是否播放，文件缺失时打 Error 并跳过。
        /// <summary>装载并播放指定视频。</summary>
        public void LoadVideo(string videoName, bool autoPlay = true)
        {
            if (!m_Ready || player == null) return;

            // 切换视频先归零，避免上一段继续播放 / 进度残留
            StopVideo();
            currentVideoName = videoName ?? "";

            if (string.IsNullOrEmpty(videoName))
            {
                Log.Warning("[TaskPrinciplePanel] 未指定视频名，跳过装载");
                return;
            }

            string path = BuildVideoPath(videoName);
#if !UNITY_WEBGL
            if (!System.IO.File.Exists(path))
            {
                Log.Error($"[TaskPrinciplePanel] 视频文件不存在：{path}（检查 ProjectData.json 的 videoName 与 StreamingAssets/{videoDir} 下的文件名是否一致）");
                return;
            }
#endif
            player.Preload(path, () =>
            {
                // 预加载完成（失败也会回调）：按实际时长刷新进度条范围
                float duration = player.GetDuration();
                if (processingSlider != null)
                {
                    processingSlider.minValue = 0f;
                    processingSlider.maxValue = duration > 0f ? duration : 1f;
                    processingSlider.SetValueWithoutNotify(0f);
                }

                if (autoPlay) PlayVideo();
                else SetButtonICO(false);
            });
        }

        /// <summary>播放。</summary>
        public void PlayVideo()
        {
            if (!m_Ready || player == null) return;

            player.Play();
            isPlaying = true;
            SetButtonICO(true);
            StartProgressSync();
            OnPlayStateChanged?.Invoke(true);
        }

        /// <summary>暂停。</summary>
        public void PauseVideo()
        {
            if (!m_Ready || player == null) return;

            PauseInternal();
            StopProgressSync();
        }

        /// <summary>播放/暂停切换（播放按钮回调）。</summary>
        public void ToggleVideo()
        {
            if (isPlaying) PauseVideo();
            else PlayVideo();
        }

        /// <summary>停止并归零进度（切换视频时调用）。</summary>
        public void StopVideo()
        {
            if (!m_Ready || player == null) return;

            StopProgressSync();
            player.Stop();
            isPlaying = false;
            SetButtonICO(false);
            if (processingSlider != null) processingSlider.SetValueWithoutNotify(0f);
            OnPlayStateChanged?.Invoke(false);
        }

        /// <summary>暂停本体（供进度协程调用，不打断自身）。</summary>
        void PauseInternal()
        {
            player.Pause();
            isPlaying = false;
            SetButtonICO(false);
            OnPlayStateChanged?.Invoke(false);
        }
        #endregion

        #region 内部实现
        /// <summary>播放按钮图标切换：子物体结构约定 [0][1] = 播放图标，[0][2] = 暂停图标。</summary>
        void SetButtonICO(bool isPlay)
        {
            var playICO = playBtn.transform.GetChild(0).GetChild(1).gameObject;
            var pauseICO = playBtn.transform.GetChild(0).GetChild(2).gameObject;
            playICO.SetActive(!isPlay);
            pauseICO.SetActive(isPlay);
        }

        string BuildVideoPath(string videoName)
        {
            string file = videoName;
            if (!file.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) file += ".mp4";
            return Application.streamingAssetsPath + "/" + videoDir + "/" + file;
        }

        void OnSliderDragStateChanged(bool dragging)
        {
            m_Seeking = dragging;
            if (dragging) return;

            // WHY: 松手才 seek，避免拖动过程中与每帧回写互相打架
            if (player != null && processingSlider != null) player.SetTime(processingSlider.value);
        }

        /// <summary>启动进度同步协程（已启动则忽略，避免重复）。</summary>
        void StartProgressSync()
        {
            if (progressCoroutine != null || processingSlider == null) return;
            progressCoroutine = StartCoroutine(ProgressSyncRoutine());
        }

        void StopProgressSync()
        {
            if (progressCoroutine == null) return;
            StopCoroutine(progressCoroutine);
            progressCoroutine = null;
        }

        // WHY: 恢复移动立即还原；静止要等 ControlIdleHideDelay 秒才收起，计时期间鼠标再动则取消这次收起。
        /// <summary>鼠标移动状态事件（GlobalInputMgr 发布）：移动还原、静止延时收起控件。</summary>
        void OnMouseMoveStateEvent(MouseMoveStateEventData e)
        {
            if (e == null || !m_Ready) return;

            if (!e.IsIdle)
            {
                StopControlHide();
                OnMouseMoveStateChanged(false);
                return;
            }

            if (controlHideCoroutine == null) controlHideCoroutine = StartCoroutine(IdleHideRoutine());
        }

        /// <summary>静止计时：到点执行一次缩放收起。</summary>
        IEnumerator IdleHideRoutine()
        {
            yield return new WaitForSeconds(ControlIdleHideDelay);

            controlHideCoroutine = null;
            OnMouseMoveStateChanged(true);
        }

        void StopControlHide()
        {
            if (controlHideCoroutine == null) return;
            StopCoroutine(controlHideCoroutine);
            controlHideCoroutine = null;
        }

        void OnMouseMoveStateChanged(bool moving)
        {
            if (ScaleChangeCoroutine != null)
            {
                StopCoroutine(ScaleChangeCoroutine);
            }
            ScaleChangeCoroutine = StartCoroutine(ScaleChange(moving));
        }

        // WHY: 拖动期间必须跳过回写（否则与 seek 互相打架），播到末尾自动暂停（不循环）。
        /// <summary>进度同步协程：播放中逐帧回写进度，停止或面板销毁时结束。</summary>
        IEnumerator ProgressSyncRoutine()
        {
            while (isPlaying && player != null && processingSlider != null)
            {
                if (!m_Seeking)
                {
                    float duration = player.GetDuration();
                    if (duration > 0f)
                    {
                        float time = player.GetTime();
                        processingSlider.SetValueWithoutNotify(Mathf.Clamp(time, 0f, duration));

                        // 播到末尾：自动暂停，协程随之结束
                        if (time >= duration - 0.05f)
                        {
                            PauseInternal();
                            break;
                        }
                    }
                }

                yield return null;
            }

            progressCoroutine = null;
        }
        
        IEnumerator ScaleChange(bool Min)
        {            
            var scale = transform.GetChild(1).GetComponent<RectTransform>();
            float currentScale = scale.localScale.x;
            // WHY: 只做 XY 等比缩放，Z 不参与——旧写法 new Vector3(x, 1f) 会把 Z 写成 0（prefab 里 VideoScale 是 0.5/0.5/1），这里用当前 Z 兜底
            float scaleZ = Mathf.Approximately(scale.localScale.z, 0f) ? 1f : scale.localScale.z;
            float targetScale = !Min ? ScaleMin : 1.0f;
            var control = processingSlider.transform.parent.GetComponent<RectTransform>();
            float currentPosY = control.anchoredPosition.y;
            float targetPosY = Min ? -control.rect.size.y: 0;
            float duration = 0.3f;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float s = Mathf.Lerp(currentScale, targetScale, t);
                scale.localScale = new Vector3(s, s, scaleZ);
                control.anchoredPosition = new Vector2(control.anchoredPosition.x, Mathf.Lerp(currentPosY, targetPosY, t));
                yield return null;
            }

            scale.localScale = new Vector3(targetScale, targetScale, scaleZ);
            control.anchoredPosition = new Vector2(control.anchoredPosition.x, targetPosY);

            ScaleChangeCoroutine = null;
        }
        
        
        
        #endregion

        #region 内容快照（AI 上下文用）
        public override string GetPanelContent()
        {
            string result = "【实验原理页面】\n";
            result += string.IsNullOrEmpty(currentVideoName)
                ? "当前未装载原理视频。\n"
                : $"当前原理视频：{currentVideoName}\n";
            result += $"视频状态：{(isPlaying ? "播放中" : "已暂停/未播放")}\n";
            return result;
        }
        #endregion
    }

    // WHY: 拖动期间不能回写进度，否则与每帧写值互相打架；松手才 seek
    /// <summary>进度条拖动探针（本面板专用）：按下/松开时上报拖动状态。</summary>
    [RequireComponent(typeof(Slider))]
    internal class SliderDragProbe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action<bool> OnDragStateChanged;

        public void OnPointerDown(PointerEventData eventData) => OnDragStateChanged?.Invoke(true);
        public void OnPointerUp(PointerEventData eventData) => OnDragStateChanged?.Invoke(false);
    }
}
