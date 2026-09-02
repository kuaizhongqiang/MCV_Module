
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>SummitPanel 面板</summary>
    [RequireController(typeof(MCV_Module.Controllers.SummitController))]
    public class SummitPanel : PanelBase
    {
        [SerializeField] Transform summitListRoot;
        [SerializeField] Transform summitStatusRoot;
        [SerializeField] Button summitBtn;
        [SerializeField] Color normalColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] Color selectedColor = new Color(0.8f, 0.8f, 0.8f);
        GameObject listClipObj;
        GameObject statusClipObj;

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            if (summitListRoot == null || summitStatusRoot == null || summitBtn == null)
            {
                Debug.LogError($"[SummitPanel] 缺少必要组件", this);
                return;
            }            

            listClipObj = summitListRoot.GetChild(0).gameObject;
            statusClipObj = summitStatusRoot.GetChild(0).gameObject;
        }
        #endregion
    }
}
