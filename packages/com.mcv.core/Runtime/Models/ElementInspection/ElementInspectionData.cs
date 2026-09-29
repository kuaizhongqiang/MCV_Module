
using System;
using System.Collections.Generic;
using MCV_Module.Objects.Interactives.TaskObj;
using UnityEngine;

namespace MCV_Module.Models.ElementInspection
{
    // WHY: 分「正确组 / 错误组」是因为同一对端子在不同检测页里正确性不同（接触器「检测1 触点」的正确对是动断触点对，「检测2 线圈」才是 A1-A2）；命中正确组置 IsPairCorrect，命中错误组读数照给但标错，两组都无则屏幕显示 Err。
    /// <summary>一个被检测元件的检测数据：记录两支表笔接上哪一对点时该读到什么（正确组 / 错误组）。</summary>
    [Serializable]
    public class InspectionData : DataBase
    {
        public List<CheckPointData> rightCheckPointDatas = new List<CheckPointData>();
        public List<CheckPointData> wrongCheckPointDatas = new List<CheckPointData>();
    }

    /// <summary>一对检测点的预期读数（电阻 / 电流 / 电压，单位 Ω / A / V）。</summary>
    [Serializable]
    public struct CheckPointData
    {
        public InspectionElementPointObj[] checkPoints;

        [Tooltip("电阻（Ω）：0 = 导通；开路请勾 openCircuit，不要靠填大数")]
        public float resistance;

        public float current;
        public float voltage;

        // WHY: 单列一个开关而不用 float.PositiveInfinity —— 教学数据里开路极常见（动合触点未吸合、线圈断路、熔体熔断），无穷大既没法在 Inspector 填、序列化/显示也不可靠。
        /// <summary>该接点对在此状态下开路（电阻无穷大）：电阻档屏幕显示 OL，不使用 resistance 的数字。</summary>
        [Tooltip("开路（电阻无穷大）：电阻档屏幕显示 OL")]
        public bool openCircuit;

        // WHY: 阻值不变的回路（线圈、绕组）就该不勾，动作前后读数一致；取哪一组由 ElementActuationState 决定，口径见 ElementCheckPointPairing.ResolveReading。
        /// <summary>动作态是否单独配置：勾上后元件已动作（按住试验按键 / 线圈通电）时改用下面那一组读数；不勾 = 动作前后不变。</summary>
        [Tooltip("动作态另配：已动作（按住试验按键 / 通电）时改用下面那一组读数；不勾 = 动作前后不变")]
        public bool hasActuated;

        [Tooltip("动作态电阻（Ω）：0 = 导通；开路请勾下面的动作态开路")]
        public float actuatedResistance;

        public float actuatedCurrent;
        public float actuatedVoltage;

        [Tooltip("动作态开路（电阻无穷大）：动作后该接点对断开")]
        public bool actuatedOpenCircuit;
    }
}
