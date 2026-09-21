using System.Collections.Generic;
using MCV_Module.Models.Addressable;
using MCV_Module.Models.Project;

namespace MCV_Module.Interfaces
{
    /// <summary>
    /// 待打包的一条内容资源 —— 内容 AB 流水线（<c>Assets/Editor/ContentBundleTools.cs</c>）的中间产物。
    ///
    /// <see cref="id"/> 必须是 <c>ProjectData.json</c> 里**实际写的键值**（如 <c>contactor_info_model</c> /
    /// <c>contactor_info_01</c>）；<c>clipId</c> / <c>device</c> / <c>bundleName</c> 由主流程按归属统一补，
    /// Provider 不必关心。
    /// </summary>
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

        /// <summary>
        /// 资源类型 —— 运行时按它选泛型加载（**必须准**：图集是 Sprite 子资产，用 Object 取只会拿到 Texture2D）。
        /// </summary>
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

    /// <summary>
    /// 内容资源提供者 —— 「一个 ProjectClip 需要打包哪些资源」的唯一扩展点。
    ///
    /// 新增一类内容（结构模型、测量模型…）时只需加一个实现，并登记进
    /// <c>Assets/Editor/ContentBundleTools.cs</c> 的 Provider 列表：id 规则、配置生成、清单同步、构建、
    /// 分级对账、旧产物清理全部由主流程复用。
    ///
    /// 实现落在 Editor 程序集（<c>Assets/Editor/ContentProviders/</c>）。本接口放运行时程序集只是与
    /// <see cref="IController"/> / <see cref="IElement"/> 同层，属 **Editor-only 契约**，框架运行时不消费它。
    /// </summary>
    public interface IContentProvider
    {
        /// <summary>对账报告里的来源名（如 <c>ModelPrefab</c>）。</summary>
        string Name { get; }

        /// <summary>
        /// 收集该 clip 的资源条目。**JSON 是唯一源**：JSON 里要求、但素材缺失的项也要产出
        /// （<see cref="ContentResourceEntry.assetPath"/> 留空），否则对账发现不了缺失。
        /// </summary>
        /// <param name="clip">当前 ProjectClip（直接读其 <c>taskXxxData</c> 字段）</param>
        /// <param name="device">器件段（<c>clip.id</c> 去掉 <c>clip_</c> 前缀，小驼峰），用于拼 id 与定位素材目录</param>
        IEnumerable<ContentResourceEntry> Collect(ProjectClip clip, string device);
    }
}
