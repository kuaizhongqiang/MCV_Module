using System;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.Video;

namespace MCV_Module.UI.Tools
{
    // WHY: module 包不引入任何第三方播放器插件；播放器一律由本接口抽象，未注入实现时用 Unity 原生 VideoPlayer。
    /// <summary>视频播放器抽象：宿主可注入自建实现，未注入时用 Unity 原生 VideoPlayer。</summary>
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

    // WHY: 宿主若要换播放器实现，自行实现 IVideoPlayer 并在宿主侧创建；框架侧不保留任何插件专用注册口。
    /// <summary>视频播放器工厂：为承载对象创建统一播放器（当前唯一实现 = Unity 原生 VideoPlayer）。</summary>
    public static class VideoPlayerFactory
    {
        // WHY: 必须 AddComponent<VideoPlayer>（host 上没有该组件是常态），漏掉就会返回一个不可用的播放器。
        /// <summary>为承载对象创建播放器（Unity 原生 VideoPlayer）。</summary>
        public static IVideoPlayer Create(GameObject host)
        {
            if (host == null) return null;
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
