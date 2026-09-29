namespace MCV_Module.Models.Addressable
{
    // WHY: 命名规则的唯一来源（编辑器流水线与测试都取这里）；包配置 id 用器件小驼峰、bundle 名用器件全小写是两套东西不能混，bundle 文件名段必须小写否则 WebGL/Linux 404。
    /// <summary>内容 AB 命名规则的唯一来源：配置 id、bundle 名、房间图标 id 的拼装与校验。</summary>
    public static class ContentNaming
    {
        /// <summary>ProjectClip.id 的前缀。</summary>
        public const string ClipIdPrefix = "clip_";

        /// <summary>bundle 的输出目录段（= 相对 StreamingAssets 的一级目录）。</summary>
        public const string BundleDirName = "Content";

        /// <summary>bundle 文件名前缀。</summary>
        public const string BundleFilePrefix = "clip_";

        /// <summary>任务段：TaskType 的枚举名小写（<c>inspection</c> 的界面显示名是「测量」）。</summary>
        public const string TaskInfo = "info";
        public const string TaskStructure = "structure";
        public const string TaskPrinciple = "principle";
        public const string TaskInspection = "inspection";

        /// <summary>资源段：模型预制体。</summary>
        public const string ResourceModel = "model";

        // WHY: 房间图标包是漫游房间共用的全局包（非一个 clip 一个包），clipId 一律留空 —— GlobalAddressableMgr.GetConfigsByClip 对空 clipId 返回空表，故不会被按 clip 的装卸链路带走。
        /// <summary>房间图标包的输出目录段（bundle = RoomOne/roomone）。</summary>
        public const string RoomOneBundleDirName = "RoomOne";

        /// <summary>房间图标包的 bundle 文件名段（**必须小写**，运行时会把末段强制小写）。</summary>
        public const string RoomOneBundleFileName = "roomone";

        /// <summary>房间图标包配置 id 的前缀。</summary>
        public const string RoomIconIdPrefix = "roomone_";

        /// <summary>bundle 名 = RoomOne/roomone（文件名段已小写，与运行时强制小写口径一致）。</summary>
        public static string RoomOneBundleName
        {
            get { return RoomOneBundleDirName + "/" + RoomOneBundleFileName; }
        }

        /// <summary>clip_contactor → roomone_contactor（房间图标的包配置 id）：由 clip.id 推导而非另立表，运行时与编辑器用同一条规则。</summary>
        public static string RoomIconId(string clipId)
        {
            return RoomIconIdOfDevice(DeviceOf(clipId));
        }

        /// <summary>contactor → roomone_contactor（已有器件段、无需先拼 clip.id 时用）。</summary>
        public static string RoomIconIdOfDevice(string device)
        {
            return RoomIconIdPrefix + device;
        }

        // WHY: UI 包是第三个全局包，与 RoomOne 同形态（clipId 留空）；但面板是"按 id 取件"的懒加载契约，故 id 由 prefab 名直接推导，不再另立一张表。
        /// <summary>UI 全局包的输出目录段（bundle = UI/ui）。</summary>
        public const string UIBundleDirName = "UI";

        /// <summary>UI 全局包的 bundle 文件名段（**必须小写**，运行时会把末段强制小写）。</summary>
        public const string UIBundleFileName = "ui";

        /// <summary>UI 包配置 id 的前缀。</summary>
        public const string UIPrefabIdPrefix = "ui_";

        /// <summary>bundle 名 = UI/ui（文件名段已小写，与运行时强制小写口径一致）。</summary>
        public static string UIBundleName
        {
            get { return UIBundleDirName + "/" + UIBundleFileName; }
        }

        /// <summary>面板 / 碎片 prefab 名 → 包配置 id（如 TipsPanel → ui_TipsPanel）。</summary>
        public static string UIPrefabId(string prefabName)
        {
            return UIPrefabIdPrefix + prefabName;
        }

        // WHY: 字体包是第四个全局包（前三个 CameraBg/camerabg、RoomOne/roomone、UI/ui），与它们同形态 —— clipId 一律留空 ⇒ 常驻、不会被按 clip 的装卸链路带走
        // （GlobalAddressableMgr.GetConfigsByClip 对空 clipId 返回空表）。
        /// <summary>字体全局包的输出目录段（bundle = Fonts/font）。</summary>
        public const string FontBundleDirName = "Fonts";

        /// <summary>字体全局包的 bundle 文件名段（**必须小写**，运行时会把末段强制小写，大小写不一致在 WebGL / Linux 上必然 404）。</summary>
        public const string FontBundleFileName = "font";

        /// <summary>字体包配置 id 的前缀。</summary>
        public const string FontAssetIdPrefix = "font_";

        /// <summary>Legacy 形态的槽位段（对应 <c>UnityEngine.Font</c>）。</summary>
        public const string FontSlotLegacy = "legacy";

        /// <summary>TMP 形态的槽位段（对应 <c>TMPro.TMP_FontAsset</c>）。</summary>
        public const string FontSlotTmp = "tmp";

        /// <summary>bundle 名 = Fonts/font（文件名段已小写，与运行时强制小写口径一致）。</summary>
        public static string FontBundleName
        {
            get { return FontBundleDirName + "/" + FontBundleFileName; }
        }

        // WHY: 一个 fontId 两种形态各占一条配置（不是一条两字段）—— 两形态是两份独立资产、运行期按不同泛型取件，合成一条就得再拆一次类型，故槽位直接进 id。
        /// <summary>fontId + 形态槽位 → 包配置 id（如 ("ui","legacy") → font_ui_legacy、("ui","tmp") → font_ui_tmp）。</summary>
        public static string FontAssetId(string fontId, string slot)
        {
            return FontAssetIdPrefix + fontId + "_" + slot;
        }

        /// <summary>clip_contactor → contactor（配置 id 用小驼峰，保持原样）。</summary>
        public static string DeviceOf(string clipId)
        {
            if (string.IsNullOrEmpty(clipId)) return clipId;
            // WHY: 用 global:: 限定 —— 本命名空间在 MCV_Module.Models 下，裸写 System 会被解析成同层的 MCV_Module.Models.System（CS0234）。
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
