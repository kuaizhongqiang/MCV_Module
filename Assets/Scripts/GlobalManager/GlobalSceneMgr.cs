using System.Collections;
using MCV_Module.Event;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCV_Module.GlobalManager
{
    public class GlobalSceneMgr : SingletonGlobalMgr<GlobalSceneMgr>
    {
        protected GlobalSceneMgr() { }

        public string CurrentScene { get; private set; }
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

        public void UnloadScene(string sceneName)
        {
            if (CurrentScene == sceneName) return;
            StartCoroutine(UnloadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAdditiveAsync(string sceneName)
        {
            IsLoading = true;
            var loadingEvent = new SceneLoadingEvent(sceneName);
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            // 90% 等待策略：先不激活，等加载到 90% 后再激活
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                loadingEvent.Progress = operation.progress;
                EventBus<SceneLoadingEvent>.Publish(loadingEvent);
                yield return null;
            }

            // 90% 完成，发布事件后激活
            loadingEvent.Progress = 0.9f;
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

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

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                loadingEvent.Progress = operation.progress;
                EventBus<SceneLoadingEvent>.Publish(loadingEvent);
                yield return null;
            }

            loadingEvent.Progress = 0.9f;
            EventBus<SceneLoadingEvent>.Publish(loadingEvent);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

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
