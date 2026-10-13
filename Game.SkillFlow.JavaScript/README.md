# Game.SkillFlow JavaScript

Game.SkillFlow JavaScript 是与 C# 库配套的 ES module 实现，可在 Node.js 和支持 BigInt 的现代浏览器中运行。它包含完整的技能流程节点解释器、效果处理器注册表、技能配置注册表和触发器扩展契约；不依赖 npm 第三方包，也不绑定具体游戏或 ECS。

## 环境要求

- Node.js 20 或更高版本
- 浏览器需支持 ES modules 和 BigInt（ES2020）

运行时不读取系统时钟。`Tick` 接收调用方提供的秒数，并使用有符号 32.32 定点数累计技能节点时间。流程配置中的时长按 C# `float` 精度存储，以便和 C# 配置保持一致。

## 快速开始

```js
import {
  NodeResult,
  NodeStatus,
  SkillExecuteComponent,
  SkillFlowConfig,
  SkillFlowContext,
  SkillFlowInterpreter,
  SkillFlowNodes
} from "@m969/game-skillflow";

const actionHost = {
  Execute(context, actionKey, frame) {
    if (actionKey === "launch-projectile") {
      // 在这里调用游戏项目自己的发射逻辑。
      return NodeResult.Success();
    }
    return NodeResult.Failure(`Unknown action: ${actionKey}`);
  }
};

const config = new SkillFlowConfig({
  SkillId: 1001,
  Name: "Projectile",
  Root: SkillFlowNodes.Sequence(
    SkillFlowNodes.Delay(0.25),
    SkillFlowNodes.Action("launch-projectile")
  )
});

const execution = new SkillExecuteComponent({
  Config: config,
  Context: new SkillFlowContext({ SkillId: config.SkillId }),
  ActionHost: actionHost
});

let result = SkillFlowInterpreter.Enter(execution);
if (result.Status === NodeStatus.Running) {
  // 在游戏自己的确定性更新循环中持续传入秒数增量。
  result = SkillFlowInterpreter.Tick(execution, 1 / 60);
}
if (result.Status !== NodeStatus.Running) {
  SkillFlowInterpreter.Exit(execution, result);
}
```

实际游戏应在后续更新中持续调用 `Tick`，直到流程完成；主动中止时调用 `Cancel`。结束后调用 `Exit` 释放事件订阅。不要在单次调用中循环推进一个等待中的流程。

## 战斗效果与事件

`EffectHandlerRegistry` 负责把稳定的 `EffectTypeId` 映射到接入方处理器。处理器使用 `Execute(context, effect, frame)` 返回 `NodeResult`。配置驱动的效果通过 `AbilitySkillLib.Register` 注册 `AbilityConfigData`，并由 `SkillFlowNodes.AbilityEffect()` 按 `SkillId` 查询。

`TriggerHandlerRegistry` 将稳定的 `TriggerTypeId` 映射到实现 `CreateRuntime(context)` 的对象。运行时的 `Start`、`Stop` 和可选 `Update(delta)` 方法由接入方调用；本包定义扩展契约，不提供游戏特定触发器。

事件源实现 `Subscribe(eventKey, callback)` 并返回取消订阅函数；也支持返回带 `dispose()` 或 `Dispose()` 方法的句柄。目标载荷可通过 `EventSource.LastTargetEntity` 提供。节点表现事件使用上下文的 `OnNodeEnter(frame, context)` 和 `OnNodeExit(frame, context, result)` 回调。

## 与 C# 版本的边界

- 流程、效果配置和节点参数采用与 C# 版本相同的 PascalCase 字段名，配置对象可用 JSON 交换。
- JS 中的游戏实体、向量和 ECS 对象是接入方拥有的不透明对象；ECS 生命周期、系统调度、向量数学及业务效果实现不属于此包。
- C# 接口在 JavaScript 中表现为明确的方法约定；缺少处理器或效果 ID 时会返回诊断 `Failure` 或抛出错误，不会静默回退。
- Trigger 处理器的运行时实例和各次技能执行状态均由各自调用方创建，不应跨执行共享。

## 测试

在本目录执行：

```sh
npm test
```

更多类型和运行时语义见 [API 参考](./API-REFERENCE.md)。
