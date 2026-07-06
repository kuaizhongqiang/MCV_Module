
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Controller;
using UnityEngine;

namespace MCV_Module.GlobalManager
{
    public class GlobalControllerMgr : SingletonGlobalMgr<GlobalControllerMgr>
    {
        protected GlobalControllerMgr(){}

        private readonly Dictionary<string, IController> _controllers = new();

        protected override void Awake()
        {
            base.Awake();
        }

        protected override IEnumerator DelayInit()
        {
            yield break;
        }

        /// <summary>注册控制器（由 ControllerBase.Start 自动调用）。</summary>
        public void Register(IController controller)
        {
            var key = controller.ControllerName;
            if (_controllers.TryGetValue(key, out var existing) && existing != null && existing != controller)
            {
                Debug.LogWarning($"[GlobalControllerMgr] 控制器 {key} 已存在，将被覆盖");
            }

            _controllers[key] = controller;
        }

        /// <summary>取消注册控制器（由 ControllerBase.OnDestroy 自动调用）。</summary>
        public void Unregister(IController controller)
        {
            var key = controller.ControllerName;
            if (_controllers.TryGetValue(key, out var existing) && existing == controller)
            {
                _controllers.Remove(key);
            }
        }

        /// <summary>按类型查找控制器。</summary>
        public T Find<T>() where T : class, IController
        {
            var key = typeof(T).Name;
            if (_controllers.TryGetValue(key, out var controller))
                return controller as T;
            return null;
        }

        /// <summary>按名称查找控制器。</summary>
        public IController Find(string name)
        {
            _controllers.TryGetValue(name, out var controller);
            return controller;
        }

        /// <summary>是否存在指定控制器。</summary>
        public bool Exist<T>() where T : IController
        {
            return _controllers.ContainsKey(typeof(T).Name);
        }

        /// <summary>获取所有已注册的控制器。</summary>
        public IReadOnlyCollection<IController> All => _controllers.Values;
    }
}