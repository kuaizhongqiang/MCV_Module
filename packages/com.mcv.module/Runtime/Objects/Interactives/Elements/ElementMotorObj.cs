
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
    public class ElementMotorObj : ElementObjBase
    {
        List<ElementPointObj> points = new List<ElementPointObj>();
        [SerializeField] ElementRunAnimation runAnimation = new ElementRunAnimation();
        public List<ElementPointObj> Points {get => points;}
        public override ElementType Type => ElementType.Motor;

        protected override void Awake()
        {
            base.Awake();
            var old = runAnimation;
            var newAnim = new ElementRunAnimation(this,
                old.runObj,
                old.rotationAxis,
                old.runSpeed,
                old.speedChangeDuration);
            runAnimation = newAnim;
            // 立即采集初始状态（默认转速/初始角度），避免首次 Play 时才初始化导致第一轮点击无效
            runAnimation.Reset();
        }

        public void MotorRun()
        {
            runAnimation.Play();
        }

        /// <summary>以指定转速（度/秒）启动电机，渐变到目标转速。</summary>
        public void MotorRun(float speed)
        {
            runAnimation.Play(speed);
        }

        public void MotorStop()
        {
            runAnimation.Stop();
        }
    }
}
