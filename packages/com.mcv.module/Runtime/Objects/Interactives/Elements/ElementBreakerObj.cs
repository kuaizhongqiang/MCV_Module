
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
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

            HighlightPluginInit(rotationAnimation.rotateObj.gameObject);
        }

        protected override void MoEnterEvent()
        {
            Highlight(true);
        }

        protected override void MoExitEvent()
        {
            Highlight(false);
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
