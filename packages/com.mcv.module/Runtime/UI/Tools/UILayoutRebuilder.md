# Contract: UILayoutRebuilder

Role: the single implementation of "re-layout after text / child state changed"; waits a frame then rebuilds bottom-up so that the layout can actually converge.

Methods:
RebuildSubtree(root)  sync; collect the subtree's LayoutGroup / ContentSizeFitter nodes, sort by depth descending, ForceRebuildLayoutImmediate each (child before parent), then rebuild root as a fallback
RebuildChain(node, stopAt)  sync; walk up the parent chain and rebuild every level (for call sites that only know their own text node, e.g. a mouse-following tip box)
RebuildSubtreeNextFrame(root)  IEnumerator; wait a frame -> Canvas.ForceUpdateCanvases -> RebuildSubtree (start it from a panel)
RebuildChainNextFrame(node, stopAt)  IEnumerator; wait a frame -> Canvas.ForceUpdateCanvases -> RebuildChain
Collect(node, depth, entries)  private; recurse the subtree collecting nodes that carry a layout controller
Entry  private struct; rect + depth, used for the depth ordering

Notes:
- Waiting a frame is not optional: in TMP form TextComponent swaps forms across frames (disable and unload the legacy Text, wait one frame, then add the TMP), and a write that arrives before `ready` only lands in its pending buffer; calling ForceRebuildLayoutImmediate in the same frame measures an empty text (the box collapses to padding only).
- Rebuilding bottom-up is not optional either: the panel prefabs are "child node owns a ContentSizeFitter + parent owns a LayoutGroup with childControlWidth = false", so the parent measures the child's *current* sizeDelta. A single LayoutRebuilder pass walks the controllers top-down (PerformLayoutControl), so the parent always reads the child's stale size; only depth-descending rebuilds converge.
- ForceRebuildLayoutImmediate silently drops components that are not activeAndEnabled (LayoutRebuilder.StripDisabledBehavioursFromList), so rebuilding an inactive hierarchy does nothing at all — hence the Verbose log here and the deferred entry point on PanelBase.
- The deferred entry point for panels is `PanelBase.RequestLayoutRebuild(root)`: it adds "one request per frame (re-entrancy guard)" and the inactive skip on top of this class. Call that from panels instead of this class directly; use this class directly only from plain (non-MonoBehaviour) classes, which per the UI/Tools convention never hold a coroutine.
