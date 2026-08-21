using System;
using System.Collections;
using UnityEngine;

namespace MCV_Module.UI.Tools
{
    /// <summary>
    /// 菜单封面流滚动逻辑（一般类，非 MonoBehaviour）。
    /// 负责滚动状态、数值计算与滚动协程：惯性减速、吸附缓动、拖动速度采样。
    /// 协程由 MenuPanel（MonoBehaviour）用 StartCoroutine 启动本类提供的协程方法；
    /// 本类通过 OnStep/OnComplete 回调通知 MenuPanel 刷新布局与收尾。
    /// 本类不触碰 UI 组件，只维护纯滚动数值状态。
    /// </summary>
    public class MenuScrollLogic
    {
        /// <summary>每推进一帧后回调（MenuPanel 在此刷新布局）。</summary>
        public Action OnStep;

        /// <summary>滚动协程完全结束时回调（MenuPanel 在此清理协程引用等）。</summary>
        public Action OnComplete;

        /// <summary>滚动所处阶段。</summary>
        public enum ScrollPhase { Idle, Inertia, Snap }

        #region 参数（由 MenuPanel 注入）
        public float dampingTime = 0.6f;    // 惯性阻尼时长（越大滑得越久衰减越慢）
        public float snapDuration = 0.3f;   // 吸附动画时长
        public float snapThreshold = 0.05f; // 惯性速度低于此值时开始吸附
        public float dragSensitivity = 3f;  // 拖拽灵敏度（越大跟手越快）
        public float wheelInitialSpeed = 4f;// 滚轮产生的初始惯性速度（每格）
        public float step = 180f;           // 按钮高度 + 间距，用于拖动位移换算
        #endregion

        #region 状态
        /// <summary>当前焦点位置（可含小数，表示滚动中间态）。</summary>
        public float Focus { get; private set; }

        /// <summary>当前滚动速度（惯性衰减用；负=向上）。</summary>
        public float Velocity { get; private set; }

        /// <summary>当前阶段。</summary>
        public ScrollPhase Phase { get; private set; } = ScrollPhase.Idle;

        float decayPerSec;      // 惯性指数衰减系数（由 dampingTime 决定）
        float snapStart;        // 吸附起点
        float snapTarget;       // 吸附目标（最近整数格）
        float snapTime;         // 吸附进度 0..1
        #endregion

        /// <summary>直接定位焦点（重新装配 / 初始化），并清空速度。</summary>
        public void SetFocus(float focus)
        {
            Focus = focus;
            Velocity = 0f;
            Phase = ScrollPhase.Idle;
        }

        /// <summary>清空速度（输入接管、重新装配时调用）。</summary>
        public void ResetVelocity()
        {
            Velocity = 0f;
        }

        /// <summary>停止当前逻辑状态（停止惯性/吸附，回到 Idle）。</summary>
        public void Stop()
        {
            Phase = ScrollPhase.Idle;
            Velocity = 0f;
        }

        /// <summary>
        /// 拖动跟手：更新焦点并平滑采样速度（松手时作为惯性初速）。
        /// </summary>
        public void DragUpdate(float newFocus, float dt)
        {
            dt = Mathf.Max(dt, 0.0001f);
            float frameVelocity = (newFocus - Focus) / dt;
            // 平滑，避免单帧抖动导致速度归零/反向
            Velocity = Mathf.Lerp(Velocity, frameVelocity, 0.5f);
            Focus = newFocus;
        }

        /// <summary>以指定初速启动惯性滚动阶段。</summary>
        public void StartInertia(float initialVelocity)
        {
            Velocity = initialVelocity;
            // 衰减系数：dampingTime 越大衰减越慢、滑得越久
            decayPerSec = Mathf.Log(0.05f) / Mathf.Max(dampingTime, 0.01f);
            Phase = ScrollPhase.Inertia;
        }

        /// <summary>
        /// 推进惯性一步（线性/指数衰减）。返回 true 表示惯性仍在进行；
        /// 返回 false 表示速度已降到阈值以下，应转入吸附。
        /// </summary>
        public bool InertiaStep(float dt)
        {
            if (Mathf.Abs(Velocity) <= snapThreshold)
            {
                Velocity = 0f;
                Phase = ScrollPhase.Idle;
                return false;
            }
            Focus += Velocity * dt;
            Velocity *= Mathf.Exp(decayPerSec * dt);
            return true;
        }

        /// <summary>启动吸附阶段：把当前位置平滑对齐到最近整数格。</summary>
        public void StartSnap()
        {
            snapStart = Focus;
            snapTarget = Mathf.RoundToInt(Focus);
            snapTime = 0f;
            Phase = ScrollPhase.Snap;
        }

        /// <summary>
        /// 推进吸附一步（平方缓动 t²）。返回 true 表示吸附仍在进行；false 表示已到位。
        /// </summary>
        public bool SnapStep(float dt)
        {
            snapTime += dt / Mathf.Max(snapDuration, 0.01f);
            if (snapTime >= 1f)
            {
                Focus = snapTarget;
                Phase = ScrollPhase.Idle;
                return false;
            }
            float eased = snapTime * snapTime;
            Focus = Mathf.Lerp(snapStart, snapTarget, eased);
            return true;
        }

        /// <summary>
        /// 惯性滚动协程：逐帧推进惯性，速度降到阈值后自动衔接吸附。
        /// 由 MenuPanel 用 StartCoroutine 启动。
        /// </summary>
        public IEnumerator ScrollInertia()
        {
            while (InertiaStep(Time.deltaTime))
            {
                OnStep?.Invoke();
                yield return null;
            }
            // 惯性结束自动转入吸附
            yield return ScrollSnap();
        }

        /// <summary>
        /// 吸附协程：逐帧推进平方缓动吸附到最近整数格。由 MenuPanel 用 StartCoroutine 启动。
        /// </summary>
        public IEnumerator ScrollSnap()
        {
            StartSnap();
            while (SnapStep(Time.deltaTime))
            {
                OnStep?.Invoke();
                yield return null;
            }
            OnComplete?.Invoke();
        }

        #region 工具方法
        /// <summary>环形取模（保证结果为非负）。</summary>
        public static int Mod(int a, int n)
        {
            return n <= 0 ? 0 : (a % n + n) % n;
        }

        /// <summary>将值夹到不小于 min 且为奇数。</summary>
        public static int ClampOdd(int value, int min)
        {
            if (value < min)
            {
                value = min;
            }
            if (value % 2 == 0)
            {
                value += 1; // 保证为奇数
            }
            return value;
        }

        /// <summary>判断屏幕坐标是否位于指定 RectTransform 的矩形范围内。</summary>
        public static bool IsPointerInside(Transform target, Vector2 screenPos)
        {
            if (target == null)
            {
                return false;
            }
            RectTransform rect = target as RectTransform;
            if (rect == null)
            {
                return false;
            }
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, null, out local);
            return rect.rect.Contains(local);
        }
        #endregion
    }
}
