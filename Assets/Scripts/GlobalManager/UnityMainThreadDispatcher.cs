using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 主线程分发器 — 允许后台线程将操作投递到 Unity 主线程执行。
///
/// GlobalCLIMgr 的 HTTP 服务器在后台线程运行，需要通过此分发器
/// 将命令执行切换到主线程。
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher s_Instance;
    private static readonly object s_Lock = new();
    private readonly ConcurrentQueue<Action> _actions = new();

    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (s_Instance == null)
            {
                lock (s_Lock)
                {
                    if (s_Instance == null)
                    {
                        var go = new GameObject("UnityMainThreadDispatcher");
                        s_Instance = go.AddComponent<UnityMainThreadDispatcher>();
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return s_Instance;
        }
    }

    /// <summary>将操作投递到主线程。</summary>
    public void Enqueue(Action action)
    {
        _actions.Enqueue(action);
    }

    private void Update()
    {
        while (_actions.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityMainThreadDispatcher] Error: {e.Message}");
            }
        }
    }
}
