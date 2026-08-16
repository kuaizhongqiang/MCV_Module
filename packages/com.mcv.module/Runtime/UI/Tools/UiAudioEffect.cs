
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MCV_Module.UI.Tools
{
    /// <summary>
    /// UI 音效基类：挂在 Button / Toggle / 任意可被射线命中的 UI 组件上，
    /// 鼠标划过（Enter/Exit）与点击（Click）时播放对应音效。
    /// 事件来源：UGUI 事件系统（IPointerEnter/Exit/ClickHandler），
    /// 与 3D 物体交互（GlobalInteractiveMgr 的射线）互不干扰。
    /// 子类可重写 Mo* 方法，在音效之外扩展表现。
    /// </summary>
    public abstract class UiAudioEffectBase : UIBase, IUiEffect,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        #region 参数
        [Header("划过音效")]
        [SerializeField] AudioEffectType enterAudio = AudioEffectType.Hover;
        [SerializeField] bool playEnter = true;

        [Header("离开音效")]
        [SerializeField] AudioEffectType exitAudio = AudioEffectType.None;
        [SerializeField] bool playExit;

        [Header("点击音效")]
        [SerializeField] AudioEffectType clickAudio = AudioEffectType.Click;
        [SerializeField] bool playClick = true;

        [Header("限制"), Tooltip("仅在组件可交互（Selectable.interactable）时播放")]
        [SerializeField] bool interactableOnly = true;
        #endregion

        #region 接口实现
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanPlay()) return;
            MoEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!CanPlay()) return;
            MoExit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanPlay()) return;
            MoClick();
        }

        /// <summary>鼠标进入：默认播放划过音效，子类可重写。</summary>
        public virtual void MoEnter()
        {
            if (playEnter) PlayEffect(enterAudio);
        }

        /// <summary>鼠标离开：默认播放离开音效（默认关闭）。</summary>
        public virtual void MoExit()
        {
            if (playExit) PlayEffect(exitAudio);
        }

        /// <summary>鼠标点击：默认播放点击音效。</summary>
        public virtual void MoClick()
        {
            if (playClick) PlayEffect(clickAudio);
        }
        #endregion

        #region 工具方法
        /// <summary>是否允许播放：本物体激活 +（可选）组件可交互。</summary>
        bool CanPlay()
        {
            if (!isActiveAndEnabled) return false;
            if (!interactableOnly) return true;
            var selectable = GetComponent<Selectable>();
            return selectable == null || selectable.interactable;
        }

        void PlayEffect(AudioEffectType type)
        {
            if (type == AudioEffectType.None) return;
            GlobalAudioMgr.PlayAudio(type);
        }
        #endregion
    }

    /// <summary>
    /// UI 音效（默认实现）：直接挂到任意 UI 组件上即可获得划过/点击音效。
    /// </summary>
    public class UiAudioEffect : UiAudioEffectBase
    {
    }
}
