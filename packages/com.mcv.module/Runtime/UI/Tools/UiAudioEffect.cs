
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MCV_Module.UI.Tools
{
    // WHY: 音效走 UGUI 事件系统（IPointerEnter/Exit/ClickHandler），与 GlobalInteractiveMgr 的 3D 射线互不干扰；组件被失活或不可交互时收不到任何事件。
    /// <summary>UI 音效基类：挂在可被射线命中的 UI 上，划过/离开/点击时播放音效，Mo* 可被子类重写。</summary>
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

    /// <summary>UI 音效默认实现：直接挂到任意 UI 组件上即可获得划过/点击音效。</summary>
    public class UiAudioEffect : UiAudioEffectBase
    {
    }
}
