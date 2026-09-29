
using MCV_Module.Models;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    /// <summary>视频组件：Legacy（RawImage）与 AVProVideo（弱引用宿主组件）两种播放宿主。</summary>
    public class VideoComponent : ComponentBase
    {
        [SerializeField] VideoType videoType = VideoType.Legacy;
        [SerializeField] string videoPath = "";
        [SerializeField] RawImage legacyVideoPlayer = null;
        // WHY: AVProVideo 已解耦 —— DisplayUGUI 留宿主，包内只用 Component 弱引用，避免 module 依赖第三方插件
        [Tooltip("AVProVideo 的 DisplayUGUI 组件（AVPro 适配器留宿主 Assets，仅当场景使用 AVPro 播放器时赋值）")]
        [SerializeField] Component avProVideoPlayer = null;
    }
}
