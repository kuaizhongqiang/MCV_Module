# Contract: ObjectBase

Role: lowest-level identity base for scene objects; holds only id / displayName / description, no behaviour.

Fields:
id:string  unique object id; public, so implicitly Unity-serialized
displayName:string  display name; public, so implicitly Unity-serialized
description:string  free text; public, so implicitly Unity-serialized

Methods:
(none)

Notes:
- Public fields on a MonoBehaviour are serialized without [SerializeField]; keep them fields, not properties.
- InteractiveBase does not inherit ObjectBase (both hierarchies coexist); never assume this chain.
- No naming rules live here: id is written by subclasses at init time.
