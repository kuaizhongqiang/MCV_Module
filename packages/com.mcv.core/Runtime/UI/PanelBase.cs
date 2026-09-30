
using System;
using System.Collections.Generic;
using MCV_Module.Utils;
using System.Reflection;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.UI.Components;
using MCV_Module.UI.Tools;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace MCV_Module.UI
{
    /// <summary>面板基类：Start 时绑定控制器，并维护本面板下挂的组件注册表。</summary>
    public abstract class PanelBase : UIBase
    {
        protected CanvasBase m_Canvas;
        List<ComponentBase> m_Components = new List<ComponentBase>();

        /// <summary>绑定控制器：优先 [RequireController] 特性，无特性时回退 XxxPanel → XxxController 命名约定。</summary>
        protected virtual void Start()
        {
            BindController();
            RefreshTextComponents();
        }

        void BindController()
        {
            // 优先 [RequireController] 强绑定（新面板由生成器自动写入）
            var attr = GetType().GetCustomAttribute<RequireControllerAttribute>(false);
            if (attr != null && attr.ControllerType != null)
            {
                // WHY: 控制器不再挂场景、由 GlobalControllerMgr 按类型创建，这里按类型取（强绑定不受改名影响）；
                //      名称兜底保留，免得历史面板的 [RequireController] 与 ControllerName 不一致时直接失联。
                string controllerName = attr.ControllerType.Name;
                var controller = FindByType(attr.ControllerType) ?? GlobalControllerMgr.Instance.Find(controllerName);
                if (controller != null)
                {
                    controller.Bind(this);
                }
                else
                {
                    Log.Error($"[PanelBase] 未找到 [RequireController] 指定 Controller：{controllerName}，面板 {GetType().Name} 未绑定");
                }
                return;
            }

            // 回退：字符串命名约定（兼容历史面板）
            string typeName = GetType().Name;
            if (typeName.EndsWith("Panel"))
            {
                typeName = typeName.Substring(0, typeName.Length - "Panel".Length);
            }
            string controllerNameLegacy = typeName + "Controller";

            var controllerLegacy = GlobalControllerMgr.Instance.Find(controllerNameLegacy);
            if (controllerLegacy != null)
            {
                controllerLegacy.Bind(this);
            }
            else
            {
                Log.Warning($"[PanelBase] 未找到对应 Controller：{controllerNameLegacy}，面板 {GetType().Name} 未绑定");
            }
        }

        // WHY: 控制器类型来自特性，编译期拿不到泛型实参，只能反射调 GlobalControllerMgr.Find<T>()；这条路径每次面板重建才走一次，开销可忽略。
        static IController FindByType(System.Type controllerType)
        {
            var mgr = GlobalControllerMgr.Instance;
            if (mgr == null || controllerType == null) return null;

            var method = typeof(GlobalControllerMgr).GetMethod(nameof(GlobalControllerMgr.Find), System.Type.EmptyTypes);
            if (method == null) return null;

            try
            {
                return method.MakeGenericMethod(controllerType).Invoke(mgr, null) as IController;
            }
            catch (System.Exception e)
            {
                Log.Error($"[PanelBase] 按类型查找 Controller {controllerType.Name} 失败：{e.Message}");
                return null;
            }
        }

        public void SetCanvas(CanvasBase canvas)
        {
            m_Canvas = canvas;
        }

        public void RegisterComponent(ComponentBase component)
        {
            if (!m_Components.Contains(component))
            {
                m_Components.Add(component);
            }
        }

        public void UnregisterComponent(ComponentBase component)
        {
            if (m_Components.Contains(component))
            {
                m_Components.Remove(component);
            }
        }

        public T GetUIComponent<T> () where T : ComponentBase
        {
            foreach (var component in m_Components)
            {
                if (component is T)
                {
                    return component as T;
                }
            }
            return null;
        }

        #region 布局重建
        /// <summary>进行中的布局重建协程（重复请求时先停掉上一次，只保留最后一次）。</summary>
        Coroutine m_LayoutRebuildCoroutine;

        // WHY: 文本 / 子物体状态变完之后**不能同帧**直接 ForceRebuildLayoutImmediate，两个理由都有实测：
        //  ① TMP 形态下 TextComponent 换形态跨帧（先禁用卸载 Legacy、等一帧才挂 TMP），未 ready 的写入只进 pending 缓冲，
        //     同帧量到的是「空文本」的尺寸（浮动框塌成只有内边距）；
        //  ② 面板 prefab 普遍是「子节点自带 ContentSizeFitter + 父级 LayoutGroup（父级 childControlWidth = false，
        //     量的是子节点当前 sizeDelta）」，而单次重建里父级先于子级算 ⇒ 必须自下而上才收敛。
        // WHY: 另外两条现场经验：取值必须走 UILayoutRebuilder 的收集（不依赖任何 Text 字段 —— TMP 形态下那些字段是
        //     "假 null"，用它们取节点会让整段重建静默失效）；重复请求只保留最后一次，所以要刷多个子树时传公共父节点。
        /// <summary>请求一次布局重建（等一帧、按深度自下而上）：文本写入 / 子节点显隐之后统一调它；root 省略时刷整个面板。</summary>
        // WHY: public 而不是 protected —— TextComponent 的 ④ 布局层要经"向所属面板请求"走同一个入口（不允许它自己直连 UILayoutRebuilder）。
        public void RequestLayoutRebuild(Transform root = null)
        {
            if (!isActiveAndEnabled)
            {
                // WHY: 未激活层级既起不了协程（Unity 直接报错），ForceRebuild 也会被 LayoutRebuilder 静默跳过；激活时 OnEnable 的脏标记会兜底
                Log.Verbose($"[{GetType().Name}] 未激活，本次布局重建被跳过（激活后的自动布局会兜底）");
                return;
            }

            if (m_LayoutRebuildCoroutine != null) StopCoroutine(m_LayoutRebuildCoroutine);
            m_LayoutRebuildCoroutine = StartCoroutine(UILayoutRebuilder.RebuildSubtreeNextFrame(root != null ? root : transform));
        }

        protected override void OnDestroy()
        {
            // WHY: 面板随 Canvas 重建被 ClearPanels 销毁，协程跟着一起收；字段置空免得留下已停句柄
            if (m_LayoutRebuildCoroutine != null)
            {
                StopCoroutine(m_LayoutRebuildCoroutine);
                m_LayoutRebuildCoroutine = null;
            }

            base.OnDestroy();
        }
        #endregion
    
        #region 文本稳定（finished）
        /// <summary>本面板下挂的全部 TextComponent（由 <see cref="RefreshTextComponents"/> 重扫维护）。</summary>
        readonly List<TextComponent> m_TextComponents = new List<TextComponent>();

        /// <summary>进行中的"等待全部文本稳定"请求。</summary>
        readonly List<TextWait> m_TextWaits = new List<TextWait>();

        /// <summary>一条面板级等待：登记完再倒计时，超时照样回调（不允许把面板卡死）。</summary>
        class TextWait
        {
            public Action callback;
            public TextFinishLayer layer;
            public int pending;
            public int framesLeft;
            public int limit;
            /// <summary>已收尾（回调只会发一次：全部达成与超时两条路都经 <see cref="FinishWait"/> 并置此位）。</summary>
            public bool done;
        }

        /// <summary>重扫本面板（含未激活子节点）的 TextComponent —— 支持运行期动态增删（如 AI 气泡）。</summary>
        void RefreshTextComponents()
        {
            m_TextComponents.Clear();
            GetComponentsInChildren(true, m_TextComponents);
        }

        /// <summary>
        /// 面板内**全部** TextComponent 达到 <paramref name="layer"/> 后回调；<paramref name="timeoutFrames"/> = 0 表示用默认
        /// （组件帧上限 + 2 帧的工程余量）。超时**照样回调**并打一条 Warning。
        /// </summary>
        // WHY: 逐个组件自己收 OnFinished 会让每个业务点都重写一遍聚合；统一放面板入口，且每次调用前重扫（动态增删节点也算得准）。
        // WHY: 倒计时的前提 —— 它是协程，**面板未激活时起不来，等于既不计时、也不会回调**，那种情况调用方必须自己先激活或自行兜底。
        //      另：已达成层的组件在登记时就会**同帧**回调，故 pending 可能在登记循环里就归零（先完成的那次是正常结果，不是竞态）。
        protected void WaitAllTextFinished(Action callback, TextFinishLayer layer = TextFinishLayer.Layout, int timeoutFrames = 0)
        {
            if (callback == null) return;

            RefreshTextComponents();
            int limit = timeoutFrames > 0 ? timeoutFrames : TextComponent.FinishFrameLimit + 2;

            if (m_TextComponents.Count == 0)
            {
                InvokeWaitSafely(callback);
                return;
            }

            var wait = new TextWait
            {
                callback = callback,
                layer = layer,
                pending = m_TextComponents.Count,
                framesLeft = limit,
                limit = limit,
            };
            m_TextWaits.Add(wait);

            // WHY: 倒计时用协程而不是 Update —— 面板基类不占每帧回调；而且未激活时协程起不来，
            // 正好等于"面板未激活就不计时、也不会回调"这条已定口径（那种情况调用方必须自己先激活或自行兜底）。
            if (isActiveAndEnabled) StartCoroutine(CountdownWait(wait));

            for (int i = 0; i < m_TextComponents.Count; i++)
            {
                TextComponent comp = m_TextComponents[i];
                if (comp == null) { CompleteOne(wait); continue; }
                comp.OnFinished(() => CompleteOne(wait), layer);
            }
        }

        /// <summary>一个文本达到目标层（已达成层会**同帧**回调，故登记过程中就可能推进到这里）。</summary>
        void CompleteOne(TextWait wait)
        {
            if (wait == null || wait.pending <= 0) return;
            wait.pending--;
            if (wait.pending == 0) FinishWait(wait);
        }

        /// <summary>收尾一条等待：回调 + 从表里摘掉（全部达成与超时共用；<see cref="TextWait.done"/> 保证只发一次）。</summary>
        void FinishWait(TextWait wait)
        {
            if (wait == null || wait.done) return;
            wait.done = true;
            m_TextWaits.Remove(wait);
            InvokeWaitSafely(wait.callback);
        }

        /// <summary>回调异常隔离：一个消费方的异常不影响面板自身与其它等待。</summary>
        void InvokeWaitSafely(Action callback)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"[{GetType().Name}] WaitAllTextFinished 回调抛异常：{e.Message}", this);
            }
        }

        // WHY: 倒计时用**帧**而不是秒 —— 与 TextComponent 的帧预算同单位，不会因帧率不同而等待时间不同。
        // WHY: 不占用面板的 Update —— 一条等待一个协程，收尾（全部达成 / 超时）后协程自然退出，常态零开销。
        /// <summary>一条等待的倒计时：每帧扣一格，扣完仍有文本未达成 ⇒ 打 Warning 并按已稳定放行。</summary>
        IEnumerator CountdownWait(TextWait wait)
        {
            while (!wait.done && wait.pending > 0 && wait.framesLeft > 0)
            {
                yield return null;
                wait.framesLeft--;
            }

            if (wait.done || wait.pending <= 0) yield break;

            // WHY: 超时也回调 —— 面板里可能有始终不激活的文本节点（finished 永远到不了），不能因此把面板卡死。
            Log.Warning($"[{GetType().Name}] WaitAllTextFinished 超时（{wait.limit} 帧），仍有 {wait.pending} 个文本未稳定，按已稳定放行");
            FinishWait(wait);
        }
        #endregion

        #region 呼吸灯闪烁
        /// <summary>呼吸灯：让一组 Image 的透明度按正弦在 minAlpha~maxAlpha 之间往复，period 为一个完整呼吸周期（秒）。</summary>
        // WHY: 原实现 `Color.white * Mathf.Sin(time)` 是把 RGB 与 alpha 一起乘 sin —— sin 取负值时颜色发黑、alpha 变成非法负值，
        //      而且会把 Image 原始颜色强行拉白；这里只改 alpha，并用 (sin + 1) / 2 把值域从 [-1,1] 映射到 [0,1] 后再 Lerp 到区间。
        // WHY: 原实现 `time += Time.deltaTime` 却 `yield return new WaitForSeconds(sequence)`，累加与恢复不同步，实际每 sequence 秒才跳变一次；
        //      改为每帧推进（yield return null），呼吸才连续可控。
        protected IEnumerator BreathLightenAnim(List<Image> images, float period, float minAlpha = 0.3f, float maxAlpha = 1f)
        {
            if (images == null || images.Count == 0)
            {
                yield break;
            }

            // WHY: period 为 0 会让 sin 的相位恒定，退化成常亮，这里兜一个默认周期
            if (period <= 0f)
            {
                period = 1.5f;
            }

            float time = 0f;

            while (true)
            {
                float k = (Mathf.Sin(time / period * Mathf.PI * 2f) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, k);

                for (int i = 0; i < images.Count; i++)
                {
                    var image = images[i];
                    // WHY: 元素可能被销毁，逐个判空，避免整个协程抛 NullReferenceException 停掉
                    if (image == null)
                    {
                        continue;
                    }

                    var color = image.color;
                    color.a = alpha;
                    image.color = color;
                }

                time += Time.deltaTime;
                yield return null;
            }
        }
        #endregion
    }
}
