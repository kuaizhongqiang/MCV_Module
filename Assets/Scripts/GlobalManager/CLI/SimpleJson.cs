using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MCV_Module.GlobalManager.CLI
{
    /// <summary>
    /// 极简 JSON 解析/序列化工具，仅支持 GlobalCLIMgr 所需的数据结构。
    /// 无第三方依赖，保持 internal 仅程序集内可见。
    /// </summary>
    internal static class SimpleJson
    {
        public class JsonObject : Dictionary<string, object>
        {
            public string GetString(string key, string defaultValue = "")
            {
                if (TryGetValue(key, out var val) && val != null)
                    return val.ToString();
                return defaultValue;
            }

            public string GetRaw(string key, string defaultValue = "{}")
            {
                return GetString(key, defaultValue);
            }
        }

        public static JsonObject Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var obj = new JsonObject();
                var trimmed = json.Trim();
                if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
                    return null;

                // 移除花括号
                var inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
                if (string.IsNullOrEmpty(inner))
                    return obj;

                // 简单 key-value 解析（支持嵌套跳过）
                int i = 0;
                while (i < inner.Length)
                {
                    // 跳过空白
                    while (i < inner.Length && char.IsWhiteSpace(inner[i])) i++;
                    if (i >= inner.Length) break;

                    // 读取 key
                    if (inner[i] != '"') { i++; continue; }
                    i++;
                    var key = new StringBuilder();
                    while (i < inner.Length && inner[i] != '"')
                    {
                        if (inner[i] == '\\') { i++; if (i < inner.Length) key.Append(inner[i]); }
                        else key.Append(inner[i]);
                        i++;
                    }
                    i++; // 跳过 closing quote

                    // 跳过冒号
                    while (i < inner.Length && (inner[i] == ':' || char.IsWhiteSpace(inner[i]))) i++;

                    // 读取 value
                    if (i < inner.Length)
                    {
                        if (inner[i] == '"')
                        {
                            // 字符串值
                            i++;
                            var val = new StringBuilder();
                            while (i < inner.Length && inner[i] != '"')
                            {
                                if (inner[i] == '\\') { i++; if (i < inner.Length) val.Append(inner[i]); }
                                else val.Append(inner[i]);
                                i++;
                            }
                            i++;
                            obj[key.ToString()] = val.ToString();
                        }
                        else if (inner[i] == '{' || inner[i] == '[')
                        {
                            // 嵌套对象/数组 — 捕获为 JSON 字符串
                            char depthChar = inner[i];
                            int start = i;
                            int depth = 1; i++;
                            while (i < inner.Length && depth > 0)
                            {
                                if (inner[i] == depthChar) depth++;
                                else if (inner[i] == (depthChar == '{' ? '}' : ']')) depth--;
                                if (depth > 0) i++;
                            }
                            i++;
                            obj[key.ToString()] = inner.Substring(start, i - start);
                        }
                        else
                        {
                            // 数字/布尔/null
                            var val = new StringBuilder();
                            while (i < inner.Length && inner[i] != ',' && inner[i] != '}' && inner[i] != ']')
                            {
                                val.Append(inner[i]);
                                i++;
                            }
                            obj[key.ToString()] = val.ToString().Trim();
                        }
                    }

                    // 跳过逗号
                    while (i < inner.Length && (inner[i] == ',' || char.IsWhiteSpace(inner[i]))) i++;
                }

                return obj;
            }
            catch
            {
                return null;
            }
        }

        public static string Serialize(object obj)
        {
            if (obj == null) return "null";
            if (obj is string s) return $"\"{EscapeJson(s)}\"";
            if (obj is bool b) return b ? "true" : "false";
            if (obj is int i) return i.ToString();
            if (obj is long l) return l.ToString();
            if (obj is float f) return f.ToString("G");
            if (obj is double d) return d.ToString("G");
            if (obj is decimal m) return m.ToString("G");
            if (obj is DateTime dt) return $"\"{dt:O}\"";
            if (obj is IEnumerable enumerable && obj is not string)
            {
                var items = new List<string>();
                foreach (var item in enumerable)
                    items.Add(Serialize(item));
                return "[" + string.Join(",", items) + "]";
            }

            // 通过反射序列化属性/字段
            var sb = new StringBuilder("{");
            var type = obj.GetType();
            bool first = true;

            // 字段
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{field.Name}\":");
                sb.Append(Serialize(field.GetValue(obj)));
                first = false;
            }

            // 属性
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    if (!first) sb.Append(",");
                    sb.Append($"\"{prop.Name}\":");
                    sb.Append(Serialize(prop.GetValue(obj, null)));
                    first = false;
                }
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
