namespace MCV_Module.Models.Addressable
{
    /// <summary>
    /// 内容 AB 的命名规则 —— **唯一来源**（编辑器流水线与测试都从这里取，避免规则散落）。
    ///
    /// 规约：
    ///   - 包配置 id = <c>{器件}_{任务}_{资源}</c>（器件小驼峰、任务用 TaskType 小写、资源 <c>model</c> 或两位序号）；
    ///   - bundle 名 = <c>Content/clip_{器件小写}</c>，**文件名段必须小写**
    ///     （运行时 <c>GlobalAddressableMgr.GetBundleUrl</c> 会把末段强制小写，写盘却按原样 → 不一致时 WebGL/Linux 必然 404）。
    ///
    /// 注意：**包配置 id 用器件小驼峰，bundle 名用器件全小写**，两者是两套东西，别混。
    /// </summary>
    public static class ContentNaming
    {
        /// <summary>ProjectClip.id 的前缀。</summary>
        public const string ClipIdPrefix = "clip_";

        /// <summary>bundle 的输出目录段（= 相对 StreamingAssets 的一级目录）。</summary>
        public const string BundleDirName = "Content";

        /// <summary>bundle 文件名前缀。</summary>
        public const string BundleFilePrefix = "clip_";

        /// <summary>
        /// 任务段：TaskType 的枚举名小写。
        /// 注：CUR 的值 9 定名 <c>Measure</c>（LOW 侧同名值叫 <c>Inspection</c>），故这里是 <c>measure</c>。
        /// </summary>
        public const string TaskInfo = "info";
        public const string TaskStructure = "structure";
        public const string TaskPrinciple = "principle";
        public const string TaskMeasure = "measure";

        // CUR 既有 6 种任务中带模型的三种（单模型型；Equipment/Principle 是列表型，暂无 Provider）
        public const string TaskPurpose = "purpose";
        public const string TaskEquipment = "equipment";
        public const string TaskLineConnection = "lineconnection";
        public const string TaskTraining = "training";

        /// <summary>资源段：模型预制体。</summary>
        public const string ResourceModel = "model";

        /// <summary>clip_contactor → contactor（配置 id 用小驼峰，保持原样）。</summary>
        public static string DeviceOf(string clipId)
        {
            if (string.IsNullOrEmpty(clipId)) return clipId;
            // 用 global:: 限定：本命名空间位于 MCV_Module.Models 下，裸写 System 会被解析成
            // 同层级的 MCV_Module.Models.System（CS0234）
            return clipId.StartsWith(ClipIdPrefix, global::System.StringComparison.OrdinalIgnoreCase)
                ? clipId.Substring(ClipIdPrefix.Length)
                : clipId;
        }

        /// <summary>contactor → Content/clip_contactor（**文件名段小写**）。</summary>
        public static string BundleNameFor(string device)
        {
            return BundleDirName + "/" + (BundleFilePrefix + device).ToLowerInvariant();
        }

        /// <summary>包配置 id：device="contactor" task="info" resource="model" → contactor_info_model。</summary>
        public static string ConfigId(string device, string task, string resource)
        {
            return $"{device}_{task}_{resource}";
        }

        /// <summary>有序图的包配置 id：{器件}_info_{NN}（NN 从 01 起）。</summary>
        public static string InfoSpriteId(string device, int index)
        {
            return ConfigId(device, TaskInfo, index.ToString("D2"));
        }

        /// <summary>bundle 名的文件名段是否全小写（对账用；目录段不参与判断）。</summary>
        public static bool IsBundleFileNameLowerCase(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName)) return false;
            string fileName = bundleName;
            int slash = fileName.LastIndexOf('/');
            if (slash >= 0) fileName = fileName.Substring(slash + 1);
            return fileName == fileName.ToLowerInvariant();
        }
    }
}
