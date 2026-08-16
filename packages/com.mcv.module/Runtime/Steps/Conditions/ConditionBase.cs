using System;
using System.Collections;
using MCV_Module.Event;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Objects.Interactives;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.Steps
{
    /// <summary>
    /// 步骤条件基类 —— 三阶段默认实现 + 协作式打断 + 订阅管理（纯类，非 MonoBehaviour）。
    /// 由 StepManager 协程 yield 驱动；StepHandler 承载显隐/动画执行，条件只做状态与交互逻辑。
    /// </summary>
    public abstract class ConditionBase : ICondition
    {
        /// <summary>宿主步骤（提供 SetObjsActive / 动画方法 / 交互字段）</summary>
        public StepHandler step;

        /// <summary>ForceComplete 置位后，Waiting 循环据此退出</summary>
        protected bool forceComplete;
        /// <summary>交互事件是否处于订阅态（Reset 时防御性退订）</summary>
        protected bool interactionSubscribed;
        /// <summary>缓存的交互委托（退订用）</summary>
        protected Action<GlobalInteractionEventData> interactionHandler;

        public abstract ConditionType Type { get; }
        public StepStutus Status { get; set; } = StepStutus.Ready;

        /// <summary>是否已被 ForceComplete 打断</summary>
        public bool IsForceCompleted => forceComplete;

        #region 初始化 / 重置

        public virtual void ConditionInit(StepHandler step)
        {
            this.step = step;
            ResetCondition();
        }

        /// <summary>跳转前置调用：清打断标志、退订残留订阅、Status 回 Ready</summary>
        public virtual void ResetCondition()
        {
            forceComplete = false;
            Status = StepStutus.Ready;
            UnsubscribeInteraction(); // EventBus.Unsubscribe 幂等，安全
        }

        #endregion

        #region 三阶段生命周期

        /// <summary>阶段①准备：通用显隐 + 隐藏动画物体 + 子类钩子</summary>
        public virtual IEnumerator Prepare()
        {
            step.SetObjsActive();      // 通用显隐（showObjs/hideObjs）
            step.HideAnimations();     // 隐藏动画物体（全员归位）
            OnPrepare();               // 子类钩子（隐藏交互物体/关面板/取消临时线）
            yield break;
        }

        /// <summary>子类补充准备逻辑</summary>
        protected virtual void OnPrepare() { }

        /// <summary>阶段②等待：子类实现交互循环（阻塞点必须用 WaitUntilOrForceComplete）</summary>
        public abstract IEnumerator Waiting();

        /// <summary>阶段③完成：隐藏交互物体 → 播放动画 → 等播完 → hideOnComplete</summary>
        public virtual IEnumerator Complete()
        {
            OnCompleteHide();               // 子类先隐藏交互物体/关面板
            step.PlayAnimations();          // 播放所有动画
            yield return new WaitUntil(() => !step.AnyAnimationPlaying()); // 等播完（非循环动画）
            step.HideAnimationsOnComplete();// hideOnComplete 处理
        }

        /// <summary>子类补充完成隐藏逻辑</summary>
        protected virtual void OnCompleteHide() { }

        #endregion

        #region 快速执行

        /// <summary>快速执行：跳过 Waiting，动画瞬间跳到最后一帧（跳转前缀用）</summary>
        public virtual IEnumerator FastForward()
        {
            yield return FastComplete();
        }

        /// <summary>快速完成：动画瞬间跳到最后一帧（normalizedTime=1 + Play + Sample + Stop）</summary>
        protected virtual IEnumerator FastComplete()
        {
            OnCompleteHide();
            step.StopAtLastFrame();
            step.HideAnimationsOnComplete();
            yield break;
        }

        #endregion

        #region 打断

        /// <summary>强制完成：置 Status=Complete 并令 Waiting 协程尽快退出（NextStep/Skip 调用）</summary>
        public void ForceComplete()
        {
            forceComplete = true;
            Status = StepStutus.Complete;
        }

        /// <summary>每帧检查 predicate；ForceComplete 置位后退出。子类所有 Waiting 阻塞点必须用此方法。</summary>
        protected IEnumerator WaitUntilOrForceComplete(Func<bool> predicate)
        {
            while (!predicate() && !forceComplete)
                yield return null;
        }

        #endregion

        #region 交互事件订阅管理（EventBus 过滤，幂等可重调）

        /// <summary>订阅全局交互事件（进入 Waiting 时调用）</summary>
        protected void SubscribeInteraction(Action<GlobalInteractionEventData> handler)
        {
            interactionHandler = handler;
            interactionSubscribed = true;
            EventBus<GlobalInteractionEventData>.Subscribe(handler);
        }

        /// <summary>退订全局交互事件（循环尾部 / ResetCondition 兜底调用；幂等）</summary>
        protected void UnsubscribeInteraction()
        {
            if (!interactionSubscribed) return;
            if (interactionHandler != null)
                EventBus<GlobalInteractionEventData>.Unsubscribe(interactionHandler);
            interactionHandler = null;
            interactionSubscribed = false;
        }

        #endregion

        #region 工具方法

        /// <summary>松开时手动 Camera 射线命中目标（Drag/Tool 的落点判定）</summary>
        protected bool RaycastHitTarget(InteractiveBase target)
        {
            if (target == null) return false;
            Camera cam = GlobalCameraMgr.Camera;
            if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit)) return false;
            return hit.collider.GetComponentInParent<InteractiveBase>() == target;
        }

        /// <summary>左键是否在本帧松开（Drag/Tool 的松开检测；不能靠 Up 事件——空白处松开只发 Target=null 的 Click）</summary>
        protected bool IsMouseUp()
        {
            return Mouse.current?.leftButton.wasReleasedThisFrame ?? false;
        }

        #endregion
    }
}
