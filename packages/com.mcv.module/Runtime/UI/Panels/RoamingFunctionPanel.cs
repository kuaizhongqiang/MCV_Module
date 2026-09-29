// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-15）—— 请按需补充业务代码
using System;
using System.Collections.Generic;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>漫游页功能面板：标题 / 版权条 + 「返回」一个入口（由 RoamingFunctionController 处理）。</summary>
    [RequireController(typeof(MCV_Module.Controllers.RoamingFunctionController))]
    public class RoamingFunctionPanel : PanelBase
    {
        [SerializeField] GameObject companyImage;
        [SerializeField] GameObject copyrightImage;
        [SerializeField] Button backBtn;
        [SerializeField] List<Image> breathImages;

        /// <summary>返回按钮点击（唤出菜单面板）</summary>
        public event Action OnBackBtnClick;

        protected override void Awake()
        {
            // WHY: 先走基类：canvasGroup 必须就绪，否则后续 SetUIActive 会空引用
            base.Awake();

            if (companyImage == null || copyrightImage == null || backBtn == null)
            {
                Log.Error("RoamingFunctionPanel 需要手动挂载组件");
                return;
            }

            // WHY: 面板每次重建都是全新实例，监听随实例销毁，无需退订
            backBtn.onClick.AddListener(() => OnBackBtnClick?.Invoke());
        }

        void OnEnable()
        {            
            StartCoroutine(BreathLightenAnim(breathImages,2f)); 
        }

        public void SetCopyright(bool ifCopyright,bool ifCompany)
        {
            companyImage.SetActive(ifCompany);
            copyrightImage.SetActive(ifCopyright);
        }
    }
}
