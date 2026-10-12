using System.Collections.Generic;

namespace NetGame.Combat
{
    /// <summary>
    /// 技能/能力配置数据，由接入方加载并注册，供运行时按技能 ID 查询。
    /// </summary>
    public partial class AbilityConfigData
    {
        public int Id;
        public string ShowName;
        public string SkillSpellTypeId = string.Empty;
        public List<Effect> Effects = new List<Effect>();
        public List<TriggerConfig> TriggerActions = new List<TriggerConfig>();
    }
}
