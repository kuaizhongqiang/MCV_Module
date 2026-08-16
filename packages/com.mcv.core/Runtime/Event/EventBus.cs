using System;
using System.Collections.Generic;

namespace MCV_Module.Event
{
    /// <summary>
    /// 类型安全的事件总线 —— 泛型 Action<T> 解耦机制。
    ///
    /// 用法：
    ///   // 发布事件
    ///   EventBus<AudioVolumeEventData>.Publish(new AudioVolumeEventData(AudioSouceType.BGM, 0.5f));
    ///
    ///   // 订阅事件
    ///   EventBus<AudioVolumeEventData>.Subscribe(OnVolumeChange);
    ///   EventBus<AudioVolumeEventData>.Unsubscribe(OnVolumeChange);
    ///
    /// 特点：
    ///   - 泛型 T 承载事件参数，编译时类型安全
    ///   - 使用强引用 List&lt;Action&lt;T&gt;&gt;，订阅者须在 OnDestroy 中 Unsubscribe
    ///   - Clear() 可在场景切换时重置所有订阅
    /// </summary>
    public static class EventBus<T> where T : class
    {
        private static readonly List<Action<T>> s_Subscribers = new List<Action<T>>();
        private static readonly object s_Lock = new object();
        // 订阅快照缓存：订阅列表未变化时复用数组，避免每次 Publish 分配 ToArray。
        // s_Revision 在 Subscribe/Unsubscribe/Clear 时递增，Publish 据此判断是否重建快照。
        private static Action<T>[] s_Snapshot = new Action<T>[0];
        private static int s_Revision;
        private static int s_SnapshotRevision = -1;

        /// <summary>订阅事件</summary>
        public static void Subscribe(Action<T> handler)
        {
            lock (s_Lock)
            {
                if (!s_Subscribers.Contains(handler))
                {
                    s_Subscribers.Add(handler);
                    s_Revision++;
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
                    s_Revision++;
                }
            }
        }

        /// <summary>发布事件 —— 通知所有订阅者</summary>
        public static void Publish(T eventData)
        {
            Action<T>[] handlers;
            lock (s_Lock)
            {
                if (s_SnapshotRevision != s_Revision)
                {
                    s_Snapshot = s_Subscribers.ToArray();
                    s_SnapshotRevision = s_Revision;
                }
                handlers = s_Snapshot;
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
                if (s_Subscribers.Count > 0)
                {
                    s_Subscribers.Clear();
                    s_Revision++;
                }
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
