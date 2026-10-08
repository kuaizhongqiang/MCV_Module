// 由 MCV Editor/创建/UI Panel 生成器生成（2026-10-08）—— 请按需补充业务代码
using System;
using System.Collections.Generic;
using MCV_Module.Models.Project;
using MCV_Module.UI;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>MenuPanel 面板</summary>
    [RequireController(typeof(MCV_Module.Controllers.MenuController))]
    public class MenuPanel : PanelBase
    {
        [SerializeField] GameObject companyImage;
        [SerializeField] GameObject copyrightImage;
        [SerializeField] Button backBtn;
        [SerializeField] Transform menuRoot;
        List<Button> menuButtons = new List<Button>();
        ProjectClip currentClip;

        public event Action<ProjectClip> OnMenuClick;
        public event Action OnBackClick;

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            if (companyImage == null || copyrightImage == null || backBtn == null || menuRoot == null)
            {
                Log.Error("MenuPanel 需要手动挂载组件");
                return;
            }

            for (int i = 0; i < menuRoot.childCount; i++)
            {
                Button btn = menuRoot.GetChild(i).GetComponent<Button>();
                if (btn != null)
                {
                    menuButtons.Add(btn);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion
    }
}
