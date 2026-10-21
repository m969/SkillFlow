# Game.SkillFlow

Game.SkillFlow 是一个面向游戏技能与战斗流程的轻量级 C# 库。它将技能流程描述为可序列化的数据节点图，并由无状态解释器推进；游戏相关的动作和效果由接入方实现并注入，核心库不依赖具体游戏业务。

## 功能

- **数据驱动的技能流程**：使用 `SkillFlowConfig`、`SkillFlowNodeConfig` 和 `SkillFlowNodes` 描述技能节点图。
- **组合与流程控制**：提供 Sequence、Selector、Parallel、Race、If、Switch、Loop、ForEach、Retry、Delay、WaitEvent、Timeout 和 CancelScope 节点。
- **接入方自定义行为**：通过 `ISkillActionHost` 执行原子动作；通过 `IAbilityEffectHost` 分派战斗效果。
- **每次执行独立状态**：`SkillExecuteComponent` 和 `SkillFlowContext` 保存单次执行的运行状态与黑板数据。
- **外部时间驱动**：解释器不读取系统时钟；调用方按秒传入 `float` 时间增量。
- **效果与触发器扩展契约**：提供基于稳定字符串 ID 的配置和处理器注册接口，不绑定具体战斗规则。

## 环境要求

- .NET 10 SDK

## 构建

在本目录执行：

```sh
dotnet build Game.SkillFlow.csproj
```

在其他项目中使用时，将 `Game.SkillFlow.csproj` 添加为项目引用；项目目标框架为 `net10.0`。

## 快速开始

下面的示例创建一个“等待一小段时间，然后执行动作”的流程。实际游戏中，应在游戏更新循环中持续调用 `Tick`，而不是在一次调用中循环推进。

```csharp
using NetGame.SkillFlow;

public sealed class GameActionHost : ISkillActionHost
{
    public NodeResult Execute(SkillFlowContext context, string actionKey, NodeFrame frame)
    {
        if (actionKey == "launch-projectile")
        {
            // 在这里调用游戏项目自己的发射逻辑。
            return NodeResult.Success();
        }

        return NodeResult.Failure("Unknown action: " + actionKey);
    }
}

public static class SkillExample
{
    public static SkillExecuteComponent CreateExecution()
    {
        var config = new SkillFlowConfig
        {
            SkillId = 1001,
            Name = "Projectile",
            Root = SkillFlowNodes.Sequence(
                SkillFlowNodes.Delay(0.25f),
                SkillFlowNodes.Action("launch-projectile"))
        };

        return new SkillExecuteComponent
        {
            Config = config,
            Context = new SkillFlowContext { SkillId = config.SkillId },
            ActionHost = new GameActionHost()
        };
    }

    public static NodeResult Start(SkillExecuteComponent execution)
    {
        return SkillFlowInterpreter.Enter(execution);
    }

    public static NodeResult Update(SkillExecuteComponent execution, float delta)
    {
        return SkillFlowInterpreter.Tick(execution, delta);
    }

    public static void Finish(SkillExecuteComponent execution, NodeResult result)
    {
        SkillFlowInterpreter.Exit(execution, result);
    }
}
```

接入时的基本生命周期如下：

1. 创建技能配置、上下文和执行宿主，并组装 `SkillExecuteComponent`。
2. 调用 `SkillFlowInterpreter.Enter` 启动流程。
3. 若结果为 `NodeStatus.Running`，在后续游戏更新中传入以秒为单位的 `float` 时间增量调用 `Tick`。
4. 流程结束或被取消后调用 `Exit`，清理尚未释放的事件订阅。需要主动取消时调用 `Cancel`。

`SkillFlowContext.Variables` 是执行级黑板，可用于在节点之间传递接入方定义的数据。接入战斗实体、事件源或效果宿主时，也应通过上下文注入对应对象；相关对象的生命周期由接入方管理。

## 战斗效果与配置

`AbilityEffect` 节点可直接携带 `Effect` 数据，也可按 `SkillFlowContext.SkillId` 从 `AbilitySkillLib` 查询已注册的 `AbilityConfigData` 效果列表。使用 `EffectHandlerRegistry` 将稳定的 `EffectTypeId` 显式映射到业务处理器；未注册的效果类型会返回 `Failure`，不会被静默忽略。

`TriggerConfig` 保存稳定的触发器 ID 和不透明负载。触发器负载的结构、版本管理及其运行时处理由接入方负责；本库不包含具体的游戏效果、触发器或旧版数据迁移适配器。

## 文档

- [API 参考](./API-REFERENCE.md)：核心类型、扩展接口和运行时行为说明。
- [JavaScript 版本](./Game.SkillFlow.JavaScript/README.md)：Node.js 与浏览器可用的 ES module 实现及运行说明。