using System;
using System.Collections.Generic;
using NetGame.SkillFlow;

namespace NetGame.Combat
{
    /// <summary>
    /// 外部效果处理器契约。
    /// 每个处理器只负责一种稳定的 <see cref="Effect.EffectTypeId"/>，具体目标选择和数值结算由业务程序集实现。
    /// </summary>
    public interface IAbilityEffectHandler
    {
        /// <summary>
        /// 执行一次效果。
        /// </summary>
        /// <param name="context">当前技能执行上下文。</param>
        /// <param name="effect">通用效果定义，外部处理器可转换为自己的派生类型。</param>
        /// <param name="frame">当前 AbilityEffect 节点状态。</param>
        /// <returns>Success、Failure 或 Running。</returns>
        NodeResult Execute(SkillFlowContext context, Effect effect, NodeFrame frame);
    }

    /// <summary>
    /// 显式的效果类型到处理器注册表。
    /// 使用实例而不是全局静态注册表，避免并行测试、多个 World 或热重载之间互相污染。
    /// 同一类型重新注册时覆盖旧处理器，便于资源热更和宿主替换。
    /// </summary>
    public sealed class EffectHandlerRegistry : IAbilityEffectHost
    {
        private readonly Dictionary<string, IAbilityEffectHandler> handlers = new Dictionary<string, IAbilityEffectHandler>();
        private readonly object syncRoot = new object();

        /// <summary>当前已注册的效果类型数量。</summary>
        public int Count
        {
            get
            {
                lock (syncRoot)
                {
                    return handlers.Count;
                }
            }
        }

        /// <summary>
        /// 注册效果处理器。
        /// </summary>
        /// <param name="effectTypeId">稳定且非空的效果类型标识。</param>
        /// <param name="handler">对应处理器。</param>
        public void Register(string effectTypeId, IAbilityEffectHandler handler)
        {
            if (string.IsNullOrWhiteSpace(effectTypeId))
            {
                throw new ArgumentException("EffectTypeId 不能为空。", nameof(effectTypeId));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (syncRoot)
            {
                handlers[effectTypeId] = handler;
            }
        }

        /// <summary>注销一个效果处理器。</summary>
        public bool Unregister(string effectTypeId)
        {
            lock (syncRoot)
            {
                return handlers.Remove(effectTypeId);
            }
        }

        /// <summary>获取指定效果类型的处理器。</summary>
        public bool TryGet(string effectTypeId, out IAbilityEffectHandler handler)
        {
            lock (syncRoot)
            {
                if (handlers.TryGetValue(effectTypeId, out var value))
                {
                    handler = value;
                    return true;
                }
            }

            handler = null!;
            return false;
        }

        /// <summary>清空处理器，供测试隔离和宿主热重载使用。</summary>
        public void Clear()
        {
            lock (syncRoot)
            {
                handlers.Clear();
            }
        }

        /// <summary>
        /// 按效果的稳定类型标识分派处理器。
        /// 未注册类型不会静默跳过，而是返回可观测的 Failure 结果。
        /// </summary>
        public NodeResult ExecuteEffect(SkillFlowContext context, Effect effect, NodeFrame frame)
        {
            if (effect == null)
            {
                return NodeResult.Failure("effect is null");
            }
            if (string.IsNullOrWhiteSpace(effect.EffectTypeId))
            {
                return NodeResult.Failure("effect type id is empty");
            }
            if (!TryGet(effect.EffectTypeId, out var handler))
            {
                return NodeResult.Failure("effect handler not registered: " + effect.EffectTypeId);
            }

            return handler.Execute(context, effect, frame);
        }
    }
}