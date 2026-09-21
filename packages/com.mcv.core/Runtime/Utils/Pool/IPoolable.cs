namespace MCV_Module.Utils.Pool
{
    /// <summary>
    /// 池化对象回调 —— 实例自己负责「出池复位 / 回池清理」。
    ///
    /// 实现位置：实例根节点或任意子节点均可（<see cref="GameObjectPool"/> 在**创建实例时扫描一次**并缓存，
    /// 运行期新增的组件不会再被回调）。
    ///
    /// 典型用法：
    ///   OnSpawn   → 复位 localPosition/localRotation/localScale、清零计时器、恢复初始显隐、重新订阅事件；
    ///   OnDespawn → 停止本实例发起的协程/动画、清空临时数据、退订事件（未退订会在下次复用时叠加回调）。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>出池时调用（此时已挂到目标父节点并激活）。</summary>
        void OnSpawn();

        /// <summary>回池时调用（此时仍在活跃状态，即将失活挂回闲置容器）。</summary>
        void OnDespawn();
    }
}
