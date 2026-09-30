
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
    /// <summary>断路器（QS）：点击开/合闸，播放旋转动画并发布状态变化事件。</summary>
    public class ElementBreakerObj : ElementObjBase
    {
        List<ElementPointObj> points = new List<ElementPointObj>();
        public List<ElementPointObj> Points {get => points;}
        public override ElementType Type => ElementType.Breaker;
        [SerializeField] ElementRotationAnimation rotationAnimation = new ElementRotationAnimation();
        [SerializeField] bool isOpen = true;
        public bool IsOpen {get => isOpen; set => isOpen = value;}
        const string OpenTag = "Open";
        const string CloseTag = "Close";
        protected override void Awake()
        {
            base.Awake();
            var old = rotationAnimation;
            rotationAnimation = new ElementRotationAnimation(this, old.rotateObj, old.RotationStructs);

            string tag = isOpen ? CloseTag : OpenTag;
            rotationAnimation.Play(tag);
            isOpen = !isOpen;
        }

        protected override void MoClickEvent()
        {
            string tag = isOpen ? CloseTag : OpenTag;
            rotationAnimation.Play(tag);
            isOpen = !isOpen;
            EventBus<ElementStateChangeEventData>.Publish(new ElementStateChangeEventData(this));
        }
    }
}
