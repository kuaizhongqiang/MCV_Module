# Contract: Message (+ Request / Response / Error / Reasoning / Content / ReasoningCache / ContentCache)

Role: LLM request and response data shells; most types are empty placeholders waiting for the wire protocol to be aligned.

Fields:
Request.thinking:bool  whether to think, default false
Request.effort:string  thinking effort, default "high"
Request.temperature:string  temperature, default "0.5"
Request.streaming:bool  streaming, default false

Methods:
(none)

Notes:
- Message / Response / Error / Reasoning / Content / ReasoningCache are empty shells.
- ContentCache is the only class without [Serializable]; remember to add it when fields are introduced, otherwise nothing serializes.
- The file name (LlmMessage.cs) differs from the main type.
