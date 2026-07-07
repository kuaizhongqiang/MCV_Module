using System;
using System.Collections.Generic;

namespace MCV_Module.Data.Project
{
    [Serializable]
    public class LineConnectionData
    {
        public List<LineGroupData> lineGroups = new List<LineGroupData>();

        /// <summary>获取指定线组的配对状态</summary>
        public bool IsGroupComplete(string groupId)
        {
            var group = lineGroups.Find(g => g.groupId == groupId);
            return group != null && group.isConnected;
        }

        /// <summary>所有线组是否都已连接</summary>
        public bool IsAllConnected()
        {
            return lineGroups.TrueForAll(g => g.isConnected);
        }
    }

    [Serializable]
    public class LineGroupData
    {
        public string groupId;

        /// <summary>启动端命名编码（命名约定: Line_GroupID_Start）</summary>
        public string startEndpointCode;

        /// <summary>目标端命名编码（命名约定: Line_GroupID_End）</summary>
        public string targetEndpointCode;

        /// <summary>是否已连接</summary>
        public bool isConnected;
    }
    [Serializable]
    public enum ElementType
    {
        None,
        Power,
        Fuse,
        Light,
    }
}
