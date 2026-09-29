
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
    /// <summary>按钮开关（SB）：按下/抬起边沿驱动位移动画并发布状态变化事件。</summary>
    public class ElementButtonSwitchObj : ElementObjBase
    {
        List<ElementPointObj> points = new List<ElementPointObj>();
        public List<ElementPointObj> Points {get => points;}
        public override ElementType Type => ElementType.ButtonSwitch;
        [SerializeField] ElementMoveAnimation elementMoveAnimation = new ElementMoveAnimation();

        /// <summary>按钮是否处于按下状态（按下=触点闭合）</summary>
        public bool IsPressed => !elementMoveAnimation.Open;
    
        protected override void Awake()
        {
            base.Awake();
            var old = elementMoveAnimation;
            var newAnim = new ElementMoveAnimation(this,
                old.moveObj, old.moveAxis, old.moveLimitation, old.duration);
            elementMoveAnimation = newAnim;

            elementMoveAnimation.Reset();
            // WHY: 强制初始为抬起态，避免按位置推断出「已按下」导致流程要先点一次
            elementMoveAnimation.Open = true;

            HighlightInit(elementMoveAnimation.moveObj.gameObject);
        }

        

        protected override void MoEnterEvent()
        {
            Highlight(true);
        }

        protected override void MoExitEvent()
        {
            Highlight(false);
        }        

        protected override void MoDownEvent()
        {
            // WHY: 只在 抬起→按下 的边沿发一次事件，否则按住期间会重复发布
            if (!elementMoveAnimation.Open) return;
            elementMoveAnimation.Open = false;
            EventBus<ElementStateChangeEventData>.Publish(new ElementStateChangeEventData(this));
        }

        protected override void MoUpEvent()
        {
            // WHY: 只在 按下→抬起 的边沿发一次事件，否则会重复发布
            if (elementMoveAnimation.Open) return;
            elementMoveAnimation.Open = true;
            EventBus<ElementStateChangeEventData>.Publish(new ElementStateChangeEventData(this));
        }
    }
}
