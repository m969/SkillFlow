using ECS;

namespace NetGame.Combat
{
    /// <summary>
    /// Trigger 效果派发契约：公共触发宿主在目标解析与状态条件通过后，把「应用哪些效果」交给端特定实现。
    /// <para>
    /// 目的：让 Trigger 扩展（何时触发）与效果落地（效果如何应用）解耦——客户端可走
    /// <c>AssignActionSystem</c>，服务端可走 <c>ServerAbilityEffectHost</c> 等；宿主不再复制派发逻辑。
    /// </para>
    /// </summary>
    public interface ITriggerEffectDispatcher
    {
        /// <summary>
        /// 应用一次触发对应的效果。<paramref name="target"/> 已经过目标回退；<paramref name="context"/> 为触发上下文。
        /// </summary>
        void Apply(AbilityTrigger trigger, EcsEntity target, ref TriggerContext context);
    }
}
