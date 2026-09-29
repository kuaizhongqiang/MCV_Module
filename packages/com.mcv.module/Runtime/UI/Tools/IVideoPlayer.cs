using System;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.Video;

namespace MCV_Module.UI.Tools
{
    // WHY: module 包不能直接依赖 AVProVideo，AVPro 只能由宿主实现本接口后注册，把插件类型带进来就破坏了第三方零依赖。
    /// <summary>视频播放器抽象：宿主注入 AVPro 实现，未注册时回退 Unity 原生 VideoPlayer。</summary>
    public interface IVideoPlayer
    {
        /// <summary>预加载视频。加载完成后触发 onComplete（失败也会触发）。</summary>
        void Preload(string path, Action onComplete = null);

        /// <summary>开始播放。</summary>
        void Play();

        /// <summary>停止播放。</summary>
        void Stop();

        /// <summary>暂停播放（可用 Resume 续播）。</summary>
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

    /// <summary>宿主 AVPro 实现创建器：返回 null 表示该 host 不支持 AVPro，工厂据此回退。</summary>
    public delegate IVideoPlayer AvProVideoPlayerCreator(GameObject host);

    // WHY: s_AvProCreator 是全局静态注册（后注册覆盖先注册、不会叠加），只有未注册或返回 null 时才走 Unity 回退。
    /// <summary>视频播放器工厂：宿主注册的 AVPro 实现优先，未注册时回退 Unity 原生 VideoPlayer。</summary>
    public static class VideoPlayerFactory
    {
        static AvProVideoPlayerCreator s_AvProCreator;

        /// <summary>宿主初始化时注册 AVPro 实现创建器（幂等，后注册覆盖先注册）。</summary>
        public static void RegisterAvPro(AvProVideoPlayerCreator creator) => s_AvProCreator = creator;

        // WHY: 回退分支必须 AddComponent<VideoPlayer>（host 上没有该组件是常态），漏掉就会返回一个不可用的播放器。
        /// <summary>为承载对象创建播放器：宿主 AVPro 优先，未注册或缺 MediaPlayer 时回退 Unity VideoPlayer。</summary>
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
                Log.Error($"[UnityVideoPlayer] 视频预加载失败: {path}, error={msg}");
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
