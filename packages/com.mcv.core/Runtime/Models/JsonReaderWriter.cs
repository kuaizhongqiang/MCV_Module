using System;
using MCV_Module.Utils;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace MCV_Module.Models
{
    public static class JsonReaderWriter
{
    static string PATH = "Data/";
    static string EXT = ".json";

#if UNITY_EDITOR
    /// <summary>
    /// 同步写入 JSON（仅限 Editor，如数据导出/调试）。
    /// WebGL 运行时不支持同步文件 IO；运行时一律走 ReadAsync / Read。
    /// 目录按完整路径创建（原实现用相对路径 "Data/" 建到了 CWD，导致写入位置错误）。
    /// </summary>
    public static void Write<T>(string name, T data, Action callback)
    {
        try
        {
            string path = FULL_PATH(name);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
            callback?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
    }
#endif

    /// <summary>
    /// **运行时**写入 JSON（同步写盘）。PC / Editor 下 <c>StreamingAssets/Data</c> 是普通可写目录；
    /// WebGL 没有本地文件系统，只告警并返回 false（数据仅留内存）。
    /// <para>用途：**设置类数据**需要跨启动记住（如 <c>SystemData.renderQuality</c>）。
    /// 游戏内容数据（ProjectData / QuestionData…）仍按只读约定，不要在运行时改。</para>
    /// </summary>
    /// <returns>是否真的写成功（WebGL / 异常时为 false）。</returns>
    public static bool WriteRuntime<T>(string name, T data)
    {
#if UNITY_WEBGL
        Log.Warning($"[JsonReaderWriter] WebGL 无本地文件系统，{name} 无法落盘（仅保留内存态）");
        return false;
#else
        try
        {
            string path = FULL_PATH(name);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"[JsonReaderWriter] {name} 写入失败：{e.Message}");
            return false;
        }
#endif
    }

    public static T Read<T>(string name ,Action<bool> callback)
    {
        try
        {
            string path = FULL_PATH(name);
            string json = File.ReadAllText(path);
            callback?.Invoke(true);
            return JsonConvert.DeserializeObject<T>(json);

        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            callback?.Invoke(false);
            return default(T);
        }
    }

    /// <summary>
    /// 异步读取 JSON（WebGL 兼容）—— 使用 UnityWebRequest 而非 File.ReadAllText，
    /// 因为 WebGL 下 StreamingAssets 路径是 HTTP URL，不能直接文件读取。
    /// </summary>
    public static IEnumerator ReadAsync<T>(string name, Action<T, bool> callback)
    {
        string path = FULL_PATH(name);

        using (var uwr = UnityWebRequest.Get(path))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string json = uwr.downloadHandler.text;
                    var data = JsonConvert.DeserializeObject<T>(json);
                    callback?.Invoke(data, true);
                }
                catch (Exception e)
                {
                    Log.Error($"[JsonReaderWriter] JSON 解析失败 [{name}]: {e.Message}");
                    callback?.Invoke(default, false);
                }
            }
            else
            {
                Log.Error($"[JsonReaderWriter] 读取失败: {path}, {uwr.error}");
                callback?.Invoke(default, false);
            }
        }
    }

    /// <summary>介质路径（日志/对账报告用；WebGL 下 StreamingAssets 本身就是 HTTP 路径）。</summary>
    public static string PathOf(string name) => FULL_PATH(name);

    /// <summary>
    /// 只读：序列化为与写盘完全一致的文本（<c>Formatting.Indented</c>）。对账（dry-run）用。
    /// </summary>
    public static string Serialize<T>(T data) where T : class
    {
        if (data == null) return null;
        try { return JsonConvert.SerializeObject(data, Formatting.Indented); }
        catch (Exception e) { Log.Error($"[JsonReaderWriter] 序列化失败：{e.Message}"); return null; }
    }

    /// <summary>只读：取介质上的原始文本；文件不存在或读取失败返回 null（**绝不抛异常、绝不写盘**）。</summary>
    public static string TryReadRaw(string name)
    {
        try
        {
            string path = FULL_PATH(name);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception e)
        {
            Log.Error($"[JsonReaderWriter] 读取失败 [{name}]：{e.Message}");
            return null;
        }
    }

    static string FULL_PATH(string name)
    {
        return Application.streamingAssetsPath + "/" + PATH + name + EXT;
    }

}
}
