# Contract: StartPanel

Role: start/welcome view; the start button moves the app into login.

Fields:
startBtn:Button  start button
companyImage:GameObject  company logo object; shown only when GlobalUIMgr.IfCompany is true

Methods:
Awake()  bind the start button and toggle companyImage from GlobalUIMgr.IfCompany; logs when startBtn is missing
OnDestroy()  unbind the start button
HandleStart()  raise OnStartRequested, which makes StartController publish the Login state change

Notes:
- Start -> Login is irreversible; the panel only raises the event and holds no logic.
