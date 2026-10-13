/**
 * 节点执行的六种终态/运行态，字符串值便于日志记录和 JSON 交换。
 */
export const NodeStatus = Object.freeze({
  Running: "Running",
  Success: "Success",
  Failure: "Failure",
  Cancelled: "Cancelled",
  Interrupted: "Interrupted",
  Timeout: "Timeout"
});

/**
 * 技能流程节点的执行结果。
 */
export class NodeResult {
  constructor(status, reason = null) {
    this.Status = status;
    this.Reason = reason;
  }

  static Running() {
    return new NodeResult(NodeStatus.Running);
  }

  static Success(reason = null) {
    return new NodeResult(NodeStatus.Success, reason);
  }

  static Failure(reason = null) {
    return new NodeResult(NodeStatus.Failure, reason);
  }

  static Cancelled(reason = null) {
    return new NodeResult(NodeStatus.Cancelled, reason);
  }

  static Interrupted(reason = null) {
    return new NodeResult(NodeStatus.Interrupted, reason);
  }

  static Timeout(reason = null) {
    return new NodeResult(NodeStatus.Timeout, reason);
  }
}
