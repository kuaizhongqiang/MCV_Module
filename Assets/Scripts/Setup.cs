using System.Collections;
using MCV_Module.GlobalManager;
using UnityEngine;

public class Setup : MonoBehaviour
{
    [SerializeField] int retryCount = 5;
    [SerializeField] float retryInterval = 1f;
    [SerializeField] private string _firstSceneName = "MainMenu";

    IEnumerator Start()
    {
        while (!IsGlobalMgrInit<GlobalAudioMgr>())
        {
            if (retryCount <= 0)
                break;
            retryCount--;
            yield return new WaitForSeconds(retryInterval);
        }

        while (!IsGlobalMgrInit<GlobalControllerMgr>())
        {
            if (retryCount <= 0)
                break;
            retryCount--;
            yield return new WaitForSeconds(retryInterval);
        }

        while (!IsGlobalMgrInit<GlobalDataMgr>())
        {
            if (retryCount <= 0)
                break;
            retryCount--;
            yield return new WaitForSeconds(retryInterval);
        }

        while (!IsGlobalMgrInit<GlobalInteractiveMgr>())
        {
            if (retryCount <= 0)
                break;
            retryCount--;
            yield return new WaitForSeconds(retryInterval);
        }

        while (!IsGlobalMgrInit<GlobalSceneMgr>())
        {
            if (retryCount <= 0)
                break;
            retryCount--;
            yield return new WaitForSeconds(retryInterval);
        }

        while (!IsGlobalMgrInit<GlobalUiMgr>())
        {
            if (retryCount <= 0)
                break;
            retryCount--;
            yield return new WaitForSeconds(retryInterval);
        }

        while (!IsGlobalMgrInit<GlobalAddressableMgr>())
        {
            if (retryCount <= 0)
                break;
            retryCount--;
            yield return new WaitForSeconds(retryInterval);
        }

        while (!IsGlobalMgrInit<GlobalCameraMgr>())
        {
            if (retryCount <= 0)
                break;
            retryCount--;
            yield return new WaitForSeconds(retryInterval);
        }

        // TODO: M3 — 步骤系统管理器初始化
        while (!IsGlobalMgrInit<GlobalStepSystemMgr>())
        {
            if (retryCount <= 0)
                break;
            retryCount--;
            yield return new WaitForSeconds(retryInterval);
        }

        Jump();
    }

    void Jump()
    {
        Debug.Log($"[Setup] 初始化完成，跳转到场景: {_firstSceneName}");
        GlobalSceneMgr.Instance.LoadSceneSingle(_firstSceneName);
    }

    bool IsGlobalMgrInit<T>() where T : SingletonGlobalMgr<T>
    {
        return T.Exists && T.Instance.isInit;
    }
}
