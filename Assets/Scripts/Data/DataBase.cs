using System;


namespace MCV_Module.Data
{
    [Serializable]
    public class DataBase
    {
        public string id;
        public string displayName;
        public string description = "";  // AgentCanvas: CLI search/embedding text
    }
}
