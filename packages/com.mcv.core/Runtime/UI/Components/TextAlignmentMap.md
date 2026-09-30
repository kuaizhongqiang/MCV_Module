# Contract: TextAlignmentMap

Role: internal static class; the two-form font-style / alignment lookup tables and their application to the current control (TMP enum values are bit combinations, so the mapping is written out explicitly instead of computed).

Fields:
(none)

Methods:
ToTmpFontStyle(style)  Legacy `FontStyle` -> TMP `FontStyles` (Normal / Bold / Italic / BoldAndItalic)
ToTmp(anchor)  Legacy `TextAnchor` -> TMP `TextAlignmentOptions` (the nine-grid only; anything else falls back to TopLeft)
ToAnchor(options)  TMP `TextAlignmentOptions` -> Legacy `TextAnchor`; the reverse of ToTmp, used when seeding a Legacy control from a TMP one
ApplyTo(Text, alignment)  push an `OverrideAlignment`; Auto and Justified leave the node's own value alone because Legacy has no justified anchor
ApplyTo(TextMeshProUGUI, alignment)  push an `OverrideAlignment`; Auto writes nothing, because the seed value already covers it

Notes:
- Extracted from TextComponent so the component keeps only the state machine; the tables are pure and reused by both the style path and the swap seeding path.
- Keep the two directions consistent: ToTmp and ToAnchor must stay exact inverses, or a swap round-trip would drift the alignment.
- The tables are `internal`: they are an implementation detail of the text components, not framework API.
