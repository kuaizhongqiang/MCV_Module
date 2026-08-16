
using System;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using UnityEngine;

namespace MCV_Module.Objects.Interactives
{
    public abstract class InteractiveBase : MonoBehaviour,IObj
    {
        [SerializeField] protected bool isInteractable = true;
        [SerializeField] protected Color highlightColor = new Color(0, 1, 0, 0.5f);
        public bool IsInteractable => isInteractable;
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
        /// <summary>高亮服务目标（HighlightPluginInit 指定，默认自身）。</summary>
        GameObject highlightTarget;

        protected void HighlightPluginInit(GameObject obj = null)
        {
            highlightTarget = obj != null ? obj : gameObject;
            IHighlightService.Instance?.Init(highlightTarget, highlightColor);
        }

        protected void Highlight(bool isHighlight)
        {
            if (highlightTarget == null) highlightTarget = gameObject;
            var service = IHighlightService.Instance;
            // 未注入宿主高亮服务（HighlightPlusAdapter）→ 静默降级为无高亮
            if (service == null) return;
            if (isHighlight) service.ApplyHighlight(highlightTarget, highlightColor);
            else service.ClearHighlight(highlightTarget);
        }
        #endregion
    }
}