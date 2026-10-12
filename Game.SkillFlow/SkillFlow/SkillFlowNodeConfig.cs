using System;
using System.Collections.Generic;
using NetGame.Combat;

namespace NetGame.SkillFlow
{
    /// <summary>
    /// 节点图配置：一个节点 = NodeType + 参数 + 子节点列表。
    /// 纯数据、可序列化、双端共享；运行时由 <c>SkillFlowInterpreter</c> 解释推进，节点本身无行为无状态。
    /// </summary>
    [Serializable]
    public class SkillFlowNodeConfig
    {
        /// <summary>节点类型。</summary>
        public SkillFlowNodeType NodeType;

        /// <summary>子节点列表（控制节点使用；原子节点为空）。</summary>
        public List<SkillFlowNodeConfig> Children = new List<SkillFlowNodeConfig>();

        /// <summary>通用浮点参数（Delay/Timeout 时长、If 阈值等）。</summary>
        public float FloatParam;

        /// <summary>通用整型参数（Loop 次数、目标数量等）。</summary>
        public int IntParam;

        /// <summary>通用字符串参数（条件表达式、黑板键名、动作键名等）。</summary>
        public string StringParam = "";

        /// <summary>通用字符串参数 2（输出键名等）。</summary>
        public string StringParam2 = "";

        /// <summary>战斗效果载荷（<see cref="SkillFlowNodeType.AbilityEffect"/> 节点使用；纯数据、可序列化）。</summary>
        public Effect Effect;

        /// <summary>通用布尔参数（如 Parallel 完成策略开关）。</summary>
        public bool BoolParam;
    }
}
