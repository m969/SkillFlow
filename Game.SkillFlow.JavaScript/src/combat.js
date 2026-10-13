import { NodeResult } from "./node-result.js";

const builtInTriggerTypeIds = new Set([
  "netgame.combat.trigger.execute",
  "netgame.combat.trigger.instant",
  "netgame.combat.trigger.action-point",
  "netgame.combat.trigger.time-condition"
]);

/**
 * 与效果类型关联的元数据，可由接入方用于构建自己的注册工具。
 */
export class EffectAttribute {
  constructor(effectType, order) {
    this.EffectType = effectType;
    this.Order = order;
  }
}

/**
 * 可扩展的效果负载；具体业务字段由效果处理器定义。
 */
export class Effect {
  constructor({
    EffectTypeId = "",
    IsSkillEffect = false,
    Enabled = false,
    ...payload
  } = {}) {
    this.EffectTypeId = EffectTypeId;
    this.IsSkillEffect = IsSkillEffect;
    this.Enabled = Enabled;
    Object.assign(this, payload);
  }

  get HideTriggerType() {
    return false;
  }

  get Label() {
    return "Effect";
  }
}

/**
 * 与技能 ID 关联的内存配置。
 */
export class AbilityConfigData {
  constructor({
    Id = 0,
    ShowName = "",
    SkillSpellTypeId = "",
    Effects = [],
    TriggerActions = []
  } = {}) {
    this.Id = Id;
    this.ShowName = ShowName;
    this.SkillSpellTypeId = SkillSpellTypeId;
    this.Effects = Effects;
    this.TriggerActions = TriggerActions;
  }
}

/**
 * Ability 配置注册表。相同 ID 的注册会覆盖旧配置。
 */
export class AbilitySkillLib {
  static #configs = new Map();

  static Register(config) {
    if (config == null) {
      throw new TypeError("config is required");
    }
    this.#configs.set(config.Id, config);
  }

  static Unregister(abilityId) {
    this.#configs.delete(abilityId);
  }

  static TryGet(abilityId) {
    return this.#configs.has(abilityId)
      ? { Found: true, Config: this.#configs.get(abilityId) }
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
 * 效果处理器注册表；未注册的效果类型显式返回 Failure。
 */
export class EffectHandlerRegistry {
  #handlers = new Map();

  get Count() {
    return this.#handlers.size;
  }

  Register(effectTypeId, handler) {
    if (typeof effectTypeId !== "string" || effectTypeId.trim() === "") {
      throw new TypeError("EffectTypeId must not be empty");
    }
    if (handler == null || typeof handler.Execute !== "function") {
      throw new TypeError("handler must implement Execute");
    }
    this.#handlers.set(effectTypeId, handler);
  }

  Unregister(effectTypeId) {
    return this.#handlers.delete(effectTypeId);
  }

  TryGet(effectTypeId) {
    return this.#handlers.has(effectTypeId)
      ? { Found: true, Handler: this.#handlers.get(effectTypeId) }
      : { Found: false, Handler: undefined };
  }

  Clear() {
    this.#handlers.clear();
  }

  ExecuteEffect(context, effect, frame) {
    if (effect == null) {
      return NodeResult.Failure("effect is null");
    }
    if (typeof effect.EffectTypeId !== "string" || effect.EffectTypeId.trim() === "") {
      return NodeResult.Failure("effect type id is empty");
    }

    const { Found, Handler } = this.TryGet(effect.EffectTypeId);
    if (!Found) {
      return NodeResult.Failure(`effect handler not registered: ${effect.EffectTypeId}`);
    }
    return Handler.Execute(context, effect, frame);
  }
}

/**
 * 触发器配置；PayloadJson 由扩展处理器解释。
 */
export class TriggerConfig {
  constructor({
    TriggerTypeId = "",
    Enabled = false,
    PayloadVersion = 0,
    PayloadJson = ""
  } = {}) {
    this.TriggerTypeId = TriggerTypeId;
    this.Enabled = Enabled;
    this.PayloadVersion = PayloadVersion;
    this.PayloadJson = PayloadJson;
  }
}

/**
 * 单次触发携带的上下文数据。
 */
export class TriggerContext {
  constructor({ AbilityTrigger = null, TriggerSource = null, Target = null } = {}) {
    this.AbilityTrigger = AbilityTrigger;
    this.TriggerSource = TriggerSource;
    this.Target = Target;
  }
}

/**
 * 创建单个触发器运行时所需的宿主、触发器和配置。
 */
export class TriggerCreateContext {
  constructor({ Host, Trigger, Config } = {}) {
    this.Host = Host;
    this.Trigger = Trigger;
    this.Config = Config;
  }
}

/**
 * 每个 Trigger 类型对应的稳定 ID。
 */
export const TriggerTypeIds = Object.freeze({
  Prefix: "netgame.combat.trigger.",
  Execute: "netgame.combat.trigger.execute",
  Instant: "netgame.combat.trigger.instant",
  ActionPoint: "netgame.combat.trigger.action-point",
  TimeCondition: "netgame.combat.trigger.time-condition",
  IsBuiltIn(triggerTypeId) {
    return builtInTriggerTypeIds.has(triggerTypeId);
  }
});

/**
 * Trigger 运行时工厂注册表；重复 ID 默认报错，可显式要求替换。
 */
export class TriggerHandlerRegistry {
  #handlers = new Map();

  get Count() {
    return this.#handlers.size;
  }

  Register(handler, replace = false) {
    if (handler == null || typeof handler.CreateRuntime !== "function") {
      throw new TypeError("handler must implement CreateRuntime");
    }
    const triggerTypeId = handler.TriggerTypeId;
    if (typeof triggerTypeId !== "string" || triggerTypeId.trim() === "") {
      throw new TypeError("TriggerTypeId must not be empty");
    }
    if (!replace && this.#handlers.has(triggerTypeId)) {
      throw new Error(`TriggerTypeId already registered: ${triggerTypeId}`);
    }
    this.#handlers.set(triggerTypeId, handler);
  }

  Unregister(triggerTypeId) {
    if (typeof triggerTypeId !== "string" || triggerTypeId.trim() === "") {
      return false;
    }
    return this.#handlers.delete(triggerTypeId);
  }

  TryGet(triggerTypeId) {
    return this.#handlers.has(triggerTypeId)
      ? { Found: true, Handler: this.#handlers.get(triggerTypeId) }
      : { Found: false, Handler: undefined };
  }

  CreateRuntime(triggerTypeId, context) {
    if (typeof triggerTypeId !== "string" || triggerTypeId.trim() === "") {
      throw new TypeError("TriggerTypeId must not be empty");
    }
    const { Found, Handler } = this.TryGet(triggerTypeId);
    if (!Found) {
      throw new Error(`Trigger handler not registered: ${triggerTypeId}`);
    }
    return Handler.CreateRuntime(context);
  }

  TryCreateRuntime(triggerTypeId, context) {
    if (typeof triggerTypeId !== "string" || triggerTypeId.trim() === "") {
      return { Success: false, Runtime: undefined, Error: "TriggerTypeId must not be empty" };
    }
    const { Found, Handler } = this.TryGet(triggerTypeId);
    if (!Found) {
      return {
        Success: false,
        Runtime: undefined,
        Error: `Trigger handler not registered: ${triggerTypeId}`
      };
    }
    return { Success: true, Runtime: Handler.CreateRuntime(context), Error: "" };
  }

  Clear() {
    this.#handlers.clear();
  }
}

/**
 * Ability、效果和触发器的轻量数据模型；实体关系由接入方负责维护。
 */
export class Ability {
  constructor({
    OwnerEntity = null,
    ParentEntity = null,
    ConfigId = 0,
    AbilityTypeId = "",
    ConfigObject = null,
    Name = "",
    AbilityEffects = [],
    AbilityTriggers = []
  } = {}) {
    Object.assign(this, {
      OwnerEntity,
      ParentEntity,
      ConfigId,
      AbilityTypeId,
      ConfigObject,
      Name,
      AbilityEffects,
      AbilityTriggers
    });
  }
}

export class AbilityEffect {
  constructor({ Name = "", OwnerAbility = null, EffectConfig = null } = {}) {
    Object.assign(this, { Name, OwnerAbility, EffectConfig });
  }

  get OwnerEntity() {
    return this.OwnerAbility.OwnerEntity;
  }

  get ParentEntity() {
    return this.OwnerAbility.ParentEntity;
  }
}

export class AbilityTrigger {
  constructor({
    OwnerAbility = null,
    TriggerConfig = null,
    ConditionParamValue = "",
    Runtime = null
  } = {}) {
    Object.assign(this, {
      OwnerAbility,
      TriggerConfig,
      ConditionParamValue,
      Runtime
    });
  }

  get OwnerEntity() {
    return this.OwnerAbility.OwnerEntity;
  }

  get ParentEntity() {
    return this.OwnerAbility.ParentEntity;
  }
}

export class AbilityExecution {
  constructor({
    Name = "",
    AbilityEntity = null,
    OwnerEntity = null,
    SkillTargets = [],
    InputTarget = null,
    InputPoint = null,
    InputDirection = null,
    InputRadian = 0,
    ActionOccupy = true
  } = {}) {
    Object.assign(this, {
      Name,
      AbilityEntity,
      OwnerEntity,
      SkillTargets,
      InputTarget,
      InputPoint,
      InputDirection,
      InputRadian,
      ActionOccupy
    });
  }
}
