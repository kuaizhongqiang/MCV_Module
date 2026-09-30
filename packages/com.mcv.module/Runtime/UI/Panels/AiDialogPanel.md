# Contract: AiDialogPanel

Role: AI chat panel (View); renders the bubble list, collects input, owns the panel on/off button (it slides the body `Shape` in and out) and the model-list assembly. The conversation itself lives in the controller and the EXE.

Fields:
bubbleList:List<AiBubbleStructBase>  created bubbles in display order
assistantContent:StringBuilder  accumulated streaming body text
assistantReasoning:StringBuilder  accumulated streaming reasoning text
bubbleParent:Transform  parent the bubbles are instantiated under (required, checked in Awake) — prefab node Shape/Layout/ContentPart/Viewport/Content
inputField:InputField  user input field, also the submit source
summitBtn:Button  send button
infoText:Text  bottom info / error line
m_InfoTextComp:TextComponent  the info node's component, cached in Awake; in TMP form the component unloads the node's legacy Text, so the Text field becomes a fake null and SetTextOn silently no-ops — writes must go through the component
modelSwitchBtn:Button  the panel on/off button (prefab node AiSwitchBtn); it sits *outside* Shape (x=25 vs Shape's 135), so it stays clickable while the body is slid out
modelSwitchBtnText:TextComponent  the button's label node (bound to AiSwitchBtn/bg/TaskNameText); unassigned -> resolved in Awake as the button's first TextComponent
modelSwitchToggleParent:Transform  the list Layout inside ProviderPart (Shape/ProviderPart/Layout); the panel only writes items into it
movePanel:RectTransform  the panel body that slides (prefab node Shape)
closeXValue / openXValue:float  anchoredPosition.x for hidden (-600) / shown (135); the prefab ships Shape already at 135 = open
ToggleListModelPrefabName  const UI-package prefab name "ModelListToggle" (see UIPrefabUtil)
ModelProviderTextNodeName / ModelNameTextNodeName  const child node names inside that fragment ("ProviderText" / "ModelNameText") — a fragment prefab cannot be referenced field-wise from the panel
ModelSwitchBtnTextKey  const "ui.ai.title"; the language key the button label is driven by
toggleDict:Dictionary<string,Toggle>  list-item identity ("provider - model") -> Toggle, used to re-check the current model
toggleProvider:Dictionary<Toggle,string>  list item -> provider name (reported on user selection)
toggleModel:Dictionary<Toggle,string>  list item -> model name (reported on user selection)
suppressToggleEvent:bool  set while the panel checks a toggle itself, so that change is not treated as a user selection
selectedProvider / selectedModel:string  mirror of the current model (truth lives in the controller); only drives the re-check
OnSendRequested:event Action<string>  raised on submit, subscribed by the controller (clear before add)
OnModelSelected:event Action<string,string>  raised when the user picks a model (provider, model)
OnPanelOpened:event Action  raised when the button opens the body (the controller uses it to retry a failed catalogue fetch)
currentBubble:AiBubbleStructBase  bubble currently being filled
hasReasoning:bool  whether the reasoning area was already expanded for this reply
HasMessage:bool  whether any bubble exists (drives the one-time welcome line)
IsPanelActive:bool  read-only; whether the body is open (anchoredPosition.x == openXValue)
panelAnimCoroutine:Coroutine  running body animation
moveCanvasGroup:CanvasGroup  CanvasGroup on movePanel for the fade part of the slide
duration:float  body animation length, 0.3s

Methods:
Awake()  cache infoText's TextComponent -> bind the send button / input submit / panel on-off button -> drive the button label with ModelSwitchBtnTextKey -> clear bubbleParent -> bind the rest; logs an error and returns when bubbleParent is missing
OnDestroy()  unbind the send button, the input field and the on-off button -> ClearModelList -> base
AddSystemMessage(text) / AddUserMessage(text) / AddAssistantMessage(text)  create the matching bubble, write the text (markdown converted), scroll to bottom
CreateSystemBubble() / CreateUserBubble() / CreateAssistantBubble()  create that bubble and make it current
SetSystemText / SetUserText / SetAssistantText(text)  write the current bubble, creating one first -> markdown -> RichText
SetAssistantTextPlain(text)  streaming body write, no markdown
SetAssistantReasoningText(text) / SetAssistantReasoningTextPlain(text)  write the reasoning area (converted / plain)
ClearAll()  destroy every bubble, reset the buffers and flags
BeginAssistantReply()  reset the buffers, create the assistant bubble, hide the reasoning area
AppendAssistantContent(delta) / AppendAssistantReasoning(delta)  append a delta and show it plain; the reasoning area expands on the first **non-blank** delta (whitespace-only reasoning is dropped, so a provider that emits no thinking never shows an empty reasoning bubble)
FinalizeAssistantReply()  the single markdown -> RichText conversion of body and reasoning; a blank reasoning buffer just hides the reasoning area instead of writing it
SubmitInput()  trim the field, raise OnSendRequested, clear the field
OnInputSubmit(text)  InputField onSubmit adapter
SetInputInteractable(interactable) / SelectInput()  enable or disable and focus the input
SetInfoText(text) / ScrollToBottom()  write the info line through m_InfoTextComp (falls back to TextComponent.SetTextOn only when the node has no component) / jump the parent ScrollRect to the bottom
ApplyCurrentModel(provider, model)  programmatic sync called by the controller: re-check the matching item, without raising OnModelSelected
SetModelListVisible(visible)  show/hide the list root (ProviderPart) and request a layout rebuild when opening; the panel itself never calls it (see Notes) — it is the entry point for whoever owns that list
BuildModelList(result)  clear -> one item per provider x model (label parts written through TextComponent) -> re-check the current model -> layout rebuild; called by the controller after it fetches / from its cache
SetModelToggle(toggle, isOn)  programmatic check, silent (suppressToggleEvent), keeps the list single-choice
OnModelToggleChanged(toggle, isOn)  user pick: unchecking the current item is bounced back, otherwise turn the others off -> remember -> collapse the list -> raise OnModelSelected
OnModelSwitchClicked()  button click: open/close the body (Shape) -> raise OnPanelOpened when opening
ModelListRoot()  the list root node (ProviderPart = modelSwitchToggleParent's parent)
TurnOffOthers(keep)  manual exclusivity (the list root has no ToggleGroup)
FindModelToggle(provider, model)  list item by identity; falls back to model-only matching when provider is empty
ClearModelList()  unsubscribe -> clear the three indexes -> destroy the items
ModelKey(provider, model)  identity string "provider - model" (also the item's node name)
ResolveModelSwitchTextComp()  the button label component, falling back to the button's first TextComponent
CreateBubble<T>()  instantiate the bubble of T under bubbleParent and register it
CreateModelListToggle(providerName, modelName)  instantiate the fragment, name it by identity, write both label nodes, force isOn = false, return its Toggle
SetItemText(item, nodeName, value)  write one label node (TextComponent when present, legacy Text as fallback)
SetPanelActive(isActive)  open/close the body with animation
SetPanelActiveImmediately(isActive)  same without animation; stops a running animation first, warns and skips when movePanel is unbound, restores button interactivity
SetPanelActiveAnim(isActive) / SetPanelActiveAnimCoroutine(isActive)  animate alpha + anchoredPosition over duration; the on-off button is disabled while it runs
EnsureMoveCanvasGroup() / StopPanelAnim() / SetModelSwitchInteractable(v)  get-or-add the body CanvasGroup / stop a running animation / guard the button (null-safe)

Notes:
- Data flow is fixed: input -> SubmitInput -> OnSendRequested -> controller; the controller writes back through AddUserMessage / BeginAssistantReply / AppendAssistantContent / AppendAssistantReasoning.
- Streaming must stay plain text: markdown tokens such as ** or <br> flicker while the message is incomplete, so the conversion happens exactly once in FinalizeAssistantReply.
- bubbleParent is mandatory: Awake logs and returns without it; the on-off button wiring is deliberately bound *before* that early return so a bad bubbleParent cannot also kill the panel toggle.
- The panel is rebuilt with its Canvas and keeps no cross-state data; the first-time welcome message bubble and the model re-check are driven by HasMessage / selectedModel.
- The send event is bound clear-before-add on the controller side, otherwise the handler is subscribed twice.
- The on-off button controls **Shape** (movePanel) only: it is the panel body's slide, and it never touches ProviderPart. Since the button lives outside Shape it survives the collapse, which is what makes the slide reversible.
- The body ships open (Shape's prefab anchoredPosition.x already equals openXValue), so the button's first click collapses it; call SetPanelActiveImmediately(false) if a build should start collapsed.
- ProviderPart (the model list) is *not* driven by this panel: nothing opens or closes it here, its prefab default is inactive, and SetModelListVisible is left for whoever owns that list. The controller still fills the items so the list is ready the moment something shows it.
- The button label goes through the language system: the prefab carries the migration tool's placeholder key `ui.TaskNameText`, which is not in LanguageData (it warns every Awake and falls back to the Chinese literal "AI 问答"), so Awake re-drives the node with the real key `ui.ai.title` ("AI 对话" / "AI Chat"). Change ModelSwitchBtnTextKey, not the node, if the copy should differ.
- Labels always go through TextComponent: the fragment nodes carry a legacy Text *and* a TextComponent, and in TMP form the component unloads the legacy Text, so writing the Text field directly would silently write nothing.
- The fragment's own isOn value is not trusted: items are created unchecked, and the current model is re-checked by ApplyCurrentModel.
- Writes into a hidden list are fine: TextComponent buffers them in `pending` until its assembly (when the node first activates), so building the list while ProviderPart is collapsed is safe.
- The reasoning bubble is opt-in on content: it opens on the first non-blank reasoning delta and is hidden again when the reply ends with nothing but blanks. The node itself starts hidden on two levels — the prefab ships `Shape/Reasoning_Bubble` inactive and AiAssistantBubbleStruct's constructor hides it again.
- Panel body show/hide is separate from UIBase.SetUIActive (root fade + SetActive): the body slide moves Shape and fades its own CanvasGroup. The content page's AI entry was removed on request (ContentFunctionPanel / ContentFunctionController no longer own the panel), so this button is now the only way to open and close it — the panel root stays active and only Shape moves.
