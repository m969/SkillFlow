using System;

namespace NetGame.Combat
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class EffectAttribute : Attribute
    {
        readonly string effectType;
        readonly int order;

        public EffectAttribute(string effectType, int order)
        {
            this.effectType = effectType;
            this.order = order;
        }

        public string EffectType
        {
            get { return effectType; }
        }

        public int Order
        {
            get { return order; }
        }
    }

    [Serializable]
    public abstract partial class Effect
    {
        /// <summary>
        /// 稳定效果标识，由接入方用于关联对应的效果处理器。
        /// 框架保留该字段以支持可序列化的效果扩展载荷。
        /// </summary>
        public string EffectTypeId = string.Empty;

        public bool IsSkillEffect;

        public bool HideTriggerType
        {
            get
            {
                return false;
            }
        }

        public virtual string Label => "Effect";

        public bool Enabled;

    }
}
