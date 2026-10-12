using NetGame.SkillFlow;

namespace NetGame.Combat
{
    /// <summary>
    /// 接入方提供的 Ability 效果处理宿主。SkillFlow 将 <see cref="Effect"/> 载荷交给宿主执行，
    /// 并根据返回的节点结果继续推进流程；接口不定义效果的解析、结算或表现规则。
    /// </summary>
    public interface IAbilityEffectHost
    {
        /// <summary>执行一次战斗效果。返回 NodeResult 供 SkillFlow 节点消费。</summary>
        NodeResult ExecuteEffect(SkillFlowContext ctx, Effect effect, NodeFrame frame);
    }
}
