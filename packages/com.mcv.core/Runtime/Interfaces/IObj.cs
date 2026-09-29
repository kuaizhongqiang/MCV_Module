using System;
using UnityEngine;

namespace MCV_Module.Interfaces
{
    // WHY: IsInteractable 只在实现方 Awake 时决定是否自订阅 Mo* 事件，运行期改它不会补/拆订阅，也不改显隐。
    /// <summary>可交互物契约：暴露组件获取、可交互开关与 8 个鼠标事件。</summary>
    public interface IObj
    {
        /// <summary>取本物体上的组件 T。</summary>
        public T GetObj<T>() where T : Component;
        /// <summary>是否可交互（false 时等同没被点到）。</summary>
        public bool IsInteractable { get; }
        /// <summary>鼠标进入。</summary>
        public event Action MoEnter;
        /// <summary>鼠标离开。</summary>
        public event Action MoExit;
        /// <summary>鼠标左键点击。</summary>
        public event Action MoClick;
        /// <summary>鼠标右键点击。</summary>
        public event Action MoClickRight;
        /// <summary>鼠标双击。</summary>
        public event Action MoClickDouble;
        /// <summary>鼠标按下。</summary>
        public event Action MoDown;
        /// <summary>鼠标抬起。</summary>
        public event Action MoUp;
        /// <summary>鼠标移动（携带位移）。</summary>
        public event Action<Vector2> MoMove;
    }
}
