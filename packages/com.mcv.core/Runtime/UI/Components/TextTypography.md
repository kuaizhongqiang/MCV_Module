# Contract: TextTypography

Role: internal static class; the pure (side-effect free) half of the CJK typography — writing and undoing the NBSP indentation. Punctuation avoidance needs a control's line layout, so it stays in TextComponent.

Fields:
Nbsp:const string  "\u00A0"; indentation is always built from it, because ordinary spaces are what the undo logic replaces
Eight:static readonly string  eight NBSPs; the one indentation unit used for the first line and after every newline
LeadingPunctuation:static readonly Regex  the punctuation that may not start a line

Methods:
IsLeadingPunctuation(c)  whether that character is one of the line-start punctuation marks
BuildDisplay(source)  source -> display text: strip any previous markers first (idempotence), then spaces -> NBSP, then 8 NBSPs after each newline and at the front
StripIndent(s)  remove only the markers this component wrote (idempotent; ordinary spaces inside the body are untouched)
Remove(s)  display text -> source (the public `TextComponent.Remove` forwards here)
RemoveNewlines(s)  drop every newline (the public `TextComponent.RemoveNewlines` forwards here)

Notes:
- Idempotence is what makes re-application safe: BuildDisplay always strips before it writes, otherwise a second pass would stack indentation on an already-indented string.
- Never replace a normal space with an NBSP anywhere outside these methods: `TextComponent.rawText` must keep the author's real spaces, or reading the text back would give NBSPs.
