# Contract: CoreEvent (+ GlobalInteractionType)

Role: payload definitions for the global EventBus; audio, camera, scene, clip-ready, login, UI state, room menu, global interaction, mouse-move, step and process, dialog and app-quit events.

Fields:
AudioVolumeEventData.SourceType:AudioSouceType / TargetVolume:float
AudioPlayEffectEventData.EffectType:AudioEffectType
AudioPlayEventData.AudioName:string / SourceType:AudioSouceType
CameraBgChangeEventData.IsSkybox:bool
CameraBlendChangeEventData.IsCut:bool / BlendTime:float
SceneLoadingEvent.SceneName:string (get) / Progress:float (get, set)
SceneLoadedEvent.SceneName:string (get)
ClipReadyEvent.ClipId:string (get)
LoginSuccessEvent.User:UserData
SceneStateChangeEventData.State:SceneState
TaskTypeChangeEventData.Clip:ProjectClip / TaskType:TaskType
SceneSwitchRequestEvent.SceneName:string (get)
RoomMenuEnterRequestEvent.Clip:ProjectClip (get)
GlobalInteractionEventData.Target:InteractiveBase / Type:GlobalInteractionType / Delta:Vector2 / s_Pool (static stack)
MouseMoveStateEventData.State:MouseMoveState / IsIdle:bool
StructInteractiveCompletedEvent.clip:ProjectClip
StepJumpRequestEvent.StepIndex:int
ProcessingJumpRequestEvent.ProcessingIndex:int / StepIndex:int
DialogRequestEvent.Id:DialogId / Content / ConfirmLabel / CancelLabel:string, ShowConfirm / ShowCancel:bool
DialogResultEvent.Id:DialogId / Confirmed:bool

Methods:
GlobalInteractionEventData.Get(target, type, delta)  static; pops an instance from the pool
GlobalInteractionEventData.Release()  clears the fields and returns the instance to the pool
DialogRequestEvent(id:DialogId, content, showConfirm, showCancel, confirmLabel, cancelLabel)  id is the result-claim key; button labels default to the Chinese literals
(all payload types)  plain constructors taking the fields above

Notes:
- EventBus<T> is constrained to `where T : class`, so every payload must be a class and never a struct.
- GlobalInteractionEventData is pooled and published synchronously: the publisher calls Release() once Publish returns, so a reference must not be kept across frames.
- ClipReadyEvent fires only after all AssetBundles of a ProjectClip have loaded. A panel's OnViewBound always runs earlier, so a synchronous fetch can be empty and consumers must subscribe this event to re-fetch.
- RoomMenuEnterRequestEvent must be handled by a persistent object (MenuController on ControllerRoot): the roaming room scene and its HUD publisher are unloaded when entering content, while the confirmation arrives later.
- MouseMoveStateEventData publishes only on state flips, so consumers use it directly without a pool.
- SceneSwitchRequestEvent means "load the new scene first, then unload the old one"; the order is part of the contract.
- DialogRequestEvent default button labels are the Chinese literals and must not be changed.
- GlobalInteractionType member order (Enter, Exit, Down, Up, Click, ClickRight, ClickDouble, Move) is config-sensitive and must not be reordered.
