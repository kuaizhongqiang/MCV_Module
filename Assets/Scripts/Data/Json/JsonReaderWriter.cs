
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace MCV_Module.Data.Json
{
    public static class JsonReaderWriter
{
    static string PATH = "Data/";
    static string EXT = ".json";

    public static void Write<T>(string name, T data,Action callback)
    {
        bool fileExist = System.IO.File.Exists(FULL_PATH(name));
        if(!fileExist) System.IO.Directory.CreateDirectory(PATH);
        try
        {
            string path = FULL_PATH(name);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            System.IO.File.WriteAllText(path, json);
            callback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        
    }

    public static T Read<T>(string name ,Action<bool> callback)
    {
        try
        {
            string path = FULL_PATH(name);
            string json = System.IO.File.ReadAllText(path);
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

    static string FULL_PATH(string name)
    {
        return Application.streamingAssetsPath + "/" + PATH + name + EXT;
    }

}
}
