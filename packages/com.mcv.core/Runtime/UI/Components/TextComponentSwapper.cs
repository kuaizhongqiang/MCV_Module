using System.Collections;
using MCV_Module.Models;
using MCV_Module.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    /// <summary>形态换向：双向对称的换组件流程与失败三级降级（两个 Graphic 槽位、播种、重试都收在这里）。</summary>
    internal static class TextComponentSwapper
    {
        /// <summary>Legacy→TMP：① 禁用 ② 卸载 ③ 等一帧 ④ 挂 TMP ⑤ 播种 ⑥ 收尾；顺序不可变。</summary>
        public static IEnumerator SwapToTmp(TextComponent host)
        {
            host.Swapping = true;
            host.SwapPending = false;

            Text dying = host.LegacyTarget;
            host.LegacyTarget = null;
            if (dying == null)
            {
                host.TmpTarget = host.gameObject.AddComponent<TextMeshProUGUI>();
                if (host.TmpTarget == null) { Fail(host); yield break; }
                host.CompleteSwap();
                yield break;
            }

            LegacySeed seed = LegacySeed.Capture(dying);

            dying.enabled = false;                                              // ① 先禁用
            Object.Destroy(dying);                                              // ② 再卸载（帧末生效）
            yield return null;                                                  // ③ 等一帧，让 CanvasRenderer 空出来

            host.TmpTarget = host.gameObject.AddComponent<TextMeshProUGUI>();   // ④ 挂 TMP
            if (host.TmpTarget == null)
            {
                // 三级降级 ①②：换向失败不能让节点空白，也不能抛异常（③ 在 Fail）。
                Log.Error($"[TextComponent] 「{host.name}」换到 TMP 失败（节点上仍有其他 Graphic？），回挂原形态 Legacy", host);
                host.LegacyTarget = host.gameObject.AddComponent<Text>();
                if (host.LegacyTarget != null)
                {
                    seed.ApplyTo(host.LegacyTarget);
                    host.CompleteSwap();
                    yield break;
                }
                Fail(host);
                yield break;
            }

            seed.ApplyTo(host.TmpTarget);                                       // ⑤ 先播种，再由 ApplyStyle 的显式字段覆盖
            host.CompleteSwap();                                                // ⑥ 收尾
        }

        /// <summary>TMP→Legacy：与 <see cref="SwapToTmp"/> 完全对称；字体不搬（按 fontId 下发）。</summary>
        public static IEnumerator SwapToLegacy(TextComponent host)
        {
            host.Swapping = true;
            host.SwapPending = false;

            TextMeshProUGUI dying = host.TmpTarget;
            host.TmpTarget = null;
            if (dying == null)
            {
                host.LegacyTarget = host.gameObject.AddComponent<Text>();
                if (host.LegacyTarget == null) { Fail(host); yield break; }
                host.CompleteSwap();
                yield break;
            }

            TmpSeed seed = TmpSeed.Capture(dying);

            dying.enabled = false;                                          // ① 先禁用
            Object.Destroy(dying);                                          // ② 再卸载（帧末生效）
            yield return null;                                              // ③ 等一帧，让 CanvasRenderer 空出来

            host.LegacyTarget = host.gameObject.AddComponent<Text>();       // ④ 挂 Legacy
            if (host.LegacyTarget == null)
            {
                Log.Error($"[TextComponent] 「{host.name}」换到 Legacy 失败（节点上仍有其他 Graphic？），回挂原形态 TMP", host);
                host.TmpTarget = host.gameObject.AddComponent<TextMeshProUGUI>();
                if (host.TmpTarget != null)
                {
                    seed.ApplyTo(host.TmpTarget);
                    host.CompleteSwap();
                    yield break;
                }
                Fail(host);
                yield break;
            }

            seed.ApplyTo(host.LegacyTarget);                                // ⑤ 播种（字体不搬）
            host.CompleteSwap();                                            // ⑥ 收尾
        }

        /// <summary>三级降级的 ③：兜底也失败 ⇒ 写入继续缓冲、下帧重试一次，再失败则 Failed + 记错、不抛异常。</summary>
        static void Fail(TextComponent host)
        {
            host.Swapping = false;
            if (!host.SwapRetried)
            {
                host.SwapRetried = true;
                host.SwapPending = true;
                if (host.isActiveAndEnabled) host.StartCoroutine(RetryNextFrame(host));
                return;
            }

            host.EnterPhase(AssemblePhase.Failed);
            host.SwapPending = false;
            Log.Error($"[TextComponent] 「{host.name}」换形态两次均失败，节点暂无文本控件（后续写入继续缓冲在 pending）", host);
        }

        /// <summary>下帧按同一方向重试一次换向，再失败即进入 <see cref="AssemblePhase.Failed"/>。</summary>
        static IEnumerator RetryNextFrame(TextComponent host)
        {
            yield return null;
            if (host.CurrentPhase != AssemblePhase.Assembling || !host.SwapPending) yield break;
            host.SwapPending = false;
            if (TextComponent.WantsTmpForm()) yield return SwapToTmp(host);
            else yield return SwapToLegacy(host);
        }
    }
}
