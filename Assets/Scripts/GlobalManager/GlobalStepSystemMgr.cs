using System.Collections;

namespace MCV_Module.GlobalManager
{
    /// <summary>
    /// 步骤系统全局管理器 —— 管理 StepDirector 的执行生命周期。
    /// TODO: M3 填入实现 —— 注册到 Setup.cs 初始化管线
    /// </summary>
    public class GlobalStepSystemMgr : SingletonGlobalMgr<GlobalStepSystemMgr>
    {
        protected GlobalStepSystemMgr() { }

        protected override IEnumerator DelayInit()
        {
            // TODO: M3 实现 —— 步骤系统初始化
            isInit = true;
            yield break;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // TODO: M3 实现 —— 清理步骤系统状态
        }
    }
}
