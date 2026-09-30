
namespace MCV_Module.UI.Components
{
    /// <summary>
    /// 视频组件：仅作"这里有视频播放宿主"的标记件，播放全部经 <c>UI/Tools/VideoTool</c>（Unity 原生 VideoPlayer）。
    /// 2026-09-30 移除 <c>VideoType</c> / <c>videoPath</c> / <c>legacyVideoPlayer</c> 三个插件期字段：无一处读取，
    /// 播放地址由数据（TaskPrincipleData.videoName 等）决定，不由组件序列化。
    /// </summary>
    public class VideoComponent : ComponentBase
    {
    }
}
