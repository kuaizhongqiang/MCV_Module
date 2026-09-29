using System.Collections;
using System.Globalization;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Models.ElementInspection;
using MCV_Module.Utils;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 屏幕只由档位、表笔吸附、元件动作态、配对数据四件事决定（任一变化整屏重算）；判定必须用 ExactValue / IsPairCorrect，屏幕值带 randomValueRange 随机浮动，且数字表没有欧姆调零。
    /// <summary>数字万用表（屏幕 + 档位旋钮 + 红黑表笔）：按档位、表笔吸附、元件动作态与配对数据整屏重算读数，命中正确接点组时置 IsPairCorrect。</summary>
    public class InspectionMultimeterObj : InteractiveBase
    {
        #region 序列化参数
        [Header("面板")]
        [SerializeField, Tooltip("数字屏用的文本（Legacy Text）")]
        Text valueText;

        // WHY: 提前缓存屏幕节点上的组件 —— TMP 形态下组件会卸载节点上的 Legacy Text，valueText 随后成"假 null"，
        // 静态入口 SetTextOn 会静默 no-op（屏幕永远不刷新），只有持有组件才写得进去。
        /// <summary>数字屏文本节点上的组件（缓存见 <see cref="Awake"/>）。</summary>
        TextComponent m_ValueTextComp;

        [Header("接线")]
        [SerializeField, Tooltip("档位旋钮；留空则档位固定为 fallbackGear（检不到换档）")]
        InspectionMultimeterKnobObj knobObj;

        [SerializeField, Tooltip("旋钮未赋值时使用的档位")]
        MultimeterGearType fallbackGear = MultimeterGearType.Resistance;

        [SerializeField, Tooltip("红表笔")]
        InspectionProbeObj redProbeObj;

        [SerializeField, Tooltip("黑表笔")]
        InspectionProbeObj blackProbeObj;

        [Header("屏幕文案")]
        [SerializeField, Tooltip("关机档的屏幕文字（默认空）")]
        string powerOffText = "";

        [SerializeField, Tooltip("没接好（电阻类档位等于开路）的屏幕文字，真数字表是 OL")]
        string openCircuitText = "OL";

        [SerializeField, Tooltip("读数超量程的屏幕文字")]
        string overRangeText = "OL";

        [Header("显示表现")]
        [SerializeField, Tooltip("从当前显示值过渡到目标读数的时间长度（秒）；<= 0 = 直接跳")]
        float valueTextChangeDuration = 0.1f;

        [SerializeField, Tooltip("显示值的随机浮动范围，如 0.1 就是正确值 ±0.1（模拟真表跳动；判定不受影响）")]
        float randomValueRange = 0.1f;
        #endregion

        #region 运行时状态
        /// <summary>红表笔当前吸附的检测点（null = 未接入）</summary>
        InspectionElementPointObj redPointObj;
        /// <summary>黑表笔当前吸附的检测点（null = 未接入）</summary>
        InspectionElementPointObj blackPointObj;

        /// <summary>屏幕当前显示的数值（动画中间值）</summary>
        float currentValue;
        /// <summary>本次读数的精确值（不含随机浮动）</summary>
        float m_ExactValue;

        /// <summary>数值过渡协程（换读数时先停旧的）</summary>
        Coroutine m_ChangeCoroutine;

        /// <summary>上一帧的档位快照（旋钮下标），只在变化时重算屏幕</summary>
        int m_LastGearKey;

        /// <summary>最近一次读数用到的元件：只缓存引用、逐帧读它的 IsActuated 缓存值，不逐帧 GetComponentInParent（那才是每帧贵的部分）。</summary>
        InspectionElementObj m_CurrentElement;
        /// <summary>上一次读数时元件的动作态（快照，用于判断要不要重算）</summary>
        bool m_LastActuated;
        #endregion

        #region 对外属性
        /// <summary>屏幕当前显示的数值（未显示读数时为 0）。</summary>
        public float DisplayValue => currentValue;

        /// <summary>本次读数的精确值（含随机浮动前的值）。</summary>
        public float ExactValue => m_ExactValue;

        /// <summary>屏幕是否正显示一条"读数"（OL / Err / 关机时都是 false）。</summary>
        public bool HasReading { get; private set; }

        /// <summary>当前接入的点对是否登记在"正确接点组"里（没接入 / 没登记时为 false）。</summary>
        public bool IsPairCorrect { get; private set; }

        /// <summary>当前档位（旋钮未赋值时是 fallbackGear）。</summary>
        public MultimeterGearType GearType => knobObj != null && knobObj.GearCount > 0 ? knobObj.CurrentGearType : fallbackGear;

        /// <summary>当前档位的量程上限（0 = 不判超量程）。</summary>
        public float GearRange => knobObj != null && knobObj.GearCount > 0 ? knobObj.CurrentRange : 0f;

        /// <summary>红表笔当前吸附的点（未接入为 null）。</summary>
        public InspectionElementPointObj RedPoint => redPointObj;

        /// <summary>黑表笔当前吸附的点（未接入为 null）。</summary>
        public InspectionElementPointObj BlackPoint => blackPointObj;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            EventBus<InspectionProbeSnapEventData>.Subscribe(OnProbeSnapped);

            // WHY: 在写屏之前解析一次（此刻节点上的 Legacy Text 还在，GetComponent 稳定可用）—— 换过形态后再解析会碰上"假 null"。
            if (valueText != null) m_ValueTextComp = valueText.GetComponent<TextComponent>();

            // WHY: 判空连组件一起看 —— TMP 形态下 valueText 是"假 null"但组件在，只看字段会刷一条假的"未赋值"警告。
            if (valueText == null && m_ValueTextComp == null) Log.Warning($"{name}: valueText 未赋值，万用表不会显示任何读数");
            if (knobObj == null) Log.Warning($"{name}: knobObj 未赋值，档位固定为 {ChnNameMap.Get(fallbackGear)}（感应不到换档）");
            if (redProbeObj == null || blackProbeObj == null) Log.Warning($"{name}: 红/黑表笔有未赋值的，那一侧的接入不会被识别");

            m_LastGearKey = GearKey();
            RefreshReading();
        }

        void Update()
        {
            int gearKey = GearKey();
            bool actuated = m_CurrentElement != null && m_CurrentElement.IsActuated;

            // 档位换了、或当前元件的动作态翻转了（按下/松开试验按键）→ 整屏重算
            if (gearKey == m_LastGearKey && actuated == m_LastActuated) return;

            m_LastGearKey = gearKey;
            m_LastActuated = actuated;
            RefreshReading();
        }

        protected override void OnDestroy()
        {
            EventBus<InspectionProbeSnapEventData>.Unsubscribe(OnProbeSnapped);
            StopChange();
            base.OnDestroy();
        }

        /// <summary>档位快照：旋钮未赋值时为 -1（永远不变，屏幕只在接入变化时重算）。</summary>
        int GearKey() => knobObj != null ? knobObj.CurrentIndex : -1;
        #endregion

        #region 表笔接入（吸附驱动）
        /// <summary>表笔吸附态翻转 → 更新这一侧接在哪个点上；脱离吸附一律记为未接入，所以显示的是"此刻两支笔都放好在点上"的状态。</summary>
        void OnProbeSnapped(InspectionProbeSnapEventData e)
        {
            if (e == null) return;

            // 解除吸附（移开 / 被重新抓起）时 e.Point 是刚才那个点，这里要显式清成 null
            var point = e.IsSnapped ? e.Point : null;

            if (IsRedProbe(e.ProbeType)) redPointObj = point;
            else if (IsBlackProbe(e.ProbeType)) blackPointObj = point;
            else return;

            RefreshReading();
        }

        /// <summary>事件里的表笔是不是"红表笔"：优先按 Inspector 配的两个表笔实例比对，未配则按类型兜底。</summary>
        bool IsRedProbe(InspectionProbeType type)
        {
            return redProbeObj != null ? type == redProbeObj.ProbeType : type == InspectionProbeType.Red;
        }

        bool IsBlackProbe(InspectionProbeType type)
        {
            return blackProbeObj != null ? type == blackProbeObj.ProbeType : type == InspectionProbeType.Black;
        }
        #endregion

        #region 读数计算
        /// <summary>整屏重算（档位 / 表笔接入变化、初始化时调用）：按「档位 → 接入 → 数据」决定显示什么，失败路径都走 ShowText（不显示读数）。</summary>
        void RefreshReading()
        {
            var gear = GearType;

            // WHY: 每次重算先清 IsPairCorrect、只有明确命中"正确组"才置 true；不能在 ShowText 里清（正确组命中但读数为开路会走 ShowText 把结论误抹掉）。
            IsPairCorrect = false;
            m_CurrentElement = null;    // 本次没构成完整接入时，别让上一帧的元件继续参与动作态比对

            if (gear == MultimeterGearType.Off)
            {
                ShowText(powerOffText);
                return;
            }

            // 还没构成"两表笔都贴在点上"的完整接入
            if (redPointObj == null || blackPointObj == null)
            {
                ShowOpen(gear);
                return;
            }

            // 两支笔必须接在同一个元件的两个点上（跨元件 = 接错，按"两点之间没通路"处理）
            var redElement = redPointObj.GetComponentInParent<InspectionElementObj>();
            var blackElement = blackPointObj.GetComponentInParent<InspectionElementObj>();
            if (redElement == null || blackElement == null || redElement != blackElement)
            {
                ShowOpen(gear);
                return;
            }

            m_CurrentElement = redElement;
            var actState = redElement.ActuationState;   // 元件动作态：决定这一对点取静止组还是动作组

            var data = redElement.Data;
            if (data != null && ElementCheckPointPairing.TryFind(data.rightCheckPointDatas, redPointObj, blackPointObj, out var right))
            {
                IsPairCorrect = true;
                ShowCheckPointValue(gear, ElementCheckPointPairing.ResolveReading(right, actState));
                return;
            }

            if (data != null && ElementCheckPointPairing.TryFind(data.wrongCheckPointDatas, redPointObj, blackPointObj, out var wrong))
            {
                ShowCheckPointValue(gear, ElementCheckPointPairing.ResolveReading(wrong, actState));
                return;
            }

            // 配对表里没登记这对接点：就等于两点之间没有通路 —— 电阻类给 OL（∞）、电压/电流类给 0（不另造"接点错误"屏显，判定看 IsPairCorrect）。
            ShowOpen(gear);
        }

        /// <summary>「两点之间没有通路」的显示：电阻类档位 = 开路 OL（∞），电压 / 电流类 = 0；没接好、跨元件、配对表里没登记都走这里。</summary>
        void ShowOpen(MultimeterGearType gear)
        {
            if (IsResistanceGear(gear)) ShowText(openCircuitText);
            else ShowValue(0f);
        }

        /// <summary>把配对数据里的值按当前档位显示：电阻类遇到开路（openCircuit）显示 OL，读不出该量（只可能是关机档）也按没有通路处理。</summary>
        void ShowCheckPointValue(MultimeterGearType gear, in CheckPointData checkpoint)
        {
            if (checkpoint.openCircuit && IsResistanceGear(gear))
            {
                ShowText(overRangeText);    // 开路：真数字表就是 OL
                return;
            }

            if (TryGetGearValue(gear, checkpoint, out float value)) ShowValue(value);
            else ShowOpen(gear);
        }

        /// <summary>档位 → 取 <see cref="CheckPointData"/> 的哪个字段。Off 之外的档位都有对应量，不会失败。</summary>
        static bool TryGetGearValue(MultimeterGearType gear, in CheckPointData checkpoint, out float value)
        {
            switch (gear)
            {
                case MultimeterGearType.Resistance:
                case MultimeterGearType.Diode:
                case MultimeterGearType.Continuity:
                    value = checkpoint.resistance;
                    return true;
                case MultimeterGearType.VoltageDC:
                case MultimeterGearType.VoltageAC:
                    value = checkpoint.voltage;
                    return true;
                case MultimeterGearType.CurrentDC:
                case MultimeterGearType.CurrentAC:
                    value = checkpoint.current;
                    return true;
                default:
                    value = 0f;
                    return false;
            }
        }

        /// <summary>是否为电阻类档位（二极管 / 通断也按电阻处理：开路显示 OL）。</summary>
        static bool IsResistanceGear(MultimeterGearType gear)
        {
            return gear == MultimeterGearType.Resistance
                || gear == MultimeterGearType.Diode
                || gear == MultimeterGearType.Continuity;
        }
        #endregion

        #region 屏幕显示
        /// <summary>显示一条读数：先记下精确值，再加随机浮动作为显示目标并过渡过去；随机浮动只影响屏幕，ExactValue 始终是数据里的值。</summary>
        void ShowValue(float value)
        {
            m_ExactValue = value;
            HasReading = true;

            float target = value + Random.Range(-randomValueRange, randomValueRange);
            PlayChange(target);
        }

        /// <summary>直接把屏幕写成一段文本（关机 / 开路 / 超量程 / 接点未登记）并清掉"有数值读数"标记；不动 IsPairCorrect（由 RefreshReading 统一管）。</summary>
        void ShowText(string text)
        {
            StopChange();

            HasReading = false;
            m_ExactValue = 0f;

            if (m_ValueTextComp != null) m_ValueTextComp.SetText(text);
            else if (valueText != null) TextComponent.SetTextOn(valueText, text);
        }

        /// <summary>开始从当前显示值过渡到目标值（时长 <= 0 或组件未激活时直接跳）。</summary>
        void PlayChange(float target)
        {
            StopChange();

            if (valueTextChangeDuration <= 0f || !isActiveAndEnabled)
            {
                currentValue = target;
                SetShowText(currentValue);
                return;
            }

            m_ChangeCoroutine = StartCoroutine(ChangeTextAnim(target));
        }

        IEnumerator ChangeTextAnim(float value)
        {
            float from = currentValue;
            float time = 0f;

            while (time < valueTextChangeDuration)
            {
                time += Time.deltaTime;
                currentValue = Mathf.Lerp(from, value, Mathf.Clamp01(time / valueTextChangeDuration));
                SetShowText(currentValue);
                yield return null;
            }

            currentValue = value;
            SetShowText(currentValue);
            m_ChangeCoroutine = null;
        }

        void StopChange()
        {
            if (m_ChangeCoroutine == null) return;

            StopCoroutine(m_ChangeCoroutine);
            m_ChangeCoroutine = null;
        }

        /// <summary>把数值按当前档位格式化后写进屏幕（含单位）。</summary>
        void SetShowText(float value)
        {
            if (valueText == null && m_ValueTextComp == null) return;
            string text = FormatReading(value, GearType, GearRange);
            if (m_ValueTextComp != null) m_ValueTextComp.SetText(text);
            else TextComponent.SetTextOn(valueText, text);
        }

        /// <summary>数值 + 单位的格式化总入口：先判超量程，再按档位分派。</summary>
        string FormatReading(float value, MultimeterGearType gear, float range)
        {
            if (range > 0f && Mathf.Abs(value) > range) return overRangeText;

            switch (gear)
            {
                case MultimeterGearType.Resistance:
                case MultimeterGearType.Diode:
                case MultimeterGearType.Continuity:
                    return FormatResistance(value, range);
                case MultimeterGearType.VoltageDC:
                case MultimeterGearType.VoltageAC:
                    return FormatVoltage(value, range);
                case MultimeterGearType.CurrentDC:
                case MultimeterGearType.CurrentAC:
                    return FormatCurrent(value, range);
                default:
                    return powerOffText;
            }
        }

        /// <summary>电阻类：量程（或读数）≥1M 用 MΩ 三位小数、≥1k 用 kΩ 两位、否则 Ω 一位。</summary>
        static string FormatResistance(float value, float range)
        {
            float abs = Mathf.Abs(value);
            if (range >= 1e6f || (range <= 0f && abs >= 1e6f)) return Num(value / 1e6f, "0.000") + "MΩ";
            if (range >= 1e3f || (range <= 0f && abs >= 1e3f)) return Num(value / 1e3f, "0.00") + "kΩ";
            return Num(value, "0.0") + "Ω";
        }

        /// <summary>电压：200mV 档显示 mV，其余按 10 / 100 分档取 3 / 2 / 1 位小数。</summary>
        static string FormatVoltage(float value, float range)
        {
            if (range > 0f && range <= 0.2f) return Num(value * 1000f, "0.0") + "mV";

            float abs = Mathf.Abs(value);
            if (abs < 10f) return Num(value, "0.000") + "V";
            return abs < 100f ? Num(value, "0.00") + "V" : Num(value, "0.0") + "V";
        }

        /// <summary>电流：μA 档显示 μA、mA 档或读数 &lt; 1A 显示 mA，其余显示 A 三位小数。</summary>
        static string FormatCurrent(float value, float range)
        {
            if (range > 0f && range <= 0.001f) return Num(value * 1e6f, "0.0") + "μA";
            if (range > 0f && range <= 0.2f) return Num(value * 1000f, "0.0") + "mA";
            return Mathf.Abs(value) < 1f ? Num(value * 1000f, "0.0") + "mA" : Num(value, "0.000") + "A";
        }

        /// <summary>固定用不变文化格式化，避免不同区域设置下小数点/千分位不一样。</summary>
        static string Num(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
        #endregion
    }
}
