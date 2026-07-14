using System;
using System.Collections.Generic;
using MCV_Module.GlobalManager.CLI;
using UnityEngine;
using UnityEngine.UIElements;

namespace MCV_Module.UI.AgentCanvas
{
    /// <summary>
    /// 页面渲染器 — AgentCanvas 核心 UI Toolkit 渲染引擎。
    ///
    /// 职责：将 PageConfig (JSON) → VisualElement 树，挂在 UIDocument 上。
    ///
    /// 外挂架构：UI Toolkit 与现有 UGUI+MVC 系统平行运行，共享 GlobalDataMgr 数据层。
    /// </summary>
    public class PageRenderer
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        private readonly PageManager _pageManager;
        private readonly InteractionCallback _callback;
        private readonly Dictionary<string, VisualElement> _elementCache = new Dictionary<string, VisualElement>();
        private string _currentPageId;
        private VisualElement _contentArea;

        public UIDocument Document => _uiDoc;
        public string CurrentPageId => _currentPageId;

        /// <summary>页面渲染完成事件（用于 WS 推送 page.rendered）。</summary>
        public event Action<string> OnPageRendered;

        /// <summary>交互事件（choice/fill 提交时触发，用于 WS 推送 interaction）。</summary>
        public event Action<string, string, string, string, object> OnInteraction;

        public PageRenderer(PageManager pageManager, InteractionCallback callback)
        {
            _pageManager = pageManager;
            _callback = callback;
            Initialize();
        }

        /// <summary>初始化 UIDocument。</summary>
        private void Initialize()
        {
            // 查找或创建 UIDocument
            _uiDoc = UnityEngine.Object.FindObjectOfType<UIDocument>();
            if (_uiDoc == null)
            {
                var go = new GameObject("AgentCanvasUIDocument");
                _uiDoc = go.AddComponent<UIDocument>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            // 设置 PanelSettings（使用默认样式）
            if (_uiDoc.panelSettings == null)
            {
                // 使用 Unity 内置默认 PanelSettings
                var defaultSettings = Resources.Load<PanelSettings>("UI/AgentCanvas/DefaultPanelSettings");
                if (defaultSettings != null)
                    _uiDoc.panelSettings = defaultSettings;
            }

            // 加载 USS
            LoadStyleSheet();

            // 获取根 VisualElement
            // 注意: rootVisualElement 是只读属性，不能赋值
            var docRoot = _uiDoc.rootVisualElement;
            docRoot.Clear();
            docRoot.style.flexGrow = 1;
            docRoot.style.width = Length.Percent(100);
            docRoot.style.height = Length.Percent(100);

            _root = new VisualElement { name = "agent-canvas-root" };
            _root.style.flexGrow = 1;
            _root.style.width = Length.Percent(100);
            _root.style.height = Length.Percent(100);
            docRoot.Add(_root);

            Debug.Log("[PageRenderer] UIDocument 初始化完成");
        }

        /// <summary>加载 USS 样式表。</summary>
        private void LoadStyleSheet()
        {
            try
            {
                var styles = Resources.Load<StyleSheet>("UI/AgentCanvas/AgentCanvasStyles");
                if (styles != null && _root != null)
                {
                    if (!_root.styleSheets.Contains(styles))
                        _root.styleSheets.Add(styles);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PageRenderer] USS 加载失败: {e.Message}（使用默认样式）");
            }
        }

        /// <summary>渲染页面配置（run 命令入口）。</summary>
        public bool RenderPage(string pageId)
        {
            var page = _pageManager.GetPage(pageId);
            if (page == null)
            {
                Debug.LogError($"[PageRenderer] 页面不存在: {pageId}");
                return false;
            }

            _currentPageId = pageId;
            _elementCache.Clear();
            _root.Clear();

            var config = page.Config;

            // 应用布局引擎
            _contentArea = LayoutEngine.CreateLayout(config.layout, _root);
            if (_contentArea == null)
            {
                Debug.LogError($"[PageRenderer] 无法创建布局: {config.layout}");
                page.Status = PageStatus.Error;
                return false;
            }

            _contentArea.name = $"layout-{config.layout}";

            // 渲染每个元件
            int elemIndex = 0;
            foreach (var elem in config.elements)
            {
                if (elem == null) continue;
                try
                {
                    var ve = ElementRenderer.Render(elem, this);
                    if (ve != null)
                    {
                        // waterfall 交替底色（USS 不支持 :nth-child）
                        if (config.layout == "waterfall")
                        {
                            ve.AddToClassList(elemIndex % 2 == 0 ? "agent-stripe-even" : "agent-stripe-odd");
                        }

                        // 处理 region（仅 three_column 布局）
                        if (!string.IsNullOrEmpty(elem.region) && config.layout == "three_column")
                        {
                            var region = _contentArea.Q<VisualElement>(elem.region);
                            if (region != null) region.Add(ve);
                            else _contentArea.Add(ve);
                        }
                        else if (config.layout == "free_stack")
                        {
                            // free_stack: 使用 x, y 定位
                            ve.style.position = Position.Absolute;
                            ve.style.left = elem.x;
                            ve.style.top = elem.y;
                            _contentArea.Add(ve);
                        }
                        else
                        {
                            _contentArea.Add(ve);
                        }

                        _elementCache[elem.id] = ve;
                        elemIndex++;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PageRenderer] 元件渲染失败: {elem.type} ({elem.id}): {e.Message}");
                }
            }

            page.Status = PageStatus.Rendered;
            page.LastRenderedAt = DateTime.Now;

            Debug.Log($"[PageRenderer] 页面渲染完成: {pageId} ({config.elements.Count} 元件, layout={config.layout})");
            OnPageRendered?.Invoke(pageId);
            return true;
        }

        /// <summary>清空页面内容。</summary>
        public void ClearPage(string scope = null)
        {
            if (_root == null) return;

            if (string.IsNullOrEmpty(scope))
            {
                _root.Clear();
                _elementCache.Clear();
                _currentPageId = null;
                Debug.Log("[PageRenderer] 页面已清空");
            }
            else
            {
                // scope 清空特定区域
                var region = _root.Q<VisualElement>(scope);
                if (region != null)
                {
                    region.Clear();
                    Debug.Log($"[PageRenderer] 清空区域: {scope}");
                }
            }
        }

        /// <summary>更新页面特定元件的展现（如 result.show 显示答题反馈）。</summary>
        public void UpdateElementVisual(string elementId, Action<VisualElement> update)
        {
            if (_elementCache.TryGetValue(elementId, out var ve))
            {
                update?.Invoke(ve);
                ve.MarkDirtyRepaint();
            }
        }

        /// <summary>获取已缓存的元件 VisualElement。</summary>
        public VisualElement GetElement(string elementId)
        {
            _elementCache.TryGetValue(elementId, out var ve);
            return ve;
        }

        /// <summary>触发交互回调（choice/fill 提交时由元件调用）。</summary>
        public void TriggerInteraction(string pageId, string elementId, string action, object data)
        {
            if (_callback != null)
            {
                var element = _pageManager.GetPage(pageId)?.Config?.elements?.Find(e => e.id == elementId);
                _callback.SendInteraction(pageId, elementId, action, data, element);
            }
            OnInteraction?.Invoke(pageId, elementId, action, "submitted", data);
        }

        /// <summary>清理资源。</summary>
        public void Cleanup()
        {
            if (_uiDoc != null && _uiDoc.gameObject != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_uiDoc.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(_uiDoc.gameObject);
            }
            _elementCache.Clear();
            _currentPageId = null;
        }
    }
}
