using System;
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Interfaces;
using MCV_Module.Singleton;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Managers
{
    // WHY: 控制器自本分支起不再是 MonoBehaviour、不挂场景（1_Content/ControllerRoot 下的 27 个实例已删除），
    //      改由本管理器在 DelayInit 里按类型表统一创建并常驻；面板仍按 1:1 名字来 Find，
    //      "登记必须早于首次 Bind"这条时序由 DelayInit 早于所有面板 Start 保证（Setup 等本管理器 IsInit 后才加载 1_Content）。
    // WHY: 去 MonoBehaviour 后控制器失去自带协程宿主，且"控制器没了协程不会自动停"；故协程统一挂在本管理器（DontDestroyOnLoad、跨场景存活），
    //      并按控制器分组登记，OnDispose / 退出时整组停止，保住原先"控制器销毁 → 协程停"的语义。
    /// <summary>全局控制器管理器：统一创建 / 登记 / 查找控制器，并作为全部控制器协程的宿主与分组停止点。</summary>
    public class GlobalControllerMgr : SingletonGlobalMgr<GlobalControllerMgr>
    {
        #region 参数
        // WHY: 类型表走"各控制器静态构造自登记 + 本类 getter 反射兜底补齐"双保险：前者保证与初始化顺序无关，
        //      后者保证全局没有任何一处静态构造被触发时（例如只走面板绑定路径）也不漏建。
        static readonly HashSet<Type> s_ControllerTypes = new HashSet<Type>();
        static readonly object s_TypeLock = new object();

        readonly Dictionary<string, IController> _controllers = new Dictionary<string, IController>();
        readonly Dictionary<Type, IController> _byType = new Dictionary<Type, IController>();
        readonly Dictionary<IController, List<Coroutine>> _routines = new Dictionary<IController, List<Coroutine>>();

        bool _isDisposing;
        #endregion

        #region 类型表
        /// <summary>控制器类型表（只有 IController 的具体实现类）。</summary>
        static IEnumerable<Type> ControllerTypes
        {
            get
            {
                lock (s_TypeLock)
                {
                    if (s_ControllerTypes.Count == 0)
                    {
                        foreach (var type in typeof(GlobalControllerMgr).Assembly.GetTypes())
                        {
                            if (!typeof(IController).IsAssignableFrom(type)) continue;
                            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition) continue;
                            s_ControllerTypes.Add(type);
                        }
                    }
                    return new List<Type>(s_ControllerTypes);
                }
            }
        }

        /// <summary>登记一个控制器类型（由 ControllerBase&lt;TView&gt; 的静态构造调用，也可供外部插件手工补登记）。</summary>
        public static void RegisterControllerType(Type type)
        {
            if (type == null || !typeof(IController).IsAssignableFrom(type)) return;
            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition) return;

            lock (s_TypeLock)
            {
                s_ControllerTypes.Add(type);
            }
        }
        #endregion

        #region 生命周期
        protected GlobalControllerMgr() { }

        protected override void Awake()
        {
            base.Awake();
        }

        // WHY: 必须在这里（而非 Start / 首次访问）把控制器建齐 —— 面板在 Start 里按名字 Find，
        //      而 Setup 等的是本管理器的 IsInit，把创建放进 DelayInit 才能保证"登记早于首次 Bind"。
        // WHY: 本方法是同步的，Setup 会阻塞等 IsInit —— 这里多花的时间会直接变成启动等待，故埋点记录总耗时与最慢的一个。
        protected override IEnumerator DelayInit()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            CreateAll();
            sw.Stop();

            if (sw.ElapsedMilliseconds > 50)
                Log.Warning($"[GlobalControllerMgr] 创建 {_controllers.Count} 个控制器耗时 {sw.ElapsedMilliseconds} ms（同步阻塞启动链）");
            else
                Log.Info($"[GlobalControllerMgr] 创建 {_controllers.Count} 个控制器耗时 {sw.ElapsedMilliseconds} ms");

            // WHY: 漏置 isInit 会让 Setup 启动链白等 15s 超时
            isInit = true;
            yield break;
        }

        // WHY: 应用退出时必须停掉全部控制器协程并退订；这里是控制器唯一的整组销毁入口。
        protected override void OnApplicationQuit()
        {
            DisposeAll();
            base.OnApplicationQuit();
        }

        /// <summary>销毁全部控制器（停协程 → OnDispose → 清表）；幂等，供退出与测试调用。</summary>
        public void DisposeAll()
        {
            if (_isDisposing) return;
            _isDisposing = true;

            var all = new List<IController>(_controllers.Values);

            // WHY: 逐个 StopAllCoroutines —— 去 MonoBehaviour 后控制器自身不再随 GameObject 一起带走协程
            for (int i = 0; i < all.Count; i++)
            {
                StopAllCoroutines(all[i]);
            }

            for (int i = 0; i < all.Count; i++)
            {
                var controller = all[i];
                if (controller == null) continue;
                try
                {
                    controller.OnDispose();
                }
                catch (Exception e)
                {
                    Log.Error($"[GlobalControllerMgr] {controller.ControllerName} OnDispose 抛异常：{e}");
                }
            }

            _controllers.Clear();
            _byType.Clear();
            _routines.Clear();
            _isDisposing = false;
        }
        #endregion

        #region 创建 / 注销
        /// <summary>按类型表创建并登记全部控制器（幂等：已登记的类型跳过）。</summary>
        void CreateAll()
        {
            foreach (var type in ControllerTypes)
            {
                if (_byType.ContainsKey(type)) continue;
                Create(type);
            }
        }

        /// <summary>创建一个控制器并按类型 / 名字双重登记（供外部插件手工补建；正常路径由 DelayInit 批量创建）。</summary>
        public IController Create(Type type)
        {
            if (type == null) { Log.Error("[GlobalControllerMgr] Create 收到空类型，跳过"); return null; }
            if (!typeof(IController).IsAssignableFrom(type))
            {
                Log.Error($"[GlobalControllerMgr] {type.Name} 未实现 IController，无法创建");
                return null;
            }
            if (_isDisposing) return null;
            if (_byType.TryGetValue(type, out var exist) && exist != null) return exist;

            IController controller;
            try
            {
                controller = (IController)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                Log.Error($"[GlobalControllerMgr] 创建控制器 {type.Name} 失败（需要公开无参构造）：{e.Message}");
                return null;
            }

            _byType[type] = controller;

            var key = controller.ControllerName;
            if (_controllers.TryGetValue(key, out var sameName) && sameName != null && !ReferenceEquals(sameName, controller))
            {
                Log.Warning($"[GlobalControllerMgr] 控制器名 {key} 已存在（{sameName.GetType().Name}），将被 {type.Name} 覆盖");
            }
            _controllers[key] = controller;

            try
            {
                controller.OnInit();
            }
            catch (Exception e)
            {
                Log.Error($"[GlobalControllerMgr] {key} OnInit 抛异常：{e}");
            }

            return controller;
        }

        /// <summary>注销并销毁一个控制器（停协程 → OnDispose → 清表）。</summary>
        public void Unregister(IController controller)
        {
            if (controller == null) return;

            StopAllCoroutines(controller);

            try
            {
                controller.OnDispose();
            }
            catch (Exception e)
            {
                Log.Error($"[GlobalControllerMgr] {controller.ControllerName} OnDispose 抛异常：{e}");
            }

            _byType.Remove(controller.GetType());
            if (_controllers.TryGetValue(controller.ControllerName, out var exist) && ReferenceEquals(exist, controller))
            {
                _controllers.Remove(controller.ControllerName);
            }
        }
        #endregion

        #region 查找
        /// <summary>按类型查找控制器。</summary>
        public T Find<T>() where T : class, IController
        {
            return _byType.TryGetValue(typeof(T), out var controller) ? controller as T : null;
        }

        /// <summary>按名称查找控制器。</summary>
        public IController Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            _controllers.TryGetValue(name, out var controller);
            return controller;
        }

        /// <summary>是否存在指定控制器。</summary>
        public bool Exist<T>() where T : IController
        {
            return _byType.ContainsKey(typeof(T));
        }

        /// <summary>获取所有已登记的控制器。</summary>
        public IReadOnlyCollection<IController> All => _controllers.Values;
        #endregion

        #region 协程（控制器共用宿主）
        /// <summary>以本管理器为宿主，为指定控制器启动一个协程并登记（控制器 OnDispose / 退出时整组停止）。</summary>
        public Coroutine RunCoroutine(IController owner, IEnumerator routine)
        {
            if (routine == null) return null;
            if (owner == null)
            {
                // WHY: 去 MonoBehaviour 后协程无法再靠宿主对象自动回收，归属不明的协程一律不启动，宁可不跑也不泄漏
                Log.Error("[GlobalControllerMgr] RunCoroutine 收到空控制器，协程未启动");
                return null;
            }
            if (_isDisposing) return null;

            var handle = StartCoroutine(routine);

            if (!_routines.TryGetValue(owner, out var list))
            {
                list = new List<Coroutine>();
                _routines[owner] = list;
            }
            list.Add(handle);
            return handle;
        }

        /// <summary>停止指定控制器启动的某一个协程，并从登记表移除。</summary>
        public void StopCoroutine(IController owner, Coroutine routine)
        {
            if (owner == null || routine == null) return;

            if (_routines.TryGetValue(owner, out var list))
            {
                list.Remove(routine);
                if (list.Count == 0) _routines.Remove(owner);
            }

            base.StopCoroutine(routine);
        }

        /// <summary>停止指定控制器启动的全部协程（在 OnDispose 之前调用）。</summary>
        public void StopAllCoroutines(IController owner)
        {
            if (owner == null) return;
            if (!_routines.TryGetValue(owner, out var list)) return;

            _routines.Remove(owner);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null) base.StopCoroutine(list[i]);
            }
        }
        #endregion
    }
}
