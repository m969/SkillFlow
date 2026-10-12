using System;
using ECS;

namespace NetGame.SkillFlow
{
    /// <summary>
    /// SkillFlow 的可注入事件源。
    /// 外部战斗项目负责把碰撞、命中、动画或领域事件映射到 eventKey；框架不依赖具体事件总线。
    /// </summary>
    public interface ISkillEventSource
    {
        /// <summary>
        /// 订阅指定事件键。
        /// 事件源应在模拟线程或调用方定义的确定性调度点执行回调，避免后台线程直接修改节点状态。
        /// </summary>
        /// <param name="eventKey">WaitEvent 节点配置中的事件键。</param>
        /// <param name="onEvent">事件到达时调用的无参数信号回调。</param>
        /// <returns>取消订阅句柄。</returns>
        IDisposable Subscribe(string eventKey, Action onEvent);
    }

    /// <summary>
    /// 可提供目标实体载荷的技能事件源扩展。
    ///
    /// <para>
    /// 基础事件源只负责通知“事件发生”，该扩展允许命中事件同时携带目标实体。
    /// SkillFlow 解释器会在事件回调的确定性调度点把目标写入
    /// <see cref="SkillFlowContext.PrimaryTarget"/> 和黑板 <c>hitTarget</c>，效果宿主即可复用同一目标，
    /// 而不需要让碰撞系统直接执行伤害或其他业务规则。
    /// </para>
    /// </summary>
    public interface ISkillEventTargetSource : ISkillEventSource
    {
        /// <summary>最近一次发布事件携带的目标实体；没有目标时为空。</summary>
        EcsEntity LastTargetEntity { get; }
    }
}
