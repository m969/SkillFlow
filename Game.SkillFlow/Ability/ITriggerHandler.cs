using TrueSync;

namespace NetGame.Combat
{
    /// <summary>
    /// Trigger 扩展处理器契约：按稳定 <see cref="TriggerTypeIds"/> 注册，
    /// 负责为该类型的每个 <see cref="AbilityTrigger"/> 创建运行时。
    /// <para>
    /// 处理器本身必须**无状态**；逐实体可变状态（计时器、订阅句柄等）必须落在
    /// <see cref="CreateRuntime"/> 返回的 <see cref="ITriggerRuntime"/> 实例上，
    /// 避免多个 Ability / World / 测试互相污染。
    /// </para>
    /// </summary>
    public interface ITriggerHandler
    {
        /// <summary>
        /// 该处理器负责的稳定 Trigger 类型 ID。
        /// 不依赖 CLR 类型名或枚举序号；一经发布不得更改或复用。
        /// </summary>
        string TriggerTypeId { get; }

        /// <summary>
        /// 为单个 <see cref="AbilityTrigger"/> 创建运行时实例。
        /// <para>
        /// 返回实例必须是 <c>EcsEntity + Component</c> 形态（组件只存数据、System 只含方法），
        /// 不得是裸对象或跨实体的共享实例。
        /// </para>
        /// </summary>
        ITriggerRuntime CreateRuntime(TriggerCreateContext context);
    }

    /// <summary>
    /// Trigger 运行时契约：每个 <see cref="AbilityTrigger"/> 一个实例，至少响应启动 / 停止。
    /// 实现应为 <c>EcsEntity</c> 派生类型，其可变状态落在挂载的 Component 上。
    /// <para>方法名用 Start / Stop 而非 Enable / Disable，避免与 <c>EcsEntity.Enable</c> 属性撞名。</para>
    /// </summary>
    public interface ITriggerRuntime
    {
        /// <summary>启动：订阅事件源 / 启动计时 / 立即提交触发等。</summary>
        void Start();

        /// <summary>停止：释放事件源 / 停止计时 / 清理逐实体状态。</summary>
        void Stop();
    }

    /// <summary>需要逐帧推进的 Trigger 运行时额外实现此接口。</summary>
    public interface ITriggerUpdateRuntime : ITriggerRuntime
    {
        /// <summary>按定点步长推进（1 / tickrate 秒），禁止使用浮点时间。</summary>
        void Update(FP delta);
    }

    /// <summary>
    /// 创建单个 Trigger 运行时的上下文：公共触发宿主 + 宿主实体 + 触发配置。
    /// </summary>
    public readonly struct TriggerCreateContext
    {
        /// <summary>公共触发宿主（负责目标回退、状态条件与效果派发）。</summary>
        public readonly ITriggerHost Host;

        /// <summary>宿主 AbilityTrigger 实体。</summary>
        public readonly AbilityTrigger Trigger;

        /// <summary>触发配置（含稳定触发类型 ID 与扩展载荷）。</summary>
        public readonly TriggerConfig Config;

        /// <summary>构造创建上下文。</summary>
        public TriggerCreateContext(ITriggerHost host, AbilityTrigger trigger, TriggerConfig config)
        {
            Host = host;
            Trigger = trigger;
            Config = config;
        }
    }

    /// <summary>
    /// 公共触发宿主：处理器 / 运行时只负责「何时触发」与上下文来源；
    /// 宿主负责目标回退、状态条件检查与效果派发，避免扩展复制派发逻辑。
    /// </summary>
    public interface ITriggerHost
    {
        /// <summary>
        /// 提交一次触发。
        /// <para>
        /// <see cref="TriggerContext"/> 是值类型（struct）：处理器可在提交前补充
        /// <see cref="TriggerContext.Target"/> / <see cref="TriggerContext.TriggerSource"/>，
        /// 宿主按 <c>ref</c> 接收并回填 <see cref="TriggerContext.AbilityTrigger"/>。
        /// </para>
        /// </summary>
        void Submit(AbilityTrigger trigger, ref TriggerContext context);
    }
}
