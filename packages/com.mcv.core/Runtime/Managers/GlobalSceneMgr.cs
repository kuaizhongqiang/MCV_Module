using System.Collections;
using MCV_Module.Event;
using MCV_Module.Singleton;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCV_Module.Managers
{
    public class GlobalSceneMgr : SingletonGlobalMgr<GlobalSceneMgr>
    {
        #region 参数
        public string CurrentScene { get; private set; }
        public bool IsLoading { get; private set; }

        /// <summary>是否启用 Addressables 场景加载（由 Setup 注入，WebGL 降级时设为 false）</summary>
        public bool UseAddressable { get; set; } = true;

        /// <summary>当前加载的 AA 切换场景（基础场景 1_Content 不参与交换，单独追踪）</summary>
        string m_LoadedAAScene = "";
        #endregion

        #region 生命周期
        protected GlobalSceneMgr() { }

        protected override void Awake()
        {
            base.Awake();
            CurrentScene = SceneManager.GetActiveScene().name;
        }

        protected override IEnumerator DelayInit()
        {
            // 事件驱动场景切换（强引用，OnDestroy 必须退订）
            EventBus<SceneSwitchRequestEvent>.Subscribe(OnSceneSwitchRequested);
            isInit = true;
            yield break;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<SceneSwitchRequestEvent>.Unsubscribe(OnSceneSwitchRequested);
        }
        #endregion

        #region 公开方法
        public void LoadSceneAdditive(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(LoadSceneAdditiveAsync(sceneName));
        }

        public void LoadSceneSingle(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(LoadSceneSingleAsync(sceneName));
        }

        /// <summary>批量加载多个场景（串行，逐个加载完成后才加载下一个）</summary>
        public void LoadScenesAdditive(params string[] sceneNames)
        {
            if (IsLoading || sceneNames == null || sceneNames.Length == 0) return;
            StartCoroutine(LoadScenesAdditiveAsync(sceneNames));
        }

        public void UnloadScene(string sceneName)
        {
            if (CurrentScene == sceneName) return;
            StartCoroutine(UnloadSceneAsync(sceneName));
        }

        /// <summary>
        /// 交换 AA 场景：先加载新场景（additive），完成后再卸载上一个 AA 场景。
        /// 任意时刻只存在一个 AA 切换场景；基础场景 1_Content 常驻不参与交换。
        /// </summary>
        public void SwitchScene(string sceneName)
        {
            if (IsLoading || string.IsNullOrEmpty(sceneName)) return;
            StartCoroutine(SwitchSceneAsync(sceneName));
        }
        #endregion

        #region 私有方法
        void OnSceneSwitchRequested(SceneSwitchRequestEvent e)
        {
            SwitchScene(e.SceneName);
        }

        private IEnumerator LoadScenesAdditiveAsync(string[] sceneNames)
        {
            IsLoading = true;
            foreach (var sceneName in sceneNames)
            {
                if (string.IsNullOrEmpty(sceneName)) continue;

                var loadingEvent = new SceneLoadingEvent(sceneName);
                EventBus<SceneLoadingEvent>.Publish(loadingEvent);

                bool isAA = UseAddressable &&
                            GlobalAddressableMgr.Instance != null &&
                            GlobalAddressableMgr.Instance.IsSceneAA(sceneName);
                Log.Verbose($"[SceneMgr] 批量加载场景: {sceneName}, IsSceneAA={isAA}, UseAddressable={UseAddressable}");

                if (isAA)
                {
                    yield return GlobalAddressableMgr.Instance.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                }
                else
                {
                    yield return LoadSceneDirectAsync(sceneName, LoadSceneMode.Additive);
                }

                loadingEvent.Progress = 1f;
                EventBus<SceneLoadingEvent>.Publish(loadingEvent);
                EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(sceneName));

                CurrentScene = sceneName;
            }
            IsLoading = false;
        }

        private IEnumerator LoadSceneAdditiveAsync(string sceneName)
        {
            IsLoading = true;
            var loadingEvent = new SceneLoadingEvent(sceneName);
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            bool isAA = UseAddressable &&
                        GlobalAddressableMgr.Instance != null &&
                        GlobalAddressableMgr.Instance.IsSceneAA(sceneName);
            Log.Verbose($"[SceneMgr] 加载场景: {sceneName}, IsSceneAA={isAA}, UseAddressable={UseAddressable}");

            if (isAA)
            {
                yield return GlobalAddressableMgr.Instance.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            else
            {
                yield return LoadSceneDirectAsync(sceneName, LoadSceneMode.Additive);
            }

            loadingEvent.Progress = 1f;
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            CurrentScene = sceneName;
            IsLoading = false;
            EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(sceneName));
        }

        private IEnumerator LoadSceneSingleAsync(string sceneName)
        {
            IsLoading = true;
            var loadingEvent = new SceneLoadingEvent(sceneName);
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            // 判断是否走 AA
            if (UseAddressable &&
                GlobalAddressableMgr.Instance != null &&
                GlobalAddressableMgr.Instance.IsSceneAA(sceneName))
            {
                yield return GlobalAddressableMgr.Instance.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            else
            {
                yield return LoadSceneDirectAsync(sceneName, LoadSceneMode.Single);
            }

            loadingEvent.Progress = 1f;
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            CurrentScene = sceneName;
            IsLoading = false;
            EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(sceneName));
        }

        private IEnumerator SwitchSceneAsync(string sceneName)
        {
            IsLoading = true;
            var loadingEvent = new SceneLoadingEvent(sceneName);
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            // 1. 先加载新 AA 场景（additive）
            yield return LoadSceneCore(sceneName, LoadSceneMode.Additive);

            // 2. 再卸载上一个 AA 场景（释放其包）
            if (!string.IsNullOrEmpty(m_LoadedAAScene) && m_LoadedAAScene != sceneName)
            {
                yield return UnloadAAScene(m_LoadedAAScene);
            }

            // 3. 更新追踪
            m_LoadedAAScene = sceneName;
            CurrentScene = sceneName;

            loadingEvent.Progress = 1f;
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);
            EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(sceneName));
            IsLoading = false;
        }

        /// <summary>核心加载：按 AA 配置路由到 GlobalAddressableMgr 或直接 SceneManager。</summary>
        private IEnumerator LoadSceneCore(string sceneName, LoadSceneMode mode)
        {
            bool isAA = UseAddressable &&
                        GlobalAddressableMgr.Instance != null &&
                        GlobalAddressableMgr.Instance.IsSceneAA(sceneName);
            if (isAA)
            {
                yield return GlobalAddressableMgr.Instance.LoadSceneAsync(sceneName, mode);
            }
            else
            {
                yield return SceneManager.LoadSceneAsync(sceneName, mode);
            }
        }

        /// <summary>卸载 AA 场景（经 handle 释放包；编辑器模式走 SceneManager）。</summary>
        private IEnumerator UnloadAAScene(string sceneName)
        {
            bool isAA = UseAddressable &&
                        GlobalAddressableMgr.Instance != null &&
                        GlobalAddressableMgr.Instance.IsSceneAA(sceneName);
            if (isAA)
            {
                yield return GlobalAddressableMgr.Instance.UnloadSceneAsync(sceneName);
            }
            else
            {
                yield return SceneManager.UnloadSceneAsync(sceneName);
            }
        }
        #endregion

        #region 工具方法
        /// <summary>直接通过 SceneManager 加载（Build Settings 中的场景）</summary>
        private IEnumerator LoadSceneDirectAsync(string sceneName, LoadSceneMode mode)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, mode);
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            yield return operation;
        }
        #endregion
    }
}
