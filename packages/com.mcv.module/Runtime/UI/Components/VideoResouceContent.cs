
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    public class VideoResouceContent : ComponentBase
    {
        [SerializeField] Transform listParent;
        [SerializeField] Text videoTitle;
        [SerializeField] Slider progressBar;
        [SerializeField] Button playPauseBtn;
        [SerializeField] Button stopBtn;
        [SerializeField] Button nextBtn;
        [SerializeField] Button prevBtn;
        [SerializeField] Text durationText;       // 格式为 00:00:00 / 00:00:00
    }
}

