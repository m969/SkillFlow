using System;

namespace NetGame.SkillFlow
{
    /// <summary>
    /// 技能流程配置：SkillId + 根节点图。纯数据、可序列化。
    /// 由 <c>SkillFlowLib</c> 按技能 ID 注册/查询；<c>SkillFlowFactory</c> 构建运行时状态树。
    /// </summary>
    [Serializable]
    public class SkillFlowConfig
    {
        /// <summary>关联技能 ID。</summary>
        public int SkillId;

        /// <summary>技能名称（调试 / 日志用）。</summary>
        public string Name = "";

        /// <summary>根节点配置（Sequence / Parallel 等）。</summary>
        public SkillFlowNodeConfig Root = null!;
    }
}
