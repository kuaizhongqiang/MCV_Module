# Contract: StepContentData (+ StepContentBase / StepUiData / StepTipsData / StepContentConverter)

Role: step text content set looked up by id; a container of polymorphic StepContentBase entries.

Fields:
contents:List<StepContentBase>  every content entry (the real elements are subclasses), not populated by the constructor
contentType:StepContentType  the discriminator field used for deserialization
StepUiData.title:string / pages:List<string>  dialog title and body pages
StepTipsData.stepTips / stepImageKey / opTips / opImageKey:string  the two tip sides, each with text plus an optional image key

Methods:
StepContentConverter.CanConvert(objectType)  accepts the base type only
StepContentConverter.ReadJson(...)  read contentType, instantiate the subclass, populate the remaining fields -> unknown type is skipped with Log.Warning
StepContentConverter.WriteJson(...)  delegate to default serialization

Notes:
- Polymorphism relies on the contentType discriminator (StepContentBase carries [JsonConverter(typeof(StepContentConverter))]); a new subclass requires a new enum value plus a converter branch.
- The converter only overrides reading (CanWrite = false): taking over writing would make serializer.Serialize re-enter the converter and recurse forever.
- CanConvert deliberately avoids IsAssignableFrom so that deserializing a concrete subclass directly is unaffected.
- A new text form only needs a subclass, an enum value and a converter branch; the container and its consumers stay untouched.
- StepUiData pages and StepTipsData text are filled by hand in JSON; no default entries may be added in constructors (Newtonsoft append semantics).
