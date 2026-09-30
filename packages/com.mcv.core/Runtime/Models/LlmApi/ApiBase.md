# Contract: ConfigOverview (+ ApiPrice / ApiConfig / AiUser / ApiProvider)

Role: LLM model and endpoint configuration data; aggregates the eleven model prices behind a static factory.

Fields:
prices:List<ApiPrice>  the price table, aggregated from the eleven factories in the constructor
ApiConfig.configName / userInfo / port / serverPath / serverAddress  endpoint and credential configuration
AiUser.id / name / unit / token / concurrencyLimit / createdAt / updatedAt / limitDate  identity and limits
ApiPrice.provider / modelName / isMillionOrThousand / output / cacheHitInput / cacheMissInput  provider, model, billing unit and the output, cache-hit and cache-miss prices

Methods:
DeepseekV4FlashPrice() / DeepseekV4ProPrice() / MimoV25Price() / MimoV25ProPrice() / Qwen37MaxPrice() / Qwen37PlusPrice() / Qwen36PlusPrice() / Qwen36FlashPrice() / DoubaoSeed20Price() / DoubaoSeed20LitePrice() / DoubaoSeed20MiniPrice()  one ApiPrice per model
ConfigOverview()  aggregate the eleven factories into prices

Notes:
- The file name (ApiBase.cs) differs from the main type: this file holds the LlmApi configuration DTOs and no ApiBase type exists.
- The price table is centralised here on purpose; never hardcode model prices in UI or managers.
- The MiMo endpoint is api.xiaomimimo.com; the older api.mimo.xiaomi.com no longer exists.
