using System;
using System.Collections.Generic;

namespace NetGame.Combat
{
    /// <summary>
    /// Trigger 扩展的显式注册表：稳定 <see cref="ITriggerHandler.TriggerTypeId"/> → <see cref="ITriggerHandler"/>。
    /// <para>
    /// 与 <see cref="EffectHandlerRegistry"/> 对齐：稳定 ID、显式注册、未注册显式失败；使用**实例**而非全局静态，
    /// 避免并行测试、多个 World 或热重载互相污染。
    /// </para>
    /// <para>
    /// 差异：Trigger ID 要求唯一，**重复注册默认抛出**（而非静默覆盖）；热重载等需要替换时显式传
    /// <c>replace: true</c>。
    /// </para>
    /// </summary>
    public sealed class TriggerHandlerRegistry
    {
        private readonly Dictionary<string, ITriggerHandler> handlers = new Dictionary<string, ITriggerHandler>(StringComparer.Ordinal);
        private readonly object syncRoot = new object();

        /// <summary>当前已注册的 Trigger 类型数量。</summary>
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
        /// 注册 Trigger 处理器。
        /// </summary>
        /// <param name="handler">处理器；其 <see cref="ITriggerHandler.TriggerTypeId"/> 必须非空。</param>
        /// <param name="replace">
        /// 为 <c>true</c> 时覆盖同 ID 旧处理器（供热重载 / 宿主替换）；
        /// 为 <c>false</c>（默认）时重复 ID 抛出 <see cref="InvalidOperationException"/>。
        /// </param>
        public void Register(ITriggerHandler handler, bool replace = false)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var triggerTypeId = handler.TriggerTypeId;
            if (string.IsNullOrWhiteSpace(triggerTypeId))
            {
                throw new ArgumentException("TriggerTypeId 不能为空。", nameof(handler));
            }

            lock (syncRoot)
            {
                if (!replace && handlers.ContainsKey(triggerTypeId))
                {
                    throw new InvalidOperationException("TriggerTypeId 重复注册: " + triggerTypeId);
                }

                handlers[triggerTypeId] = handler;
            }
        }

        /// <summary>注销一个 Trigger 处理器。</summary>
        public bool Unregister(string triggerTypeId)
        {
            if (string.IsNullOrWhiteSpace(triggerTypeId))
            {
                return false;
            }

            lock (syncRoot)
            {
                return handlers.Remove(triggerTypeId);
            }
        }

        /// <summary>获取指定 Trigger 类型的处理器。</summary>
        public bool TryGet(string triggerTypeId, out ITriggerHandler handler)
        {
            if (!string.IsNullOrWhiteSpace(triggerTypeId))
            {
                lock (syncRoot)
                {
                    if (handlers.TryGetValue(triggerTypeId, out var value))
                    {
                        handler = value;
                        return true;
                    }
                }
            }

            handler = null!;
            return false;
        }

        /// <summary>
        /// 按稳定 ID 创建 Trigger 运行时。
        /// 未注册或 ID 空白时**显式抛出**，不静默忽略或退化成默认触发。
        /// </summary>
        public ITriggerRuntime CreateRuntime(string triggerTypeId, TriggerCreateContext context)
        {
            if (string.IsNullOrWhiteSpace(triggerTypeId))
            {
                throw new ArgumentException("TriggerTypeId 不能为空。", nameof(triggerTypeId));
            }

            if (!TryGet(triggerTypeId, out var handler))
            {
                throw new KeyNotFoundException("Trigger handler not registered: " + triggerTypeId);
            }

            return handler.CreateRuntime(context);
        }

        /// <summary>
        /// 非抛出式创建：未注册时返回 <c>false</c> 并给出可诊断错误。
        /// </summary>
        public bool TryCreateRuntime(
            string triggerTypeId,
            TriggerCreateContext context,
            out ITriggerRuntime runtime,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(triggerTypeId))
            {
                runtime = null!;
                error = "TriggerTypeId 不能为空。";
                return false;
            }

            if (!TryGet(triggerTypeId, out var handler))
            {
                runtime = null!;
                error = "Trigger handler not registered: " + triggerTypeId;
                return false;
            }

            runtime = handler.CreateRuntime(context);
            error = string.Empty;
            return true;
        }

        /// <summary>清空处理器，供测试隔离和宿主热重载使用。</summary>
        public void Clear()
        {
            lock (syncRoot)
            {
                handlers.Clear();
            }
        }
    }
}
