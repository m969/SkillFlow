# JavaScript API 参考

所有 API 均可从包入口 `@m969/game-skillflow` 导入，源码模块为 ES modules。

## 技能流程

- `SkillFlowConfig` 和 `SkillFlowNodeConfig` 保存可序列化的技能图；字段名与 C# 版本一致。
- `SkillFlowNodes` 提供 `Sequence`、`Selector`、`ParallelAll`、`ParallelAny`、
  `RaceSuccess`、`RaceFailure`、`If`、`Switch`、`Loop`、`ForEach`、`Retry`、
  `Delay`、`WaitEvent`、`Timeout`、`CancelScope`、`Action` 和 `AbilityEffect`。
- `SkillExecuteComponent`、`SkillFlowContext` 与 `NodeFrame` 保存一次运行的配置、
  黑板和状态。每个活动的技能实例都应使用独立对象。
- `SkillFlowInterpreter.Enter` 启动流程；结果为 `NodeStatus.Running` 时，在后续更新中
  用 `Tick(execution, deltaSeconds)` 推进。完成或取消后调用 `Exit` 清理订阅；
  可主动调用 `Cancel`。时间步长由调用方提供，不使用系统时钟。
- 内部计时器以 32.32 定点数累计；调用 `Tick` 时传入以秒为单位的有限数字。
- `SkillFlowContext.Variables` 是 `Map`，`SkillFlowContextSystem` 提供
  `SetVariable` / `GetVariable` 读写帮助方法；读取不存在的键时可返回默认值。
- `ActionHost` 实现 `Execute(context, actionKey, frame)`；`EffectHost` 实现
  `ExecuteEffect(context, effect, frame)`。两者均返回 `NodeResult`。
- 可选的 `EventSource.Subscribe(eventKey, callback)` 返回取消函数，或包含 `dispose()`
  / `Dispose()` 方法的句柄。黑板中同名布尔值仍可作为无事件源时的等待信号。
- 节点进入、退出事件通过上下文中的 `OnNodeEnter` 和 `OnNodeExit` 回调通知。

## 效果与战斗扩展

- `Effect`、`AbilityConfigData`、`TriggerConfig` 和 `TriggerContext` 是可扩展数据模型。
- `AbilitySkillLib` 按配置 `Id` 保存技能效果列表；`SkillFlowNodes.AbilityEffect(effect)`
  可直接执行效果，省略参数时按当前上下文 `SkillId` 执行已注册效果。
- `EffectHandlerRegistry` 为 `EffectTypeId` 注册处理器对象，处理器须实现
  `Execute(context, effect, frame)`。未注册 ID 明确返回 `Failure`。
- `TriggerTypeIds` 暴露框架内置稳定 ID。`TriggerHandlerRegistry.Register(handler, replace)`
  要求处理器提供 `TriggerTypeId` 与 `CreateRuntime(context)`；重复注册默认抛错，
  可通过 `replace: true` 显式替换。`TryCreateRuntime` 返回
  `{ Success, Runtime, Error }`。
- `Ability`、`AbilityEffect`、`AbilityTrigger` 和 `AbilityExecution` 是轻量数据模型。
  实体关系、触发运行时生命周期及具体触发器由接入方管理。

## 状态与错误处理

`NodeStatus` 包含 `Running`、`Success`、`Failure`、`Cancelled`、`Interrupted` 和
`Timeout`。`NodeResult` 保存状态及可选诊断原因。缺少动作宿主或效果处理器时不作
成功形状的回退；错误配置通过参数校验抛出异常。
