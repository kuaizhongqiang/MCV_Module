
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
    /// <summary>电动机（M）：通过转动动画实现启停，支持指定转速启动。</summary>
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
            // WHY: 必须在此采集初始状态，否则首次 Play 才初始化，第一轮点击会无效
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
