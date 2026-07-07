
using System;
using MCV_Module.GlobalManager;
using UnityEngine;

namespace MCV_Module.Interactive
{
    public abstract class InteractiveBase : MonoBehaviour,IObj
    {
        [SerializeField] protected bool isInteractable = true;
        public bool IsInteractable => isInteractable;
        
        public event Action MoEnter;
        public event Action MoExit;
        public event Action MoClick;
        public event Action MoClickRight;
        public event Action MoClickDouble;
        public event Action MoDown;
        public event Action MoUp;
        public event Action<Vector2> MoMove;

        void Awake()
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

        void OnDestroy()
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


    }
}