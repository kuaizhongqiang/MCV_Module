using System;
using UnityEngine;

namespace MCV_Module.Models
{
    /// <summary>
    /// 步骤动画条目 —— 一条步骤动画的播放配置（对齐 Tuanjie StepAnimation）。
    /// 使用 Legacy Animation 组件精确控帧（Play + Sample + Stop）。
    /// </summary>
    [Serializable]
    public class StepAnimation
    {
        /// <summary>承载动画的 Legacy Animation 组件（步骤动画物体不在 StepManager 层级内，Inspector 拖拽）</summary>
        [SerializeField] public Animation animation;
        /// <summary>要播放的片段（必须是非循环动画，否则 Complete 阶段等播完永不结束）</summary>
        [SerializeField] public AnimationClip clip;
        /// <summary>播放完成后是否隐藏该动画物体</summary>
        [SerializeField] public bool hideOnComplete;
    }
}
