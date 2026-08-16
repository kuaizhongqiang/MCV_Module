using System;
using UnityEngine;
using UnityEngine.Video;

namespace MCV_Module.UI.Tools
{
    /// <summary>
    /// 视频播放器抽象（module 包不直接依赖 AVProVideo）。
    /// 宿主可注入 AVPro 实现（AVProVideoPlayerAdapter）；未注册时回退 Unity 原生 VideoPlayer 实现。
    /// 覆盖原 VideoTool 的播放/暂停/停止/预加载/时间/音量/事件调用面。
    /// </summary>
    public interface IVideoPlayer
    {
        /// <summary>预加载视频。加载完成后触发 onComplete（失败也会触发）。</summary>
        void Preload(string path, Action onComplete = null);

        void Play();

        void Stop();

        void Pause();

        /// <summary>从暂停恢复播放（未在播放时直接 Play）。</summary>
        void Resume();

        /// <summary>设置当前播放时间（秒）。</summary>
        void SetTime(float time);

        /// <summary>获取当前播放时间（秒）。</summary>
        float GetTime();

        /// <summary>获取视频总时长（秒），未准备好时可能返回 0。</summary>
        float GetDuration();

        /// <summary>设置音量。</summary>
        void SetVolume(float volume);

        /// <summary>获取音量。</summary>
        float GetVolume();

        /// <summary>是否正在播放。</summary>
        bool IsPlaying();
    }

    /// <summary>
    /// 宿主 AVPro 实现创建器：给定承载播放组件的 GameObject，返回 AVPro 实现；不支持（无 MediaPlayer）时返回 null。
    /// </summary>
    public delegate IVideoPlayer AvProVideoPlayerCreator(GameObject host);

    /// <summary>
    /// 视频播放器工厂：优先使用宿主注册的 AVPro 实现，未注册时回退 Unity 原生 VideoPlayer。
    /// </summary>
    public static class VideoPlayerFactory
    {
        static AvProVideoPlayerCreator s_AvProCreator;

        /// <summary>宿主初始化时注册 AVPro 实现创建器（幂等，后注册覆盖先注册）。</summary>
        public static void RegisterAvPro(AvProVideoPlayerCreator creator) => s_AvProCreator = creator;

        /// <summary>
        /// 为承载对象创建视频播放器实现。
        /// 优先宿主 AVPro 实现；未注册或宿主缺少 MediaPlayer 时回退 Unity 原生 VideoPlayer（缺失则自动添加组件）。
        /// </summary>
        public static IVideoPlayer Create(GameObject host)
        {
            if (host == null) return null;
            var avPro = s_AvProCreator?.Invoke(host);
            if (avPro != null) return avPro;
            var player = host.GetComponent<VideoPlayer>();
            if (player == null) player = host.AddComponent<VideoPlayer>();
            return new UnityVideoPlayer(player);
        }
    }

    /// <summary>Unity 原生 VideoPlayer 实现（随包，引擎自带）。</summary>
    public class UnityVideoPlayer : IVideoPlayer
    {
        readonly VideoPlayer m_Player;

        public UnityVideoPlayer(VideoPlayer player)
        {
            m_Player = player;
            if (player != null)
            {
                player.playOnAwake = false;
                player.isLooping = false;
                player.waitForFirstFrame = false;
                player.aspectRatio = VideoAspectRatio.Stretch;
            }
        }

        public void Preload(string path, Action onComplete = null)
        {
            if (m_Player == null) return;
            m_Player.url = path;
            m_Player.Prepare();

            if (onComplete == null) return;
            // 已经准备完毕则直接回调
            if (m_Player.isPrepared)
            {
                onComplete();
                return;
            }

            VideoPlayer.EventHandler prepareHandler = null;
            VideoPlayer.ErrorEventHandler errorHandler = null;
            prepareHandler = (vp) =>
            {
                vp.prepareCompleted -= prepareHandler;
                vp.errorReceived -= errorHandler;
                onComplete();
            };
            errorHandler = (vp, msg) =>
            {
                Debug.LogError($"[UnityVideoPlayer] 视频预加载失败: {path}, error={msg}");
                vp.prepareCompleted -= prepareHandler;
                vp.errorReceived -= errorHandler;
                onComplete();
            };
            m_Player.prepareCompleted += prepareHandler;
            m_Player.errorReceived += errorHandler;
        }

        public void Play() { if (m_Player != null) m_Player.Play(); }

        public void Stop() { if (m_Player != null) m_Player.Stop(); }

        public void Pause() { if (m_Player != null) m_Player.Pause(); }

        public void Resume() { if (m_Player != null && !m_Player.isPlaying) m_Player.Play(); }

        public void SetTime(float time) { if (m_Player != null) m_Player.time = time; }

        public float GetTime() => m_Player != null ? (float)m_Player.time : 0f;

        public float GetDuration() => m_Player != null ? (float)m_Player.length : 0f;

        public void SetVolume(float volume) { if (m_Player != null) m_Player.SetDirectAudioVolume(0, Mathf.Clamp01(volume)); }

        public float GetVolume() => m_Player != null ? m_Player.GetDirectAudioVolume(0) : 0f;

        public bool IsPlaying() => m_Player != null && m_Player.isPlaying;
    }
}
