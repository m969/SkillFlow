using ECS;

namespace NetGame.SkillFlow
{
    /// <summary>
    /// 技能编排节点进入事件处理器（表现/View 层实现，响应节点进入驱动动画特效）。
    /// 由 <c>SkillFlowInterpreter.Step</c> 经执行载体 <c>EcsEntity.Dispatch</c> 广播，与核心逻辑解耦。
    /// 替代对象树 SkillFlow 的节点事件机制（阶段 D 后客户端表现层用共享版事件）。
    /// </summary>
    public interface ISkillFlowNodeEnterHandle : IDispatch
    {
        /// <summary>节点进入（首次推进）时回调。</summary>
        void OnSkillFlowNodeEnter(NodeFrame frame, SkillFlowContext context);
    }

    /// <summary>
    /// 技能编排节点退出事件处理器（表现/View 层实现，响应节点退出回收/停止表现）。
    /// </summary>
    public interface ISkillFlowNodeExitHandle : IDispatch
    {
        /// <summary>节点退出（成功 / 失败 / 取消 / 超时等终态）时回调。</summary>
        void OnSkillFlowNodeExit(NodeFrame frame, SkillFlowContext context, NodeResult result);
    }
}
