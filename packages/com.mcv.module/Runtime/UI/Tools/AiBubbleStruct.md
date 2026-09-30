# Contract: AiBubbleStructBase (+ AiSystem/AiUser/AiAssistant subclasses)

Role: AI chat bubble wrapper; fetches its prefab from the UI package via UIPrefabUtil in the constructor, writes plain text and rebuilds the layout.

Fields:
parent:Transform  the panel node the bubble is instantiated under
content:Text  bubble body text component
contentComp:TextComponent  正文节点上的组件，与 content 在 CreateBubble 的同一处解析（含 SetText 的懒创建路径）；TMP 形态下组件会卸载节点上的 Legacy Text（content 随即成"假 null"），不持有它写读都会静默失效
bubble:GameObject  the instantiated bubble; null when the prefab is missing
PrefabName:string  [abstract] UI-package prefab name of the bubble prefab (see UIPrefabUtil)
BubbleName:string  [abstract] name given to the instantiated bubble
ContentChildIndex:int  [abstract] child index the body Text is taken from
reasoningContent:Text  [AiAssistantBubbleStruct] reasoning text, resolved lazily
reasoningContentComp:TextComponent  [AiAssistantBubbleStruct] 思考区节点上的组件，与 reasoningContent 在构造函数、以及 SetReasoningText 的懒解析处一起缓存；不持有它思考文本静默写不进去
reasoningBubble:GameObject  [AiAssistantBubbleStruct] reasoning node toggled by SetReasoningBubbleActive

Methods:
AiBubbleStructBase(parent)  instantiate the bubble immediately
CreateBubble()  UIPrefabUtil.Get(PrefabName) -> Instantiate -> apply BubbleName -> resolve content -> supportRichText = false; warns and returns null when the prefab is missing
GetText(parent)  virtual; default is parent.GetChild(0).GetChild(ContentChildIndex)
GetTextComponent(parent)  virtual; same hierarchy path as GetText, component from the same node (TMP form keeps only the component)
SetText(text)  no-op only when neither content nor contentComp is present; creates the bubble on demand (resolving the component on that path too) -> skips when the text is unchanged -> RebuildLayout(contentComp.transform)
ReadContent()  protected; read the body back, preferring contentComp.RawText
SetTextPlain(text)  same as SetText, kept as the streaming entry point
RebuildLayout(anchor)  protected; resolve the anchor (parameter -> contentComp -> content -> bubble) then UILayoutRebuilder.RebuildChain(anchor, parent.parent): bottom-up over the same span as before (child before parent), instead of one ForceRebuildLayoutImmediate on the outermost node
AiAssistantBubbleStruct(parent)  cache the body, the reasoning bubble and the reasoning text component from the freshly instantiated bubble, then hide the reasoning bubble (belt and braces: the prefab now ships Reasoning_Bubble inactive as well, the area is opt-in)
AiAssistantBubbleStruct.SetContentText(text)  same as SetText
AiAssistantBubbleStruct.SetReasoningBubbleActive(active)  lazily resolve the reasoning bubble -> SetActive
AiAssistantBubbleStruct.SetReasoningText(text)  no-op only when neither reasoningContent nor reasoningContentComp is present (lazily resolving both) -> skips when unchanged -> RebuildLayout(reasoningContentComp.transform)
AiAssistantBubbleStruct.SetReasoningTextPlain(text)  same as SetReasoningText
AiAssistantBubbleStruct.GetReasoningBubble(root) / GetReasoningText(root) / GetReasoningTextComponent(root)  prefab-layout lookups (root -> Child0 -> Child1; inside it -> Child0 -> Child1)

Notes:
- RebuildLayout stays synchronous on purpose: this is a plain class (UI/Tools convention: plain classes never hold a coroutine) and in TMP form a text write is only buffered until the component is ready, so the very first chunk of a stream can measure an empty text; the bubble self-corrects on the next chunk. A fully deferred refresh would need the host panel to start `UILayoutRebuilder.RebuildChainNextFrame` for it.
- A missing prefab is degraded, never thrown: bubble/content end up null, so every call site must null-check.
- Text is written as plain text (supportRichText = false, set in CreateBubble) so any <xxx> the AI emits is displayed verbatim; re-enabling rich text silently eats those tags.
- The reasoning area is opt-in on three levels: the prefab ships `Shape/Reasoning_Bubble` **inactive** (changed 2026-09-29, the UI bundle was rebuilt for it), the constructor hides it again so every creation path (AddAssistantMessage / SetAssistantText / BeginAssistantReply) is covered, and the panel only opens it for non-blank reasoning.
- The prefab name, the bubble name and the child indices are hard-wired to the prefab hierarchy: renaming a prefab or reordering children shows the wrong node instead of failing.
- The Assistant bubble layout is fixed: root -> Child0 -> Child1 = reasoning bubble, Child2 = body text; inside the reasoning bubble -> Child0 -> Child1 = reasoning text.
- A bubble created in the constructor means the panel must pass a parent that already exists; a later SetText can still create it on demand.
