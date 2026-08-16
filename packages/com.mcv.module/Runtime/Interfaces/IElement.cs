using UnityEngine;
using MCV_Module.Models;
using System.Collections.Generic;
using MCV_Module.Objects.Interactives.Elements;

namespace MCV_Module.Interfaces
{
    public interface IElement
    {
        DataBase Data{get;}
        ElementType Type{get;}
    }

    public interface IElePoint
    {
        GameObject CreateTmpLine();
        void UpdateTmpLine(GameObject line);
        void CreateLine();
        void DestroyLine();
    }

    public interface IEleLine
    {
        void EditLinePoint(List<ElementPointObj> points);
        void CreateLine();
        void DestroyLine();
    }
}
