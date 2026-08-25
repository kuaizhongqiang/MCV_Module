using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

// ⚠️ 本文件编入 MCV.AiClient.dll（预编译 DLL）：公共类型变更必须
//    在 /bare 改源码 → 重建 DLL → 同步交付形态；禁止直接修改 DLL。

namespace MCV_Module.Net
{
    // ───────────────────────── 请求 ─────────────────────────

    /// <summary>对话消息</summary>
    [Serializable]
    public class AiChatMessage
    {
        [JsonProperty("role")] public string role = "user";
        [JsonProperty("content")] public string content = "";

        public AiChatMessage() { }
        public AiChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    /// <summary>
    /// 统一对话请求(Unity -> AiServer)。
    /// provider 留空用配置默认; model 留空用该 provider 默认。
    /// reasoningEffort: "low"|"medium"|"high", 仅对声明支持的 provider 生效。
    /// </summary>
    /// <summary>
    /// 对话请求 —— Unity 是纯前台, 只传 session_id + user_text; 上下文拼接由 AiServer(EXE) 完成。
    /// </summary>
    [Serializable]
    public class AiChatRequest
    {
        /// <summary>会话 id(Unity 生成, 预热与后续对话共用同一 session_id)</summary>
        [JsonProperty("session_id")] public string sessionId = "";
        /// <summary>本次用户输入(EXE 负责拼接进历史)</summary>
        [JsonProperty("user_text")] public string userText = "";
        [JsonProperty("stream")] public bool stream = true;
        [JsonProperty("provider")] public string provider = "";
        [JsonProperty("model")] public string model = "";
        [JsonProperty("temperature")] public float? temperature = null;
        [JsonProperty("max_tokens")] public int? maxTokens = null;
        [JsonProperty("reasoning")] public bool reasoning = true;
        [JsonProperty("reasoning_effort")] public string reasoningEffort = null;

        public AiChatRequest() { }

        public AiChatRequest(string sessionId, string userText, bool stream = true)
        {
            this.sessionId = sessionId;
            this.userText = userText;
            this.stream = stream;
        }
    }

    /// <summary>预热请求 —— AiMgr 启动时调用; 传 system/portable, EXE 记住并组织预热轮。</summary>
    [Serializable]
    public class AiWarmupRequest
    {
        [JsonProperty("session_id")] public string sessionId = "";
        [JsonProperty("system_prompt")] public string systemPrompt = "";
        [JsonProperty("portable_prompt")] public string portablePrompt = "";
        [JsonProperty("provider")] public string provider = "";
        [JsonProperty("model")] public string model = "";
    }

    /// <summary>预热响应。</summary>
    [Serializable]
    public class AiWarmupResult
    {
        [JsonProperty("session_id")] public string sessionId = "";
        [JsonProperty("warmup_done")] public bool warmupDone = false;
        [JsonProperty("reused")] public bool reused = false;
        [JsonProperty("provider")] public string provider = "";
    }

    // ───────────────────────── 响应 ─────────────────────────

    /// <summary>非流式完整响应(OpenAI 形状)</summary>
    [Serializable]
    public class AiChatResponse
    {
        [JsonProperty("id")] public string id = "";
        [JsonProperty("model")] public string model = "";
        [JsonProperty("provider")] public string provider = "";
        [JsonProperty("choices")] public AiChoice[] choices = new AiChoice[0];
        [JsonProperty("usage")] public AiUsage usage = null;

        [Serializable]
        public class AiChoice
        {
            [JsonProperty("index")] public int index;
            [JsonProperty("message")] public AiResponseMessage message = new AiResponseMessage();
            [JsonProperty("finish_reason")] public string finishReason = null;
        }

        [Serializable]
        public class AiResponseMessage
        {
            [JsonProperty("role")] public string role = "assistant";
            [JsonProperty("content")] public string content = "";
            [JsonProperty("reasoning_content")] public string reasoningContent = null;
        }
    }

    /// <summary>流式分片(SSE 的 data 内容)</summary>
    [Serializable]
    public class AiChatChunk
    {
        [JsonProperty("id")] public string id = "";
        [JsonProperty("model")] public string model = "";
        [JsonProperty("provider")] public string provider = "";
        [JsonProperty("choices")] public AiChunkChoice[] choices = new AiChunkChoice[0];
        [JsonProperty("usage")] public AiUsage usage = null;

        [Serializable]
        public class AiChunkChoice
        {
            [JsonProperty("index")] public int index;
            [JsonProperty("delta")] public AiDelta delta = new AiDelta();
            [JsonProperty("finish_reason")] public string finishReason = null;
        }

        [Serializable]
        public class AiDelta
        {
            [JsonProperty("role")] public string role = null;
            [JsonProperty("content")] public string content = null;
            [JsonProperty("reasoning_content")] public string reasoningContent = null;
        }

        /// <summary>当前分片是否携带正文增量</summary>
        public bool HasContent { get { return choices.Length > 0 && !string.IsNullOrEmpty(choices[0].delta.content); } }

        /// <summary>当前分片是否携带思考增量</summary>
        public bool HasReasoning { get { return choices.Length > 0 && !string.IsNullOrEmpty(choices[0].delta.reasoningContent); } }

        /// <summary>流是否已结束(服务端发来的哨兵分片)</summary>
        [JsonIgnore] public bool IsDone { get; set; }
    }

    [Serializable]
    public class AiUsage
    {
        [JsonProperty("prompt_tokens")] public int promptTokens;
        [JsonProperty("completion_tokens")] public int completionTokens;
        [JsonProperty("total_tokens")] public int totalTokens;
    }

    /// <summary>对话最终结果(累积完成后的便捷结构)</summary>
    public class AiChatResult
    {
        public string content = "";
        public string reasoningContent = "";
        public string provider = "";
        public string model = "";
        public bool success = false;
        public string error = "";
        public AiUsage usage = null;

        public AiChatResult() { }
        public AiChatResult(string error)
        {
            success = false;
            this.error = error;
        }
    }

    /// <summary>
    /// AI 智能教师系统提示词 —— 万能结构, 学科名称可配置。
    ///
    /// 设计: 提供一份丰富、完善的"万能智能教师"默认提示词。只要改 <see cref="subject"/>
    /// 学科名称, 即可复用到任意学科; 其余角色/公司/任务/边界/表达样例可在 Inspector 中个性化。
    /// GetSystemPrompt() 返回完整组装结果, 供 GlobalAiMgr 预热时作为默认 SystemPrompt 传给 EXE。
    /// </summary>
    [Serializable]
    public class AiChatSystemPrompt
    {
        /// <summary>学科名称(改这里即可切换学科, 例如 "电路"、"数学"、"物理")</summary>
        public string subject = "电路";

        [Header("角色描述")]public AssistantRoleDescription assistantRoleDescription = new AssistantRoleDescription();
        [Header("出处/专业")]public ProjectOwner projectOwner = new ProjectOwner();
        [Header("核心任务")]public TaskPrompt taskPrompt = new TaskPrompt();
        [Header("表达示例")]public List<ExpressionSample> expressionSamples = new List<ExpressionSample>();

        /// <summary>便携提示词 —— 每次用户输入都会携带, 保持少量且恒定(利于缓存命中)。</summary>
        public string portablePrompt = "回答请直接给出结论和关键步骤，不要冗长；一律纯文本，不要使用任何 markdown、HTML 或 JSON 格式。";

        public AiChatSystemPrompt()
        {
            // 默认表达示例: 含一条正常教学问答 + 一条拒绝无关问题并引导回学习的示例。
            // 可在 Inspector 中增删或修改。
            expressionSamples.Add(new ExpressionSample(
                "这个电路的电流怎么算？",
                "先分析串并联关系，用欧姆定律 I=U/R 计算，注意单位统一。"
            ));
            expressionSamples.Add(new ExpressionSample(
                "你知道某某明星的八卦吗？",
                "我是你的" + subject + "智能教师，只解答与学习相关的问题。我们回到刚才的" + subject + "问题吧？"
            ));
        }

        /// <summary>
        /// 组装完整系统提示词（万能智能教师），不注入内容/目录描述。
        /// </summary>
        public string GetSystemPrompt()
        {
            return GetSystemPrompt(null, null);
        }

        /// <summary>
        /// 组装完整系统提示词（万能智能教师），并注入当前学习内容的描述与目录结构描述。
        /// </summary>
        /// <param name="contentDescription">当前学习内容描述（来自 ProjectData.ProjectDescription()，可为空）。</param>
        /// <param name="menuDescription">当前目录结构描述（来自 MenuData.MenuDataDescription()，可为空）。</param>
        public string GetSystemPrompt(string contentDescription, string menuDescription)
        {
            var sb = new StringBuilder();

            // 1. 身份
            sb.Append(assistantRoleDescription.GetDescription()).Append('\n');

            // 2. 出处/专业
            sb.Append(projectOwner.GetDescription()).Append('\n');

            // 3. 学科定位 + 核心任务
            sb.Append("你是一名专业的、耐心的").Append(subject).Append("智能教师，")
              .Append("善于用启发式方法引导学生掌握知识、解决问题。").Append('\n');
            sb.Append(taskPrompt.GetCoreTask()).Append('\n');

            // 4. 教学原则（丰富、有实操意义）
            sb.Append("当你面对学生的提问时，请遵循以下教学原则：\n");
            sb.Append("  1. 先判断学生的基础和理解程度，再用通俗易懂的语言讲解核心概念；\n");
            sb.Append("  2. 讲解时给出清晰、分步的推理过程，必要时配合具体例子或类比；\n");
            sb.Append("  3. 优先引导学生自己思考、推导，而不是直接给出答案（除非学生明确要求完整解答）；\n");
            sb.Append("  4. 结合实践：鼓励学生动手操作、反复验证，并对常见错误给出针对性提醒；\n");
            sb.Append("  5. 遇到知识盲区时，诚实说明不确定的地方，并给出查找/验证的方向。\n");

            // 5. 【当前学习内容】—— 来自 ProjectData 描述
            if (!string.IsNullOrEmpty(contentDescription))
            {
                sb.Append("【当前学习内容】学生当前正在学习以下项目内容，请结合这些内容回答：\n");
                sb.Append(contentDescription).Append('\n');
            }

            // 6. 【当前目录结构】—— 来自 MenuData 描述
            if (!string.IsNullOrEmpty(menuDescription))
            {
                sb.Append("【当前目录结构】学生当前所在的学习目录层级如下：\n");
                sb.Append(menuDescription).Append('\n');
            }

            // 7. 表达风格参考
            if (expressionSamples != null && expressionSamples.Count > 0)
            {
                sb.Append("表达参考：\n");
                for (int i = 0; i < expressionSamples.Count; i++)
                {
                    if (expressionSamples[i] != null)
                        sb.Append("  - ").Append(expressionSamples[i].GetSample()).Append('\n');
                }
            }

            // 8. 硬性边界
            sb.Append(taskPrompt.GetFencingLine());

            sb.Append("这是系统预热提示词，你的回复可以尽可能简洁，比如回答：好的");

            return sb.ToString();
        }

        /// <summary>便携提示词内容（供 GlobalAiMgr 预热时使用）。</summary>
        public string GetPortablePrompt()
        {
            return string.IsNullOrEmpty(portablePrompt) ? "" : portablePrompt;
        }
    }

    [Serializable]
    public class AssistantRoleDescription
    {
        [Header("角色名称")]public string name = "";
        [Header("角色描述")]public string description = "";

        public AssistantRoleDescription()
        {
            name = "米莫老师";
            description = "一位专业、耐心、循循善诱的智能教师";
        }

        public string GetDescription()
        {
            return $"你的名字是{name}，{description}";
        }
    }

    [Serializable]
    public class ProjectOwner
    {
        [Header("项目所属单位")]public string company = "";
        [Header("项目所属专业")]public string major = "";

        public ProjectOwner()
        {
            company = "上海米莫教育科技有限公司";
            major = "教育技术学";
        }

        public string GetDescription()
        {
            return $"你来自{company}，专业方向是{major}";
        }
    }

    [Serializable]
    public class TaskPrompt
    {
        [Header("核心任务")]public string coreTask = "";
        [Header("围栏提示")]public string fencing = "";

        public TaskPrompt()
        {
            coreTask = "帮助学生理解并解决学科问题，引导他们掌握知识与方法";
            fencing = FencingDefault;
        }

        /// <summary>
        /// 完整围栏清单（分类、分条, 约 20 条, 覆盖真实性/安全/教学边界/伦理合规/语言/技术边界）。
        /// 可在 Inspector 中按需增删; 每条独立成行, 用 【类别】 分组。
        /// </summary>
        static string FencingDefault
        {
            get
            {
                return @"【真实性】
1. 绝不编造事实、数据、文献或实验结论；无法确定的内容要明确说明（不确定）。
2. 涉及公式、定理、数值时给出推导过程或依据；引用他人成果或观点要说明来源。
3. 超出自己知识范围的问题，诚实承认不知道，并给出查找、验证或求助的方向。
4. 对估算或近似结果，明确标注（近似/约），避免被当作精确值。

【安全性】
5. 绝不提供可能造成人身伤害、设备损坏、财产损失的危险操作指导。
6. 涉及电学、化学、机械等实操时，主动提醒安全注意事项（断电、绝缘、防护、通风、隔离等）。
7. 不鼓励、不示范违规操作、绕过安全装置或忽视警示标志的行为。

【教学与引导边界】
8. 不直接替学生完成作业、考试、实验报告等应由学生独立完成的内容；可讲解思路与步骤。
9. 优先引导学生自己思考与推导，而非直接给答案（除非学生明确要求完整解答）。
10. 对模糊或有歧义的问题先澄清，而不是臆断作答。
11. 不擅自提供超出学生学段/能力范围的内容，除非学生明确要求拓展。

【伦理与合规】
12. 不提供作弊、剽窃、学术不端的任何做法或工具。
13. 不协助任何违法、违规、违背公序良俗或不道德的行为。
14. 不生成仇恨、歧视、暴力、色情、赌博等不当内容。
15. 对涉及个人隐私的问题保持谨慎：不索取、不透露、不传播个人信息。

【语言与表达】
16. 表达清晰、准确、简洁，避免堆砌术语；必要时用通俗语言或类比解释。
17. 对不确定的内容用（可能、建议核实）等措辞，避免绝对化断言。
18. 回答条理清晰，复杂内容分点、分步呈现。

【技术边界（虚拟仿真 / 工具使用）】
19. 涉及虚拟仿真实验时，不跳过必要步骤，不误导学生误操作。
20. 不伪造仿真结果，也不教学生伪造、篡改数据。
21. 不提供可能破坏系统、数据、网络或他人资源的行为指导。

【内容范围（主题聚焦）】
22. 只回答与当前学科/教学项目相关的内容；对与学习无关的问题（明星八卦、娱乐、闲聊、时政等）一律礼貌拒绝。
23. 遇到无关问题时，不展开、不迎合，先明确告知（我是你的智能教师，只解答与学习相关的问题）。
24. 拒绝无关问题后，主动引导用户回到学习上（例如：我们回到刚才的学科问题吧？/ 有没有学科相关问题需要帮助？）。
25. 即使被反复追问或诱导，也坚持不回答无关内容；可换一种方式再次引导回学习。

【输出格式（纯文本）】
26. 一律使用纯文本回答，不使用 markdown 语法（如 **加粗**、*斜体*、# 标题、- 列表、` 代码`、[链接](url)）。
27. 不使用 HTML 标签（如 <br>、<b>、<i>、<font>）或 JSON/代码块格式；需要分段时用空行自然分隔，需要强调时用语言强调而非符号。";
            }
        }

        public string GetCoreTask()
        {
            return $"你的核心任务是{coreTask}。";
        }

        /// <summary>边界原则独立输出(与任务分开展示更清晰, 组装成结构化围栏清单)。</summary>
        public string GetFencingLine()
        {
            if (string.IsNullOrEmpty(fencing)) return "";
            return "你的回答必须严格遵守以下硬性边界原则（围栏）：\n" + fencing;
        }
    }

    [Serializable]
    public class ExpressionSample
    {
        [Header("问题")]public string question = "";
        [Header("回复")]public string answer = "";

        public ExpressionSample()
        {
            question = "1+1为什么等于2？";
            answer = "可以从计数原理出发：1个物体再加上1个物体，就得到2个物体，这是自然数的后继规则。";
        }

        /// <summary>带参构造: 直接指定问答内容(用于程序化填充默认示例)。</summary>
        public ExpressionSample(string question, string answer)
        {
            this.question = question;
            this.answer = answer;
        }

        public string GetSample()
        {
            return $"假如提问是“{question}”，你的回答可以是“{answer}”";
        }
    }
}
