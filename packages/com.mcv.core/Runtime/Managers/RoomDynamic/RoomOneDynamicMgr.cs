using System.Collections;
using MCV_Module.Managers;
using MCV_Module.Models.Addressable;
using MCV_Module.Objects.Interactives.RoomDynamic;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.Video;

namespace MCV_Module.Managers.RoomDynamic
{
    /// <summary>房间 1 的场景级动态：HUD 背景视频（入场片接循环片）+ 从 RoomOne 全局包领项目 HUD 图标。</summary>
    public class RoomOneDynamicMgr : RoomDynamicMgrBase<RoomOneDynamicMgr>
    {
        [SerializeField] VideoPlayer onceAlphaVideoPlayer;
        [SerializeField] VideoPlayer onceColorVideoPlayer;
        [SerializeField] VideoPlayer loopAlphaVideoPlayer;
        [SerializeField] VideoPlayer loopColorVideoPlayer;
        [Tooltip("入场短片时长（秒）—— 只在播放器读不到 length 时兜底，正常以 VideoPlayer.length 为准")]
        [SerializeField] float onceVideoDuration = 1f;

        [Header("项目 HUD 图标（RoomOne 全局包）")]
        [Tooltip("项目 HUD 的父物体（11_Room1 里的 MenuObjs）。其下每个 RoomMenuObj 各按自己的 projectName 领图标；" +
                 "留空则整段图标加载跳过（只告警，不影响背景视频）")]
        [SerializeField] Transform menuObjsRoot;

        const string onceAlphaVideoPath = "/Video/HUD_Bg_Alpha.mp4";
        const string onceColorVideoPath = "/Video/HUD_Bg_Color.mp4";
        const string loopAlphaVideoPath = "/Video/HUD_Bg_Alpha_Loop.mp4";
        const string loopColorVideoPath = "/Video/HUD_Bg_Color_Loop.mp4";

        /// <summary>单个播放器等待 Prepare 就绪的上限（秒）：超时就告警并按现状继续，不把流程卡死</summary>
        const float PrepareTimeout = 5f;

        /// <summary>入场短片放完的精确信号（<c>loopPointReached</c> 置位）</summary>
        bool m_OnceFinished;

        /// <summary>本实例是否已销毁：图标加载是异步回调，回来时房间可能已经卸了</summary>
        bool m_Destroyed;

        #region 生命周期
        // WHY: 不重写 Awake —— 基类只对胜出的实例启动 DelayInit，被判重而 Destroy(this) 的副本不会跑到这里，两个实例不会抢同一对 RT
        protected override IEnumerator DelayInit()
        {
            yield return null;   // 与基类同口径：等一帧，让 Inspector 上的引用与场景就绪

            // WHY: 图标异步加载不 yield 等（视频时序不该被资源 IO 拖住），且放在校验之前 —— 视频没挂全也照样出图标
            LoadMenuIcons();

            if (!Validate())
            {
                // 校验失败也算"初始化结束"：否则 IsInit 永远为 false，外部若据此等待会被等死
                isInit = true;
                yield break;
            }

            Configure();
            isInit = true;       // 播放器与 url 都配好了，IsInit 从此为真

            yield return PlayRoutine();
        }

        void OnDestroy()
        {
            m_Destroyed = true;

            if (onceAlphaVideoPlayer != null)
                onceAlphaVideoPlayer.loopPointReached -= OnOnceLoopPointReached;

            // 卸 RoomOne 全局包：先失效缓存再卸 bundle（全局包 clipId 留空，走不了 UnloadClip）
            if (m_LoadedIconIds.Count > 0 && GlobalAddressableMgr.Exists && GlobalAddressableMgr.Instance != null)
            {
                GlobalAddressableMgr.Instance.UnloadByBundleName(ContentNaming.RoomOneBundleName);
                m_LoadedIconIds.Clear();
            }

            // 只清自己那份单例：Awake 里被判重而 Destroy(this) 的副本不能把真身清掉
            if (Instance == this) Instance = null;
        }
        #endregion

        #region 项目 HUD 图标（RoomOne 全局包）
        /// <summary>本次已请求加载图标的包配置 id；卸载按 bundle 名整体失效缓存，这里只用来判「有没有加载过」</summary>
        readonly System.Collections.Generic.List<string> m_LoadedIconIds = new System.Collections.Generic.List<string>();

        /// <summary>按 menuObjsRoot 下各 RoomMenuObj 的 ProjectName 取图标（id 由 HUD 自己推，不按下标配对）。</summary>
        void LoadMenuIcons()
        {
            if (menuObjsRoot == null)
            {
                Log.Warning("[RoomOneDynamicMgr] menuObjsRoot 未挂（应指向 MenuObjs），项目 HUD 图标不加载", this);
                return;
            }

            var targets = new System.Collections.Generic.List<RoomMenuObj>();
            var ids = new System.Collections.Generic.List<string>();

            for (int i = 0; i < menuObjsRoot.childCount; i++)
            {
                Transform child = menuObjsRoot.GetChild(i);
                if (child == null) continue;

                var hud = child.GetComponent<RoomMenuObj>();
                if (hud == null) continue;      // 父物体下允许挂非 HUD 的装饰物

                if (string.IsNullOrEmpty(hud.ProjectName))
                {
                    Log.Warning($"[RoomOneDynamicMgr] {child.name} 的 projectName 为空，跳过（填 ProjectClip.id 才能领到图标）", child);
                    continue;
                }

                targets.Add(hud);
                ids.Add(ContentNaming.RoomIconId(hud.ProjectName));
            }

            if (ids.Count == 0)
            {
                Log.Warning("[RoomOneDynamicMgr] menuObjsRoot 下没有可用的 RoomMenuObj，图标不加载", this);
                return;
            }

            m_LoadedIconIds.Clear();
            m_LoadedIconIds.AddRange(ids);

            GlobalAssetsMgr.LoadSpritesByPackageIdsAsync(ids, _ =>
            {
                // 房间可能在 IO 期间就被卸了，此时 HUD 已随之销毁，不能再碰
                if (m_Destroyed) return;

                int applied = 0;
                foreach (RoomMenuObj hud in targets)
                {
                    if (hud == null) continue;

                    string id = ContentNaming.RoomIconId(hud.ProjectName);
                    Sprite icon = GlobalAssetsMgr.GetSpriteByPackageId(id);
                    if (icon == null)
                    {
                        Log.Warning($"[RoomOneDynamicMgr] 取不到图标 {id}（RoomOne 包内资源键与 RoomIconId 规则对不上？）");
                        continue;
                    }

                    hud.SetIcon(icon);
                    applied++;
                }

                Log.Info($"[RoomOneDynamicMgr] 项目 HUD 图标已装载 {applied}/{targets.Count}");
            },
            error => Log.Warning($"[RoomOneDynamicMgr] RoomOne 图标加载失败：{error}"));
        }
        #endregion

        #region 配置
        bool Validate()
        {
            if (onceAlphaVideoPlayer == null || onceColorVideoPlayer == null ||
                loopAlphaVideoPlayer == null || loopColorVideoPlayer == null)
            {
                Log.Error("[RoomOneDynamicMgr] 四个 VideoPlayer 未挂全，HUD 背景动效不启动", this);
                return false;
            }

            return true;
        }

        void Configure()
        {
            string root = Application.streamingAssetsPath;

            // 入场片：非循环，放完由 loopPointReached 通知
            Setup(onceAlphaVideoPlayer, root + onceAlphaVideoPath, false);
            Setup(onceColorVideoPlayer, root + onceColorVideoPath, false);
            // 循环片：一直放
            Setup(loopAlphaVideoPlayer, root + loopAlphaVideoPath, true);
            Setup(loopColorVideoPlayer, root + loopColorVideoPath, true);

            // 先清后加防重复订阅（房间反复进出时基类单例可能复用同一个组件）
            onceAlphaVideoPlayer.loopPointReached -= OnOnceLoopPointReached;
            onceAlphaVideoPlayer.loopPointReached += OnOnceLoopPointReached;

            m_OnceFinished = false;
        }

        /// <summary>统一配置：源 / url / 循环 / 不自动播，并先 Prepare 起来等就绪。</summary>
        static void Setup(VideoPlayer player, string url, bool isLooping)
        {
            player.source = VideoSource.Url;
            player.url = url;
            player.isLooping = isLooping;
            player.playOnAwake = false;
            player.Prepare();
        }

        void OnOnceLoopPointReached(VideoPlayer player)
        {
            m_OnceFinished = true;
        }
        #endregion

        #region 播放
        IEnumerator PlayRoutine()
        {
            // 1) 入场片：两层都就绪再同帧起播（错帧会让 alpha 描边抖一下）
            yield return WaitPrepared(onceAlphaVideoPlayer);
            yield return WaitPrepared(onceColorVideoPlayer);
            PlayPair(onceAlphaVideoPlayer, onceColorVideoPlayer);

            // WHY: loopPointReached 是精确信号，再拿长度兜底 —— 解码失败时不会回调，没上限会永远停在入场片
            float limit = GetOnceLimit();
            float elapsed = 0f;
            while (!m_OnceFinished && elapsed < limit)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!m_OnceFinished)
                Log.Warning($"[RoomOneDynamicMgr] 入场短片 {limit:0.#}s 内未回调 loopPointReached，按兜底时长切循环片");

            // 3) 先停入场再起循环：同层两个播放器喂同一张 RT，不能交叠
            StopPair(onceAlphaVideoPlayer, onceColorVideoPlayer);

            yield return WaitPrepared(loopAlphaVideoPlayer);
            yield return WaitPrepared(loopColorVideoPlayer);
            PlayPair(loopAlphaVideoPlayer, loopColorVideoPlayer);

            Log.Info("[RoomOneDynamicMgr] HUD 背景已切到循环片");
        }

        /// <summary>入场片时长上限：优先播放器真实 length（Prepare 后可读），读不到再用 onceVideoDuration，加 0.5s 余量。</summary>
        float GetOnceLimit()
        {
            double length = onceAlphaVideoPlayer.length;
            float baseLength = length > 0.01 ? (float)length : Mathf.Max(onceVideoDuration, 0f);
            return baseLength + 0.5f;
        }

        /// <summary>等播放器 Prepare 就绪；超时告警后按现状继续（不把流程卡死）。</summary>
        IEnumerator WaitPrepared(VideoPlayer player)
        {
            float elapsed = 0f;
            while (!player.isPrepared && elapsed < PrepareTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!player.isPrepared)
                Log.Warning($"[RoomOneDynamicMgr] 视频 {PrepareTimeout}s 内未就绪，将按现状尝试播放：{player.url}");
        }

        /// <summary>成对同帧起播（alpha / color 两层，错帧会露出描边）。</summary>
        static void PlayPair(VideoPlayer alpha, VideoPlayer color)
        {
            alpha.Play();
            color.Play();
        }

        /// <summary>成对停止（切层前必须停掉，否则两路抢同一张 RenderTexture）。</summary>
        static void StopPair(VideoPlayer alpha, VideoPlayer color)
        {
            alpha.Stop();
            color.Stop();
        }
        #endregion
    }
}
