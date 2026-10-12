namespace NetGame.Combat
{
    /// <summary>
    /// 内置 Trigger 的稳定类型 ID。
    /// <para>
    /// 规则：全小写、点分、固定前缀；ID 一经发布**不得更改或复用**，
    /// 不依赖业务枚举序号或 CLR 类型名。
    /// 外部扩展应使用自己的反向域名前缀，例如 <c>com.example.combat.trigger.on-hit</c>。
    /// </para>
    /// </summary>
    public static class TriggerTypeIds
    {
        /// <summary>框架内置 Trigger ID 的命名空间前缀。</summary>
        public const string Prefix = "netgame.combat.trigger.";

        /// <summary>主动触发：由外部显式提交触发上下文。</summary>
        public const string Execute = Prefix + "execute";

        /// <summary>能力激活时立即触发。</summary>
        public const string Instant = Prefix + "instant";

        /// <summary>按行动点事件触发。</summary>
        public const string ActionPoint = Prefix + "action-point";

        /// <summary>按计时 / 条件事件触发。</summary>
        public const string TimeCondition = Prefix + "time-condition";

        /// <summary>判断给定 ID 是否为本框架内置 Trigger ID。</summary>
        public static bool IsBuiltIn(string triggerTypeId)
        {
            switch (triggerTypeId)
            {
                case Execute:
                case Instant:
                case ActionPoint:
                case TimeCondition:
                    return true;
                default:
                    return false;
            }
        }
    }
}
