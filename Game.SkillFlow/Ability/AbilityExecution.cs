using System.Collections.Generic;
using ECS;

namespace NetGame.Combat
{
    /// <summary>
    /// 技能执行体，执行体就是控制角色表现和技能表现的，包括角色动作、移动、变身等表现的，以及技能生成碰撞体等表现
    /// </summary>
    public sealed partial class AbilityExecution : EcsEntity
    {
        public string Name { get; set; }
        public Ability AbilityEntity { get; set; }
        public EcsEntity OwnerEntity { get; set; }
        public List<EcsEntity> SkillTargets { get; set; } = new List<EcsEntity>();
        public EcsEntity InputTarget { get; set; }
        public TrueSync.TSVector InputPoint { get; set; }
        public TrueSync.TSVector InputDirection { get; set; }
        public float InputRadian { get; set; }
        /// 行为占用
        public bool ActionOccupy { get; set; } = true;
    }
}
