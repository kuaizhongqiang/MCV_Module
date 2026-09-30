# Contract: TextComponentSwapper

Role: internal static class; the form swapper — the symmetric Legacy<->TMP component exchange plus the three-step failure fallback. Every method takes the host component, so TextComponent owns no swap logic itself.

Fields:
(none)

Methods:
SwapToTmp(host)  IEnumerator; Legacy -> TMP in the invariable order (1) disable (2) unload (3) wait one frame (4) add TMP (5) seed (6) finish
SwapToLegacy(host)  IEnumerator; TMP -> Legacy, exactly symmetric; the font is not carried over (it comes from fontId)
Fail(host)  private; step 3 of the fallback — keep writing buffered, retry **once** on the next frame, then park at `AssemblePhase.Failed` and log without throwing
RetryNextFrame(host)  private IEnumerator; one retry in the same direction

Notes:
- The order is not negotiable: the old control must be **disabled before** the destroy (Legacy Text and TMP_Text share one CanvasRenderer, and `Destroy` only lands at the end of the frame, so disabling alone is what prevents doubled glyphs) and it must be **unloaded** rather than merely disabled, because Unity refuses `AddComponent` while another Graphic still sits on the GameObject (it returns null).
- The three-step fallback: (1) log an error, (2) re-add the **pre-swap** form as a fallback, (3) if even that fails, keep the component writing into `pending`, retry once on the next frame, then stop at `Failed` — a node may end up without a text control, but nothing throws.
- The seed is captured before the disable/destroy pair; see `TextControlSeed.md`.
- The host's internal surface used here (`LegacyTarget` / `TmpTarget` / `Swapping` / `SwapPending` / `SwapRetried` / `CurrentPhase` / `CompleteSwap` / `EnterPhase` / `WantsTmpForm`) exists for this class only — do not widen it without a second consumer.
- Starting the coroutine is always the host's job (`host.StartCoroutine`), which is also why an inactive host only sets `SwapPending` and lets `OnEnable` drive the swap.
