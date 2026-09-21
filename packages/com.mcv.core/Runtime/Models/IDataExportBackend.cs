using MCV_Module.Utils;

namespace MCV_Module.Models
{
    /// <summary>
    /// 数据导出后端 —— 「SO 数据 → 目标介质」的**唯一出口**。
    ///
    /// 为什么要有这一层（§7.2 L-4）：数据真源是 <c>*DataSO.asset</c>，运行期只读导出产物；
    /// 将来若把运行期格式换成 Lua，只需替换本接口的实现（导出后端 + 读取后端两侧），
    /// 上层 <see cref="DataSO"/> 与编辑器流水线**一行都不用改**。
    /// </summary>
    public interface IDataExportBackend
    {
        /// <summary>文件名取数据类型名（如 <c>SystemData</c> → <c>SystemData.json</c>）。</summary>
        void Write<T>(string name, T data) where T : class;

        /// <summary>
        /// 只读对账（**绝不写盘**）：介质内容与本次数据一致时返回 null，否则返回差异摘要。
        /// </summary>
        string Diff<T>(string name, T data) where T : class;

        /// <summary>介质位置描述（日志与对账报告用，如 <c>StreamingAssets/Data/SystemData.json</c>）。</summary>
        string Describe(string name);
    }

    /// <summary>
    /// JSON 导出后端（当前唯一实现）：写 <c>StreamingAssets/Data/{name}.json</c>。
    /// 真正的读写在 <see cref="JsonReaderWriter"/> 里（同步 IO 仅 Editor；运行期读走 ReadAsync）。
    /// </summary>
    public sealed class JsonExportBackend : IDataExportBackend
    {
        public void Write<T>(string name, T data) where T : class
        {
#if UNITY_EDITOR
            JsonReaderWriter.Write(name, data, null);
#else
            // 运行期同步写盘只在设置类数据上允许（WebGL 会降级为只告警）
            JsonReaderWriter.WriteRuntime(name, data);
#endif
        }

        public string Diff<T>(string name, T data) where T : class
        {
            if (data == null) return $"{name}：数据为空，无法对账";

            string current = JsonReaderWriter.TryReadRaw(name);
            if (current == null) return $"{name}：介质上还没有该文件（未导出过）";

            string expected = JsonReaderWriter.Serialize(data);
            if (expected == null) return $"{name}：序列化失败";

            // 逐字符比较：SO 是唯一源，导出产物应当与 SO 完全一致（缩进/换行也一致）
            return current.Replace("\r\n", "\n") == expected.Replace("\r\n", "\n")
                ? null
                : $"{name}：介质内容与 SO 不一致（需要重新导出）";
        }

        public string Describe(string name)
        {
            return "StreamingAssets/Data/" + name + ".json";
        }
    }
}
