
using MCV_Module.Models;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    public class VideoComponent : ComponentBase
    {
        [SerializeField] VideoType videoType = VideoType.Legacy;
        [SerializeField] string videoPath = "";
        [SerializeField] RawImage legacyVideoPlayer = null;
        // AVProVideo 已解耦：DisplayUGUI（AVPro 插件类型）留宿主，包内以 Component 弱引用，避免 module 依赖第三方插件
        [Tooltip("AVProVideo 的 DisplayUGUI 组件（AVPro 适配器留宿主 Assets，仅当场景使用 AVPro 播放器时赋值）")]
        [SerializeField] Component avProVideoPlayer = null;
    }
}
