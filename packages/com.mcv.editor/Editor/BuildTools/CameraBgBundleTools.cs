using MCV_Module.EditorTools.Common;
using MCV_Module.Interfaces;
using UnityEditor;

// WHY: B1.5 —— 本文件原先自带 8 段流程（编译就绪守卫 / 素材校验 / 写包配置 / 回读自检 / 同步清单 / 弹窗 / 构建 / 文案），与 RoomOneBundleTools 几乎逐行重复；现已全部收进 Assets/Editor/Common/GlobalBundleRunner，本文件只剩菜单入口与「用哪个 provider」。
// WHY: 条目表（camerabg_room / camerabg_contactor 两张图的下标顺序）原样保留在 GlobalProviders/CameraBgGlobalProvider.cs —— 顺序即材质 _Texture_1 / _Texture_2，不能动。
/// <summary>CameraBg 背景图 AB 的工具入口（bundle <c>CameraBg/camerabg</c>）：只负责菜单文字与非交互入口，流程走 <see cref="GlobalBundleRunner"/>。</summary>
public static class CameraBgBundleTools
{
    /// <summary>本包的 provider（无状态，可复用）。</summary>
    static readonly IGlobalBundleProvider Provider = new CameraBgGlobalProvider();

    [MenuItem("MCV Build/CameraBg 背景图 AB（两张打成一包）", false, 63)]
    public static void Build()
    {
        Run(build: true, interactive: true);
    }

    /// <summary>非交互核心：生成配置 → 同步清单 → 构建；<paramref name="interactive"/> = false 时不弹任何对话框，供自动化 / 测试直接调用。</summary>
    public static void Run(bool build, bool interactive)
    {
        GlobalBundleRunner.Run(Provider, build, interactive);
    }
}
