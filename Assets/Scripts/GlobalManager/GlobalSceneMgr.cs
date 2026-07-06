using System.Collections;
using MCV_Module.Event;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCV_Module.GlobalManager
{
    public class GlobalSceneMgr : SingletonGlobalMgr<GlobalSceneMgr>
    {
        protected GlobalSceneMgr() { }

        /// <summary>当前主场景名称</summary>
        public string CurrentScene { get; private set; }

        /// <summary>是否正在加载</summary>
        public bool IsLoading { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            CurrentScene = SceneManager.GetActiveScene().name;
        }

        protected override IEnumerator DelayInit()
        {
            isInit = true;
            yield break;
        }

        #region 场景加载

        /// <summary>
        /// 异步 Additive 加载场景，发布 SceneLoadingEvent / SceneLoadedEvent。
        /// 加载期间可通过 LoadingCanvas 展示进度条。
        /// </summary>
        public void LoadSceneAdditive(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneMgr] 正在加载中，忽略 LoadSceneAdditive({sceneName})");
                return;
            }

            StartCoroutine(LoadSceneAdditiveAsync(sceneName));
        }

        /// <summary>
        /// 异步加载并切换主场景（卸载当前场景，加载新场景）
        /// </summary>
        public void LoadSceneSingle(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneMgr] 正在加载中，忽略 LoadSceneSingle({sceneName})");
                return;
            }

            StartCoroutine(LoadSceneSingleAsync(sceneName));
        }

        /// <summary>卸载指定 Additive 场景</summary>
        public void UnloadScene(string sceneName)
        {
            if (CurrentScene == sceneName)
            {
                Debug.LogWarning($"[SceneMgr] 不能卸载当前主场景: {sceneName}");
                return;
            }

            StartCoroutine(UnloadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAdditiveAsync(string sceneName)
        {
            IsLoading = true;

            // 发布加载开始事件
            var loadingEvent = new SceneLoadingEvent(sceneName);
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            operation.allowSceneActivation = true;

            // 更新加载进度
            while (!operation.isDone)
            {
                loadingEvent.Progress = operation.progress;
                EventBus<SceneLoadingEvent>.Publish(loadingEvent);
                yield return null;
            }

            loadingEvent.Progress = 1f;
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            CurrentScene = sceneName;
            IsLoading = false;

            // 发布加载完成事件
            EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(sceneName));
        }

        private IEnumerator LoadSceneSingleAsync(string sceneName)
        {
            IsLoading = true;

            var loadingEvent = new SceneLoadingEvent(sceneName);
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                loadingEvent.Progress = operation.progress;
                EventBus<SceneLoadingEvent>.Publish(loadingEvent);
                yield return null;
            }

            loadingEvent.Progress = 1f;
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            CurrentScene = sceneName;
            IsLoading = false;

            EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(sceneName));
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            yield return operation;
        }

        #endregion
    }
}
