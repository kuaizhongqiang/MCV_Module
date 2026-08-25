
using UnityEngine;
using MCV_Module.Utils;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class TipsPanel : PanelBase
    {
        [SerializeField] Text tipsText;
        public string GetText()
        {
            return tipsText.text;
        }

        protected override void Awake()
        {
            base.Awake();
            if (tipsText == null)
            {
                Log.Error("需要手动挂载组件");
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
