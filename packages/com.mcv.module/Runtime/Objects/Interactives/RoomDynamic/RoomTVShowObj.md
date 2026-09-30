# Contract: RoomTVShowObj

Role: auto-carousel of images on the room TV / display screen (slide-show style); each switch runs a single 0->1->0 pulse.

Fields:
showPics:List<Texture2D>  the carousel frames, played in order
freezeTime:float  [SerializeField] seconds each frame stays; default 10
randomPercent:float  [SerializeField,Range(0,1)] random spread of the stay duration; default 0.5
animDuration:float  [SerializeField] total length of one switch animation in seconds; default 2
matIndex:int  [SerializeField] material slot index on the renderer; default 1
minColor:Color  [SerializeField] darkest color of the switch
maxColor:Color  [SerializeField] normal (HDR) brightness
MainTexName / SecondTexName / LerpName / BlurLerpName / ColorName  const material property names
curIndex:int / curTex / nextTex:Texture2D  current and next frame
targetRenderer:Renderer / targetMaterial:Material  target renderer and cloned material instance
playCoroutine:Coroutine  the carousel coroutine
isHovering:bool  hovering (pauses the stay timer)

Methods:
Awake()  base -> CacheTarget
OnEnable()  validate material and frame count -> ResetToCurrent -> clear isHovering -> start PlayRoutine
OnDisable()  stop the carousel coroutine
CacheTarget()  resolve the renderer and the matIndex material (cloned through materials)
ResetToCurrent()  reset the parameters to the current frame at rest
PlayRoutine()  loop: stay -> switch to the next frame
WaitFreeze(float)  pausable wait (time does not advance while isHovering)
PlayOneSwitch(int)  one pulse; commits the next frame at the peak (t >= 0.5)
Pulse(float)  static 0->1->0 sine curve
CommitSwitch(int)  write the A slot, reset the parameters, advance the cursor
GetFreezeTime()  stay duration including the randomPercent spread
SetLerp / SetBlur / SetColor  write the material parameters
MoEnterEvent / MoExitEvent  set isHovering (pause / resume the dwell timer)
MoClickEvent  empty

Notes:
- At the pulse peak the next frame must also be written into the A slot (_MainTex), otherwise the falling half jumps back to the old image.
- materials[] clones at runtime so the shared material asset on disk is never written to.
- The whole animDuration is one synchronous pulse of _Lerp / _BlurLerp / _Color; there is no four-segment subdivision in the code.
