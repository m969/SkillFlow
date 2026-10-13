# API 参考

## `NetGame.SkillFlow`

- `SkillFlowConfig` 和 `SkillFlowNodeConfig` 用于描述可序列化的节点图。
- `SkillFlowNodes` 用于构造 Sequence、Selector、Parallel、Race、If、Switch、
  Loop、ForEach、Retry、Delay、WaitEvent、Timeout、CancelScope、SkillAction 和
  AbilityEffect 节点。
- `SkillExecuteComponent` 保存一次执行所需的配置、上下文、宿主和节点帧。
  每个运行中的技能执行都应创建一个实例。
- `SkillFlowContext` 保存执行级变量、通用 EcsNode 实体引用、可选事件源和
  可选效果宿主。
- `ISkillActionHost` 将稳定的 action key 映射到应用层行为。
- `ISkillEventSource` 提供 WaitEvent 使用的事件订阅。
- `SkillFlowInterpreter.Enter` 用于启动节点图。当其返回 `Running` 时，使用
  确定性的 `TrueSync.FP` 时间增量调用 `Tick`。执行结束或被取消时调用 `Exit`，
  以释放尚未结束的订阅。

## `NetGame.Combat`

- `Effect` 是一种可扩展的数据负载，通过 `EffectTypeId` 标识。
- `IAbilityEffectHost` 和 `IAbilityEffectHandler` 将效果派发与游戏特定的效果行为
  分离。`EffectHandlerRegistry` 显式地将 ID 映射到处理器；遇到未注册的 ID 时，
  会返回诊断结果 `Failure`。
- `AbilityConfigData` 和 `AbilitySkillLib` 提供内存中的配置注册表，用于保存已配置
  的效果列表。
- `Ability` 和 `AbilityConfigData` 使用由调用方管理的 `AbilityTypeId` 和
  `SkillSpellTypeId` 字符串，而不是业务枚举。
- `TriggerConfig` 包含 `Enabled`、稳定的 `TriggerTypeId`，以及不透明的
  `PayloadJson`；负载版本 `PayloadVersion` 由处理器管理。核心逻辑不会解析该负载。
- `AbilityExecution`、`AbilityEffect` 和 `AbilityTrigger` 仍是数据模型。
- 本仓库不提供旧版战斗枚举 DTO 或迁移适配器。扩展 ID、负载结构，以及所需的旧版
  数据转换均由使用方负责。
- 本项目不包含 MultiBattle 触发系统，也不提供具体的触发器或效果处理器。
- `ITriggerHandler`、`ITriggerRuntime` 和 `TriggerHandlerRegistry` 定义了显式的扩展
  注册契约；运行时行为由使用方提供。

## 运行时行为

解释器通过静态方法运行，执行状态保存在各自的 `SkillExecuteComponent` /
`NodeFrame` 中。时间由调用方注入；本库不会读取系统时钟。效果和动作仅通过使用方
提供的宿主执行。

可运行示例见 [`samples/Game.SkillFlow.QuickStart`](../../samples/Game.SkillFlow.QuickStart/)；
构建命令见 [`README.md`](./README.md)。

## JavaScript 版本

仓库同时提供 [Game.SkillFlow JavaScript](../Game.SkillFlow.JavaScript/README.md)，包含技能流程解释器、效果注册和触发器扩展契约。JavaScript 版本要求支持 BigInt 的现代浏览器或 Node.js 20+，API 与运行时差异见 [JavaScript API 参考](../Game.SkillFlow.JavaScript/API-REFERENCE.md)。
