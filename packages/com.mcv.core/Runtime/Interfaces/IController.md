# Contract: IController

Role: UI-controller contract; exposes its name, binds a controller to its View by a 1:1 name convention, and has its lifecycle driven explicitly by GlobalControllerMgr.

Fields:
ControllerName:string  controller name; must equal the paired View / Panel name

Methods:
OnInit()  called once by GlobalControllerMgr right after creation and registration -> permanent subscriptions go here
OnDispose()  called once before destruction, after the manager stopped all of this controller's coroutines -> permanent unsubscriptions go here
Bind(PanelBase panel)  called by the panel lifecycle; binds the matching View by name -> errors when the type does not match

Notes:
- Implementer: ControllerBase<TView> (a plain C# class, NOT a MonoBehaviour); concrete subclasses include StepUIController and StepQuestionController.
- Controllers no longer live in a scene: GlobalControllerMgr creates them in DelayInit and owns their lifetime, so there is no Awake/OnDestroy to rely on.
- Controllers are indexed both by ControllerName and by concrete type; PanelBase prefers the type lookup ([RequireController]) and falls back to the name.
- The name must still equal the panel name for the legacy fallback path, or the panel never finds the controller and Bind never fires.
