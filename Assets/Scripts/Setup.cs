using System.Collections;
using MCV_Module.GlobalManager;
using UnityEngine;

public class Setup : MonoBehaviour
{
    [SerializeField] int retryCount = 5;
    [SerializeField] float retryInterval = 1f;
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

        Jump();
    }

    void Jump()
    {
        // TODO : Jump to the scene
        Debug.Log("Jump to the scene");
    }

    bool IsGlobalMgrInit<T>() where T : SingletonGlobalMgr<T>
    {
        // TODO : Check if the global manager is initialized
        return true;
    }
}


