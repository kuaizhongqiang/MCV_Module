

using System.Collections;

namespace MCV_Module.GlobalManager
{
    public class GlobalSceneMgr : SingletonGlobalMgr<GlobalSceneMgr>
    {
        protected GlobalSceneMgr(){}

        protected override void Awake()
        {
            base.Awake();
        }

        protected override IEnumerator DelayInit()
        {
            yield break;
        }
    }
}