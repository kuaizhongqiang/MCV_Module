
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
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
            // 按钮初始抬起（释放态），避免位置推断导致初始按下，确保流程不需先点一次
            elementMoveAnimation.Open = true;

            HighlightPluginInit(elementMoveAnimation.moveObj.gameObject);
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
            // 仅在 抬起→按下 的边沿发布一次状态事件，避免按住期间重复发送
            if (!elementMoveAnimation.Open) return;
            elementMoveAnimation.Open = false;
            EventBus<ElementStateChangeEventData>.Publish(new ElementStateChangeEventData(this));
        }

        protected override void MoUpEvent()
        {
            // 仅在 按下→抬起 的边沿发布一次状态事件，避免重复发送
            if (elementMoveAnimation.Open) return;
            elementMoveAnimation.Open = true;
            EventBus<ElementStateChangeEventData>.Publish(new ElementStateChangeEventData(this));
        }
    }
}
