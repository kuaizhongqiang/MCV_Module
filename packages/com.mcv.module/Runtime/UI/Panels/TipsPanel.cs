
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class TipsPanel : PanelBase
    {
        [SerializeField] Text tipsText;

        protected override void Awake()
        {
            base.Awake();
            if (tipsText == null)
            {
                Debug.LogError("需要手动挂载组件");
                return;
            }
            tipsText.text = "";
        }

        public void SetText(string text)
        {
            if (tipsText == null || tipsText.text == text) return;
            tipsText.text = text;
            var rect = tipsText.transform.parent.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }
}
