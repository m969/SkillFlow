using System;
using System.Collections.Generic;

namespace NetGame.SkillFlow
{
    /// <summary>
    /// 运行时节点状态：平行于数据图，由 <c>SkillFlowInterpreter</c> 读写。
    /// 纯数据，无业务逻辑、无行为方法。关键：状态进 Component，而非放在节点对象字段（符合 ECS 规范）。
    /// </summary>
    public class NodeFrame
    {
        /// <summary>节点类型。</summary>
        public SkillFlowNodeType Type;

        /// <summary>对应配置节点（含 Effect/SkillAction 载荷），供表现层读取节点数据；生命期内稳定。</summary>
        public SkillFlowNodeConfig Config;

        /// <summary>当前节点执行状态。</summary>
        public NodeStatus Status = NodeStatus.Running;

        /// <summary>控制节点推进游标（当前执行到的子节点下标）。</summary>
        public int ChildIndex;

        /// <summary>AbilityEffect 配置效果推进游标。</summary>
        public int EffectIndex;

        /// <summary>计时器（Delay / Timeout，单位为秒）。</summary>
        public float Timer;

        /// <summary>重试计数（Retry）。</summary>
        public int RetryCount;

        /// <summary>是否已广播节点进入事件（Enter，去重）。</summary>
        public bool Entered;

        /// <summary>是否已订阅（WaitEvent 去重）。</summary>
        public bool Subscribed;

        /// <summary>WaitEvent 收到外部事件后的信号；状态存于节点帧而非解释器静态字段。</summary>
        public bool EventReceived;

        /// <summary>WaitEvent 的订阅句柄，节点结束或取消时由解释器释放。</summary>
        public IDisposable EventSubscription;

        /// <summary>局部黑板（ForEach 的 item 等，避免污染全局上下文）。</summary>
        public Dictionary<string, object> Locals = new Dictionary<string, object>();

        /// <summary>子节点运行时状态（平行于 <c>SkillFlowNodeConfig.Children</c>）。</summary>
        public List<NodeFrame> ChildFrames = new List<NodeFrame>();
    }
}
