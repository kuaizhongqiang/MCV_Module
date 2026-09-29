using MCV_Module.EditorTools.Common;
using MCV_Module.Interfaces;
using UnityEditor;

// WHY: B1.5 —— 本文件原先自带 8 段流程（编译就绪守卫 / 素材校验 / 残留清理 / 写包配置 / 回读自检 / 同步清单 / 弹窗 / 构建），与 CameraBgBundleTools 几乎逐行重复；现已全部收进 Assets/Editor/Common/GlobalBundleRunner，本文件只剩菜单入口与「用哪个 provider」。
// WHY: 图标表（8 条 id / 路径、Pic-xcy.png 不进包）原样保留在 GlobalProviders/RoomOneGlobalProvider.cs。
/// <summary>RoomOne 房间图标 AB 的工具入口（bundle <c>RoomOne/roomone</c>）：只负责菜单文字与非交互入口，流程走 <see cref="GlobalBundleRunner"/>。</summary>
public static class RoomOneBundleTools
{
    /// <summary>本包的 provider（无状态，可复用）。</summary>
    static readonly IGlobalBundleProvider Provider = new RoomOneGlobalProvider();

    #region 菜单
    [MenuItem("MCV Build/RoomOne 房间图标 AB（八张打成一包）", false, 64)]
    public static void BuildMenu()
    {
        Run(build: true, interactive: true);
    }

    [MenuItem("MCV Build/内容 AB/RoomOne 仅生成配置（不构建）", false, 85)]
    public static void ConfigOnlyMenu()
    {
        Run(build: false, interactive: true);
    }
    #endregion

    /// <summary>非交互核心：生成配置 → 同步清单 → 构建；<paramref name="interactive"/> = false 时不弹任何对话框，供自动化 / 测试直接调用。</summary>
    public static void Run(bool build, bool interactive)
    {
        GlobalBundleRunner.Run(Provider, build, interactive);
    }
}
