# Contract: VideoTool

Role: static wrapper around Unity's native VideoPlayer; the only way the framework plays video (no plug-in player, no player abstraction).

Methods:
InitVideoPlayer(player)  playOnAwake / isLooping / waitForFirstFrame off, aspect Stretch
VideoPlayerPreload(player, path, onComplete)  set the url and Prepare; onComplete fires on prepareCompleted and on errorReceived
Play(player)  Play (an unprepared player prepares implicitly)
Stop(player) / Pause(player) / Resume(player)  playback control
SetTime(player, time) / GetTime(player)  current playback time in seconds
GetDuration(player)  total length in seconds, 0 while not prepared
SetVolume(player, volume) / GetVolume(player)  SetDirectAudioVolume(0, clamp01); effective in Direct output mode only
IsPlaying(player)  playback state
CreateVideoPlayer(host)  null host -> null; Get/AddComponent<VideoPlayer> -> InitVideoPlayer -> the native player

Notes:
- This file must stay free of third-party player types: the framework ships no player plug-in and defines no abstraction for one.
- onComplete fires on failure too and both handlers are unsubscribed first, otherwise a failing video would leave the caller waiting forever.
- No method null-checks the player: passing a destroyed player throws. Callers (TaskPrinciplePanel) hold the null guard.
- Volume only applies in Direct audio output mode; other modes ignore SetDirectAudioVolume.
