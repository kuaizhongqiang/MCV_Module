using UnityEngine;
using MCV_Module.Models;
using System.Collections.Generic;
using MCV_Module.Objects.Interactives.Elements;

namespace MCV_Module.Interfaces
{
    // WHY: 元件必须持有 DataBase 并暴露 ElementType，命名/注册/反查都依赖它，不能缺。
    /// <summary>电路元件契约：暴露数据模型与元件类型。</summary>
    public interface IElement
    {
        /// <summary>元件数据模型。</summary>
        DataBase Data{get;}
        /// <summary>元件类型。</summary>
        ElementType Type{get;}
    }

    // WHY: CreateTmpLine 返回的临时线必须交给 UpdateTmpLine 逐帧更新，再用 CreateLine 提交或 DestroyLine 丢弃，否则残留对象。
    /// <summary>接线端子契约：拖线预览、临时线与成线。</summary>
    public interface IElePoint
    {
        /// <summary>创建从本点出发的临时拖线，返回供更新的对象。</summary>
        GameObject CreateTmpLine();
        /// <summary>更新临时拖线。</summary>
        void UpdateTmpLine(GameObject line);
        /// <summary>结束连线，生成正式导线。</summary>
        void CreateLine();
        /// <summary>销毁临时/正式导线。</summary>
        void DestroyLine();
    }

    // WHY: 点列表首尾即导线命名与 Matches 判定依据，顺序/空项错会连带名字与碰撞错乱。
    /// <summary>已连接导线契约：编辑端点与建/清导线。</summary>
    public interface IEleLine
    {
        /// <summary>设置导线的端子点列表（顺序即首尾）。</summary>
        void EditLinePoint(List<ElementPointObj> points);
        /// <summary>创建/重建导线网格。</summary>
        void CreateLine();
        /// <summary>销毁导线网格。</summary>
        void DestroyLine();
    }
}
