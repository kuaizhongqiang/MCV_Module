using MCV_Module.GlobalManager;
using UnityEngine;

/// <summary>
/// Addressable 三策略验证 —— 在 Unity Editor 中手动运行。
/// 挂载到场景中运行即可验证三种加载策略均可正常工作。
/// </summary>
public class AddressableVerification : MonoBehaviour
{
    [SerializeField] private string _aaPackageId = "scene_main_menu";
    [SerializeField] private string _abPackageId = "model_microscope";
    [SerializeField] private string _defaultPackageId = "ui_start_panel";

    private void Start()
    {
        Debug.Log("[AddrTest] ========== Addressable 三策略验证开始 ==========");
        TestDefault();
        TestAA();
        TestAB();
    }

    /// <summary>验证 Default (Resources) 加载</summary>
    private void TestDefault()
    {
        var config = GlobalAddressableMgr.Instance.GetConfig(_defaultPackageId);
        if (config != null)
            Debug.Log($"[AddrTest] ✅ Default 策略: 包配置 {_defaultPackageId} 已加载");
        else
            Debug.LogWarning($"[AddrTest] ⚠️ Default 策略: 包配置 {_defaultPackageId} 未找到（若未配置可忽略）");

        GlobalAddressableMgr.Instance.LoadAssetAsync<GameObject>(_defaultPackageId, result =>
        {
            if (result != null)
                Debug.Log($"[AddrTest] ✅ Default 策略: 资产 {_defaultPackageId} 加载成功");
            else
                Debug.LogWarning($"[AddrTest] ⚠️ Default 策略: 资产 {_defaultPackageId} 加载失败（若未配置可忽略）");
        });
    }

    /// <summary>验证 AA (Addressables) 加载</summary>
    private void TestAA()
    {
        GlobalAddressableMgr.Instance.LoadAssetAsync<GameObject>(_aaPackageId, result =>
        {
            if (result != null)
                Debug.Log($"[AddrTest] ✅ AA 策略: 场景 {_aaPackageId} 加载成功");
            else
                Debug.LogWarning($"[AddrTest] ⚠️ AA 策略: 场景 {_aaPackageId} 加载失败（若未配置可忽略）");
        });
    }

    /// <summary>验证 AB (AssetBundle) 加载</summary>
    private void TestAB()
    {
        GlobalAddressableMgr.Instance.LoadAssetAsync<GameObject>(_abPackageId, result =>
        {
            if (result != null)
                Debug.Log($"[AddrTest] ✅ AB 策略: 模型 {_abPackageId} 加载成功");
            else
                Debug.LogWarning($"[AddrTest] ⚠️ AB 策略: 模型 {_abPackageId} 加载失败（若未配置可忽略）");
        });
    }
}
