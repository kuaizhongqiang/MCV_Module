using System;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.Video;

namespace MCV_Module.UI.Tools
{
    // WHY: 框架只用引擎原生的 VideoPlayer，不引任何播放器插件，也不为插件留抽象
    /// <summary>Unity VideoPlayer 的静态封装（框架播放视频的唯一入口）。</summary>
    public static class VideoTool
    {
        public static void InitVideoPlayer(VideoPlayer player)
        {
            player.playOnAwake = false;
            player.isLooping = false;
            player.waitForFirstFrame = false;
            player.aspectRatio = VideoAspectRatio.Stretch;
        }

        // WHY: 失败(errorReceived)也必须回调 onComplete，否则调用方永远等不到结果；回调前先解绑两个事件，防止重复触发。
        /// <summary>预加载视频：Prepare 完成后触发 onComplete，失败也触发。</summary>
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

        /// <summary>获取当前播放时间（秒）。</summary>
        public static float GetTime(VideoPlayer player)
        {
            return (float)player.time;
        }

        /// <summary>获取视频总时长（秒），未准备好时可能返回 0。</summary>
        public static float GetDuration(VideoPlayer player)
        {
            return (float)player.length;
        }

        public static void SetVolume(VideoPlayer player, float volume)
        {
            // 仅 Direct 音频输出模式下生效
            player.SetDirectAudioVolume(0, Mathf.Clamp01(volume));
        }

        /// <summary>获取音量（Direct 输出模式下的第 0 路）。</summary>
        public static float GetVolume(VideoPlayer player)
        {
            return player.GetDirectAudioVolume(0);
        }

        /// <summary>是否正在播放。</summary>
        public static bool IsPlaying(VideoPlayer player)
        {
            return player.isPlaying;
        }

        // WHY: 承载对象上不一定有播放器组件，必须 AddComponent，否则调用方拿到的是 null 或死播放器
        /// <summary>为承载对象准备一个已按约定初始化的 Unity VideoPlayer（没有就挂一个）。</summary>
        public static VideoPlayer CreateVideoPlayer(GameObject host)
        {
            if (host == null) return null;

            var player = host.GetComponent<VideoPlayer>();
            if (player == null) player = host.AddComponent<VideoPlayer>();

            InitVideoPlayer(player);
            return player;
        }
    }
}
