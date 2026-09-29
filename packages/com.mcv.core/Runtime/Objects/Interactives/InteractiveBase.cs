
using System;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using UnityEngine;

namespace MCV_Module.Objects.Interactives
{
    /// <summary>可交互物基类：注册到 GlobalInteractiveMgr，把指针事件转成 Mo* 事件并托管悬停高亮。</summary>
    public abstract class InteractiveBase : MonoBehaviour,IObj
    {
        [SerializeField] protected bool isInteractable = true;
        [SerializeField] protected Color highlightColor = new Color(0, 1, 0, 0.5f);

        // WHY: Awake 里 isInteractable 只决定是否自订阅 Mo* 事件，运行期改本属性不会补/拆订阅，也不改显隐。
        /// <summary>是否可交互（Inspector 可配、运行时可切）；为 false 时该物体等同「没被点到」。</summary>
        public bool IsInteractable { get => isInteractable; set => isInteractable = value; }
        public event Action MoEnter;
        public event Action MoExit;
        public event Action MoClick;
        public event Action MoClickRight;
        public event Action MoClickDouble;
        public event Action MoDown;
        public event Action MoUp;
        public event Action<Vector2> MoMove;

        protected virtual void Awake()
        {
            if (isInteractable)
            {
                MoEnter += MoEnterEvent;
                MoExit += MoExitEvent;
                MoClick += MoClickEvent;
                MoClickRight += MoClickRightEvent;
                MoClickDouble += MoClickDoubleEvent;
                MoDown += MoDownEvent;
                MoUp += MoUpEvent;
                MoMove += MoMoveEvent;
            }

            GlobalInteractiveMgr.Register(this);
        }

        protected virtual void OnDestroy()
        {
            if (isInteractable)
            {
                MoEnter -= MoEnterEvent;
                MoExit -= MoExitEvent;
                MoClick -= MoClickEvent;
                MoClickRight -= MoClickRightEvent;
                MoClickDouble -= MoClickDoubleEvent;
                MoDown -= MoDownEvent;
                MoUp -= MoUpEvent;
                MoMove -= MoMoveEvent;
            }
            if (GlobalInteractiveMgr.Instance != null)
                GlobalInteractiveMgr.Unregister(this);
        }

        public T GetObj<T>() where T : Component
        {
            return GetComponent<T>();
        }
        protected virtual void MoEnterEvent()
        {
            
        }

        protected virtual void MoExitEvent()
        {
            
        }

        protected virtual void MoClickEvent()
        {
            
        }

        protected virtual void MoClickRightEvent()
        {
            
        }

        protected virtual void MoClickDoubleEvent()
        {
            
        }

        protected virtual void MoDownEvent()
        {
            
        }

        protected virtual void MoUpEvent()
        {
            
        }

        protected virtual void MoMoveEvent(Vector2 pos)
        {
            
        }

        #region 事件触发（供 GlobalInteractiveMgr 调用）
        public void InvokeMoEnter() => MoEnter?.Invoke();
        public void InvokeMoExit() => MoExit?.Invoke();
        public void InvokeMoClick() => MoClick?.Invoke();
        public void InvokeMoClickRight() => MoClickRight?.Invoke();
        public void InvokeMoClickDouble() => MoClickDouble?.Invoke();
        public void InvokeMoDown() => MoDown?.Invoke();
        public void InvokeMoUp() => MoUp?.Invoke();
        public void InvokeMoMove(Vector2 delta) => MoMove?.Invoke(delta);
        #endregion

        #region 工具方法
        /// <summary>高亮服务目标（HighlightInit 指定，默认自身）。</summary>
        GameObject highlightTarget;

        protected void HighlightInit(GameObject obj = null)
        {
            highlightTarget = obj != null ? obj : gameObject;
            IHighlightService.Instance?.Init(highlightTarget, highlightColor);
        }

        protected void Highlight(bool isHighlight)
        {
            if (highlightTarget == null) highlightTarget = gameObject;
            var service = IHighlightService.Instance;
            // WHY: 未注入宿主高亮服务时静默降级为无高亮，不可改成报错、也不得在框架内直连第三方高亮插件
            if (service == null) return;
            if (isHighlight) service.ApplyHighlight(highlightTarget, highlightColor);
            else service.ClearHighlight(highlightTarget);
        }
        #endregion
    }
}