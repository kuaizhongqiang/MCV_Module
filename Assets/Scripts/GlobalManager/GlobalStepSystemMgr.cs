using System.Collections;
using System.Collections.Generic;
using MCV_Module.StepSystem;
using UnityEngine;

namespace MCV_Module.GlobalManager
{
    /// <summary>
    /// 步骤系统全局管理器 —— 管理所有 StepDirector 实例。
    /// </summary>
    public class GlobalStepSystemMgr : SingletonGlobalMgr<GlobalStepSystemMgr>
    {
        protected GlobalStepSystemMgr() { }

        private readonly List<StepDirector> _directors = new List<StepDirector>();

        protected override IEnumerator DelayInit()
        {
            isInit = true;
            yield break;
        }

        public void RegisterDirector(StepDirector director)
        {
            if (!_directors.Contains(director))
                _directors.Add(director);
        }

        public void UnregisterDirector(StepDirector director)
        {
            if (_directors.Contains(director))
                _directors.Remove(director);
        }

        /// <summary>获取所有已注册的 Director</summary>
        public IReadOnlyList<StepDirector> Directors => _directors;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _directors.Clear();
        }
    }
}
