# Contract: RoamingFunctionPanel

Role: roaming-page function panel; title and copyright bar plus one back entry.

Fields:
companyImage:GameObject  company object toggled by SetCopyright
copyrightImage:GameObject  copyright object toggled by SetCopyright
backBtn:Button  back entry that summons the menu panel
breathImages:List<Image>  images driven by the breathing-light coroutine started in OnEnable

Methods:
Awake()  validate the references and bind Back; logs and returns when a reference is missing
OnEnable()  start BreathLightenAnim(breathImages, 2f) -- a fresh coroutine on every enable
SetCopyright(ifCopyright, ifCompany)  toggle the company and copyright objects

Notes:
- Closes the menu -> roaming -> menu -> content-page loop; RoamingFunctionController handles the click by instantiating MenuPanel under the roaming canvas.
- The click binding dies with the instance, so no unsubscribe is needed.
