# Contract: TextControlSeed

Role: internal structs holding a snapshot of the control being swapped out, so the "node's own settings" survive the swap in both directions (the file holds `LegacySeed` and `TmpSeed`).

Fields:
LegacySeed.wrap / verticalOverflow / raycast / richText / alignment  the Legacy control's node settings, in Legacy terms
TmpSeed.wrap / overflow / raycast / richText / alignment  the TMP control's node settings, in TMP terms

Methods:
LegacySeed.Capture(Text)  snapshot a Legacy control; MUST be called before it is disabled and destroyed
LegacySeed.ApplyTo(Text)  fallback path: write the snapshot back to a Legacy control unchanged
LegacySeed.ApplyTo(TextMeshProUGUI)  success path: convert the snapshot to TMP terms
TmpSeed.Capture(TextMeshProUGUI)  snapshot a TMP control; same "before destroy" rule
TmpSeed.ApplyTo(TextMeshProUGUI)  fallback path: write the snapshot back to a TMP control unchanged
TmpSeed.ApplyTo(Text)  success path: convert the snapshot to Legacy terms

Notes:
- Capture has to happen before the disable/destroy pair: `Destroy` only takes effect at the end of the frame, and after it the reference reads as a fake null and every field is gone.
- `raycastTarget` and `richText` must always be carried over: the first or clickable text silently stops receiving clicks, the second turns off the "plain text" requirement the AI bubble relies on (TMP's `richText` defaults to true, so without copying it an AI answer containing `<xxx>` would be swallowed as a tag).
- The font is deliberately not part of the seed: both forms get their font from `fontId` through the style path, so carrying it would only be overwritten immediately.
- `wrap` / `overflow` / `alignment` keep "what the form looked like" as the baseline for `Auto`, which the style path intentionally leaves alone.
