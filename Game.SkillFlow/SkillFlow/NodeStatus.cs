namespace NetGame.SkillFlow
{
    /// <summary>
    /// 节点执行状态（与 docs/skill-script.md §3 对齐）。
    /// 六态不仅覆盖成功/失败，还覆盖异步推进、取消、打断、超时，支撑实时战斗的可中断语义。
    /// </summary>
    public enum NodeStatus
    {
        /// <summary>尚未完成，需后续 Tick / 事件推进。</summary>
        Running,
        /// <summary>正常完成。</summary>
        Success,
        /// <summary>无法完成或规则失败（不一定是报错，如无合法目标）。</summary>
        Failure,
        /// <summary>被外部取消（眩晕、死亡、切场景等）。</summary>
        Cancelled,
        /// <summary>被更高优先级行为打断。</summary>
        Interrupted,
        /// <summary>等待或执行超时。</summary>
        Timeout,
    }

    /// <summary>
    /// 节点执行结果：状态 + 可选原因。与 <see cref="NodeStatus"/> 语义绑定。
    /// 使用只读结构，避免类实例分配；静态工厂方法收敛构造入口。
    /// </summary>
    public readonly struct NodeResult
    {
        /// <summary>结果状态。</summary>
        public readonly NodeStatus Status;

        /// <summary>附带原因（诊断 / 遥测，可为空）。</summary>
        public readonly string Reason;

        /// <summary>构造。</summary>
        public NodeResult(NodeStatus status, string reason = null)
        {
            Status = status;
            Reason = reason;
        }

        /// <summary>构建 Running 结果。</summary>
        public static NodeResult Running() => new NodeResult(NodeStatus.Running);

        /// <summary>构建 Success 结果。</summary>
        public static NodeResult Success(string reason = null) => new NodeResult(NodeStatus.Success, reason);

        /// <summary>构建 Failure 结果。</summary>
        public static NodeResult Failure(string reason = null) => new NodeResult(NodeStatus.Failure, reason);

        /// <summary>构建 Cancelled 结果。</summary>
        public static NodeResult Cancelled(string reason = null) => new NodeResult(NodeStatus.Cancelled, reason);

        /// <summary>构建 Interrupted 结果。</summary>
        public static NodeResult Interrupted(string reason = null) => new NodeResult(NodeStatus.Interrupted, reason);

        /// <summary>构建 Timeout 结果。</summary>
        public static NodeResult Timeout(string reason = null) => new NodeResult(NodeStatus.Timeout, reason);
    }
}
