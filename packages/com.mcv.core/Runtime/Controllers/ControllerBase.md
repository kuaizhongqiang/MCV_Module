# Contract: ControllerBase<TView>

Role: base class for controllers; a plain C# class whose lifetime GlobalControllerMgr drives (OnInit -> Bind -> OnDispose).

Fields:
ControllerName:string  property; the concrete type name
View:TView  property; the bound panel

Methods:
static ControllerBase()  registers typeof(ControllerBase<TView>) into GlobalControllerMgr's type table
OnInit()  virtual; permanent subscriptions (runs once, before any panel binds)
OnViewBound()  virtual; register view listeners here (clear then add on every rebuild)
OnDispose()  virtual; permanent unsubscriptions (runs once, after coroutines are stopped)
Bind(panel)  panel is TView -> set View + OnViewBound(); else Log.Error type mismatch
Run(IEnumerator)  start a coroutine hosted by GlobalControllerMgr and tracked per controller
Halt(Coroutine)  stop one coroutine started via Run
HaltAll()  stop every coroutine of this controller
ClearView()  drop the View reference without firing OnViewBound

Notes:
- No MonoBehaviour: controllers do not live in a scene. GlobalControllerMgr creates one instance per type in DelayInit and keeps it for the app lifetime.
- Coroutines have no per-object host any more. They run on GlobalControllerMgr (DontDestroyOnLoad) and are grouped per controller, so OnDispose/quit stops them exactly like the old "controller destroyed -> coroutine stops" behaviour.
- The View is rebuilt by the Canvas; the panel lifecycle finds this controller (type first, then the 1:1 name convention TitlePanel -> TitleController) and calls Bind, so a rebuild binds a brand-new panel without polling or event matching.
- Data flow is one-way: Controller -> View and Controller -> Service.Instance; the controller never holds a Canvas.
- OnViewBound runs again after every rebuild, so any subscription there must clear before adding.
- Lifecycle ordering matters: registration happens in DelayInit, before 1_Content loads, so it is always earlier than the first panel Start -> Bind.
