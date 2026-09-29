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
    // WHY: WebGL 运行时不支持同步文件 IO，运行时一律走 ReadAsync / Read；目录须按完整路径创建（相对路径 "Data/" 会建到 CWD 导致写错位置）。
    /// <summary>同步写入 JSON（仅限 Editor，如数据导出/调试）。</summary>
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

    // WHY: WebGL 无本地文件系统，只告警并返回 false（数据仅留内存）；运行时只写设置类数据（如 SystemData.renderQuality），游戏内容数据（ProjectData / QuestionData…）按只读约定不许改。
    /// <summary>运行时写入 JSON（同步写盘），返回是否真的写成功。</summary>
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

    // WHY: WebGL 下 StreamingAssets 路径是 HTTP URL，不能直接文件读取，故用 UnityWebRequest 而非 File.ReadAllText。
    /// <summary>异步读取 JSON（WebGL 兼容）。</summary>
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

    static string FULL_PATH(string name)
    {
        return Application.streamingAssetsPath + "/" + PATH + name + EXT;
    }

}
}
