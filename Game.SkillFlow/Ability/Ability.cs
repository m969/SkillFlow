using ECS;
using System.Collections.Generic;

namespace NetGame.Combat
{
    public interface IAbilityConfig
    {
        int GetId();
        string GetKeyName();
        string GetName();
        string GetTypeName();
        string GetTargetGroup();
        string GetTargetSelect();
    }

    public class Ability : EcsEntity
    {
        public EcsEntity OwnerEntity { get { return GetParent<EcsEntity>(); } set { } }
        public EcsEntity ParentEntity { get => Parent; }
        public int ConfigId { get; set; }
        public string AbilityTypeId { get; set; } = string.Empty;
        public AbilityConfigData ConfigObject { get; set; }
        public string Name { get; set; }
        public List<AbilityEffect> AbilityEffects { get; private set; } = new List<AbilityEffect>();
        public List<AbilityTrigger> AbilityTriggers { get; private set; } = new List<AbilityTrigger>();
    }
}
