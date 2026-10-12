using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    /// <summary>
    /// 实体是树形节点的机制，父子节点具有包含的属性，同类节点具有独立分离的属性，能同时满足系统和组件的需求
    /// </summary>
    public class EcsEntity : EcsObject
    {
        public long Id { get; set; }
        public long InstanceId { get; set; }
        //public long UniqueId { get; set; }
        public bool IsDisposed
        {
            get
            {
                return InstanceId == 0;
            }
        }
        public Dictionary<long, EcsEntity> Id2Children = new Dictionary<long, EcsEntity>();
        public Dictionary<Type, EcsComponent> Components { get; set; } = new Dictionary<Type, EcsComponent>();

        public EcsEntity Parent { get; set; }
        private bool enable;
        public bool Enable
        {
            get { return enable; }
            set
            {
                if (enable != value)
                {
                    if (!enable && value)
                    {
                        enable = value;
                        foreach (var item in Components.Values)
                        {
                            item.Enable = value;
                        }
                        EcsNode.DriveEntitySystems(this, typeof(IEnable));
                    }
                    if (enable && !value)
                    {
                        enable = value;
                        foreach (var item in Components.Values)
                        {
                            item.Enable = value;
                        }
                        EcsNode.DriveEntitySystems(this, typeof(IDisable));
                    }
                }
            }
        }

        public EcsNode EcsNode
        {
            get
            {
                if (this is EcsNode node)
                    return node;
                return Parent.EcsNode;
            }
        }

        public T GetParent<T>() where T : EcsEntity
        {
            return (T)Parent;
        }

        public T As<T>() where T : EcsEntity
        {
            return (T)this;
        }

        public T AddChild<T>(Action<T> beforeAwake = null) where T : EcsEntity, new()
        {
            return AddChild(EcsNode.NewEntityId(), beforeAwake);
        }

        public T AddChild<T>(long id, Action<T> beforeAwake = null) where T : EcsEntity, new()
        {
            var entity = new T();
            entity.Id = id;
            entity.InstanceId = EcsNode.NewInstanceId();
            entity.Parent = this;
            Id2Children.Add(entity.Id, entity);
            beforeAwake?.Invoke(entity);
            EcsNode.AddEntity(entity);
            DriveAwake(entity);
            return entity;
        }

        public EcsEntity AddChild(Type type, Action<EcsEntity> beforeAwake = null)
        {
            return AddChild(EcsNode.NewInstanceId(), type, beforeAwake);
        }

        public EcsEntity AddChild(long id, Type type, Action<EcsEntity> beforeAwake = null)
        {
            var entity = Activator.CreateInstance(type) as EcsEntity;
            entity.Id = id;
            entity.InstanceId = EcsNode.NewInstanceId();

            entity.Parent = this;
            Id2Children.Add(entity.Id, entity);
            beforeAwake?.Invoke(entity);
            EcsNode.AddEntity(entity);
            DriveAwake(entity);
            return entity;
        }

        public T GetChild<T>(long id) where T : EcsEntity, new()
        {
            Id2Children.TryGetValue(id, out var entity);
            return (T)entity;
        }

        public void RemoveChild(EcsEntity entity)
        {
            DriveDestroy(entity);
            Id2Children.Remove(entity.Id);
            EcsNode.RemoveEntity(entity);
        }

        public T AddComponent<T>(Action<T> beforeAwake = null) where T : EcsComponent, new()
        {
            var component = new T();
            component.Id = EcsNode.NewEntityId();
            component.Entity = this;
            Components.Add(typeof(T), component);
            beforeAwake?.Invoke(component);
            DriveAwake(component);
            EcsNode.DriveEntitySystems(this, typeof(IOnAddComponent), new object[] { this, component });
            return component;
        }

        public void AddComponent(EcsComponent component)
        {
            var type = component.GetType();
            component.Id = EcsNode.NewEntityId();
            component.Entity = this;
            Components.Add(type, component);
            EcsNode.DriveEntitySystems(this, typeof(IOnAddComponent), new object[] { this, component });
        }

        public void RemoveComponent<T>() where T : EcsComponent, new()
        {
            RemoveComponent(typeof(T));
        }

        public void RemoveComponent(Type type)
        {
            var component = Components[type];
            DriveDestroy(component);
            EcsNode.DriveEntitySystems(this, typeof(IOnRemoveComponent), new object[] { this, component });
            Components.Remove(type);
        }

        public T GetComponent<T>() where T : EcsComponent, new()
        {
            Components.TryGetValue(typeof(T), out var component);
            return component as T;
        }

        public bool TryGetComponent<T>(out T component) where T : EcsComponent, new()
        {
            Components.TryGetValue(typeof(T), out var component2);
            component = component2 as T;
            return component2 != null;
        }
        
        public void ComponentChange<T>() where T : EcsComponent, new()
        {
            var entity = this;
            var component = GetComponent<T>();
            if (component == null)
            {
                return;
            }
            entity.EcsNode.DriveComponentSystems(entity, component, typeof(IOnChange));
            entity.EcsNode.DriveEntitySystems(entity, typeof(IOnChange), new object[] { entity });
        }

        public void Change<T>(Action<T> changeAction) where T : EcsComponent, new()
        {
            changeAction?.Invoke(GetComponent<T>());
            ComponentChange<T>();
        }

        private void DriveAwake(EcsEntity entity)
        {
            EcsNode.DriveEntitySystems(entity, typeof(IAwake));
        }

        private void DriveAwake<T>(T component) where T : EcsComponent, new()
        {
            EcsNode.DriveComponentSystems(this, component, typeof(IAwake));
        }

        public void Init()
        {
            var ecsObject = this;
            if (ecsObject is EcsNode ecsNode)
            {
                //因为EcsNode是根节点，不走AddChild的Awake，所以这里手动Awake一下
                DriveAwake(ecsNode);
            }

            if (ecsObject is EcsEntity entity)
            {
                var arr = entity.Components.Values.ToArray();
                foreach (var item in arr)
                {
                    entity.EcsNode.DriveComponentSystems(entity, item, typeof(IInit));
                }
                entity.EcsNode.DriveEntitySystems(entity, typeof(IInit));

                foreach (var item in arr)
                {
                    entity.EcsNode.DriveComponentSystems(entity, item, typeof(IAfterInit));
                }
                entity.EcsNode.DriveEntitySystems(entity, typeof(IAfterInit));
            }
        }

        private void DriveDestroy(EcsEntity entity)
        {
            EcsNode.DriveEntitySystems(entity, typeof(IDestroy));
        }

        private void DriveDestroy<T>(T component) where T : EcsComponent, new()
        {
            EcsNode.DriveComponentSystems(this, component, typeof(IDestroy));
        }

        /// <summary>
        /// 不可信任，独立隔离的异常捕获，用于场景、UI等和游戏核心逻辑无关的视图显示，任何异常都不应该影响游戏核心逻辑
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public void Dispatch<T>(Action<T> action) where T : IDispatch
        {
            var entity = this;
            if (entity == null || entity.IsDisposed)
            {
                return;
            }
            if (entity.EcsNode.EntityType2Systems.TryGetValue(typeof(EcsEntity), out var systems1))
            {
                foreach (var item in systems1)
                {
                    if (item is T eventHandleSystem)
                    {
                        try
                        {
                            action.Invoke(eventHandleSystem);
                        }
                        catch (Exception ex)
                        {
                            ConsoleLog.Error($"Dispatch Exception: {ex}");
                        }
                    }
                }
            }
            if (entity.EcsNode.EntityType2Systems.TryGetValue(entity.GetType(), out var systems2))
            {
                foreach (var item in systems2)
                {
                    if (item is T eventHandleSystem)
                    {
                        try
                        {
                            action.Invoke(eventHandleSystem);
                        }
                        catch (Exception ex)
                        {
                            ConsoleLog.Error($"Dispatch Exception: {ex}");
                        }
                    }
                }
            }
        }

        public T Fetch<T>()
        {
            var entity = this;
            if (entity == null || entity.IsDisposed)
            {
                return default(T);
            }

            if (entity.EcsNode.EntityType2Systems.TryGetValue(entity.GetType(), out var systems2))
            {
                foreach (var item in systems2)
                {
                    if (item is T eventHandleSystem)
                    {
                        return eventHandleSystem;
                    }
                }
            }

            if (entity.EcsNode.EntityType2Systems.TryGetValue(typeof(EcsEntity), out var systems1))
            {
                foreach (var item in systems1)
                {
                    if (item is T eventHandleSystem)
                    {
                        return eventHandleSystem;
                    }
                }
            }

            return default(T);
        }
    }
}