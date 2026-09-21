using System.IO;
using MCV_Module.Models.Addressable;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    /// <summary>
    /// Editor 工具链的工程路径常量 —— **唯一来源**。
    ///
    /// 收敛对象：此前同一路径字面量散落在 <c>ContentBundleTools</c> / <c>SceneAddressableTools</c> /
    /// <c>DataSOExporter</c> 等多处，换目录要逐个文件找。这里只放**当前真实被引用的**路径。
    ///
    /// ⚠ 本文件按 CUR 实情裁剪：LOW 版另有 CameraBg / RoomOne / Font / Legacy 四组路径，
    /// 对应「未移植的全局包与多语言字体」（§9 / X-2），本工程不引入。
    /// </summary>
    public static class EditorPaths
    {
        /// <summary>
        /// 内容 AB 的输入数据 —— 运行时侧由 <c>JsonReaderWriter</c> 按
        /// <c>streamingAssetsPath + "/Data/" + name + ".json"</c> 拼出同一文件。
        /// </summary>
        public const string ProjectDataJson = "Assets/StreamingAssets/Data/ProjectData.json";

        /// <summary>
        /// 包配置主清单（<c>PackageDatabaseSO.AutoCollect</c> 的落点）。
        /// 运行时经 <c>Resources.Load&lt;PackageDatabaseSO&gt;("Config/PackageDB_Master")</c> 加载，**路径即契约**。
        /// </summary>
        public const string PackageDbAsset = "Assets/Resources/Config/PackageDB_Master.asset";

        /// <summary>
        /// 编译就绪守卫盯的脚本：它没编译完时 <c>CreateAsset</c> 会写出 <c>m_Script:{fileID:0}</c> 的坏资产。
        /// </summary>
        public const string AbConfigScript = "Assets/Scripts/Models/Addressable/ABPackageConfigSO.cs";

        /// <summary>内容包配置目录（一个 clip 的资源 → 多个 <c>AB_{id}.asset</c>）。</summary>
        public const string ContentConfigDir = "Assets/Resources/Config/ContentPackages";

        /// <summary>AssetBundle 输出根（相对工程根）。</summary>
        public const string StreamingAssetsRoot = "Assets/StreamingAssets";

        /// <summary>
        /// 内容 bundle 的输出目录段 —— 直接引用 <see cref="ContentNaming.BundleDirName"/>，
        /// 保证「运行时拼 URL」与「工具写盘」用的是同一个常量。
        /// </summary>
        public const string ContentBundleDirName = ContentNaming.BundleDirName;

        /// <summary>
        /// 构建中转目录（Temp 下，**不入库**）：<c>Temp/MCV_{name}</c>。
        /// 铁律：先构建到 Temp，只有 bundle 本体被拷进 StreamingAssets（不带 .manifest）。
        /// </summary>
        public static string TempRoot(string name)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/MCV_" + name));
        }
    }
}
