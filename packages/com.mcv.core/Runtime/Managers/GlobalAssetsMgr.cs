using System;
using MCV_Module.Utils;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MCV_Module.Singleton;
using UnityEngine;
using UnityEngine.Networking;

namespace MCV_Module.Managers
{
    /// <summary>
    /// 全局资产管理器 —— 统一管理 StreamingAssets 中图片、视频等资源的异步加载
    ///
    /// 图片加载带 LRU 缓存（上限 MaxCachedImages）：
    ///   - 命中缓存直接回调（并刷新 LRU 顺序），不再重复请求
    ///   - 超上限时淘汰最久未使用的 Sprite 并 Destroy（连同其 Texture），避免内存无限增长
    /// 注意：淘汰会使持有该 Sprite 的 UI 引用失效，调用方应只短暂持有或自行复制。
    /// </summary>
    public class GlobalAssetsMgr : SingletonGlobalMgr<GlobalAssetsMgr>
    {
        #region 参数
        /// <summary>图片缓存上限，超过后按最近最少使用淘汰</summary>
        const int MaxCachedImages = 32;

        readonly Dictionary<string, Sprite> imageCache = new Dictionary<string, Sprite>();
        readonly List<string> cacheOrder = new List<string>();
        #endregion

        #region 生命周期
        protected GlobalAssetsMgr() { }

        protected override IEnumerator DelayInit()
        {
            isInit = true;
            yield break;
        }
        #endregion

        #region 静态方法
        /// <summary>
        /// 从 StreamingAssets/Image/ 异步加载图片并回调 Sprite（带 LRU 缓存）
        /// </summary>
        /// <param name="imageName"> 图片文件名（如 "header.png"）</param>
        /// <param name="onSuccess"> 加载成功回调，返回 Sprite </param>
        /// <param name="onError"> 加载失败回调，返回错误信息（可选）</param>
        public static void LoadImageAsync(string imageName, Action<Sprite> onSuccess, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(imageName))
            {
                onError?.Invoke("imageName 为空");
                return;
            }

            var mgr = Instance;

            // 缓存命中：直接回调并刷新 LRU 顺序
            if (mgr.imageCache.TryGetValue(imageName, out var cached))
            {
                mgr.Touch(imageName);
                onSuccess?.Invoke(cached);
                return;
            }

            mgr.StartCoroutine(mgr.LoadImageCoroutine(imageName, onSuccess, onError));
        }
        #endregion

        #region 私有方法
        IEnumerator LoadImageCoroutine(string imageName, Action<Sprite> onSuccess, Action<string> onError)
        {
            var imagePath = Path.Combine("Image", imageName);
            var url = GetStreamingUrl(imagePath);

            using (var uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(uwr);
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                    Log.Info($"[GlobalAssetsMgr] 图片加载成功：{imagePath}");

                    // 并发加载同一图片时，CacheImage 会销毁重复的，返回生效的那份
                    Sprite effective = CacheImage(imageName, sprite);
                    onSuccess?.Invoke(effective);
                }
                else
                {
                    var error = $"[GlobalAssetsMgr] 加载图片失败: {url}, {uwr.error}";
                    Log.Error(error);
                    onError?.Invoke(error);
                }
            }
        }

        /// <summary>写入缓存；若已存在（并发重复）则销毁新加载的并返回已有缓存。</summary>
        Sprite CacheImage(string imageName, Sprite sprite)
        {
            if (imageCache.TryGetValue(imageName, out var existing))
            {
                if (sprite != null && sprite != existing)
                {
                    if (sprite.texture != null) Destroy(sprite.texture);
                    Destroy(sprite);
                }
                return existing;
            }

            imageCache[imageName] = sprite;
            cacheOrder.Add(imageName);
            EvictIfNeeded();
            return sprite;
        }

        /// <summary>刷新 LRU 顺序（移到队尾 = 最近使用）</summary>
        void Touch(string imageName)
        {
            cacheOrder.Remove(imageName);
            cacheOrder.Add(imageName);
        }

        /// <summary>超出上限时淘汰最久未使用的图片（Destroy Sprite + Texture）</summary>
        void EvictIfNeeded()
        {
            while (cacheOrder.Count > MaxCachedImages)
            {
                string oldest = cacheOrder[0];
                cacheOrder.RemoveAt(0);
                if (imageCache.TryGetValue(oldest, out var sprite))
                {
                    imageCache.Remove(oldest);
                    if (sprite != null)
                    {
                        if (sprite.texture != null) Destroy(sprite.texture);
                        Destroy(sprite);
                        Log.Info($"[GlobalAssetsMgr] 缓存淘汰：{oldest}");
                    }
                }
            }
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 构造 StreamingAssets 完整路径（WebGL 兼容）
        /// </summary>
        static string GetStreamingUrl(string relativePath)
        {
            var path = Path.Combine(Application.streamingAssetsPath, relativePath);
#if UNITY_WEBGL && !UNITY_EDITOR
            return path;
#else
            return "file:///" + path;
#endif
        }
        #endregion
    }
}
