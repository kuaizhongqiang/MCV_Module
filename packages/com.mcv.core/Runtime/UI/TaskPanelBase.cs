using System.Collections;
using System.Collections.Generic;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.UI
{
    /// <summary>任务面板基类：只加一个内容契约，供 GlobalUIMgr 组装 AI 上下文时读取。</summary>
    public abstract class TaskPanelBase : PanelBase
    {
        public abstract string GetPanelContent();
    }
}