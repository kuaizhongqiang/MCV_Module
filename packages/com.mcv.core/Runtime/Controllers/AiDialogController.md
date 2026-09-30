# Contract: AiDialogController

Role: AI dialog scheduler between AiDialogPanel and GlobalAiMgr; subscribes the send event, streams assistant bubbles, gates input on warmup, retries while AiServer is not ready, and owns the model catalogue / current model.

Fields:
MAX_RETRY:int  const; max retries when AiServer is not ready
busy:bool  a chat request is in flight (blocks new sends)
warmupWaitCoroutine:Coroutine  warmup-wait coroutine ref; also the once-only guard
cachedModels:AiModelsResult  the fetched catalogue (providers, defaultProvider); null = never fetched. Cached here because the panel is rebuilt on every Canvas rebuild while the catalogue is not
modelsFetching:bool  a catalogue request is in flight (dedupes one request per rebuild); reset on failure and in OnDispose
currentProvider / currentModel:string  the selected model; empty = never selected, the request leaves both fields empty and the EXE uses its own default

Methods:
OnViewBound()  clear then add OnSendRequested / OnPanelOpened / OnModelSelected; before warmup -> disable input + info text (Lang.Get ui.ai.initializing) + StartWaitWarmupOnce(), else input interactable = !busy; seed a greeting bubble when no message; lastly RequestModelList()
OnDispose()  unsubscribe the three view events, reset modelsFetching, drop the warmup coroutine ref -> ClearView
IsWarmupDone()  true when GlobalAiMgr.Instance exists and its IsWarmupDone is set
StartWaitWarmupOnce() / WaitWarmupAndEnableInput()  start the wait coroutine once / poll warmup (max 300 x 0.1s) then enable input or show a failure text
HandleSend(userText)  reject on busy / empty / warmup-incomplete -> add user bubble + BeginAssistantReply -> busy = true -> RunChat(userText, MAX_RETRY)
BuildUserText(userText)  prefix GlobalUIMgr.CurrentStateDescription(); empty state -> user text only
RunChat(userText, retriesLeft)  AiChatRequest(SessionId, text, stream) + currentProvider/currentModel -> ChatAsync(onDelta appends reasoning and content, onDone FinalizeAssistantReply, onError retries while the message says "未就绪")
RetryAfterDelay(userText, retriesLeft)  wait 1s -> RunChat(retriesLeft - 1)
Finish(success, message)  busy = false -> restore input + SelectInput; on failure show the error and Log.Error -> DumpServerLogs
DumpServerLogs()  FetchServerLogsAsync(15) -> Log.Error
RequestModelList()  cache hit -> View.BuildModelList + ApplyCurrentModel; else one FetchModels when not already in flight. Also the handler of View.OnPanelOpened, i.e. the retry path after a failed fetch
FetchModels()  GlobalAiMgr.FetchModelsAsync -> cache the result -> View.BuildModelList -> ApplyDefaultModel when nothing is selected yet, else re-apply the current model; on error reset modelsFetching and Log.Warning (the next panel open retries)
ApplyDefaultModel(result)  pick defaultProvider **when it is configured**, else the first configured provider, else the (unconfigured) defaultProvider, else the first one; model = its defaultModel (fallback: models[0]) -> remember it + View.ApplyCurrentModel
HandleModelSelected(provider, model)  remember the choice (every later request carries it)

Notes:
- Unity is a pure front end: context assembly, history and token truncation all live in AiServer (the EXE); never concatenate here.
- The controller is persistent (ControllerRoot) and outlives canvases, so the panel re-binds on every rebuild: keep the subscription in OnViewBound and clear it before adding.
- The warmup gate (GlobalAiMgr.IsWarmupDone) is the only input permission.
- The warmup info text goes through Lang.Get (ui.ai.initializing, registered in LanguageDataSO): a hard-coded Chinese literal here bypasses the key system and leaves this line Chinese in the English build. Note the AI context itself stays Chinese on purpose (§5); only panel copy is localised.
- The literal "未就绪" in the error string is what drives the retry; rewording it silently disables retry.
- Only session_id + user_text + the selected provider/model go to GlobalAiMgr; history is assembled server-side.
- The provider/model this controller sends must be a **configured** one: the AiServer only falls back to the first configured provider when the request carries no provider, and answers 503 for an explicitly named provider without a key. That is why ApplyDefaultModel prefers `configured` over `default_provider`.
- Chat is rejected with 428 unless the session was warmed up (server.ts: `isWarmupDone` guard), so GlobalAiMgr.enableStartupWarmup = false leaves the panel able to show UI but unable to get answers.
- Model switching is split as View reports / Controller decides: the panel raises OnPanelOpened (body opened) and OnModelSelected (user picked), the controller fetches the catalogue, keeps it and injects provider/model into each request.
- The catalogue is fetched once per bind and re-applied from the cache afterwards (the panel's list is empty after every rebuild), so re-opening the panel never hits the server a second time unless the first attempt failed.
- The fetch callback checks `View == null` before touching it: the panel can be destroyed while the request is in flight, and Unity's == already treats a destroyed object as null.
- The controller's coroutines are stopped by GlobalControllerMgr before OnDispose, which is why modelsFetching has to be reset there — otherwise that request would stay "in flight" forever and the list would never load again.
