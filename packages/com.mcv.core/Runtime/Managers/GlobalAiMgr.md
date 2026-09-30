# Contract: GlobalAiMgr

Role: AI entry point; connects to AiServer (StreamingAssets/AiServer/AiServer.exe) -- creates the protocol client and the host process, holds the session id, waits for readiness, runs the startup warm-up, and shuts the EXE down on quit.

Fields:
enableAi:bool  [SerializeField] master switch, true; false = no client, no EXE, no session, no warm-up, every call refused
enableStartupWarmup:bool  [SerializeField] startup warm-up switch, true; false = still launch/wait for the server, but send no warm-up round
readyTimeoutSeconds:float  [SerializeField] readiness wait, 30 (first launch of the 88MB EXE plus antivirus scan is slow)
_authName:string  [SerializeField] auth name, must match one entry of the EXE's CLIENT_WHITELIST
_authToken:string  [SerializeField] auth token, same whitelist
Client:AiServerClient  protocol client (MCV.AiClient.dll); null when AI is disabled
_process:AiServerProcess  EXE host manager (source side, #if !UNITY_WEBGL); null when AI is disabled
SessionId:string  GUID created once per app run; the warm-up and every chat share it
IsWarmupDone:bool  set when the warm-up round returns warmup_done; with the warm-up switch off it means "no warm-up needed" and is set once the server is ready
_systemPrompt:string  [SerializeField] optional system prompt override
SystemPrompt:string  _systemPrompt when set, else the default tutor prompt
defaultPrompt:AiChatSystemPrompt  [SerializeField] default tutor prompt; edit subject to change discipline
_portablePrompt:string  [SerializeField] optional portable prompt override
PortablePrompt:string  _portablePrompt when set, else the default
IsAiEnabled / IsStartupWarmupEnabled  public read-only views of the two switches (settable at runtime)
IsServerReady / ServerUrl  public read-only views (IsServerReady also requires enableAi)

Methods:
DelayInit()  AI off -> clear client/process/session/warm-up flag, log, isInit, return; else create the client from the inspector credentials, create the process, create SessionId, isInit, then start EnsureReadyAndWarmupAsync(enableStartupWarmup)
OnApplicationQuit()  _process.ShutdownNow() then base
RejectIfAiDisabled(onError, api)  guard for every entry point; true = already refused, the caller should yield break
EnsureReadyAndWarmupAsync(warmup = true)  refuse when disabled -> EnsureReadyAsync(_process.TryLaunch, ready) -> warn, or run the warm-up when not yet done; warmup = false skips the round and sets IsWarmupDone = true so the UI gate opens (the session then sends no warm-up at all, a later re-call finds IsWarmupDone already true); idempotent and safe to re-call
Ask(userText, onDone, onError) / AskStream(userText, onDelta, onDone, onError)  single-shot / streaming chat, both building an AiChatRequest with SessionId
ChatAsync(request, onDelta, onDone, onError)  fill an empty sessionId, ensure readiness, then Client.ChatAsync; success routes to onDone, failure to onError
FetchServerLogsAsync(tail, onResult)  server log tail for troubleshooting; empty string when disabled
FetchModelsAsync(onResult, onError) / FetchInfoAsync(onResult, onError)  provider/model catalogue and service info
StartWarmupAsync()  build AiWarmupRequest (SessionId, SystemPrompt, CurrentStateDescription + PortablePrompt) -> Client.WarmupAsync; success sets IsWarmupDone, failure keeps it false
DefaultSystemPrompt  property; composes the tutor prompt from GlobalDataMgr.ProjectData.ProjectDescription() and MenuData.MenuDataDescription()
DefaultPortablePrompt  property; defaultPrompt.GetPortablePrompt()

Notes:
- Unity is a thin front end: it only displays, takes input, and hands session_id + user_text to the EXE. Session history, system/portable assembly, token truncation, warm-up and tool calls all live in the EXE.
- The warm-up round is the session's history start (prefix continuity helps KVCache hits), and user input must stay blocked until IsWarmupDone.
- enableAi and enableStartupWarmup are independent: the first decides whether the AI module loads at all, the second only whether the startup warm-up round is sent (UI development wants the panel usable without paying for a warm-up every run).
- With enableStartupWarmup = false the session gets no warm-up round: IsWarmupDone is true as soon as the server is ready, so the UI gate opens without waiting. This is a debugging mode for the AI UI (no warm-up cost per run) -- the EXE side effect of a missing warm-up round is not verified, so keep the switch on for shipped builds.
- Credentials are configured on this component and injected at runtime; the DLL holds no hard-coded keys.
- DelayInit must set isInit even when AI is disabled, otherwise the Setup start chain blocks.
- A failed warm-up does not block startup, but IsWarmupDone stays false so user input is refused.
- The EXE handle exists only outside WebGL; on WebGL the manager still probes a remote service.
