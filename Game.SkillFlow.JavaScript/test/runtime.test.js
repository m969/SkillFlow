import test from "node:test";
import assert from "node:assert/strict";
import {
  AbilityConfigData,
  AbilitySkillLib,
  Effect,
  EffectHandlerRegistry,
  NodeResult,
  NodeStatus,
  SkillExecuteComponent,
  SkillFlowConfig,
  SkillFlowContext,
  SkillFlowInterpreter,
  SkillFlowNodes,
  TriggerHandlerRegistry,
  TriggerTypeIds
} from "../src/index.js";

function execution(root, { context = new SkillFlowContext(), host } = {}) {
  return new SkillExecuteComponent({
    Config: new SkillFlowConfig({ SkillId: 17, Root: root }),
    Context: context,
    ActionHost: host ?? { Execute: () => NodeResult.Success() }
  });
}

test("Sequence 按浮点时间累计并在延迟后执行动作", () => {
  const actions = [];
  const run = execution(
    SkillFlowNodes.Sequence(
      SkillFlowNodes.Delay(0.25),
      SkillFlowNodes.Action("launch")
    ),
    {
      host: {
        Execute(_context, key) {
          actions.push(key);
          return NodeResult.Success();
        }
      }
    }
  );

  assert.equal(SkillFlowInterpreter.Enter(run).Status, NodeStatus.Running);
  assert.equal(SkillFlowInterpreter.Tick(run, 0.125).Status, NodeStatus.Running);
  assert.equal(SkillFlowInterpreter.Tick(run, 0.125).Status, NodeStatus.Success);
  assert.deepEqual(actions, ["launch"]);
  assert.equal(run.IsFinished, true);
});

test("If、Switch、Loop、ForEach 和 Retry 保持预期分支及重试次数", () => {
  const calls = [];
  const context = new SkillFlowContext({
    Variables: new Map([
      ["enabled", true],
      ["route", 2],
      ["items", ["a", "b"]]
    ])
  });
  const run = execution(
    SkillFlowNodes.Sequence(
      SkillFlowNodes.If("enabled", SkillFlowNodes.Action("if-true")),
      SkillFlowNodes.Switch("route", SkillFlowNodes.Action("default"), [
        { Value: 1, Branch: SkillFlowNodes.Action("one") },
        { Value: 2, Branch: SkillFlowNodes.Action("two") }
      ]),
      SkillFlowNodes.Loop(2, SkillFlowNodes.Action("loop")),
      SkillFlowNodes.ForEach("items", SkillFlowNodes.Action("foreach")),
      SkillFlowNodes.Retry(2, SkillFlowNodes.Action("retry"))
    ),
    {
      context,
      host: {
        Execute(_context, key) {
          calls.push(key);
          if (key === "retry" && calls.filter((item) => item === "retry").length < 3) {
            return NodeResult.Failure("try again");
          }
          return NodeResult.Success();
        }
      }
    }
  );

  assert.equal(SkillFlowInterpreter.Enter(run).Status, NodeStatus.Success);
  assert.deepEqual(calls, [
    "if-true",
    "two",
    "loop",
    "loop",
    "foreach",
    "foreach",
    "retry",
    "retry",
    "retry"
  ]);
});

test("ParallelAny 取消仍运行的分支", () => {
  const run = execution(
    SkillFlowNodes.ParallelAny(
      SkillFlowNodes.Delay(10),
      SkillFlowNodes.Action("complete")
    )
  );

  assert.equal(SkillFlowInterpreter.Enter(run).Status, NodeStatus.Success);
  assert.equal(run.RootFrame.ChildFrames[0].Status, NodeStatus.Cancelled);
});

test("Selector、ParallelAll 和 Race 按各自完成策略推进", () => {
  const calls = [];
  const host = {
    Execute(_context, key) {
      calls.push(key);
      return key === "fail" ? NodeResult.Failure() : NodeResult.Success();
    }
  };
  const selected = execution(
    SkillFlowNodes.Selector(
      SkillFlowNodes.Action("fail"),
      SkillFlowNodes.Action("selected")
    ),
    { host }
  );
  assert.equal(SkillFlowInterpreter.Enter(selected).Status, NodeStatus.Success);
  assert.deepEqual(calls, ["fail", "selected"]);

  const parallel = execution(
    SkillFlowNodes.ParallelAll(
      SkillFlowNodes.Delay(0.125),
      SkillFlowNodes.Action("parallel")
    )
  );
  assert.equal(SkillFlowInterpreter.Enter(parallel).Status, NodeStatus.Running);
  assert.equal(SkillFlowInterpreter.Tick(parallel, 0.125).Status, NodeStatus.Success);

  const successRace = execution(
    SkillFlowNodes.RaceSuccess(
      SkillFlowNodes.Delay(10),
      SkillFlowNodes.Action("winner")
    )
  );
  assert.equal(SkillFlowInterpreter.Enter(successRace).Status, NodeStatus.Success);
  assert.equal(successRace.RootFrame.ChildFrames[0].Status, NodeStatus.Cancelled);

  const failureRace = execution(
    SkillFlowNodes.RaceFailure(
      SkillFlowNodes.Action("fail"),
      SkillFlowNodes.Delay(10)
    ),
    { host }
  );
  assert.equal(SkillFlowInterpreter.Enter(failureRace).Status, NodeStatus.Failure);
  assert.equal(failureRace.RootFrame.ChildFrames[1].Status, NodeStatus.Cancelled);
});

test("Timeout 取消超时的子节点", () => {
  const run = execution(
    SkillFlowNodes.Timeout(0.125, SkillFlowNodes.Delay(10))
  );

  assert.equal(SkillFlowInterpreter.Enter(run).Status, NodeStatus.Running);
  assert.equal(SkillFlowInterpreter.Tick(run, 0.125).Status, NodeStatus.Timeout);
  assert.equal(run.RootFrame.ChildFrames[0].Status, NodeStatus.Cancelled);
});

test("WaitEvent 捕获目标并释放订阅", () => {
  let publish;
  let unsubscribeCount = 0;
  const target = { id: "target-1" };
  const context = new SkillFlowContext({
    EventSource: {
      LastTargetEntity: target,
      Subscribe(key, callback) {
        assert.equal(key, "hit");
        publish = callback;
        return () => unsubscribeCount++;
      }
    }
  });
  const run = execution(SkillFlowNodes.WaitEvent("hit"), { context });

  assert.equal(SkillFlowInterpreter.Enter(run).Status, NodeStatus.Running);
  publish();
  assert.equal(SkillFlowInterpreter.Tick(run, 0).Status, NodeStatus.Success);
  assert.equal(context.PrimaryTarget, target);
  assert.equal(context.Variables.get("hitTarget"), target);
  assert.equal(unsubscribeCount, 1);
});

test("CancelScope 内的脱手子树在外层取消后仍可推进", () => {
  const run = execution(
    SkillFlowNodes.Sequence(
      SkillFlowNodes.Action("start"),
      SkillFlowNodes.CancelScope(SkillFlowNodes.Delay(0.5))
    )
  );

  assert.equal(SkillFlowInterpreter.Enter(run).Status, NodeStatus.Running);
  SkillFlowInterpreter.Cancel(run);
  assert.equal(run.IsFinished, false);
  assert.equal(SkillFlowInterpreter.Tick(run, 0.5).Status, NodeStatus.Success);
  assert.equal(run.IsFinished, true);
  assert.equal(run.RootFrame.ChildFrames[1].ChildFrames[0].Status, NodeStatus.Success);
});

test("AbilityEffect 通过注册表执行配置中的启用效果", () => {
  AbilitySkillLib.Clear();
  const seen = [];
  const effects = [
    new Effect({ EffectTypeId: "damage", Enabled: true, Amount: 12 }),
    new Effect({ EffectTypeId: "heal", Enabled: false })
  ];
  AbilitySkillLib.Register(new AbilityConfigData({ Id: 17, Effects: effects }));
  const effectHost = new EffectHandlerRegistry();
  effectHost.Register("damage", {
    Execute(_context, effect) {
      seen.push(effect.Amount);
      return NodeResult.Success();
    }
  });
  const context = new SkillFlowContext({ SkillId: 17, EffectHost: effectHost });
  const run = execution(SkillFlowNodes.AbilityEffect(), { context });

  assert.equal(SkillFlowInterpreter.Enter(run).Status, NodeStatus.Success);
  assert.deepEqual(seen, [12]);
  AbilitySkillLib.Clear();
});

test("未注册效果类型显式返回 Failure", () => {
  const registry = new EffectHandlerRegistry();
  const result = registry.ExecuteEffect(
    new SkillFlowContext(),
    new Effect({ EffectTypeId: "missing" }),
    {}
  );

  assert.equal(result.Status, NodeStatus.Failure);
  assert.match(result.Reason, /not registered/);
});

test("TriggerHandlerRegistry 拒绝重复 ID 并可显式替换", () => {
  const registry = new TriggerHandlerRegistry();
  const handler = {
    TriggerTypeId: "example.trigger",
    CreateRuntime: (context) => ({ context })
  };
  registry.Register(handler);
  assert.throws(() => registry.Register(handler), /already registered/);
  registry.Register(handler, true);
  assert.equal(registry.Count, 1);
  assert.equal(TriggerTypeIds.IsBuiltIn(TriggerTypeIds.Instant), true);
});
