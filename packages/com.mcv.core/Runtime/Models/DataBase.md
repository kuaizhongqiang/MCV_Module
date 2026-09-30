# Contract: DataBase

Role: shared base of data models; carries the id, display name and description fields.

Fields:
id:string  unique identifier; public field, serialized by both Newtonsoft and Unity
displayName:string  display name
description:string  free text

Methods:
(none)

Notes:
- The fields are public, so they serialize without [SerializeField]; renaming one breaks deserialization of JSON already written to disk.
- Subclasses inherit this identity contract, so id stays the lookup key used across the data layer.
