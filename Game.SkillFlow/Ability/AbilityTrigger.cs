using ECS;
using System.Collections.Generic;

namespace NetGame.Combat
{
    /// <summary>
    /// 能力触发器
    /// </summary>
    public class AbilityTrigger : EcsEntity
    {
        public Ability OwnerAbility => GetParent<Ability>();
        public EcsEntity OwnerEntity => OwnerAbility.OwnerEntity;
        public EcsEntity ParentEntity => OwnerAbility.ParentEntity;
        public TriggerConfig TriggerConfig { get; set; }
        public string ConditionParamValue { get; set; }

        /// <summary>由 Trigger 运行时系统按稳定 ID 创建的逐实体运行时实例（未创建时为 null）。</summary>
        public ITriggerRuntime Runtime { get; set; }
    }
}
