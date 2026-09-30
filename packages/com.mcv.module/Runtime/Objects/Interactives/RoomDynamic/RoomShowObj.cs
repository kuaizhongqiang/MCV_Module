using System.Collections;
using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.RoomDynamic
{
    // WHY: minPos/maxPos 是「该轴上的绝对本地坐标」而非偏移（8 个实例 minPos=1.07 正对应摆放 localPos.y），只换 moveAxis 一个分量，另两轴保持 Awake 原值
    /// <summary>房间展品的「悬浮 + 悬停」表现：常态上下浮动、悬停惯性减速、移出惯性加速、点击留口</summary>
    public class RoomShowObj : InteractiveBase
    {
        #region 序列化参数（原有字段，名字不可改 —— 场景里 8 个实例已按名字存了值）
        [Tooltip("点击时要展示的内容键 —— 留给后续接线用（现在没有消费方）")]
        [SerializeField] string uiConetnt = "";
        [Tooltip("浮动速度（单位/秒，沿 moveAxis 量）")]
        [SerializeField] float animMoveSpeed = 0.5f;
        [Tooltip("浮动所在的轴（只动这一个分量，另两轴保持摆放时的值）")]
        [SerializeField] ObjAxis moveAxis = ObjAxis.Y;
        [Tooltip("浮动的上限位置（该轴上的绝对本地坐标）")]
        [SerializeField] float maxPos = 1;
        [Tooltip("浮动的下限位置（该轴上的绝对本地坐标）")]
        [SerializeField] float minPos = -1;
        [Tooltip("是否随机起步位置：一排展品若同相位摆动，会像整排在一起呼吸；随机后各自错开")]
        [SerializeField] bool ifRondomPosStart = true;
        #endregion

        #region 序列化参数（新增）
        [Header("悬停惯性")]
        [Tooltip("移入后浮动速度降到常态的几倍：0 = 完全停住（默认，即惯性减速停止）；0.2 = 只减速")]
        [SerializeField, Range(0f, 1f)] float hoverSpeedFactor = 0f;
        [Tooltip("速度变化的惯性时长（秒）：越大越拖，越小越跟手")]
        [SerializeField] float inertiaDuration = 0.35f;

        [Header("缓动")]
        [Tooltip("缓动强度：1 = 匀速（折返处会顿一下），2 = 平方缓动（默认），越大折返越软")]
        [SerializeField, Range(1f, 4f)] float easePower = 2f;
        #endregion

        #region 运行时状态
        Coroutine moveCoroutine;              // 正常的上下浮动协程
        Coroutine mouseInteractCoroutine;     // 鼠标移入移除会惯性减速停止和惯性恢复移动

        /// <summary>Awake 时记下的本地位置：浮动只改 <see cref="moveAxis"/> 一个分量，另两轴以它为基准</summary>
        Vector3 m_BaseLocalPos;

        /// <summary>当前速度倍率：1 = 常态，<see cref="hoverSpeedFactor"/> = 悬停中；由惯性协程平滑逼近</summary>
        float m_SpeedScale = 1f;

        /// <summary>惯性用的速度中间量（SmoothDamp 持有）</summary>
        float m_SpeedVelocity;

        /// <summary>是否已做过首次起步定位（重新启用时不重随机，从当前实际位置接着走）</summary>
        bool m_Placed;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            m_BaseLocalPos = transform.localPosition;
        }

        void OnEnable()
        {
            // 协程不随 OnDisable 自动恢复，每次启用都要重开
            m_SpeedScale = 1f;              // 复位速度倍率，避免把上次悬停的慢速带进来
            m_SpeedVelocity = 0f;
            moveCoroutine = StartCoroutine(MoveRoutine());
        }

        void OnDisable()
        {
            StopMotion();
        }

        /// <summary>停掉两个协程并复位惯性状态（禁用 / 销毁共用）。</summary>
        void StopMotion()
        {
            if (moveCoroutine != null) { StopCoroutine(moveCoroutine); moveCoroutine = null; }
            if (mouseInteractCoroutine != null) { StopCoroutine(mouseInteractCoroutine); mouseInteractCoroutine = null; }
            m_SpeedScale = 1f;
            m_SpeedVelocity = 0f;
        }
        #endregion

        #region 浮动
        IEnumerator MoveRoutine()
        {
            float span = maxPos - minPos;

            if (Mathf.Approximately(span, 0f) || animMoveSpeed <= 0f)
            {
                // 参数无效时停在 minPos，并留一条告警 —— 不启动一个永远不动的空转协程
                SetAxisValue(minPos);
                Log.Warning($"[RoomShowObj] {name} 的浮动参数无效（minPos={minPos} maxPos={maxPos} animMoveSpeed={animMoveSpeed}），已停在 minPos", this);
                yield break;
            }

            if (!m_Placed)
            {
                // 首次起步：随机相位（一排同相位会在视觉上"整排一起呼吸"）
                SetAxisValue(ifRondomPosStart ? Random.Range(minPos, maxPos) : minPos);
                m_Placed = true;
            }

            // 从当前实际位置接着走（重新启用时不会跳变）；夹进区间，防止外部挪动后越界
            float current = Mathf.Clamp(GetAxisValue(), minPos, maxPos);
            // 先奔向更近的那一端，避免起步就长跑一趟
            bool forward = Mathf.Abs(maxPos - current) <= Mathf.Abs(current - minPos);

            while (true)
            {
                float target = forward ? maxPos : minPos;
                float from = current;
                float legLength = Mathf.Abs(target - from);

                if (legLength > 0.0001f)
                {
                    float walked = 0f;
                    while (walked < legLength)
                    {
                        // 速度 = 常态速度 × 当前倍率；悬停时倍率趋 0 → 浮动自然停住
                        walked += animMoveSpeed * m_SpeedScale * Time.deltaTime;
                        float t = Mathf.Clamp01(walked / legLength);

                        // ★ 平方缓动：两端导数为 0，折返不再"顿"（向下那半程尤其明显）
                        SetAxisValue(Mathf.Lerp(from, target, EaseSquare(t)));
                        yield return null;
                    }
                }

                current = target;
                SetAxisValue(current);
                forward = !forward;      // 换向
            }
        }

        // WHY: 必须两端对称缓动——匀速折返会「顿」（向下半程尤其生硬），只平方 t 只缓起点、终点更急
        /// <summary>单程内缓动 t^p/(t^p+(1-t)^p)，两端导数为 0；p=1 退化为匀速，强度靠 easePower 调</summary>
        float EaseSquare(float t)
        {
            t = Mathf.Clamp01(t);
            if (easePower <= 1.0001f) return t;

            float a = Mathf.Pow(t, easePower);
            float b = Mathf.Pow(1f - t, easePower);
            float sum = a + b;
            return sum < 1e-6f ? t : a / sum;
        }

        /// <summary>把值写到 <see cref="moveAxis"/> 对应的分量上，另两轴保持 Awake 时的原值。</summary>
        void SetAxisValue(float value)
        {
            Vector3 p = m_BaseLocalPos;
            switch (moveAxis)
            {
                case ObjAxis.X: p.x = value; break;
                case ObjAxis.Z: p.z = value; break;
                default:        p.y = value; break;   // ObjAxis.Y
            }
            transform.localPosition = p;
        }

        /// <summary>读当前 <see cref="moveAxis"/> 分量（重新启用时据此接着走，不跳变）。</summary>
        float GetAxisValue()
        {
            Vector3 p = transform.localPosition;
            switch (moveAxis)
            {
                case ObjAxis.X: return p.x;
                case ObjAxis.Z: return p.z;
                default:        return p.y;
            }
        }
        #endregion

        #region 悬停惯性
        /// <summary>起一段惯性：把速度倍率平滑推向 target；同一时刻只保留一段，移入途中移出直接改目标</summary>
        void StartInertia(float target)
        {
            if (mouseInteractCoroutine != null) StopCoroutine(mouseInteractCoroutine);
            mouseInteractCoroutine = StartCoroutine(InertiaRoutine(target));
        }

        IEnumerator InertiaRoutine(float target)
        {
            while (true)
            {
                m_SpeedScale = Mathf.SmoothDamp(m_SpeedScale, target, ref m_SpeedVelocity, inertiaDuration);

                if (Mathf.Abs(m_SpeedScale - target) <= 0.001f)
                {
                    m_SpeedScale = target;
                    break;
                }

                yield return null;
            }

            mouseInteractCoroutine = null;
        }
        #endregion

        #region 事件重写
        protected override void MoEnterEvent()
        {
            StartInertia(hoverSpeedFactor);
        }

        protected override void MoExitEvent()
        {
            StartInertia(1f);
        }

        /// <summary>点击：转交 OnClick，本类刻意不写任何逻辑，由后续接线决定</summary>
        protected override void MoClickEvent()
        {
            OnClick();
        }

        /// <summary>点击钩子：留口给后续接内容（查 UiContent / 弹面板 / 切镜头 / 发事件）；基类空实现</summary>
        protected virtual void OnClick() { }
        #endregion

        #region 公开接口
        // WHY: 字段名 uiConetnt 是历史拼写不能改——改了会丢场景里 8 个实例已按名字存的值
        /// <summary>Inspector 上的内容键（拼写 uiConetnt 不可改）；点击接线用它决定展示什么</summary>
        public string UiContent { get { return uiConetnt; } }

        /// <summary>当前速度倍率（1 = 常态，0 = 已停住）。</summary>
        public float SpeedScale { get { return m_SpeedScale; } }
        #endregion
    }
}
