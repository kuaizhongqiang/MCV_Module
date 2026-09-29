namespace MCV_Module.Utils.Pool
{
    // WHY: GameObjectPool 只在创建实例时扫描并缓存回调，运行期新增的 IPoolable 组件不会被调用；未在 OnDespawn 退订的事件会在下次复用时叠加。
    /// <summary>池化对象回调：实例自己负责出池复位（OnSpawn）与回池清理（OnDespawn），可挂在实例根节点或任意子节点。</summary>
    public interface IPoolable
    {
        /// <summary>出池时调用（此时已挂到目标父节点并激活）。</summary>
        void OnSpawn();

        /// <summary>回池时调用（此时仍在活跃状态，即将失活挂回闲置容器）。</summary>
        void OnDespawn();
    }
}
