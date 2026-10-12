namespace NetGame.SkillFlow
{
    /// <summary>
    /// 原子动作宿主接口。共享核心只依赖本接口，具体行为由接入方实现。
    /// </summary>
    public interface ISkillActionHost
    {
        /// <summary>
        /// 执行一个原子动作。
        /// </summary>
        /// <param name="context">当前施法上下文（黑板）。</param>
        /// <param name="actionKey">动作键名（对应节点 StringParam，用于两端映射到具体行为）。</param>
        /// <param name="frame">当前节点运行时状态。</param>
        /// <returns>节点结果：同步完成直接返回 Success/Failure；需要异步推进返回 Running。</returns>
        NodeResult Execute(SkillFlowContext context, string actionKey, NodeFrame frame);
    }
}
