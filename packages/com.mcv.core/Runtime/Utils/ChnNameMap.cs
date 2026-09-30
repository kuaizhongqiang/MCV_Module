using System.Collections.Generic;
using MCV_Module.Models;

namespace MCV_Module.Utils
{
    // WHY: 枚举新增取值必须在这里补中文名，缺项会静默回退成英文枚举名（测试兜底已于 2026-09-30 移除，需人工核对）。
    /// <summary>枚举 → 中文名映射中心（仅供界面与操作记录显示用）。</summary>
    public static class ChnNameMap
    {
        /// <summary>元器件类型 → 中文名</summary>
        static readonly Dictionary<ElementType, string> ElementChn = new Dictionary<ElementType, string>()
        {
            {ElementType.None, "空类型"},
            {ElementType.Resistor, "电阻"},
            {ElementType.Capacitor, "电容"},
            {ElementType.Inductor, "电感"},
            {ElementType.Thermistor, "热继电器"},
            {ElementType.Fuse, "熔断器"},
            {ElementType.Contactor, "接触器"},
            {ElementType.ButtonSwitch, "按钮开关"},
            {ElementType.KnobSwitch, "旋钮开关"},
            {ElementType.SliderSwitch, "滑块开关"},
            {ElementType.Power, "电源"},
            {ElementType.Breaker, "断路器"},
            {ElementType.Relay, "继电器"},
            {ElementType.TimerRelay, "时间继电器"},
            {ElementType.Motor, "电动机"},
            {ElementType.Point, "点"},
            {ElementType.Line, "线"},
        };

        /// <summary>检测笔类型 → 中文名</summary>
        static readonly Dictionary<InspectionProbeType, string> ProbeChn = new Dictionary<InspectionProbeType, string>()
        {
            {InspectionProbeType.Red, "红表笔"},
            {InspectionProbeType.Black, "黑表笔"},
        };

        /// <summary>万用表档位 → 中文名</summary>
        static readonly Dictionary<MultimeterGearType, string> GearChn = new Dictionary<MultimeterGearType, string>()
        {
            {MultimeterGearType.Off, "关机"},
            {MultimeterGearType.Resistance, "电阻"},
            {MultimeterGearType.VoltageDC, "直流电压"},
            {MultimeterGearType.VoltageAC, "交流电压"},
            {MultimeterGearType.CurrentDC, "直流电流"},
            {MultimeterGearType.CurrentAC, "交流电流"},
            {MultimeterGearType.Diode, "二极管"},
            {MultimeterGearType.Continuity, "通断"},
        };

        /// <summary>元器件中文名（缺项回退枚举英文名，便于在界面上一眼看出漏配）。</summary>
        public static string Get(ElementType type)
        {
            return ElementChn.TryGetValue(type, out var name) ? name : type.ToString();
        }

        /// <summary>检测笔中文名（缺项回退枚举英文名）。</summary>
        public static string Get(InspectionProbeType type)
        {
            return ProbeChn.TryGetValue(type, out var name) ? name : type.ToString();
        }

        /// <summary>万用表档位中文名（缺项回退枚举英文名）。</summary>
        public static string Get(MultimeterGearType type)
        {
            return GearChn.TryGetValue(type, out var name) ? name : type.ToString();
        }
    }
}
