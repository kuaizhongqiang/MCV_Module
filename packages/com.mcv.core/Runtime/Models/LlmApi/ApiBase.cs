
using System;
using System.Collections.Generic;

namespace MCV_Module.Models.LlmApi
{
    [Serializable]
    public class ConfigOverview
    {
        public List<ApiPrice> prices;

        // ---- DeepSeek（官方 API，新峰值价，2026-08-17 起） ----
        public static ApiPrice DeepseekV4FlashPrice()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "DeepSeek", endPoint = "https://api.deepseek.com" },
                modelName = "deepseek-v4-flash",
                isMillionOrThousand = true,   // 每百万 tokens
                cacheHitInput = 0.10f,
                cacheMissInput = 3.00f,
                output = 9.00f
            };
        }

        public static ApiPrice DeepseekV4ProPrice()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "DeepSeek", endPoint = "https://api.deepseek.com" },
                modelName = "deepseek-v4-pro",
                isMillionOrThousand = true,
                cacheHitInput = 0.30f,
                cacheMissInput = 9.00f,
                output = 27.00f
            };
        }

        // ---- 小米 MiMo（对标 DeepSeek，永久降价后） ----
        // 注意: 官方 OpenAI 兼容域名是 api.xiaomimimo.com（原 api.mimo.xiaomi.com 域名已不存在, 见 mimo.mi.com 文档）
        public static ApiPrice MimoV25Price()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "Xiaomi MiMo", endPoint = "https://api.xiaomimimo.com" },
                modelName = "mimo-v2.5",
                isMillionOrThousand = true,
                cacheHitInput = 0.02f,
                cacheMissInput = 1.00f,
                output = 2.00f
            };
        }

        public static ApiPrice MimoV25ProPrice()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "Xiaomi MiMo", endPoint = "https://api.xiaomimimo.com" },
                modelName = "mimo-v2.5-pro",
                isMillionOrThousand = true,
                cacheHitInput = 0.025f,
                cacheMissInput = 3.00f,
                output = 6.00f
            };
        }

        // ---- 通义千问 Qwen（阿里云百炼，华北2北京，≤256K 档） ----
        public static ApiPrice Qwen37MaxPrice()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "Qwen", endPoint = "https://dashscope.aliyuncs.com" },
                modelName = "qwen3.7-max",
                isMillionOrThousand = true,
                cacheHitInput = 12.00f,   // 无缓存命中价，按输入价填
                cacheMissInput = 12.00f,
                output = 36.00f
            };
        }

        public static ApiPrice Qwen37PlusPrice()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "Qwen", endPoint = "https://dashscope.aliyuncs.com" },
                modelName = "qwen3.7-plus",
                isMillionOrThousand = true,
                cacheHitInput = 2.00f,
                cacheMissInput = 2.00f,
                output = 8.00f
            };
        }

        public static ApiPrice Qwen36PlusPrice()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "Qwen", endPoint = "https://dashscope.aliyuncs.com" },
                modelName = "qwen3.6-plus",
                isMillionOrThousand = true,
                cacheHitInput = 2.00f,
                cacheMissInput = 2.00f,
                output = 12.00f
            };
        }

        public static ApiPrice Qwen36FlashPrice()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "Qwen", endPoint = "https://dashscope.aliyuncs.com" },
                modelName = "qwen3.6-flash",
                isMillionOrThousand = true,
                cacheHitInput = 1.20f,
                cacheMissInput = 1.20f,
                output = 7.20f
            };
        }

        // ---- 豆包 Doubao（火山方舟） ----
        public static ApiPrice DoubaoSeed20Price()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "Doubao", endPoint = "https://ark.cn-beijing.volces.com" },
                modelName = "doubao-seed-2.0",
                isMillionOrThousand = true,
                cacheHitInput = 0.80f,
                cacheMissInput = 0.80f,
                output = 2.00f
            };
        }

        public static ApiPrice DoubaoSeed20LitePrice()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "Doubao", endPoint = "https://ark.cn-beijing.volces.com" },
                modelName = "doubao-seed-2.0-lite",
                isMillionOrThousand = true,
                cacheHitInput = 0.60f,
                cacheMissInput = 0.60f,
                output = 3.60f
            };
        }

        public static ApiPrice DoubaoSeed20MiniPrice()
        {
            return new ApiPrice
            {
                provider = new ApiProvider { displayName = "Doubao", endPoint = "https://ark.cn-beijing.volces.com" },
                modelName = "doubao-seed-2.0-mini",
                isMillionOrThousand = true,
                cacheHitInput = 0.15f,
                cacheMissInput = 0.15f,
                output = 0.60f
            };
        }

        public ConfigOverview()
        {
            prices = new List<ApiPrice>()
            {
                DeepseekV4FlashPrice(),
                DeepseekV4ProPrice(),
                MimoV25Price(),
                MimoV25ProPrice(),
                Qwen37MaxPrice(),
                Qwen37PlusPrice(),
                Qwen36PlusPrice(),
                Qwen36FlashPrice(),
                DoubaoSeed20Price(),
                DoubaoSeed20LitePrice(),
                DoubaoSeed20MiniPrice()
            };        
        }

    }
    
    [Serializable]
    public class ApiConfig
    {
        public string configName;
        public AiUser userInfo;
        public int port;
        public string serverPath;
        public string serverAddress;

        public ApiConfig()
        {
            configName = string.Empty;
            userInfo = new AiUser();
            port = 0;
            serverPath = string.Empty;
            serverAddress = string.Empty;
        }
    }

    [Serializable]
    public class AiUser
    {
        public string id;
        public string name;               // 用户名称
        public string unit;               // 单位
        public string token;              // 令牌
        public int concurrencyLimit;      // 并发限制
        public DateTime createdAt;        // 创建时间
        public DateTime updatedAt;        // 更新时间
        public DateTime limitDate;        // 限制时间

        public AiUser()
        {
            id = string.Empty;
            name = string.Empty;
            unit = string.Empty;
            concurrencyLimit = 0;
            createdAt = DateTime.MinValue;
            updatedAt = DateTime.MinValue;
            limitDate = DateTime.MinValue;
        }

        public AiUser(string id, string name, string unit, int concurrencyLimit, DateTime createdAt, DateTime updatedAt, DateTime limitDate)
        {
            this.id = id;
            this.name = name;
            this.unit = unit;
            this.concurrencyLimit = concurrencyLimit;
            this.createdAt = createdAt;
            this.updatedAt = updatedAt;
            this.limitDate = limitDate;
        }
    }

    [Serializable]
    public class ApiProvider
    {
        public string displayName;
        public string endPoint;
    }

    [Serializable]
    public class ApiPrice
    {
        public ApiProvider provider;
        public string modelName;
        public bool isMillionOrThousand;
        public float output;
        public float cacheHitInput;
        public float cacheMissInput;
    }
}