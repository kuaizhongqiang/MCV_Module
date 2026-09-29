
using System;

namespace MCV_Module.Models
{
    [Serializable]
    public class DataBase
    {
        public string id;
        public string displayName;
        public string displayNameEn;   // 英文列（空 = 回退中文）
        public string description;
    }
}