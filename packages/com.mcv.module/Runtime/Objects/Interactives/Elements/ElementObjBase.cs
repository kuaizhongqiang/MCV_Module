
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
    public abstract class ElementObjBase : InteractiveBase, IElement
    {
        [SerializeField] protected DataBase data = new DataBase();
        public DataBase Data {get => data;}
        public virtual ElementType Type {get;}
        public bool isInit{get; set;} = false;
        
        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            StartCoroutine(DelayInit());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (ElementManagerBase.Instance != null) ElementManagerBase.Instance.UnregisterElement(this);
        }

        protected virtual IEnumerator DelayInit()
        {
            // 父级 Manager 的 Awake 先于子元素执行；此处仅在父级变更时重新查找，
            // 避免每帧 GetComponentInParent（场景元素首帧即解析，运行时实例化元素等待挂载）。
            var mgr = GetManagerCached();
            while (mgr == null)
            {
                yield return null;
                mgr = GetManagerCached();
            }

            gameObject.name = GetName();
            data.id = gameObject.name;
            mgr.RegisterElement(this);
            isInit = true;
        }

        /// <summary>按父级缓存管理器引用：父级未变化时不再做层级查找。</summary>
        ElementManagerBase GetManagerCached()
        {
            if (cachedParent != transform.parent)
            {
                cachedParent = transform.parent;
                cachedManager = transform.parent != null
                    ? transform.parent.GetComponentInParent<ElementManagerBase>()
                    : null;
            }
            return cachedManager;
        }

        Transform cachedParent;
        ElementManagerBase cachedManager;
        #endregion

        #region 接口实现
        protected override void MoEnterEvent()
        {
            
        }

        protected override void MoExitEvent()
        {
            
        }

        protected override void MoClickEvent()
        {
            
        }

        protected override void MoClickRightEvent()
        {
            
        }

        protected override void MoClickDoubleEvent()
        {
            
        }

        protected override void MoDownEvent()
        {
            
        }

        protected override void MoUpEvent()
        {
            
        }

        protected override void MoMoveEvent(Vector2 pos)
        {
            
        }

        #endregion

        protected virtual string GetName()
        {
            return ElementNameMap.GetName(Type) + data.displayName;
        }

        
        
    }

    
    // 元器件名称映射
    // 覆盖全部 ElementType / ElementPointNameType（含 Point/Line），
    // 反查使用双向索引（O(1)）；正查缺键时返回 "None" 而非抛异常。
    public static class ElementNameMap
    {
        static readonly Dictionary<ElementType, string> ElementRemap = new Dictionary<ElementType, string>()
        {
            {ElementType.None, "None"},
            {ElementType.Resistor, "R"},
            {ElementType.Capacitor, "C"},
            {ElementType.Inductor, "L"},
            {ElementType.Thermistor, "FR"},
            {ElementType.Fuse, "R"},
            {ElementType.Contactor, "KM"},
            {ElementType.ButtonSwitch, "SB"},
            {ElementType.KnobSwitch, "K"},
            {ElementType.SliderSwitch, "S"},
            {ElementType.Power, "P"},
            {ElementType.Breaker, "QS"},
            {ElementType.Relay, "KA"},
            {ElementType.TimerRelay, "KT"},
            {ElementType.Motor, "M"},
            {ElementType.Point, "点"},
            {ElementType.Line, "线"},
        };
        static readonly Dictionary<ElementPointNameType, string> PointRemap = new Dictionary<ElementPointNameType, string>()
        {
            {ElementPointNameType.None, "None"},
            {ElementPointNameType.One, "1"},
            {ElementPointNameType.Two, "2"},
            {ElementPointNameType.Three, "3"},
            {ElementPointNameType.Four, "4"},
            {ElementPointNameType.Five, "5"},
            {ElementPointNameType.Six, "6"},
            {ElementPointNameType.Seven, "7"},
            {ElementPointNameType.Eight, "8"},
            {ElementPointNameType.Input1, "1L1"},
            {ElementPointNameType.Input2, "3L2"},
            {ElementPointNameType.Input3, "5L3"},
            {ElementPointNameType.Output1, "2T1"},
            {ElementPointNameType.Output2, "4T2"},
            {ElementPointNameType.Output3, "6T3"},
            {ElementPointNameType.NO_In_1, "13NO"},
            {ElementPointNameType.NO_In_2, "15NO"},
            {ElementPointNameType.NO_In_3, "17NO"},
            {ElementPointNameType.NO_Out_1, "14NO"},
            {ElementPointNameType.NO_Out_2, "16NO"},
            {ElementPointNameType.NO_Out_3, "18NO"},
            {ElementPointNameType.NC_In_1, "21NC"},
            {ElementPointNameType.NC_In_2, "23NC"},
            {ElementPointNameType.NC_In_3, "25NC"},
            {ElementPointNameType.NC_Out_1, "22NC"},
            {ElementPointNameType.NC_Out_2, "24NC"},
            {ElementPointNameType.NC_Out_3, "26NC"},
            {ElementPointNameType.A1, "A1"},
            {ElementPointNameType.A2, "A2"},
            {ElementPointNameType.U1, "U1"},
            {ElementPointNameType.V1, "V1"},
            {ElementPointNameType.W1, "W1"},
            {ElementPointNameType.U2, "U2"},
            {ElementPointNameType.V2, "V2"},
            {ElementPointNameType.W2, "W2"},
            {ElementPointNameType.PE, "PE"},
            {ElementPointNameType.NinetyFive, "95"},
            {ElementPointNameType.NinetySix, "96"},
        };

        // 反向索引：名称 → 枚举。值重复时保留先插入的（与原线性扫描语义一致，如 Fuse/R 与 Resistor/R）。
        static readonly Dictionary<string, ElementType> ElementReverse;
        static readonly Dictionary<string, ElementPointNameType> PointReverse;

        static ElementNameMap()
        {
            ElementReverse = new Dictionary<string, ElementType>();
            foreach (var kv in ElementRemap)
                if (!ElementReverse.ContainsKey(kv.Value))
                    ElementReverse[kv.Value] = kv.Key;

            PointReverse = new Dictionary<string, ElementPointNameType>();
            foreach (var kv in PointRemap)
                if (!PointReverse.ContainsKey(kv.Value))
                    PointReverse[kv.Value] = kv.Key;
        }

        public static string GetName(ElementType elementType)
        {
            return ElementRemap.TryGetValue(elementType, out var name) ? name : "None";
        }

        public static ElementType GetElementType(string name)
        {
            return name != null && ElementReverse.TryGetValue(name, out var type) ? type : ElementType.None;
        }

        public static string GetName(ElementPointNameType pointNameType)
        {
            return PointRemap.TryGetValue(pointNameType, out var name) ? name : "None";
        }

        public static ElementPointNameType GetPointNameType(string name)
        {
            return name != null && PointReverse.TryGetValue(name, out var type) ? type : ElementPointNameType.None;
        }
    }

}
