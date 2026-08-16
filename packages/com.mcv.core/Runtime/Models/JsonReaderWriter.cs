using System;
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
            Debug.LogError(e.Message);
        }
    }
#endif

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
            Debug.LogError(e.Message);
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
                    Debug.LogError($"[JsonReaderWriter] JSON 解析失败 [{name}]: {e.Message}");
                    callback?.Invoke(default, false);
                }
            }
            else
            {
                Debug.LogError($"[JsonReaderWriter] 读取失败: {path}, {uwr.error}");
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
