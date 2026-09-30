using System;
using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Objects.Interactives;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>步骤节点：承载单个步骤的数据与运行时条件，由 ProcessingHandler 收集、StepManager 驱动。</summary>
    public class StepHandler : MonoBehaviour
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField] string description;
        [SerializeField] ConditionType conditionType = ConditionType.Default;
        [SerializeField] List<GameObject> showObjs = new List<GameObject>();
        [SerializeField] List<GameObject> hideObjs = new List<GameObject>();
        [SerializeField] List<StepAnimation> animations = new List<StepAnimation>();
        [SerializeField] string tipsId;
        [SerializeField] string audioId;
        [SerializeField] InteractiveBase targetObj;
        [SerializeField] InteractiveBase dragObj;
        [SerializeField] string usingId; // 可能是 ui / tool / question
        [SerializeField] List<InteractiveBase> lines = new List<InteractiveBase>();
        [SerializeField] List<InteractiveBase> points = new List<InteractiveBase>(); // 测量点对（ConditionMeasurePair 用，恰好 2 个检测点）
        [SerializeField] MultimeterGearType gearType = MultimeterGearType.Off;       // 正确档位（ConditionGearAdjust 用）

        /// <summary>运行时条件（按 conditionType 创建）</summary>
        [NonSerialized] public ConditionBase condition;

        public string Id => id;
        public string DisplayName => displayName;
        public ConditionType Type => conditionType;
        /// <summary>步骤说明（作者在 Inspector 配的文案，TipsController 取作步骤提示）</summary>
        public string Description => description;
        /// <summary>图文提示资源 id（预留：提示表 / AB 资源就绪后由 TipsController 使用）</summary>
        public string TipsId => tipsId;
        /// <summary>讲解音频 id（预留）</summary>
        public string AudioId => audioId;

        /// <summary>点击/拖拽目标物体（ConditionClick/Drag/Tool 用）</summary>
        public InteractiveBase TargetObj => targetObj;
        /// <summary>拖拽源物体（ConditionDrag 用）</summary>
        public InteractiveBase DragObj => dragObj;
        /// <summary>工具/UI/题目 ID（ConditionTool/UI/Question 用）</summary>
        public string UsingId => usingId;
        /// <summary>连线模板（ConditionLineConnect 用）</summary>
        public List<InteractiveBase> Lines => lines;
        /// <summary>测量点对（ConditionMeasurePair 用，恰好 2 个 InspectionElementPointObj，红黑对调算同一对）</summary>
        public List<InteractiveBase> Points => points;
        /// <summary>正确档位（ConditionGearAdjust 用；只判功能类型，不判量程）</summary>
        public MultimeterGearType GearType => gearType;

        void Awake()
        {
            // WHY: 显式 id 优先——为空才按层级生成，层级一挪 id 就变，有外部引用的步骤必须填死。
            var processing = transform.parent;
            int processingIndex = processing != null ? processing.GetSiblingIndex() : 0;
            int index = transform.GetSiblingIndex();
            if (string.IsNullOrEmpty(id))
                id = $"Step_{processingIndex}_{index}";
            displayName = $"{id}_{Type}";
            gameObject.name = displayName;

            condition = CreateCondition();
            condition.ConditionInit(this);
        }

        /// <summary>显示 showObjs、隐藏 hideObjs（步骤开始时调用）</summary>
        public void SetObjsActive()
        {
            foreach (var obj in showObjs)
            {
                obj.SetActive(true);
            }
            foreach (var obj in hideObjs)
            {
                obj.SetActive(false);
            }
        }

        #region 动画控制（Legacy Animation 精确控帧）

        /// <summary>隐藏所有动画物体（Prepare 归位用）</summary>
        public void HideAnimations()
        {
            foreach (var sa in animations)
                if (sa.animation != null) sa.animation.gameObject.SetActive(false);
        }

        /// <summary>显示动画物体并停在第一帧（Waiting 进入时用）</summary>
        public void ShowAnimationsAtFirstFrame()
        {
            foreach (var sa in animations)
            {
                if (sa.animation == null) continue;
                sa.animation.gameObject.SetActive(true);
                if (sa.clip == null) continue;
                sa.animation.clip = sa.clip;
                AnimationState state = sa.animation[sa.clip.name];
                if (state == null) continue;
                state.normalizedTime = 0f;
                sa.animation.Play(); sa.animation.Sample(); sa.animation.Stop();
            }
        }

        /// <summary>播放所有动画（Complete 阶段用）</summary>
        public void PlayAnimations()
        {
            foreach (var sa in animations)
                if (sa.animation != null && sa.clip != null)
                {
                    sa.animation.clip = sa.clip;
                    sa.animation.Play();
                }
        }

        /// <summary>动画瞬间跳到最后一帧（FastComplete 用）</summary>
        public void StopAtLastFrame()
        {
            foreach (var sa in animations)
            {
                if (sa.animation == null) continue;
                sa.animation.gameObject.SetActive(true);
                if (sa.clip == null) continue;
                sa.animation.clip = sa.clip;
                AnimationState state = sa.animation[sa.clip.name];
                if (state == null) continue;
                state.normalizedTime = 1f;
                sa.animation.Play(); sa.animation.Sample(); sa.animation.Stop();
            }
        }

        /// <summary>是否还有动画在播放（Complete 等播完用）</summary>
        public bool AnyAnimationPlaying()
        {
            foreach (var sa in animations)
                if (sa.animation != null && sa.animation.isPlaying) return true;
            return false;
        }

        /// <summary>按 hideOnComplete 隐藏动画物体</summary>
        public void HideAnimationsOnComplete()
        {
            foreach (var sa in animations)
                if (sa.hideOnComplete && sa.animation != null)
                    sa.animation.gameObject.SetActive(false);
        }

        #endregion

        ConditionBase CreateCondition()
        {
            switch (conditionType)
            {
                case ConditionType.Click: return new ConditionClick();
                case ConditionType.Drag: return new ConditionDrag();
                case ConditionType.Tool: return new ConditionTool();
                case ConditionType.UI: return new ConditionUI();
                case ConditionType.Question: return new ConditionQuestion();
                case ConditionType.Finish: return new ConditionFinish();
                case ConditionType.Start: return new ConditionStart();
                default: return new ConditionDefault();
            }
        }
    }
}
