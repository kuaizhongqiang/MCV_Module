# Contract: JsonReaderWriter

Role: static JSON read/write helper; editor synchronous write, runtime synchronous write and runtime asynchronous read (WebGL compatible).

Fields:
PATH  const sub-directory "Data/"
EXT  const extension ".json"

Methods:
Write<T>(name, data, callback)  editor only; create the directory from the full path and write synchronously -> Log.Error on failure
WriteRuntime<T>(name, data)  runtime synchronous write -> true on success, false with a warning on WebGL, false on failure
Read<T>(name, callback)  synchronous read and deserialize -> callback true; on failure Log.Error, callback false and return default
ReadAsync<T>(name, callback)  coroutine; UnityWebRequest read (WebGL compatible) -> callback(data, true) on success, callback(default, false) on failure
FULL_PATH(name)  static; streamingAssetsPath + "/" + PATH + name + EXT

Notes:
- Write is editor only: WebGL has no synchronous file IO, so runtime must use ReadAsync or Read.
- The directory must be created from the full path; the old relative "Data/" created it under the current working directory and wrote to the wrong place.
- WriteRuntime is for settings data only (e.g. SystemData.renderQuality); content data stays read-only at runtime.
- ReadAsync exists because on WebGL StreamingAssets is an HTTP URL and cannot be read as a file.
