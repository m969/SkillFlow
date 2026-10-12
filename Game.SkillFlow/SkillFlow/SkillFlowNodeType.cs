namespace NetGame.SkillFlow
{
    /// <summary>
    /// 节点类型：数据化节点图用枚举区分节点，而非多态节点类。
    /// 控制语义（<see cref="Sequence"/> 至 <see cref="CancelScope"/>）由 <c>SkillFlowInterpreter</c> 静态分派；
    /// 原子动作（<see cref="SkillAction"/>）经 <c>ISkillActionHost</c> 分端实现。
    /// </summary>
    public enum SkillFlowNodeType
    {
        /// <summary>顺序：子节点按序执行，任一失败则停止。</summary>
        Sequence,
        /// <summary>选择：按序尝试，直到一个成功。</summary>
        Selector,
        /// <summary>并行：多子节点并发推进（All / Any 完成策略）。</summary>
        Parallel,
        /// <summary>竞速：分支竞争，最先达成目标结果者获胜。</summary>
        Race,
        /// <summary>条件分支：黑板布尔值选择分支。</summary>
        If,
        /// <summary>多路分支：黑板值匹配分支。</summary>
        Switch,
        /// <summary>循环：Count / While。</summary>
        Loop,
        /// <summary>遍历：遍历黑板集合逐元素执行。</summary>
        ForEach,
        /// <summary>重试：子节点失败后重试。</summary>
        Retry,
        /// <summary>延迟：等待指定时长。</summary>
        Delay,
        /// <summary>事件等待：等待业务事件 / 条件成立。</summary>
        WaitEvent,
        /// <summary>超时：包裹子节点，超时取消并返回 Timeout。</summary>
        Timeout,
        /// <summary>取消作用域：取消传播边界，脱手子树继续运行。</summary>
        CancelScope,
        /// <summary>原子动作：执行一次由接入方定义的行为。</summary>
        SkillAction,
        /// <summary>效果载荷：交由接入方注入的效果宿主处理。</summary>
        AbilityEffect,
    }
}
