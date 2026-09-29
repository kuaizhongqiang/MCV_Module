using System;
using System.Collections.Generic;
using MCV_Module.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCV_Module.Models.Project
{
    /// <summary>步骤文字内容集（承载 StreamingAssets/Data/StepContentData.json）：按 id 查的多态条目集合，实际元素是 StepContentBase 的子类。</summary>
    [Serializable]
    public class StepContentData : DataBase
    {
        /// <summary>全部内容条目（按 id 查找；实际类型是 <see cref="StepContentBase"/> 的子类）</summary>
        public List<StepContentBase> contents = new List<StepContentBase>();

        public StepContentData()
        {
            id = "stepContentData";
            displayName = "步骤内容集";
            description = "步骤文字内容（UI 说明 / 提示等）：多态条目按 id 取用。";
            // WHY: 不向 contents 填默认项——Newtonsoft 对已初始化集合是「追加」而非「替换」，会导致 JSON 往返后重复。
        }
    }

    // WHY: contentType 是 Newtonsoft 的判别字段（默认不做多态），新增子类务必同步 StepContentType 与 StepContentConverter，否则无法从 JSON 还原子类。
    /// <summary>步骤文字内容抽象基类；所有"给学员看的一段文字"都继承它。</summary>
    [Serializable]
    [JsonConverter(typeof(StepContentConverter))]
    public abstract class StepContentBase : DataBase
    {
        /// <summary>内容类型（反序列化判别用）；子类构造函数里写成自己的类型</summary>
        public StepContentType contentType = StepContentType.None;
    }

    /// <summary>UI 说明内容（服务步骤条件 ConditionUI）：一个标题 + 若干页正文，可配多条；仅 1 页时面板隐藏翻页按钮。</summary>
    [Serializable]
    public class StepUiData : StepContentBase
    {
        /// <summary>弹框标题（显示在 TitlePrefab 上）</summary>
        public string title;
        public string titleEn;                             // 英文列（空 = 回退中文）

        /// <summary>正文页（按顺序展示，一页一段）</summary>
        public List<string> pages = new List<string>();
        public List<string> pagesEn = new List<string>();   // 英文列（必须与 pages 等长，空 = 回退中文）

        public StepUiData()
        {
            contentType = StepContentType.UI;
            id = "stepUi";
            displayName = "UI说明";
            // WHY: 不塞默认页，避免 JSON 往返后重复。
        }
    }

    /// <summary>提示条内容（服务 TipsController/TipsPanel）：分步骤提示与操作提示两侧，各含文案 + 可选图文 key（AB 包配置 id，加载失败回退文案）。</summary>
    [Serializable]
    public class StepTipsData : StepContentBase
    {
        /// <summary>步骤提示文案</summary>
        public string stepTips;
        public string stepTipsEn;                          // 英文列（空 = 回退中文）

        /// <summary>步骤提示的图文资源 key（AB 包配置 id；留空则只显示文案）</summary>
        public string stepImageKey;

        /// <summary>操作提示文案</summary>
        public string opTips;
        public string opTipsEn;                            // 英文列（空 = 回退中文）

        /// <summary>操作提示的图文资源 key（AB 包配置 id；留空则只显示文案）</summary>
        public string opImageKey;

        public StepTipsData()
        {
            contentType = StepContentType.Tips;
            id = "stepTips";
            displayName = "提示";
        }
    }

    // WHY: 只重写读取（CanWrite=false）——若接管写入，serializer.Serialize 会被本转换器再次接管而无限递归。
    // WHY: CanConvert 只认基类本身（不用 IsAssignableFrom），避免影响直接反序列化具体子类的场景。
    /// <summary>StepContentBase 的多态反序列化器：先读判别字段 contentType，new 出对应子类，再把其余字段灌入。</summary>
    public class StepContentConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(StepContentBase);

        /// <summary>写入交给默认逻辑 —— 本转换器只负责回答"这段 JSON 该还原成哪个子类"</summary>
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            JObject jo = JObject.Load(reader);
            var type = jo[nameof(StepContentBase.contentType)]?.ToObject<StepContentType>() ?? StepContentType.None;

            StepContentBase result = type switch
            {
                StepContentType.UI => new StepUiData(),
                StepContentType.Tips => new StepTipsData(),
                _ => null,
            };

            if (result == null)
            {
                // WHY: 未知/缺失 contentType 时跳过该条而非抛异常，免得一条坏数据把整份内容集带崩。
                Log.Warning($"[StepContentConverter] 未知 contentType={type}，该条目被跳过");
                return null;
            }

            serializer.Populate(jo.CreateReader(), result);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // WHY: CanWrite=false 时正常不会走到这里，兜底也走默认序列化。
            serializer.Serialize(writer, value);
        }
    }
}
