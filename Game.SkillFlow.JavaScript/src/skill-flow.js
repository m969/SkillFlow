import { AbilitySkillLib } from "./combat.js";
import { NodeResult, NodeStatus } from "./node-result.js";

/**
 * 技能流程节点类型；值采用稳定字符串，便于保存和跨语言交换。
 */
export const SkillFlowNodeType = Object.freeze({
  Sequence: "Sequence",
  Selector: "Selector",
  Parallel: "Parallel",
  Race: "Race",
  If: "If",
  Switch: "Switch",
  Loop: "Loop",
  ForEach: "ForEach",
  Retry: "Retry",
  Delay: "Delay",
  WaitEvent: "WaitEvent",
  Timeout: "Timeout",
  CancelScope: "CancelScope",
  SkillAction: "SkillAction",
  AbilityEffect: "AbilityEffect"
});

/**
 * 可序列化的节点配置。属性名称与 C# 版本保持一致，便于迁移配置。
 */
export class SkillFlowNodeConfig {
  constructor({
    NodeType,
    Children = [],
    FloatParam = 0,
    IntParam = 0,
    StringParam = "",
    StringParam2 = "",
    Effect = null,
    BoolParam = false
  } = {}) {
    this.NodeType = NodeType;
    this.Children = Children;
    this.FloatParam = Math.fround(FloatParam);
    this.IntParam = IntParam;
    this.StringParam = StringParam;
    this.StringParam2 = StringParam2;
    this.Effect = Effect;
    this.BoolParam = BoolParam;
  }
}

/**
 * 技能流程配置：技能 ID、名称和根节点。
 */
export class SkillFlowConfig {
  constructor({ SkillId = 0, Name = "", Root } = {}) {
    this.SkillId = SkillId;
    this.Name = Name;
    this.Root = Root;
  }
}

function validateKey(key, parameterName) {
  if (typeof key !== "string" || key.trim() === "") {
    throw new TypeError(`${parameterName} must not be empty`);
  }
}

function validateDuration(seconds, parameterName) {
  if (typeof seconds !== "number" || !Number.isFinite(Math.fround(seconds)) || seconds < 0) {
    throw new RangeError(`${parameterName} must be finite and non-negative`);
  }
}

function validateInt32(value, parameterName, minimum = -2147483648) {
  if (!Number.isInteger(value) || value < minimum || value > 2147483647) {
    throw new RangeError(`${parameterName} must be a valid 32-bit integer`);
  }
}

function composite(nodeType, children) {
  if (!Array.isArray(children)) {
    throw new TypeError("children must be an array");
  }
  if (children.some((child) => child == null)) {
    throw new TypeError("child nodes cannot be null");
  }
  return new SkillFlowNodeConfig({ NodeType: nodeType, Children: [...children] });
}

function unary(nodeType, child) {
  if (child == null) {
    throw new TypeError("child node is required");
  }
  return composite(nodeType, [child]);
}

/**
 * 创建可序列化的节点配置，控制节点不持有运行时状态。
 */
export class SkillFlowNodes {
  static Sequence(...children) {
    return composite(SkillFlowNodeType.Sequence, children);
  }

  static Selector(...children) {
    return composite(SkillFlowNodeType.Selector, children);
  }

  static ParallelAll(...children) {
    const node = composite(SkillFlowNodeType.Parallel, children);
    node.BoolParam = false;
    return node;
  }

  static ParallelAny(...children) {
    const node = composite(SkillFlowNodeType.Parallel, children);
    node.BoolParam = true;
    return node;
  }

  static RaceSuccess(...children) {
    const node = composite(SkillFlowNodeType.Race, children);
    node.BoolParam = false;
    return node;
  }

  static RaceFailure(...children) {
    const node = composite(SkillFlowNodeType.Race, children);
    node.BoolParam = true;
    return node;
  }

  static If(conditionKey, whenTrue, whenFalse = undefined) {
    validateKey(conditionKey, "conditionKey");
    if (whenTrue == null) {
      throw new TypeError("whenTrue is required");
    }
    const children = whenFalse === undefined ? [whenTrue] : [whenTrue, whenFalse];
    const node = composite(SkillFlowNodeType.If, children);
    node.StringParam = conditionKey;
    return node;
  }

  static Switch(selectorKey, defaultBranch, cases = []) {
    validateKey(selectorKey, "selectorKey");
    if (defaultBranch == null) {
      throw new TypeError("defaultBranch is required");
    }
    if (!Array.isArray(cases)) {
      throw new TypeError("cases must be an array");
    }
    const seen = new Set();
    const branches = cases.map(({ Value, Branch }) => {
      validateInt32(Value, "case value");
      if (Branch == null) {
        throw new TypeError("Switch case branches cannot be null");
      }
      if (seen.has(Value)) {
        throw new TypeError("Switch case values must be unique");
      }
      seen.add(Value);
      Branch.IntParam = Value;
      return Branch;
    });
    const node = composite(SkillFlowNodeType.Switch, [...branches, defaultBranch]);
    node.StringParam = selectorKey;
    return node;
  }

  static Loop(count, body) {
    validateInt32(count, "count", 0);
    const node = unary(SkillFlowNodeType.Loop, body);
    node.IntParam = count;
    return node;
  }

  static ForEach(collectionKey, body) {
    validateKey(collectionKey, "collectionKey");
    const node = unary(SkillFlowNodeType.ForEach, body);
    node.StringParam = collectionKey;
    return node;
  }

  static Retry(maxRetries, body) {
    validateInt32(maxRetries, "maxRetries", 0);
    const node = unary(SkillFlowNodeType.Retry, body);
    node.IntParam = maxRetries;
    return node;
  }

  static Delay(seconds) {
    validateDuration(seconds, "seconds");
    return new SkillFlowNodeConfig({
      NodeType: SkillFlowNodeType.Delay,
      FloatParam: seconds
    });
  }

  static WaitEvent(eventKey) {
    validateKey(eventKey, "eventKey");
    return new SkillFlowNodeConfig({
      NodeType: SkillFlowNodeType.WaitEvent,
      StringParam: eventKey
    });
  }

  static Timeout(seconds, body) {
    validateDuration(seconds, "seconds");
    const node = unary(SkillFlowNodeType.Timeout, body);
    node.FloatParam = Math.fround(seconds);
    return node;
  }

  static CancelScope(body) {
    return unary(SkillFlowNodeType.CancelScope, body);
  }

  static Action(actionKey) {
    validateKey(actionKey, "actionKey");
    return new SkillFlowNodeConfig({
      NodeType: SkillFlowNodeType.SkillAction,
      StringParam: actionKey
    });
  }

  static AbilityEffect(effect = undefined) {
    if (arguments.length > 0 && effect == null) {
      throw new TypeError("effect is required when provided");
    }
    return new SkillFlowNodeConfig({
      NodeType: SkillFlowNodeType.AbilityEffect,
      Effect: effect ?? null
    });
  }
}

/**
 * 每次施放独立的上下文和黑板；实体字段可保存接入方对象。
 */
export class SkillFlowContext {
  constructor({
    ExecutionId = 0,
    SkillId = 0,
    Caster = null,
    PrimaryTarget = null,
    ExecutionEntity = null,
    TargetPosition = null,
    InputDirection = null,
    CastStartTick = 0,
    Variables = new Map(),
    EffectHost = null,
    EventSource = null,
    SpawnedEntities = [],
    Subscriptions = [],
    OnNodeEnter = null,
    OnNodeExit = null
  } = {}) {
    Object.assign(this, {
      ExecutionId,
      SkillId,
      Caster,
      PrimaryTarget,
      ExecutionEntity,
      TargetPosition,
      InputDirection,
      CastStartTick,
      Variables: Variables instanceof Map ? Variables : new Map(Object.entries(Variables)),
      EffectHost,
      EventSource,
      SpawnedEntities,
      Subscriptions,
      OnNodeEnter,
      OnNodeExit
    });
  }
}

/**
 * 黑板访问方法；读取不存在的键时返回调用方提供的默认值。
 */
export class SkillFlowContextSystem {
  static SetVariable(context, key, value) {
    context.Variables.set(key, value);
  }

  static GetVariable(context, key, defaultValue = undefined) {
    return context.Variables.has(key) ? context.Variables.get(key) : defaultValue;
  }
}

/**
 * 按技能 ID 保存流程配置的注册表。
 */
export class SkillFlowLib {
  static #configs = new Map();

  static Register(config) {
    if (config == null) {
      throw new TypeError("config is required");
    }
    this.#configs.set(config.SkillId, config);
  }

  static Unregister(skillId) {
    this.#configs.delete(skillId);
  }

  static TryGet(skillId) {
    return this.#configs.has(skillId)
      ? { Found: true, Config: this.#configs.get(skillId) }
      : { Found: false, Config: undefined };
  }

  static Clear() {
    this.#configs.clear();
  }

  static get Count() {
    return this.#configs.size;
  }
}

/**
 * 单个节点的运行状态；组合节点的游标、计时器和订阅均保存在此对象。
 */
export class NodeFrame {
  constructor(config) {
    this.Type = config.NodeType;
    this.Config = config;
    this.Status = NodeStatus.Running;
    this.ChildIndex = 0;
    this.EffectIndex = 0;
    this.Timer = 0;
    this.RetryCount = 0;
    this.Entered = false;
    this.Subscribed = false;
    this.EventReceived = false;
    this.EventSubscription = null;
    this.Locals = new Map();
    this.ChildFrames = config.Children.map((child) => new NodeFrame(child));
    this._timerRaw = 0n;
  }
}

/**
 * 一次技能执行实例；禁止在不同执行之间共享运行时状态。
 */
export class SkillExecuteComponent {
  constructor({ Config, Context, ActionHost } = {}) {
    this.Config = Config;
    this.Context = Context;
    this.ActionHost = ActionHost;
    this.RootFrame = null;
    this.IsStarted = false;
    this.IsFinished = false;
  }
}

const FIXED_ONE = 1n << 32n;
const FLOAT64_BUFFER = new ArrayBuffer(8);
const FLOAT64_VIEW = new DataView(FLOAT64_BUFFER);

function toFixedRaw(value, asFloat32 = false) {
  const number = asFloat32 ? Math.fround(value) : value;
  if (!Number.isFinite(number)) {
    throw new RangeError("time delta must be finite");
  }
  FLOAT64_VIEW.setFloat64(0, number, false);
  const bits = FLOAT64_VIEW.getBigUint64(0, false);
  const exponent = Number((bits >> 52n) & 0x7ffn);
  const fraction = bits & ((1n << 52n) - 1n);
  if (exponent === 0 && fraction === 0n) {
    return 0n;
  }

  const significand = exponent === 0 ? fraction : (1n << 52n) | fraction;
  const shift = (exponent === 0 ? -1022 : exponent - 1023) - 52 + 32;
  let raw = shift >= 0 ? significand << BigInt(shift) : significand >> BigInt(-shift);
  if ((bits >> 63n) !== 0n) {
    raw = -raw;
  }
  return BigInt.asIntN(64, raw);
}

function addFixedRaw(left, right) {
  return BigInt.asIntN(64, left + right);
}

function dispose(subscription) {
  if (typeof subscription === "function") {
    subscription();
  } else if (typeof subscription.Dispose === "function") {
    subscription.Dispose();
  } else {
    subscription.dispose();
  }
}

function broadcastNodeEnter(context, frame) {
  if (typeof context.OnNodeEnter === "function") {
    context.OnNodeEnter(frame, context);
  }
}

function broadcastNodeExit(context, frame, result) {
  if (typeof context.OnNodeExit === "function") {
    context.OnNodeExit(frame, context, result);
  }
}

function disposeFrameSubscription(frame) {
  if (frame.EventSubscription == null) {
    return;
  }
  dispose(frame.EventSubscription);
  frame.EventSubscription = null;
  frame.EventReceived = false;
}

function disposeFrameSubscriptions(frame) {
  disposeFrameSubscription(frame);
  for (const child of frame.ChildFrames) {
    disposeFrameSubscriptions(child);
  }
}

function disposeContextSubscriptions(context) {
  for (const subscription of context.Subscriptions) {
    dispose(subscription);
  }
  context.Subscriptions.length = 0;
}

function getBoolean(context, key, fallback = false) {
  const value = SkillFlowContextSystem.GetVariable(context, key);
  return typeof value === "boolean" ? value : fallback;
}

function getInteger(context, key, fallback = 0) {
  const value = SkillFlowContextSystem.GetVariable(context, key);
  return Number.isInteger(value) && value >= -2147483648 && value <= 2147483647
    ? value
    : fallback;
}

function captureEventTarget(context) {
  const target = context.EventSource?.LastTargetEntity;
  if (target == null) {
    return;
  }
  context.PrimaryTarget = target;
  context.Variables.set("hitTarget", target);
}

function abilityEffect(frame, config, context) {
  if (context.EffectHost == null) {
    return NodeResult.Failure("no ability effect host");
  }
  if (config.Effect != null) {
    return context.EffectHost.ExecuteEffect(context, config.Effect, frame);
  }

  const { Found, Config } = AbilitySkillLib.TryGet(context.SkillId);
  if (!Found) {
    return NodeResult.Failure(`ability config not found for skill ${context.SkillId}`);
  }
  const effects = Config.Effects;
  if (!Array.isArray(effects) || effects.length === 0) {
    return NodeResult.Failure(`no effects registered for skill ${context.SkillId}`);
  }

  while (frame.EffectIndex < effects.length) {
    const effect = effects[frame.EffectIndex];
    if (effect == null || !effect.Enabled) {
      frame.EffectIndex++;
      continue;
    }
    const result = context.EffectHost.ExecuteEffect(context, effect, frame);
    if (result.Status !== NodeStatus.Success) {
      return result;
    }
    frame.EffectIndex++;
  }
  return NodeResult.Success();
}

function step(frame, config, context, host, deltaRaw) {
  if (!frame.Entered) {
    frame.Entered = true;
    broadcastNodeEnter(context, frame);
  }

  let result;
  switch (frame.Type) {
    case SkillFlowNodeType.Sequence:
      result = sequence(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.Selector:
      result = selector(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.Parallel:
      result = parallel(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.Race:
      result = race(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.If:
      result = branchIf(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.Switch:
      result = branchSwitch(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.Loop:
      result = loop(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.ForEach:
      result = forEach(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.Retry:
      result = retry(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.Delay:
      frame._timerRaw = addFixedRaw(frame._timerRaw, deltaRaw);
      frame.Timer = Number(frame._timerRaw) / Number(FIXED_ONE);
      result = frame._timerRaw >= toFixedRaw(config.FloatParam, true)
        ? NodeResult.Success()
        : NodeResult.Running();
      break;
    case SkillFlowNodeType.WaitEvent:
      result = waitEvent(frame, config, context);
      break;
    case SkillFlowNodeType.Timeout:
      result = timeout(frame, config, context, host, deltaRaw);
      break;
    case SkillFlowNodeType.CancelScope:
      result = config.Children.length === 0
        ? NodeResult.Failure("cancelscope body missing")
        : step(frame.ChildFrames[0], config.Children[0], context, host, deltaRaw);
      break;
    case SkillFlowNodeType.SkillAction:
      result = host.Execute(context, config.StringParam, frame);
      break;
    case SkillFlowNodeType.AbilityEffect:
      result = abilityEffect(frame, config, context);
      break;
    default:
      result = NodeResult.Failure(`unknown node type ${frame.Type}`);
  }

  frame.Status = result.Status;
  if (result.Status !== NodeStatus.Running) {
    broadcastNodeExit(context, frame, result);
  }
  return result;
}

function sequence(frame, config, context, host, deltaRaw) {
  while (frame.ChildIndex < config.Children.length) {
    const result = step(
      frame.ChildFrames[frame.ChildIndex],
      config.Children[frame.ChildIndex],
      context,
      host,
      deltaRaw
    );
    if (result.Status === NodeStatus.Running) {
      return NodeResult.Running();
    }
    if (result.Status !== NodeStatus.Success) {
      return result;
    }
    frame.ChildIndex++;
  }
  return NodeResult.Success();
}

function selector(frame, config, context, host, deltaRaw) {
  while (frame.ChildIndex < config.Children.length) {
    const result = step(
      frame.ChildFrames[frame.ChildIndex],
      config.Children[frame.ChildIndex],
      context,
      host,
      deltaRaw
    );
    if (result.Status === NodeStatus.Running) {
      return NodeResult.Running();
    }
    if (result.Status === NodeStatus.Success) {
      return NodeResult.Success();
    }
    frame.ChildIndex++;
  }
  return NodeResult.Failure("selector: all failed");
}

function cancelAllRunning(frame) {
  for (const child of frame.ChildFrames) {
    if (child.Status === NodeStatus.Running) {
      cancelFrame(child);
    }
  }
}

function parallel(frame, config, context, host, deltaRaw) {
  const any = config.BoolParam;
  let anySuccess = false;
  let allTerminated = true;

  for (let i = 0; i < config.Children.length; i++) {
    const child = frame.ChildFrames[i];
    if (child.Status === NodeStatus.Running) {
      step(child, config.Children[i], context, host, deltaRaw);
    }
    if (child.Status === NodeStatus.Running) {
      allTerminated = false;
    } else if (child.Status === NodeStatus.Success) {
      anySuccess = true;
    }
  }

  if (any) {
    if (anySuccess) {
      cancelAllRunning(frame);
      return NodeResult.Success();
    }
    return allTerminated
      ? NodeResult.Failure("parallel any: no success")
      : NodeResult.Running();
  }

  for (const child of frame.ChildFrames) {
    if (child.Status !== NodeStatus.Running && child.Status !== NodeStatus.Success) {
      cancelAllRunning(frame);
      return new NodeResult(child.Status);
    }
  }
  return allTerminated ? NodeResult.Success() : NodeResult.Running();
}

function race(frame, config, context, host, deltaRaw) {
  const raceOnFailure = config.BoolParam;
  for (let i = 0; i < config.Children.length; i++) {
    const child = frame.ChildFrames[i];
    if (child.Status === NodeStatus.Running) {
      step(child, config.Children[i], context, host, deltaRaw);
    }
    const wins = raceOnFailure
      ? child.Status === NodeStatus.Failure
      : child.Status === NodeStatus.Success;
    if (wins) {
      cancelAllRunning(frame);
      return new NodeResult(child.Status);
    }
  }
  return frame.ChildFrames.some((child) => child.Status === NodeStatus.Running)
    ? NodeResult.Running()
    : NodeResult.Failure("race no winner");
}

function branchIf(frame, config, context, host, deltaRaw) {
  if (!frame.Subscribed) {
    frame.Subscribed = true;
    frame.ChildIndex = getBoolean(context, config.StringParam) ? 0 : 1;
  }
  if (frame.ChildIndex === 1 && config.Children.length < 2) {
    return NodeResult.Success();
  }
  if (frame.ChildIndex >= config.Children.length) {
    return NodeResult.Failure("if branch missing");
  }
  return step(
    frame.ChildFrames[frame.ChildIndex],
    config.Children[frame.ChildIndex],
    context,
    host,
    deltaRaw
  );
}

function branchSwitch(frame, config, context, host, deltaRaw) {
  if (!frame.Subscribed) {
    frame.Subscribed = true;
    const selector = getInteger(context, config.StringParam);
    let index = config.Children.findIndex((child) => child.IntParam === selector);
    if (index < 0) {
      index = config.Children.length - 1;
    }
    frame.ChildIndex = index;
  }
  if (frame.ChildIndex < 0 || frame.ChildIndex >= config.Children.length) {
    return NodeResult.Success();
  }
  return step(
    frame.ChildFrames[frame.ChildIndex],
    config.Children[frame.ChildIndex],
    context,
    host,
    deltaRaw
  );
}

function resetFrame(frame) {
  disposeFrameSubscriptions(frame);
  frame.Status = NodeStatus.Running;
  frame.ChildIndex = 0;
  frame.EffectIndex = 0;
  frame.Timer = 0;
  frame._timerRaw = 0n;
  frame.RetryCount = 0;
  frame.Subscribed = false;
  frame.EventReceived = false;
  frame.Locals.clear();
  for (const child of frame.ChildFrames) {
    resetFrame(child);
  }
}

function loop(frame, config, context, host, deltaRaw) {
  if (config.Children.length === 0) {
    return NodeResult.Failure("loop body missing");
  }
  const body = frame.ChildFrames[0];
  const bodyConfig = config.Children[0];
  while (frame.ChildIndex < config.IntParam) {
    if (body.Status !== NodeStatus.Running) {
      frame.Locals.set("_index", frame.ChildIndex);
      resetFrame(body);
    }
    const result = step(body, bodyConfig, context, host, deltaRaw);
    if (result.Status === NodeStatus.Running) {
      return NodeResult.Running();
    }
    if (result.Status !== NodeStatus.Success) {
      return result;
    }
    frame.ChildIndex++;
  }
  return NodeResult.Success();
}

function forEach(frame, config, context, host, deltaRaw) {
  if (!frame.Subscribed) {
    frame.Subscribed = true;
    frame.Locals.set("_items", SkillFlowContextSystem.GetVariable(context, config.StringParam));
  }
  const items = frame.Locals.get("_items");
  if (!Array.isArray(items)) {
    return NodeResult.Failure(`foreach source missing: ${config.StringParam}`);
  }
  if (config.Children.length === 0) {
    return NodeResult.Failure("foreach body missing");
  }

  const body = frame.ChildFrames[0];
  const bodyConfig = config.Children[0];
  while (frame.ChildIndex < items.length) {
    if (body.Status !== NodeStatus.Running) {
      frame.Locals.set("_item", items[frame.ChildIndex]);
      resetFrame(body);
    }
    const result = step(body, bodyConfig, context, host, deltaRaw);
    if (result.Status === NodeStatus.Running) {
      return NodeResult.Running();
    }
    if (result.Status !== NodeStatus.Success) {
      return result;
    }
    frame.ChildIndex++;
  }
  return NodeResult.Success();
}

function retry(frame, config, context, host, deltaRaw) {
  if (config.Children.length === 0) {
    return NodeResult.Failure("retry body missing");
  }
  const body = frame.ChildFrames[0];
  const bodyConfig = config.Children[0];
  while (true) {
    if (body.Status !== NodeStatus.Running) {
      frame.Locals.set("_attempt", frame.RetryCount);
      resetFrame(body);
    }
    const result = step(body, bodyConfig, context, host, deltaRaw);
    if (result.Status === NodeStatus.Running || result.Status === NodeStatus.Success) {
      return result.Status === NodeStatus.Running ? NodeResult.Running() : NodeResult.Success();
    }
    if (frame.RetryCount >= config.IntParam) {
      return result;
    }
    frame.RetryCount++;
  }
}

function waitEvent(frame, config, context) {
  if (!frame.Subscribed) {
    frame.Subscribed = true;
    if (context.EventSource != null) {
      frame.EventSubscription = context.EventSource.Subscribe(config.StringParam, () => {
        captureEventTarget(context);
        frame.EventReceived = true;
      });
    }
  }
  if (frame.EventReceived || getBoolean(context, config.StringParam)) {
    disposeFrameSubscription(frame);
    return NodeResult.Success();
  }
  return NodeResult.Running();
}

function timeout(frame, config, context, host, deltaRaw) {
  if (config.Children.length === 0) {
    return NodeResult.Failure("timeout body missing");
  }
  const bodyResult = step(
    frame.ChildFrames[0],
    config.Children[0],
    context,
    host,
    deltaRaw
  );
  if (bodyResult.Status !== NodeStatus.Running) {
    return bodyResult;
  }
  frame._timerRaw = addFixedRaw(frame._timerRaw, deltaRaw);
  frame.Timer = Number(frame._timerRaw) / Number(FIXED_ONE);
  if (frame._timerRaw >= toFixedRaw(config.FloatParam, true)) {
    cancelFrame(frame.ChildFrames[0]);
    return NodeResult.Timeout("timeout");
  }
  return NodeResult.Running();
}

function hasRunningDetached(frame) {
  if (frame.Status === NodeStatus.Running) {
    return true;
  }
  return frame.ChildFrames.some(hasRunningDetached);
}

function driveDetached(frame, config, execution, deltaRaw) {
  if (frame.Type === SkillFlowNodeType.CancelScope) {
    for (let i = 0; i < frame.ChildFrames.length; i++) {
      if (frame.ChildFrames[i].Status === NodeStatus.Running) {
        step(
          frame.ChildFrames[i],
          config.Children[i],
          execution.Context,
          execution.ActionHost,
          deltaRaw
        );
      }
    }
    return;
  }
  for (let i = 0; i < frame.ChildFrames.length; i++) {
    if (hasRunningDetached(frame.ChildFrames[i])) {
      driveDetached(frame.ChildFrames[i], config.Children[i], execution, deltaRaw);
    }
  }
}

function cancelFrame(frame) {
  if (frame.Status !== NodeStatus.Running) {
    disposeFrameSubscription(frame);
    return;
  }
  if (frame.Type === SkillFlowNodeType.CancelScope) {
    frame.Status = NodeStatus.Cancelled;
    disposeFrameSubscription(frame);
    return;
  }
  for (const child of frame.ChildFrames) {
    cancelFrame(child);
  }
  disposeFrameSubscription(frame);
  frame.Status = NodeStatus.Cancelled;
}

/**
 * 无全局可变运行状态的解释器；所有状态存于 SkillExecuteComponent/NodeFrame。
 * 时间参数以秒传入，并在内部通过 32.32 定点数累计，不读取系统时钟。
 */
export class SkillFlowInterpreter {
  static Enter(execution) {
    if (execution.RootFrame == null) {
      execution.RootFrame = new NodeFrame(execution.Config.Root);
    }
    execution.IsStarted = true;
    return step(
      execution.RootFrame,
      execution.Config.Root,
      execution.Context,
      execution.ActionHost,
      0n
    );
  }

  static Tick(execution, deltaSeconds) {
    const deltaRaw = toFixedRaw(deltaSeconds);
    if (execution.RootFrame.Status === NodeStatus.Running) {
      const result = step(
        execution.RootFrame,
        execution.Config.Root,
        execution.Context,
        execution.ActionHost,
        deltaRaw
      );
      if (result.Status !== NodeStatus.Running) {
        execution.IsFinished = true;
      }
      return result;
    }

    if (hasRunningDetached(execution.RootFrame)) {
      driveDetached(execution.RootFrame, execution.Config.Root, execution, deltaRaw);
    }
    if (!hasRunningDetached(execution.RootFrame)) {
      execution.IsFinished = true;
    }
    return NodeResult.Success();
  }

  static Exit(execution, result) {
    disposeFrameSubscriptions(execution.RootFrame);
    disposeContextSubscriptions(execution.Context);
    execution.IsFinished = true;
  }

  static Cancel(execution) {
    cancelFrame(execution.RootFrame);
    if (!hasRunningDetached(execution.RootFrame)) {
      disposeContextSubscriptions(execution.Context);
      execution.IsFinished = true;
    }
  }

  static HasRunningDetached(frame) {
    return hasRunningDetached(frame);
  }
}
