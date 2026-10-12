using ECS;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ECS
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SystemFilterAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SystemPlatformFilterAttribute : SystemFilterAttribute
    {
        public int Platform { get; private set; }

        public SystemPlatformFilterAttribute(int platform)
        {
            this.Platform = platform;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SystemGameFilterAttribute : SystemFilterAttribute
    {
        public int GameType { get; private set; }

        public SystemGameFilterAttribute(int gameType)
        {
            this.GameType = gameType;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SystemEcsFilterAttribute : SystemFilterAttribute
    {
        public int EcsType { get; private set; }

        public SystemEcsFilterAttribute(int ecsType)
        {
            this.EcsType = ecsType;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SystemMuteAttribute : SystemFilterAttribute
    {
        public SystemMuteAttribute()
        {
        }
    }

    /// <summary>
    /// 指定 System 只在实体拥有指定组件时才触发 IDrive 生命周期（IAwake/IInit/IUpdate/IDestroy 等）。
    /// 可叠加多个组件，AND 语义——所有组件都存在时才执行。
    /// 运行时动态添加/移除组件后，下一帧立即生效。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SystemRequireComponentAttribute : SystemFilterAttribute
    {
        public Type[] ComponentTypes { get; private set; }

        public SystemRequireComponentAttribute(params Type[] componentTypes)
        {
            this.ComponentTypes = componentTypes;
        }
    }
}
