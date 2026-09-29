using System.IO;
using MCV_Module.Models.Addressable;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    // WHY: 共享内核 —— 同一路径字面量此前散落在 ContentBundleTools / CameraBgBundleTools / SceneAddressableTools / DataSOExporter 等处，换目录要逐个文件找；这里只放当前真实被引用的路径，不预先罗列将来可能用到的路径。
    /// <summary>Editor 工具链的工程路径常量 —— 唯一来源。</summary>
    public static class EditorPaths
    {
        /// <summary>内容 AB 的输入数据；运行时侧由 <c>JsonReaderWriter.FULL_PATH</c> 按 <c>streamingAssetsPath + "/Data/" + name + ".json"</c> 拼出同一文件。</summary>
        public const string ProjectDataJson = "Assets/StreamingAssets/Data/ProjectData.json";

        /// <summary>包配置主清单（<c>PackageDatabaseSO.AutoCollect</c> 的落点）；运行时经 <c>Resources.Load&lt;PackageDatabaseSO&gt;("Config/PackageDB_Master")</c> 加载，路径即契约。</summary>
        public const string PackageDbAsset = "Assets/Resources/Config/PackageDB_Master.asset";

        /// <summary>编译就绪守卫盯的脚本；它没编译完时 <c>CreateAsset</c> 会写出 <c>m_Script:{fileID:0}</c> 的坏资产。</summary>
        public const string AbConfigScript = "Assets/Scripts/Models/Addressable/ABPackageConfigSO.cs";

        /// <summary>内容包配置目录（一个 clip 的资源 → 多个 <c>AB_{id}.asset</c>）。</summary>
        public const string ContentConfigDir = "Assets/Resources/Config/ContentPackages";

        /// <summary>全局包配置目录（CameraBg 这类非内容资源）。</summary>
        public const string CameraBgConfigDir = "Assets/Resources/Config/CameraBgPackages";

        /// <summary>房间图标全局包配置目录（RoomOne：漫游房间 9 块项目 HUD 的图标，非内容资源）。</summary>
        public const string RoomOneConfigDir = "Assets/Resources/Config/RoomOnePackages";

        // ── B1.5：UI 全局包（面板 + 碎片 prefab 打成一包，bundle UI/ui）的路径 ──
        /// <summary>面板 prefab 目录（文件名 = 面板类名，id = <c>ui_{文件名}</c>；由工具扫描产出条目，不写死路径）。</summary>
        public const string UIPanelPrefabDir = "Assets/Prefabs/UI/Panels";

        /// <summary>碎片 prefab 目录（与面板同形，同样按 id 取件）。</summary>
        public const string UIFragmentPrefabDir = "Assets/Prefabs/UI/Fragments";

        /// <summary>UI 全局包配置目录（与内容包 / CameraBg / RoomOne 各占一个目录同口径）。</summary>
        public const string UIConfigDir = "Assets/Resources/Config/UIPackages";

        /// <summary>AssetBundle 输出根（相对工程根）。</summary>
        public const string StreamingAssetsRoot = "Assets/StreamingAssets";

        /// <summary>内容 bundle 的输出目录段 —— 直接引用 <see cref="ContentNaming.BundleDirName"/>，保证「运行时拼 URL」与「工具写盘」用同一常量。</summary>
        public const string ContentBundleDirName = ContentNaming.BundleDirName;

        /// <summary>CameraBg bundle 的输出目录段。</summary>
        public const string CameraBgBundleDirName = "CameraBg";

        /// <summary>RoomOne bundle 的输出目录段 —— 引用 <see cref="ContentNaming.RoomOneBundleDirName"/>，保证「运行时拼 URL」与「工具写盘」用同一常量（同 <see cref="ContentBundleDirName"/> 的口径）。</summary>
        public const string RoomOneBundleDirName = ContentNaming.RoomOneBundleDirName;

        /// <summary>B1.5：UI bundle 的输出目录段 —— 引用 <see cref="ContentNaming.UIBundleDirName"/>，保证「运行时拼 URL」与「工具写盘」用同一常量（同 <see cref="ContentBundleDirName"/> 的口径）。</summary>
        public const string UIBundleDirName = ContentNaming.UIBundleDirName;

        // ── B3：字体全局包（Legacy TTF + TMP SDF 打成一包，bundle Fonts/font）的路径 ──
        /// <summary>字体全局包配置目录（与内容包 / CameraBg / RoomOne / UI 各占一个目录同口径）。</summary>
        public const string FontConfigDir = "Assets/Resources/Config/FontPackages";

        /// <summary>字体资产目录：Legacy <c>.ttf</c> 与 TMP <c>SDF.asset</c> 都在这里。仅供注释 / 人工核对 —— 条目路径由 <c>FontCatalogSO</c> 逐条给出，本常量不参与收集。</summary>
        public const string FontAssetDir = "Assets/Fonts";

        /// <summary>B3：字体 bundle 的输出目录段 —— 引用 <see cref="ContentNaming.FontBundleDirName"/>，保证「运行时拼 URL」与「工具写盘」用同一常量（同 <see cref="ContentBundleDirName"/> 的口径）。</summary>
        public const string FontBundleDirName = ContentNaming.FontBundleDirName;

        /// <summary>旧产物（P1 迁移遗留）：旧配置目录。迁移已完成，实测这些目录都不存在。</summary>
        public static readonly string[] LegacyConfigDirs =
        {
            "Assets/Resources/Config/InfoPackages",        // 旧简介图集配置（48 条）
            "Assets/Resources/Config/InfoObjPackages",     // 旧简介模型配置（曾计划，实际未产出）
        };

        /// <summary>旧产物（P1 迁移遗留）：旧 bundle 目录。迁移已完成，实测这些目录都不存在。</summary>
        public static readonly string[] LegacyBundleDirs =
        {
            "Assets/StreamingAssets/Info",                 // 旧简介图集包（8 个）
            "Assets/StreamingAssets/InfoModel",            // 旧简介模型包（曾计划，实际未产出）
        };

        /// <summary>构建中转目录（Temp 下，不入库）：<c>Temp/MCV_{name}</c>；铁律②：先构建到 Temp，只有 bundle 本体被拷进 StreamingAssets（不带 .manifest）。</summary>
        public static string TempRoot(string name)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/MCV_" + name));
        }
    }
}
