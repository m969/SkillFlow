using System;

namespace ECS
{
    /// <summary>
    /// 指定 System 在同实体类型的驱动（<c>IUpdate</c> / <c>IFixedUpdate</c> / 生命周期）中的执行顺序。
    /// <para>
    /// 数值小的先执行；未标注默认为 0。相同值保持注册顺序（内部使用稳定排序）。
    /// 用于**显式声明**读写冲突的 System 顺序，禁止依赖程序集扫描或注册顺序
    /// （项目规范：System 执行顺序必须显式定义）。
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SystemOrderAttribute : Attribute
    {
        /// <summary>执行顺序（小的先执行）。</summary>
        public int Order { get; }

        /// <summary>构造。</summary>
        public SystemOrderAttribute(int order)
        {
            Order = order;
        }
    }
}
