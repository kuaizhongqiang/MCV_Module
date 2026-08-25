using System;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.Video;

namespace MCV_Module.UI.Tools
{
    /// <summary>
    /// 视频播放工具：对 Unity 自带 VideoPlayer 提供统一封装。
    /// AVProVideo 已解耦（见 IVideoPlayer/VideoPlayerFactory）：宿主注册 AVPro 实现，未注册时回退 Unity VideoPlayer。
    /// </summary>
    public static class VideoTool
    {
        public static void InitVideoPlayer(VideoPlayer player)
        {
            player.playOnAwake = false;
            player.isLooping = false;
            player.waitForFirstFrame = false;
            player.aspectRatio = VideoAspectRatio.Stretch;
        }

        /// <summary>
        /// 预加载视频。加载完成后触发 onComplete（失败也会触发）。
        /// </summary>
        public static void VideoPlayerPreload(VideoPlayer player, string path, Action onComplete = null)
        {
            player.url = path;
            player.Prepare();

            if (onComplete == null) return;
            // 已经准备完毕则直接回调
            if (player.isPrepared)
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
                Log.Error($"[VideoTool] 视频预加载失败: {path}, error={msg}");
                vp.prepareCompleted -= prepareHandler;
                vp.errorReceived -= errorHandler;
                onComplete();
            };
            player.prepareCompleted += prepareHandler;
            player.errorReceived += errorHandler;
        }

        public static void Play(VideoPlayer player)
        {
            // 未准备时调用 Play 会自动触发 Prepare
            player.Play();
        }

        public static void Stop(VideoPlayer player)
        {
            player.Stop();
        }

        public static void Pause(VideoPlayer player)
        {
            player.Pause();
        }

        public static void Resume(VideoPlayer player)
        {
            if (!player.isPlaying) player.Play();
        }

        public static void SetTime(VideoPlayer player, float time)
        {
            player.time = time;
        }

        /// <summary>
        /// 获取当前播放时间（秒）。
        /// </summary>
        public static float GetTime(VideoPlayer player)
        {
            return (float)player.time;
        }

        /// <summary>
        /// 获取视频总时长（秒），未准备好时可能返回 0。
        /// </summary>
        public static float GetDuration(VideoPlayer player)
        {
            return (float)player.length;
        }

        public static void SetVolume(VideoPlayer player, float volume)
        {
            // 仅 Direct 音频输出模式下生效
            player.SetDirectAudioVolume(0, Mathf.Clamp01(volume));
        }

        /// <summary>
        /// 获取音量。
        /// </summary>
        public static float GetVolume(VideoPlayer player)
        {
            return player.GetDirectAudioVolume(0);
        }

        /// <summary>
        /// 是否正在播放。
        /// </summary>
        public static bool IsPlaying(VideoPlayer player)
        {
            return player.isPlaying;
        }

        /// <summary>
        /// 通过工厂创建统一视频播放器（宿主 AVPro 实现优先，未注册回退 Unity VideoPlayer）。
        /// </summary>
        public static IVideoPlayer CreatePlayer(GameObject host)
        {
            return VideoPlayerFactory.Create(host);
        }
    }
}
