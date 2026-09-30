using System;
using System.Collections.Generic;
using MCV_Module.Models;

namespace MCV_Module.UI.Components
{
    /// <summary>finished（显示稳定）状态机：只记"已达层 + 帧预算 + 已注册回调"，不含任何 Unity 生命周期钩子或协程。</summary>
    internal sealed class TextFinishTracker
    {
        /// <summary>一条"稳定后回调"请求。</summary>
        struct Request
        {
            public TextFinishLayer layer;
            public Action callback;
        }

        readonly List<Request> requests = new List<Request>();

        /// <summary>当前已达层。</summary>
        public TextFinishLayer Reached { get; private set; } = TextFinishLayer.Write;

        /// <summary>本轮是否还在等更高的层。</summary>
        public bool Running { get; private set; }

        /// <summary>④ 布局是否已请求（未激活时留到 OnEnable 再请求）。</summary>
        public bool LayoutRequested { get; private set; }

        /// <summary>本轮已耗帧数。</summary>
        public int Frames { get; private set; }

        /// <summary>请求 ④ 的那一帧（用于"下一帧末"确认布局达成）。</summary>
        int layoutFrame;

        /// <summary>② 赋值 / 样式变更后开新一轮：复位已达层与帧数，**已注册回调保留**。</summary>
        public void Begin()
        {
            Reached = TextFinishLayer.Write;
            Frames = 0;
            Running = true;
            LayoutRequested = false;
        }

        /// <summary>③ 排版收敛（避头跑完 / 无需避头）。</summary>
        public void MarkTypography()
        {
            if (!Running) return;
            if (Reached < TextFinishLayer.Typography) Reached = TextFinishLayer.Typography;
        }

        /// <summary>已向面板请求 ④（等下一帧末确认）。</summary>
        public void NoteLayoutRequested(int frame)
        {
            LayoutRequested = true;
            layoutFrame = frame;
        }

        /// <summary>无需 ④（没有面板祖先）：直接当达成。</summary>
        public void SkipLayout()
        {
            Reached = TextFinishLayer.Layout;
        }

        /// <summary>帧预算用尽：按当前层强行放行到 Layout（不收敛时也不能把消费方卡住）。</summary>
        public void ForceSettle()
        {
            Reached = TextFinishLayer.Layout;
        }

        /// <summary>推进一帧：帧数自增，并在 ④ 请求后的下一帧末确认 Layout；返回本轮是否已达成。</summary>
        public bool Advance(int frame)
        {
            if (!Running) return true;

            Frames++;
            if (LayoutRequested && Reached == TextFinishLayer.Typography && frame > layoutFrame)
                Reached = TextFinishLayer.Layout;

            return Reached >= TextFinishLayer.Layout;
        }

        /// <summary>注册回调：已达成则**同帧立即**回调；未达成则挂起等下次达成（重复注册不去重）。</summary>
        public void Add(TextFinishLayer layer, Action callback, Action<Action> invoke)
        {
            if (callback == null) return;

            if (!Running && Reached >= layer)
            {
                invoke(callback);
                return;
            }
            requests.Add(new Request { layer = layer, callback = callback });
        }

        /// <summary>本轮结束：逐个唤醒已达层的回调（未达层的保留到下一轮），异常隔离由 <paramref name="invoke"/> 负责。</summary>
        public void Complete(Action<Action> invoke)
        {
            Running = false;

            for (int i = requests.Count - 1; i >= 0; i--)
            {
                Request request = requests[i];
                if (Reached < request.layer) continue;
                requests.RemoveAt(i);
                invoke(request.callback);
            }
        }

        /// <summary>丢弃全部回调（组件销毁）。</summary>
        public void Clear() => requests.Clear();
    }
}
