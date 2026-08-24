


using System.Collections;
using System.Collections.Generic;
using MCV_Module.InputController;
using MCV_Module.Singleton;

namespace MCV_Module.Managers
{
    public class GlobalInputMgr : SingletonGlobalMgr<GlobalInputMgr>
    {
        protected GlobalInputMgr() { }

        Dictionary<string, InputControllerBase> m_ControllerDict = new Dictionary<string, InputControllerBase>();

        protected override IEnumerator DelayInit()
        {
            yield return null;
            isInit = true;
        }

        public static void RegisterController(string name, InputControllerBase controller)
        {
            if (!Instance.m_ControllerDict.ContainsKey(name))
            {
                Instance.m_ControllerDict[name] = controller;
            }
        }

        public static void UnregisterController(string name)
        {
            if (Instance.m_ControllerDict.ContainsKey(name))
            {
                Instance.m_ControllerDict.Remove(name);
            }
        }

        /// <summary>
        /// 按类型获取已注册的 Controller（T 的类型名作为查找 key）
        /// </summary>
        public static T GetController<T>() where T : InputControllerBase
        {
            string key = typeof(T).Name;
            if (Instance.m_ControllerDict.TryGetValue(key, out var controller))
                return controller as T;
            return null;
        }
    }
}