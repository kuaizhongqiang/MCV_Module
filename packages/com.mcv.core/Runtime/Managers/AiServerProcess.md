# Contract: AiServerProcess

Role: host-process manager for the AiServer EXE: launch, graceful shutdown, Kill fallback; all no-op on WebGL.

Fields:
_client:AiServerClient  protocol client; supplies Host/Port/BaseUrl/AuthName/AuthToken and gets MarkStopped on shutdown
_process:Process  the EXE handle; compiled only under #if !UNITY_WEBGL

Methods:
ExePath  static; StreamingAssets/AiServer/AiServer.exe
TryLaunch()  start the EXE with --host --port --parent-pid; returns early when already running
Kill()  force-kill with a 2s wait; always clears the handle
ShutdownNow()  synchronous close for OnApplicationQuit: POST /v1/shutdown with auth headers -> Kill -> MarkStopped

Notes:
- The file carries #if !UNITY_WEBGL, which precompiled DLLs do not evaluate -> it must be compiled on the Unity source side (MCV.Runtime), never into MCV.AiClient.dll.
- TryLaunch is the callback AiServerClient.EnsureReadyAsync invokes when its health check fails; it guards against launching twice.
- ShutdownNow is synchronous by design: OnApplicationQuit cannot wait for a coroutine.
- Missing EXE -> Log.Error with a build.bat hint; never throws.
- Pure protocol (auth/dialog/log) lives in AiServerClient (MCV.AiClient.dll).
