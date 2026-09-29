using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Objects
{
    /// <summary>场景对象最底层的标识基类：只有 id / 显示名 / 描述三个字段，无行为。</summary>
    public abstract class ObjectBase : MonoBehaviour
    {
        public string id;
        public string displayName;
        public string description;
    }
}