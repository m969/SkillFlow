using System;
using System.Collections.Generic;
using ECS;
using NetGame.Combat;
using TrueSync;

namespace NetGame.SkillFlow
{
    /// <summary>
    /// 技能编排运行时上下文：每次施法独立实例，节点经黑板（Variables）传递数据，杜绝多角色同技能状态污染。
    /// 双端共用（服务端 / 客户端均基于 EcsNode + TrueSync）；时间用定点 <c>CastStartTick</c>，不存墙钟。
    /// </summary>
    public class SkillFlowContext
    {
        /// <summary>本次施放唯一 ID。</summary>
        public long ExecutionId;

        /// <summary>技能定义 ID。</summary>
        public int SkillId;

        /// <summary>施法者（双端均基于 EcsEntity）。</summary>
        public EcsEntity Caster = null!;

        /// <summary>初始目标（可为空，转场时按需赋值）。</summary>
        public EcsEntity PrimaryTarget = null!;

        /// <summary>执行载体实体（服务端 / 客户端各自传入），供节点事件经其 <c>Dispatch</c> 广播到表现层。</summary>
        public EcsEntity ExecutionEntity = null!;

        /// <summary>初始目标点。</summary>
        public TSVector TargetPosition;

        /// <summary>施法朝向。</summary>
        public TSVector InputDirection;

        /// <summary>施法起始 tick（定点，确定性时间基准）。</summary>
        public FP CastStartTick;

        /// <summary>黑板：节点间传递接入方定义的数据。</summary>
        public Dictionary<string, object> Variables = new Dictionary<string, object>();

        /// <summary>战斗效果执行宿主（<see cref="SkillFlowNodeType.AbilityEffect"/> 节点使用；服务端 / 客户端各自注入，可空）。</summary>
        public IAbilityEffectHost EffectHost;

        /// <summary>
        /// 外部事件源（<see cref="SkillFlowNodeType.WaitEvent"/> 节点使用，可空）。
        /// 未注入时继续使用 Variables 黑板轮询，保证旧 headless 调用方兼容。
        /// </summary>
        public ISkillEventSource EventSource;

        /// <summary>执行期间创建的实体，由宿主用于结束时的资源回收判断。</summary>
        public List<EcsEntity> SpawnedEntities = new List<EcsEntity>();

        /// <summary>事件订阅句柄（WaitEventNode 使用，Exit/Cancel 时释放）。</summary>
        public List<IDisposable> Subscriptions = new List<IDisposable>();
    }
}
