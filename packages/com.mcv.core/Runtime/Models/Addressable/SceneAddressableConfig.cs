using System;
using System.Collections.Generic;
using UnityEngine;

namespace MCV_Module.Models.Addressable
{
    // WHY: 场景是否走 AA 的唯一判定源 —— GlobalAddressableMgr.IsSceneAA / GetSceneAddress 读它；资产放 Resources/ 下运行时与 Editor 共用。
    /// <summary>场景 AA 配置表：列出哪些场景走 Addressables 及各自的 address。</summary>
    [CreateAssetMenu(fileName = "SceneAAConfig", menuName = "MCV/Scene AA Config")]
    public class SceneAddressableConfig : ScriptableObject
    {
        [Tooltip("需要走 AA 的场景列表")]
        public List<SceneAAEntry> scenes = new List<SceneAAEntry>();

        /// <summary>根据场景名称查找配置（运行时）</summary>
        public SceneAAEntry GetEntryBySceneName(string sceneName)
        {
            return scenes.Find(e => e.sceneName == sceneName);
        }

        /// <summary>根据 address 查找配置</summary>
        public SceneAAEntry GetEntryByAddress(string address)
        {
            return scenes.Find(e => e.address == address);
        }
    }

    [Serializable]
    public class SceneAAEntry
    {
        [Tooltip("场景文件名（不含扩展名），如 1_Content")]
        public string sceneName;

        [Tooltip("Addressables 运行时地址，如 scene/1_Content")]
        public string address;

#if UNITY_EDITOR
        [Tooltip("场景资源引用（Editor 用，用于解析路径）")]
        public UnityEditor.SceneAsset sceneAsset;
#endif
    }
}
