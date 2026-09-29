
using MCV_Module.Models;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    /// <summary>视频组件：只承载 Unity 原生播放宿主（Legacy RawImage）；第三方播放器一律不进口包。</summary>
    public class VideoComponent : ComponentBase
    {
        [SerializeField] VideoType videoType = VideoType.Legacy;
        [SerializeField] string videoPath = "";
        [SerializeField] RawImage legacyVideoPlayer = null;
    }
}
