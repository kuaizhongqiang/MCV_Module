
using System.Collections.Generic;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
    public class ElementResistorObj : ElementObjBase
    {
        List<ElementPointObj> points = new List<ElementPointObj>();
        public List<ElementPointObj> Points {get => points;}
        public override ElementType Type => ElementType.Resistor;
    }
}
