using System;
using System.Collections.Generic;

namespace NetGame.SkillFlow
{
    /// <summary>
    /// 技能流程配置注册表：按技能 ID 注册/查询 <c>SkillFlowConfig</c>（纯数据，可 headless）。
    /// 服务端与客户端共用同一份注册表；资源（如 Unity ScriptableObject）加载后注册。
    /// 纯静态工具类，无状态，符合「只含方法」精神（Count 为只读元数据）。
    /// </summary>
    public static class SkillFlowLib
    {
        /// <summary>技能 ID → 流程配置。</summary>
        private static readonly Dictionary<int, SkillFlowConfig> Id2Configs = new Dictionary<int, SkillFlowConfig>();

        /// <summary>
        /// 注册流程配置（同技能 ID 覆盖）。为 null 时抛异常，避免掩盖配置缺失的问题。
        /// </summary>
        public static void Register(SkillFlowConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            Id2Configs[config.SkillId] = config;
        }

        /// <summary>
        /// 按技能 ID 注销。
        /// </summary>
        public static void Unregister(int skillId)
        {
            Id2Configs.Remove(skillId);
        }

        /// <summary>
        /// 尝试获取流程配置。
        /// </summary>
        public static bool TryGet(int skillId, out SkillFlowConfig config)
        {
            return Id2Configs.TryGetValue(skillId, out config);
        }

        /// <summary>
        /// 清空全部注册（测试 / 热更重载时使用）。
        /// </summary>
        public static void Clear()
        {
            Id2Configs.Clear();
        }

        /// <summary>
        /// 当前注册数量。
        /// </summary>
        public static int Count
        {
            get { return Id2Configs.Count; }
        }
    }
}
