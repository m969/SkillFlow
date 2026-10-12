namespace NetGame.SkillFlow
{
    /// <summary>
    /// 黑板读写静态接口。节点不得依赖全局变量或技能类字段，必须经本系统显式读写当前施法上下文。
    /// 纯静态方法，无状态，符合「System 只含方法」规范。
    /// </summary>
    public static class SkillFlowContextSystem
    {
        /// <summary>写入黑板变量（覆盖同键旧值）。</summary>
        public static void SetVariable(SkillFlowContext context, string key, object value)
        {
            context.Variables[key] = value;
        }

        /// <summary>读取黑板变量（不存在返回 null）。</summary>
        public static object GetVariable(SkillFlowContext context, string key)
        {
            return context.Variables.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>读取并转换黑板变量（不存在或类型不符返回默认值）。</summary>
        public static T GetVariable<T>(SkillFlowContext context, string key, T defaultValue = default)
        {
            return context.Variables.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;
        }
    }
}
