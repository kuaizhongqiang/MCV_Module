
using System;
using System.Collections.Generic;

namespace MCV_Module.Data.Project
{
    /// <summary>
    /// 连线数据 —— 连线端子配对、线组定义。
    /// TODO: M2 填入完整字段 —— 参考 Tuanjie LineConnectionData
    /// </summary>
    [Serializable]
    public class LineConnectionData
    {
        // TODO: M2 实现 —— 连线组的集合
        public List<LineGroupData> lineGroups = new List<LineGroupData>();

        // TODO: M2 实现 —— 连线完成触点事件
        // public UnityEvent onLineConnectionComplete;
    }

    /// <summary>
    /// 线组数据 —— 一组配对端子的定义。
    /// </summary>
    [Serializable]
    public class LineGroupData
    {
        // TODO: M2 实现 —— 线组名称 / 编号
        public string groupId;

        // TODO: M2 实现 —— 启动端与目标端
        // public LineEndpoint startEndpoint;
        // public LineEndpoint targetEndpoint;

        // TODO: M2 实现 —— 是否已连接
        public bool isConnected;
    }

    /// <summary>
    /// 端子元素类型（命名编码配对机制）。
    /// </summary>
    public enum ElementType
    {
        None,
        // TODO: M2 补充 —— 端子类型枚举值
        Socket,
        Plug,
        Terminal,
    }
}
