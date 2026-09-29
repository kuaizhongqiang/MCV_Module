using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MCV_Module.Managers.RoomDynamic
{
    /// <summary>房态动态管理器的泛型单例基类：第一个 Awake 占位，重复的销毁自身。</summary>
    public abstract class RoomDynamicMgrBase<T> : MonoBehaviour where T : RoomDynamicMgrBase<T>
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        public static bool Exists
        {
            get
            {
                return _instance != null;
            }
        }
        protected bool isInit = false;
        public bool IsInit
        {
            get
            {
                return isInit;
            }
        }
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                StartCoroutine(DelayInit());
            }
            else
            {
                Destroy(this);
            }


        }

        protected virtual IEnumerator DelayInit()
        {
            yield return null;
            isInit = true;

        }
    }
}
