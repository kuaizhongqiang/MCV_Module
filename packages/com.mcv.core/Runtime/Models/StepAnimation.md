# Contract: StepAnimation

Role: one step-animation entry; the playback configuration of a single step animation, driven by a Legacy Animation component (Play + Sample + Stop).

Fields:
animation:Animation  [SerializeField] the Legacy Animation component; the animated object sits outside the StepManager hierarchy, so it is dragged in from the inspector
clip:AnimationClip  [SerializeField] the clip to play
hideOnComplete:bool  [SerializeField] hide the animated object once playback finished

Methods:
(none)

Notes:
- The clip must be non-looping, otherwise the Complete stage waits for playback to finish forever.
- The animated object is outside the step hierarchy, so the component can only be assigned by hand in the inspector.
