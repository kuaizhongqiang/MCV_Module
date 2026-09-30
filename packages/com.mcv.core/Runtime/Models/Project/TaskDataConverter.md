# Contract: TaskDataConverter

Role: static converter between TaskData representations; dispatches JSON import and export by TaskType.

Methods:
FromJson(json, taskType)  deserialize by TaskType -> Log.Error and null on failure
ToJson(data)  serialize to indented JSON -> Log.Error and null on failure
Clone<TData>(source)  deep clone through a JSON round trip
SafeCast<TData>(data)  as-cast
FromJObject(obj, taskType)  stringify the JObject and go through FromJson

Notes:
- FromJson's switch is the contract: a new TaskType that is not added there silently returns null.
- Clone goes through ToJson / FromJson rather than MemberwiseClone so reference-type fields are copied too.
