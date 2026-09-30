# Contract: CameraBg

Role: camera background quad; sizes itself from the camera FOV and aspect and cross-fades between two AB-loaded textures.

Fields:
BgPackageIds:string[]  the background image package-config ids; index order maps to the material _Texture_1 / _Texture_2 slots
cam:Camera  the target camera, resolved once in DelayInit
screenSize:Vector2  last size used to size the quad
material:Material  the quad's material, the target of the texture writes
TexFadeCoroutine:Coroutine  the running cross-fade

Methods:
Update()  recomputes the quad size only when fov, aspect or local Z changed
SetScale()  size the quad from fov, aspect and local Z
InitMesh()  grab the MeshRenderer and its shared material
SmoothFadeTexture(bool)  restart the cross-fade toward 0 or 1
FadeTexture(bool)  one-second lerp of the _CutTex cut parameter
SetTwoTexture(Texture2D, Texture2D)  write _Texture_1 / _Texture_2
LoadBgTextures()  load both backgrounds from their AB packages as Sprite and take .texture
DelayInit()  coroutine; waits one frame, then resolves the camera and loads the textures

Notes:
- DelayInit must wait one frame before touching the camera: StartCoroutine runs the body synchronously up to the first yield and this component may be created by GlobalCameraMgr's Instantiate, so resolving the camera in Awake would recurse (Instantiate -> Awake -> Instantiate) and overflow the stack.
- BgPackageIds order must match the ids in Assets/Editor/BuildTools/CameraBgBundleTools.cs and the material's _Texture_1 / _Texture_2 slots, otherwise the two images swap.
- The textures are imported as Sprite, so the package's main asset is a Sprite and .texture is taken after loading as Sprite.
