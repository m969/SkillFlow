using System;

namespace NetGame.Combat
{
    [Serializable]
    public class TriggerConfig
    {
        /// <summary>
        /// 稳定触发类型 ID（<see cref="TriggerTypeIds"/> 或扩展自有 ID）。
        /// </summary>
        public string TriggerTypeId = string.Empty;

        public bool Enabled;

        /// <summary>
        /// TriggerTypeId 对应处理器维护的不透明负载结构版本。
        /// 0 表示配置不包含负载。
        /// </summary>
        public int PayloadVersion;

        /// <summary>
        /// 由扩展自行定义的不透明 JSON。SkillFlow 核心不会解析或解释其内容。
        /// </summary>
        public string PayloadJson = string.Empty;
    }
}
