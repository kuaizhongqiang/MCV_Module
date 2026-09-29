using System.Collections.Generic;
using MCV_Module.Models.Addressable;
using MCV_Module.Models.Project;

namespace MCV_Module.Interfaces
{
    // WHY: id 必须是 ProjectData.json 里实际写的键值；clipId/device/bundleName 由主流程统一补，Provider 自己拼会错位。
    /// <summary>待打包的一条内容资源：内容 AB 流水线的中间产物。</summary>
    public class ContentResourceEntry
    {
        /// <summary>包配置 id（= JSON 里的键值）。</summary>
        public string id;

        /// <summary>资源工程路径（<c>Assets/...</c>）。为空表示 JSON 要求但素材缺失。</summary>
        public string assetPath;

        /// <summary>Error 级：阻塞构建（素材缺失 / id 不符合命名规则 / 重复占用 id 等）。</summary>
        public bool isError;

        /// <summary>Warning 级：只进对账报告，不参与打包（素材有而 JSON 未引用）。</summary>
        public bool isExtra;

        // WHY: 资源类型必须准——运行时按它选泛型加载，图集是 Sprite 子资产，用 Object 取只会拿到 Texture2D。
        /// <summary>资源类型：运行时按它选泛型加载。</summary>
        public ContentAssetKind kind = ContentAssetKind.Prefab;

        /// <summary>对账报告里的备注（缺失原因 / 来源说明等）。</summary>
        public string note;

        public ContentResourceEntry() { }

        public ContentResourceEntry(string id, string assetPath, string note = null,
                                    ContentAssetKind kind = ContentAssetKind.Prefab)
        {
            this.id = id;
            this.assetPath = assetPath;
            this.note = note;
            this.kind = kind;
        }
    }

    // WHY: 这是「一个 ProjectClip 打包哪些资源」的唯一扩展点；实现落在 Editor 程序集，须登记进 ContentBundleTools.cs 的 Provider 列表，漏登记不会被打包。
    /// <summary>内容资源提供者（Editor-only 契约）：收集单个 ProjectClip 待打包的资源条目。</summary>
    public interface IContentProvider
    {
        /// <summary>对账报告里的来源名（如 <c>InfoSprite</c>）。</summary>
        string Name { get; }

        // WHY: JSON 是唯一源——素材缺失的项也要产出（assetPath 留空），只产出已存在的素材会让对账漏掉缺失。
        /// <summary>收集该 clip 的资源条目（直接读 taskXxxData 字段）。</summary>
        IEnumerable<ContentResourceEntry> Collect(ProjectClip clip, string device);
    }
}
