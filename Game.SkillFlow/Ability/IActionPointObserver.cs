using ECS;

namespace NetGame.Combat
{
    /// <summary>
    /// 行动点观察者契约：注册到实体 <c>BehaviourPointComponent</c> 的行动点，发布时收到回调。
    /// <para>
    /// 由「行动点触发」运行时实现；发布方（战斗系统）与观察方（Trigger 运行时）通过该契约解耦，
    /// 无需共享具体 System 类型。
    /// </para>
    /// </summary>
    public interface IActionPointObserver
    {
        /// <summary>行动点触发回调；<paramref name="actionContext"/> 为触发来源（如本次战斗行动，可空）。</summary>
        void OnActionPoint(EcsEntity actionContext);
    }
}
