# Contract: TextFinishTracker

Role: internal plain class (no MonoBehaviour hooks, no coroutines); the `finished` state machine — achieved layer, frame budget and registered callbacks. TextComponent drives it from a coroutine.

Fields:
requests:List<Request>  registered "when stable" callbacks (never de-duplicated)
Request.layer / Request.callback  one request: wait until this layer, then call
Reached:TextFinishLayer  the layer achieved so far, reset to Write by Begin()
Running:bool  whether this round is still waiting for a higher layer
LayoutRequested:bool  whether the panel rebuild was already requested (false while inactive, retried in OnEnable)
Frames:int  frames spent in this round
layoutFrame:int  private; the frame the rebuild was requested on, used to confirm Layout at the end of the next frame

Methods:
Begin()  start a round: reset the achieved layer and the frame count; **registered callbacks are kept** and wait for this round
MarkTypography()  step 3 settled (avoidance finished, or not applicable)
NoteLayoutRequested(frame)  the panel rebuild was requested; Layout is confirmed at the end of the next frame
SkipLayout()  no panel ancestor: take Layout as achieved in the same frame
ForceSettle()  frame budget exhausted: release to Layout so a non-converging text cannot block callers
Advance(frame)  tick one frame: bump the count, then confirm Layout when the requested frame has passed; returns whether the round is done
Add(layer, callback, invoke)  register; if the layer is already achieved the callback fires **in the same frame**, otherwise it is parked
Complete(invoke)  end the round: fire every callback whose layer was achieved (each through `invoke`, which isolates exceptions); higher-layer ones stay parked
Clear()  drop every callback (component destroyed)

Notes:
- `Running` and `Reached` are the only things a consumer indirectly sees; there is deliberately no public polling property — the agreed interface is `OnFinished(Action, layer)`.
- The frame budget lives in `TextComponent.FinishFrameLimit`; the tracker only counts, so the two halves stay independent.
- Because a callback registered for a higher layer stays parked, a caller waiting for `Layout` on a text that never gets there is released by `ForceSettle`, not by dropping its request.
