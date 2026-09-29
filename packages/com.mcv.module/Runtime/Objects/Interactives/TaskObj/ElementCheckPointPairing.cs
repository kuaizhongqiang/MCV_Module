using System.Collections.Generic;
using System.Text;
using MCV_Module.Models;
using MCV_Module.Models.ElementInspection;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 配对规则只此一份——Editor 自动配对/清理与运行时查表都走这里，避免「编辑器生成的配对运行时查不到」的口径分歧。
    // WHY: 「一对」= checkPoints 里的两个点且顺序无关（红黑可对调），点数不为 2 或含空引用一律无效。
    // WHY: 配对按元件类型取表——不同电器端子号体系不同（按钮 1-2 是动断、时间继电器 1-3/1-4 才是延时），混一张表必配错；表里 kind 决定默认读数、配合元件动作态决定方向。
    /// <summary>检测点配对工具：按元件类型维护端子对表，提供点名规范化、点位分组、自动配对、查表与无效条目清理。</summary>
    public static class ElementCheckPointPairing
    {
        #region 端子对表（按元件类型）

        /// <summary>一对端子：两个端子的标号 + 类型（类型只影响默认读数）。</summary>
        public struct TerminalPair
        {
            public string a;
            public string b;
            public TerminalPairKind kind;

            public TerminalPair(string a, string b, TerminalPairKind kind)
            {
                this.a = a;
                this.b = b;
                this.kind = kind;
            }
        }

        static TerminalPair P(string a, string b, TerminalPairKind kind) => new TerminalPair(a, b, kind);

        // ── 通用标号（多个电器共用）──────────────────────────────

        /// <summary>线圈 A1-A2（接触器 / 继电器 / 时间继电器）——阻值按实物填</summary>
        static readonly TerminalPair[] Coil = { P("A1", "A2", TerminalPairKind.Other) };

        /// <summary>主触点（接触器 / 热继电器）：1L1-2T1、3L2-4T2、5L3-6T3，静止断开</summary>
        static readonly TerminalPair[] MainContacts =
        {
            P("1L1", "2T1", TerminalPairKind.NoContact),
            P("3L2", "4T2", TerminalPairKind.NoContact),
            P("5L3", "6T3", TerminalPairKind.NoContact),
        };

        /// <summary>动合辅助触点：IEC 标号 13NO-14NO / 15NO-16NO / 17NO-18NO，另有 53NO-54NO / 83NO-84NO 体系</summary>
        static readonly TerminalPair[] AuxNo =
        {
            P("13NO", "14NO", TerminalPairKind.NoContact),
            P("15NO", "16NO", TerminalPairKind.NoContact),
            P("17NO", "18NO", TerminalPairKind.NoContact),
            P("53NO", "54NO", TerminalPairKind.NoContact),
            P("83NO", "84NO", TerminalPairKind.NoContact),
        };

        /// <summary>动断辅助触点：IEC 标号 21NC-22NC / 23NC-24NC / 25NC-26NC，另有 61NC-62NC / 71NC-72NC 体系</summary>
        static readonly TerminalPair[] AuxNc =
        {
            P("21NC", "22NC", TerminalPairKind.NcContact),
            P("23NC", "24NC", TerminalPairKind.NcContact),
            P("25NC", "26NC", TerminalPairKind.NcContact),
            P("61NC", "62NC", TerminalPairKind.NcContact),
            P("71NC", "72NC", TerminalPairKind.NcContact),
        };

        /// <summary>电机的三相绕组（U1-U2 / V1-V2 / W1-W2）——阻值按实物填</summary>
        static readonly TerminalPair[] MotorWindings =
        {
            P("U1", "U2", TerminalPairKind.Other),
            P("V1", "V2", TerminalPairKind.Other),
            P("W1", "W2", TerminalPairKind.Other),
        };

        /// <summary>热继电器：热元件（正常导通）+ 动断 95-96 + 动合 97-98（报警）</summary>
        static readonly TerminalPair[] ThermalRelay =
        {
            P("1L1", "2T1", TerminalPairKind.NcContact),
            P("3L2", "4T2", TerminalPairKind.NcContact),
            P("5L3", "6T3", TerminalPairKind.NcContact),
            P("95", "96", TerminalPairKind.NcContact),
            P("97", "98", TerminalPairKind.NoContact),
        };

        /// <summary>时间继电器：线圈 A1-A2 与 JS7-A 的 2-7；延时触点 1-3 / 1-4、55NO-56NO / 65NC-66NC（通断取决于延时型式，不猜默认值）；1 号端子两对共用。</summary>
        static readonly TerminalPair[] TimerRelayContacts =
        {
            P("2", "7", TerminalPairKind.Other),
            P("1", "3", TerminalPairKind.Other),
            P("1", "4", TerminalPairKind.Other),
            P("55NO", "56NO", TerminalPairKind.Other),
            P("65NC", "66NC", TerminalPairKind.Other),
        };

        /// <summary>按钮开关：动断 1-2（静止闭合）、动合 3-4（静止断开），复合按钮另有 13NO-14NO / 21NC-22NC</summary>
        static readonly TerminalPair[] ButtonContacts =
        {
            P("1", "2", TerminalPairKind.NcContact),
            P("3", "4", TerminalPairKind.NoContact),
            P("13NO", "14NO", TerminalPairKind.NoContact),
            P("21NC", "22NC", TerminalPairKind.NcContact),
        };

        /// <summary>旋钮开关 / 滑块开关（转换开关）：同一端子对在不同档位通断不同，不猜默认值。</summary>
        static readonly TerminalPair[] SwitchContacts =
        {
            P("1", "2", TerminalPairKind.Other),
            P("3", "4", TerminalPairKind.Other),
            P("5", "6", TerminalPairKind.Other),
        };

        /// <summary>熔断器：熔体两端（正常导通，熔断后开路）</summary>
        static readonly TerminalPair[] FuseBody = { P("1", "2", TerminalPairKind.NcContact) };

        /// <summary>断路器：每极进出两端（合闸导通 / 分闸断开），N 极同样</summary>
        static readonly TerminalPair[] BreakerPoles =
        {
            P("1", "2", TerminalPairKind.NcContact),
            P("3", "4", TerminalPairKind.NcContact),
            P("5", "6", TerminalPairKind.NcContact),
            P("N1", "N2", TerminalPairKind.NcContact),
        };

        /// <summary>两端没有专门标号的元件（电阻 / 电容 / 电感）</summary>
        static readonly TerminalPair[] TwoTerminal = { P("1", "2", TerminalPairKind.Other) };

        /// <summary>空表（不做测量的元件，如电源）</summary>
        static readonly TerminalPair[] EmptyPairs = new TerminalPair[0];

        /// <summary>通用表：元件类型没有专属表时的兜底（线圈 + 主触点 + 动合动断辅助触点 + 三相绕组）</summary>
        static readonly TerminalPair[] CommonPairs = Concat(Coil, MainContacts, AuxNo, AuxNc, MotorWindings);

        static readonly Dictionary<ElementType, TerminalPair[]> s_PairsByElement;
        static readonly Dictionary<ElementType, Dictionary<string, List<int>>> s_IndexByElement;
        /// <summary>查询用的临时列表（避免每次查询都 new；单线程使用足够）</summary>
        static readonly List<int> s_TempIndices = new List<int>();

        static ElementCheckPointPairing()
        {
            s_PairsByElement = BuildPairsByElement();
            s_IndexByElement = BuildIndexByElement();
        }

        /// <summary>每种元件类型一张表：新增元件类型一定要在这里补一张，漏了会退回通用表、认不出专属标号。</summary>
        static Dictionary<ElementType, TerminalPair[]> BuildPairsByElement()
        {
            return new Dictionary<ElementType, TerminalPair[]>
            {
                { ElementType.None, CommonPairs },
                { ElementType.Resistor, TwoTerminal },
                { ElementType.Capacitor, TwoTerminal },
                { ElementType.Inductor, TwoTerminal },
                { ElementType.Thermistor, ThermalRelay },
                { ElementType.Fuse, FuseBody },
                { ElementType.Contactor, Concat(Coil, MainContacts, AuxNo, AuxNc) },
                { ElementType.ButtonSwitch, ButtonContacts },
                { ElementType.KnobSwitch, SwitchContacts },
                { ElementType.SliderSwitch, SwitchContacts },
                { ElementType.Power, EmptyPairs },
                { ElementType.Breaker, BreakerPoles },
                { ElementType.Relay, Concat(Coil, AuxNo, AuxNc) },
                { ElementType.TimerRelay, Concat(Coil, TimerRelayContacts, AuxNo, AuxNc) },
                { ElementType.Motor, MotorWindings },
                { ElementType.Point, EmptyPairs },
                { ElementType.Line, EmptyPairs },
            };
        }

        static Dictionary<ElementType, Dictionary<string, List<int>>> BuildIndexByElement()
        {
            var map = new Dictionary<ElementType, Dictionary<string, List<int>>>();
            foreach (var kv in s_PairsByElement) map[kv.Key] = BuildIndex(kv.Value);
            return map;
        }

        /// <summary>规范化点名/标号 → 表内下标列表：同一端子可能出现在好几对里（如 JS7-A 的 1 号端子同属 1-3 与 1-4），一个点可同时属于多对。</summary>
        static Dictionary<string, List<int>> BuildIndex(TerminalPair[] pairs)
        {
            var index = new Dictionary<string, List<int>>();
            if (pairs == null) return index;

            for (int i = 0; i < pairs.Length; i++)
            {
                AddIndex(index, NormalizePointName(pairs[i].a), i);
                AddIndex(index, NormalizePointName(pairs[i].b), i);
            }
            return index;
        }

        static void AddIndex(Dictionary<string, List<int>> index, string key, int pairIndex)
        {
            if (key.Length == 0) return;

            if (!index.TryGetValue(key, out var list))
            {
                list = new List<int>();
                index.Add(key, list);
            }
            if (!list.Contains(pairIndex)) list.Add(pairIndex);
        }

        static TerminalPair[] Concat(params TerminalPair[][] tables)
        {
            var list = new List<TerminalPair>();
            for (int i = 0; i < tables.Length; i++)
            {
                if (tables[i] == null) continue;
                list.AddRange(tables[i]);
            }
            return list.ToArray();
        }

        /// <summary>取该元件类型要用的端子对表（没有专属表的类型退回通用表）。</summary>
        public static TerminalPair[] GetTerminalPairs(ElementType elementType)
        {
            return s_PairsByElement.TryGetValue(elementType, out var pairs) ? pairs : CommonPairs;
        }
        #endregion

        #region 点位名

        /// <summary>规范化点名：只留下字母和数字并统一大写（忽略空格 / 括号 / 连字符 / 下划线）。</summary>
        public static string NormalizePointName(string pointName)
        {
            if (string.IsNullOrEmpty(pointName)) return string.Empty;

            var sb = new StringBuilder(pointName.Length);
            for (int i = 0; i < pointName.Length; i++)
            {
                char c = char.ToUpperInvariant(pointName[i]);
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>按元件类型 + 点名收集该点所属的端子对下标（可能不止一对，共用端子），识别不到时结果为空。</summary>
        public static void CollectTerminalPairIndices(string pointName, ElementType elementType, List<int> results)
        {
            results?.Clear();

            string key = NormalizePointName(pointName);
            if (results == null || key.Length == 0) return;

            if (!s_IndexByElement.TryGetValue(elementType, out var indexMap)) indexMap = s_IndexByElement[ElementType.None];
            if (!indexMap.TryGetValue(key, out var indices)) return;

            results.AddRange(indices);
        }

        /// <summary>按元件类型 + 点名查端子对下标（只看第一对）；识别不到返回 false。</summary>
        public static bool TryGetTerminalPairIndex(string pointName, ElementType elementType, out int index)
        {
            index = -1;

            CollectTerminalPairIndices(pointName, elementType, s_TempIndices);
            if (s_TempIndices.Count == 0) return false;

            index = s_TempIndices[0];
            return true;
        }

        /// <summary>该点属于本元件类型的哪一组端子对（共用端子时取第一对）；不在表里返回 -1。</summary>
        public static int ResolveTerminalPairIndex(InspectionElementPointObj point, ElementType elementType)
        {
            if (point == null) return -1;
            return TryGetTerminalPairIndex(point.name, elementType, out int index) ? index : -1;
        }

        /// <summary>端子对显示名，如「1L1-2T1」（Inspector 的配对预览用）。</summary>
        public static string GetTerminalPairLabel(ElementType elementType, int index)
        {
            var pairs = GetTerminalPairs(elementType);
            if (index < 0 || index >= pairs.Length) return string.Empty;

            return $"{pairs[index].a}-{pairs[index].b}";
        }
        #endregion

        #region 收集与配对

        /// <summary>收集 root 下所有检测点（含未激活物体——未激活点不会注册进 GlobalInteractiveMgr 但仍要参与配对），顺序 = GetComponentsInChildren 深度优先，扫描结果稳定。</summary>
        public static void CollectChildPoints(GameObject root, List<InspectionElementPointObj> results)
        {
            if (results == null) return;
            results.Clear();
            if (root == null) return;

            results.AddRange(root.GetComponentsInChildren<InspectionElementPointObj>(true));
        }

        /// <summary>按端子对把点分组（groups 键 = 下标、值 = 落在该对上的点）；识别不到的点与该组只放一个点的都记进 unmatched。Inspector 预览与自动配对共用，面板写几对点下去就是几对。</summary>
        public static void CollectTerminalGroups(IList<InspectionElementPointObj> points, ElementType elementType,
            Dictionary<int, List<InspectionElementPointObj>> groups, List<InspectionElementPointObj> unmatched)
        {
            groups?.Clear();
            unmatched?.Clear();
            if (points == null) return;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point == null) continue;

                CollectTerminalPairIndices(point.name, elementType, s_TempIndices);
                if (groups == null || s_TempIndices.Count == 0)
                {
                    unmatched?.Add(point);
                    continue;
                }

                // 共用端子的点要落进它参与的**每一对**里（如 JS7-A 时间继电器的 1 号端子同时属于 1-3 与 1-4）
                for (int j = 0; j < s_TempIndices.Count; j++)
                {
                    int index = s_TempIndices[j];
                    if (!groups.TryGetValue(index, out var bucket))
                    {
                        bucket = new List<InspectionElementPointObj>();
                        groups.Add(index, bucket);
                    }
                    bucket.Add(point);
                }
            }

            if (groups == null) return;

            // 只放 1 个点的端子对配不成对、整个拆掉；其中没有落到任何其它配对的点才算落单（共用端子的点可能已配在别处，别误报）
            var lonely = new List<int>();
            foreach (var kv in groups)
            {
                if (kv.Value.Count >= 2) continue;
                lonely.Add(kv.Key);
            }

            for (int i = 0; i < lonely.Count; i++)
            {
                var bucket = groups[lonely[i]];
                groups.Remove(lonely[i]);

                for (int j = 0; j < bucket.Count; j++)
                {
                    if (IsInAnyGroup(groups, bucket[j])) continue;
                    unmatched?.Add(bucket[j]);
                }
            }
        }

        /// <summary>该点是不是还落在某个（已确认成立的）端子对里。</summary>
        static bool IsInAnyGroup(Dictionary<int, List<InspectionElementPointObj>> groups, InspectionElementPointObj point)
        {
            foreach (var kv in groups)
            {
                if (kv.Value.Contains(point)) return true;
            }
            return false;
        }

        /// <summary>按端子对自动配对：同组两个端子配成一对补进 target（已有跳过、不覆盖已填读数），新条目按端子对类型填默认读数、生成顺序 = 表顺序；unmatched 收下识别不到 / 落单的点。</summary>
        public static int BuildTerminalPairs(IList<InspectionElementPointObj> points, ElementType elementType,
            List<CheckPointData> target, List<CheckPointData> excluded = null,
            List<InspectionElementPointObj> unmatched = null)
        {
            var groups = new Dictionary<int, List<InspectionElementPointObj>>();
            CollectTerminalGroups(points, elementType, groups, unmatched);

            var pairs = GetTerminalPairs(elementType);
            int added = 0;

            for (int i = 0; i < pairs.Length; i++)
            {
                if (!groups.TryGetValue(i, out var bucket)) continue;

                for (int x = 0; x < bucket.Count; x++)
                {
                    for (int y = x + 1; y < bucket.Count; y++)
                    {
                        var a = bucket[x];
                        var b = bucket[y];
                        if (ContainsPair(target, a, b)) continue;
                        if (excluded != null && ContainsPair(excluded, a, b)) continue;

                        target.Add(CreatePair(a, b, pairs[i].kind));
                        added++;
                    }
                }
            }
            return added;
        }

        /// <summary>建一条配对并填默认读数（只用于**新生成**的条目）。</summary>
        static CheckPointData CreatePair(InspectionElementPointObj a, InspectionElementPointObj b,
            TerminalPairKind kind)
        {
            var checkpoint = new CheckPointData { checkPoints = new[] { a, b } };
            ApplyDefaultReading(ref checkpoint, kind);
            return checkpoint;
        }

        /// <summary>按端子对类型填默认读数——静止与动作两组一起填，运行时由动作态决定取哪组：动合静止断开→动作导通，动断相反；线圈与三相绕组不猜（不置 hasActuated）。</summary>
        static void ApplyDefaultReading(ref CheckPointData checkpoint, TerminalPairKind kind)
        {
            switch (kind)
            {
                case TerminalPairKind.NoContact:
                    checkpoint.openCircuit = true;          // 静止：∞
                    checkpoint.hasActuated = true;
                    checkpoint.actuatedResistance = 0f;     // 动作：导通
                    checkpoint.actuatedOpenCircuit = false;
                    break;

                case TerminalPairKind.NcContact:
                    checkpoint.resistance = 0f;             // 静止：导通
                    checkpoint.hasActuated = true;
                    checkpoint.actuatedResistance = 0f;
                    checkpoint.actuatedOpenCircuit = true;  // 动作：∞
                    break;

                default:
                    break;
            }
        }

        /// <summary>取这一对点在给定状态下该读到的值：元件已动作且该对配了动作态（hasActuated）才用动作态那组，否则沿用静态；返回已投影好的副本，消费方无需关心状态。</summary>
        public static CheckPointData ResolveReading(CheckPointData checkpoint, ElementActuationState state)
        {
            if (state != ElementActuationState.Actuated || !checkpoint.hasActuated) return checkpoint;

            var resolved = checkpoint;
            resolved.resistance = checkpoint.actuatedResistance;
            resolved.current = checkpoint.actuatedCurrent;
            resolved.voltage = checkpoint.actuatedVoltage;
            resolved.openCircuit = checkpoint.actuatedOpenCircuit;
            return resolved;
        }

        /// <summary>把 points 两两全组合（i &lt; j，顺序即数组顺序）补进 target：已有 / excluded 里已登记的跳过、不覆盖已填读数，返回新增条数；全组合 n 个点有 n(n−1)/2 条且不填默认读数，只适合 3~4 个点的小元件，正常元件用 BuildTerminalPairs。</summary>
        public static int BuildAllPairs(IList<InspectionElementPointObj> points,
            List<CheckPointData> target, List<CheckPointData> excluded = null)
        {
            if (points == null || target == null) return 0;

            int added = 0;
            for (int i = 0; i < points.Count; i++)
            {
                var a = points[i];
                if (a == null) continue;

                for (int j = i + 1; j < points.Count; j++)
                {
                    var b = points[j];
                    if (b == null || a == b) continue;
                    if (ContainsPair(target, a, b)) continue;
                    if (excluded != null && ContainsPair(excluded, a, b)) continue;

                    target.Add(new CheckPointData { checkPoints = new[] { a, b } });
                    added++;
                }
            }
            return added;
        }
        #endregion

        #region 查表与清理

        /// <summary>表里有没有这一对（顺序无关）。</summary>
        public static bool ContainsPair(List<CheckPointData> list, InspectionElementPointObj a, InspectionElementPointObj b)
        {
            return TryFind(list, a, b, out _);
        }

        /// <summary>按点对查表（顺序无关：红黑对调也算命中）。</summary>
        public static bool TryFind(List<CheckPointData> list, InspectionElementPointObj a, InspectionElementPointObj b,
            out CheckPointData result)
        {
            result = default;
            if (list == null || a == null || b == null) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (!IsSamePair(list[i], a, b)) continue;

                result = list[i];
                return true;
            }
            return false;
        }

        /// <summary>这条配对是不是 (a, b) 这一对（顺序无关；点数不是 2 或含空引用都不算）。</summary>
        public static bool IsSamePair(in CheckPointData data, InspectionElementPointObj a, InspectionElementPointObj b)
        {
            var points = data.checkPoints;
            if (points == null || points.Length != 2 || a == null || b == null) return false;

            return (points[0] == a && points[1] == b) || (points[0] == b && points[1] == a);
        }

        /// <summary>清理无效条目（点数不是 2、含空引用——点物体被删后表里会留下 Missing），返回移除条数；Editor 自动配对前先跑一遍。</summary>
        public static int RemoveInvalidPairs(List<CheckPointData> list)
        {
            if (list == null) return 0;

            int removed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var points = list[i].checkPoints;
                if (points != null && points.Length == 2 && points[0] != null && points[1] != null) continue;

                list.RemoveAt(i);
                removed++;
            }
            return removed;
        }
        #endregion
    }
}
