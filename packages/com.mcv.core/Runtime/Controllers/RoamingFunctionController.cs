// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-15）—— 请按需补充业务代码
using MCV_Module.Managers;
using MCV_Module.UI.Panels;
using MCV_Module.Utils;

namespace MCV_Module.Controllers
{
    /// <summary>漫游页功能控制器 —— 把 RoamingFunctionPanel 的「返回」入口变成动作：在当前（漫游）Canvas 下实例化 MenuPanel 作为菜单弹层（再点一次收起）；弹层里的两个去向都由 MenuController 处理。</summary>
    public class RoamingFunctionController : ControllerBase<RoamingFunctionPanel>
    {
        /// <summary>漫游期间唤出的菜单面板；随 Canvas 重建被销毁后自动变伪 null，下次点击会重新实例化</summary>
        MenuPanel m_RoamingMenu;

        public override void OnViewBound()
        {
            if (View == null) return;

            // 先退后订：面板每次重建都是新实例
            View.OnBackBtnClick -= OnBackClick;
            View.OnBackBtnClick += OnBackClick;

            View.SetCopyright(GlobalUIMgr.IfCopyright, GlobalUIMgr.IfCompany);
        }

        public override void OnDispose()
        {
            if (View != null)
            {
                View.OnBackBtnClick -= OnBackClick;
            }
            base.OnDispose();
        }

        // WHY: 不复用 MenuCanvas 上的菜单——两处同时存在会争抢 MenuController 的 View 绑定
        /// <summary>返回按钮：在漫游 Canvas 下唤出 / 收起 MenuPanel。</summary>
        void OnBackClick()
        {
            var canvas = GlobalUIMgr.GetActiveCanvas();
            if (canvas == null)
            {
                Log.Warning("[RoamingFunctionController] 当前没有激活 Canvas，菜单面板无法实例化");
                return;
            }

            // 首次点击才创建：GetPanel 会在该 Canvas 下实例化并按 [RequireController] 绑定 MenuController
            if (m_RoamingMenu == null)
            {
                m_RoamingMenu = canvas.GetPanel<MenuPanel>();
                if (m_RoamingMenu == null) return;

                m_RoamingMenu.SetUIActive(true);
                Log.Info("[RoamingFunctionController] 唤出菜单面板");
                return;
            }

            // 已存在则切换显隐：收起不销毁，下次唤出省一次实例化
            // WHY: 不能用 gameObject.activeSelf —— MenuPanel 走 Animator 显隐、GameObject 恒激活（恒为 true），
            //      只能读面板自报的 IsShowing，否则第二次点击永远走"收起"分支（表现为返回键点击无效）
            bool isShowing = m_RoamingMenu.IsShowing;
            m_RoamingMenu.SetUIActive(!isShowing);
            Log.Info($"[RoamingFunctionController] {(isShowing ? "收起" : "唤出")}菜单面板");
        }
    }
}
