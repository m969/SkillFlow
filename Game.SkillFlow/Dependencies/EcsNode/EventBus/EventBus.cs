using System;
using System.Collections;
using System.Collections.Generic;

namespace ECS
{
    public interface IEvent
    {
    }

    public class EventBusNode
    {
        public EventBusNode PreNode { get; set; }
        public EcsNode EcsNode { get; set; }
        public EventBusNode NextNode { get; set; }
    }

    public class EventBus
    {
        public static EventBus Instance = new EventBus();

        //public List<EcsNode> EcsNodes = new List<EcsNode>();
        public EventBusNode FirstNode { get; set; }
        public EventBusNode LastNode { get; set; }


        public void AddEcs(EcsNode ecsNode)
        {
            if (FirstNode == null)
            {
                FirstNode = new EventBusNode() { EcsNode = ecsNode, PreNode = null, NextNode = null };
                LastNode = FirstNode;
                return;
            }
            LastNode.NextNode = new EventBusNode() { PreNode = LastNode, EcsNode = ecsNode, NextNode = null };
            LastNode = LastNode.NextNode;
            //if (!EcsNodes.Contains(ecsNode))
            //{
            //    EcsNodes.Add(ecsNode);
            //}
        }

        public void RemoveEcs(EcsNode ecsNode)
        {
            var node = FirstNode;
            while (node != null)
            {
                if (node.EcsNode == ecsNode)
                {
                    if (node.PreNode != null)
                    {
                        node.PreNode.NextNode = node.NextNode;
                    }
                    else
                    {
                        FirstNode = node.NextNode;
                    }

                    if (node.NextNode != null)
                    {
                        node.NextNode.PreNode = node.PreNode;
                    }
                    else
                    {
                        LastNode = node.PreNode;
                    }
                    break;
                }
                node = node.NextNode;
            }
            //if (EcsNodes.Contains(ecsNode))
            //{
            //    EcsNodes.Remove(ecsNode);
            //}
        }

        public EcsNode GetEcs(ushort ecsType)
        {
            var node = FirstNode;
            while (node != null)
            {
                if (node.EcsNode.EcsTypeId == ecsType)
                {
                    return node.EcsNode;
                }
                node = node.NextNode;
            }
            return null;
        }

        public void ForeachNodeFromFirst(System.Action<EcsNode> action)
        {
            var node = FirstNode;
            while (node != null)
            {
                action(node.EcsNode);
                node = node.NextNode;
            }
        }

        public void ForeachNodeFromLast(System.Action<EcsNode> action)
        {
            var node = LastNode;
            while (node != null)
            {
                action(node.EcsNode);
                node = node.PreNode;
            }
        }

        public void DriveUpdate()
        {
            List<EcsNode> removeNodes = null;
            var node = FirstNode;
            while (node != null)
            {
                if (node.EcsNode.IsDisposed)
                {
                    if (removeNodes == null)
                    {
                        removeNodes = new List<EcsNode>();
                    }
                    removeNodes.Add(node.EcsNode);
                }
                else
                {
                    node.EcsNode.DriveEntityUpdate();
                }
                node = node.NextNode;
            }

            if (removeNodes != null)
            {
                foreach (var removeNode in removeNodes)
                {
                    RemoveEcs(removeNode);
                }
            }
        }

        public void DriveFixedUpdate()
        {
            var node = FirstNode;
            while (node != null)
            {
                if (!node.EcsNode.IsDisposed)
                {
                    node.EcsNode.DriveEntityFixedUpdate();
                }
                node = node.NextNode;
            }
        }

        public static void Send<T>(T eventObject) where T : IEvent
        {
            Instance.ForeachNodeFromLast((ecsNode) =>
            {
                ecsNode.Dispatch<IEventHandle<T>>(x => x.OnHandleEvent(ecsNode, eventObject));
            });
        }

        /// <summary>
        /// 记录当前被提前终止的事件实例集合。
        /// 由 <see cref="End{T}"/> 写入，由 <see cref="Start{T}"/> 在每次开始前清除并在结束后清理。
        /// </summary>
        private static readonly HashSet<IEvent> _endedEvents = new HashSet<IEvent>();

        /// <summary>
        /// 派发可中断事件。与 <see cref="Send{T}"/> 行为相同（从 Last→First 遍历节点），
        /// 但在派发过程中若有 Handler 调用了 <see cref="End{T}"/>，后续节点将跳过该事件的处理。
        /// </summary>
        /// <typeparam name="T">事件类型，需实现 <see cref="IEvent"/>。</typeparam>
        /// <param name="eventObject">事件对象。</param>
        public static void Start<T>(T eventObject) where T : IEvent
        {
            _endedEvents.Remove(eventObject);
            Instance.ForeachNodeFromLast((ecsNode) =>
            {
                if (_endedEvents.Contains(eventObject)) return;
                ecsNode.Dispatch<IEventHandle<T>>(x => x.OnHandleEvent(ecsNode, eventObject));
            });
            _endedEvents.Remove(eventObject);
        }

        /// <summary>
        /// 提前终止当前正在派发的 <see cref="Start{T}"/> 事件。
        /// 在某个节点的 Handler 内调用后，后续节点将不再收到该事件。
        /// 对 <see cref="Send{T}"/> 无效。
        /// </summary>
        /// <typeparam name="T">要终止的事件类型。</typeparam>
        /// <param name="eventObject">要终止的事件实例。</param>
        public static void End<T>(T eventObject) where T : IEvent
        {
            _endedEvents.Add(eventObject);
        }

        
    }
}