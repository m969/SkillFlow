using ECS;

namespace NetGame.SkillFlow
{
    /// <summary>
    /// 执行状态组件：由宿主持有，保存一次流程执行的配置、上下文和运行时状态。
    /// 是 <c>SkillFlowInterpreter</c> 唯一操作对象（符合「只 Get 自己组件」规范）。
    /// 纯数据容器。
    /// </summary>
    public class SkillExecuteComponent : EcsComponent
    {
        /// <summary>技能流程配置（数据图）。</summary>
        public SkillFlowConfig Config = null!;

        /// <summary>施法上下文（黑板）。</summary>
        public SkillFlowContext Context = null!;

        /// <summary>运行时状态树根（平行于 Config.Root）。</summary>
        public NodeFrame RootFrame = null!;

        /// <summary>原子动作分端宿主（服务端 / 客户端各自注入）。</summary>
        public ISkillActionHost ActionHost = null!;

        /// <summary>是否已启动（Enter 已调用）。</summary>
        public bool IsStarted;

        /// <summary>是否已结束（Success/Failure/Cancelled/Timeout 等非 Running 终态）。</summary>
        public bool IsFinished;
    }
}
