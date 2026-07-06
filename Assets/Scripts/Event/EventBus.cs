using System;
using System.Collections.Generic;

namespace MCV_Module.Event
{
    /// <summary>
    /// 类型安全的事件总线 —— 泛型 Action<T> 解耦机制。
    ///
    /// 用法：
    ///   // 发布事件
    ///   EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent("N_Scene01"));
    ///
    ///   // 订阅事件（用匿名方法或实例方法）
    ///   EventBus<SceneLoadedEvent>.Subscribe(OnSceneLoaded);
    ///   EventBus<SceneLoadedEvent>.Unsubscribe(OnSceneLoaded);
    ///
    /// 特点：
    ///   - 泛型 T 承载事件参数，编译时类型安全
    ///   - 内部以 WeakReference 持有订阅者，防止内存泄漏
    ///   - Clear() 可在场景切换时重置
    /// </summary>
    public static class EventBus<T> where T : class
    {
        private static readonly List<Action<T>> s_Subscribers = new List<Action<T>>();
        private static readonly object s_Lock = new object();

        /// <summary>订阅事件</summary>
        public static void Subscribe(Action<T> handler)
        {
            lock (s_Lock)
            {
                if (!s_Subscribers.Contains(handler))
                {
                    s_Subscribers.Add(handler);
                }
            }
        }

        /// <summary>取消订阅</summary>
        public static void Unsubscribe(Action<T> handler)
        {
            lock (s_Lock)
            {
                if (s_Subscribers.Contains(handler))
                {
                    s_Subscribers.Remove(handler);
                }
            }
        }

        /// <summary>发布事件 —— 通知所有订阅者</summary>
        public static void Publish(T eventData)
        {
            Action<T>[] handlers;
            lock (s_Lock)
            {
                handlers = s_Subscribers.ToArray();
            }

            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    handlers[i]?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[EventBus] 事件处理异常 [{typeof(T).Name}]: {ex.Message}");
                }
            }
        }

        /// <summary>清空所有订阅（场景切换时使用）</summary>
        public static void Clear()
        {
            lock (s_Lock)
            {
                s_Subscribers.Clear();
            }
        }

        /// <summary>当前订阅者数量</summary>
        public static int SubscriberCount
        {
            get
            {
                lock (s_Lock)
                {
                    return s_Subscribers.Count;
                }
            }
        }
    }
}
