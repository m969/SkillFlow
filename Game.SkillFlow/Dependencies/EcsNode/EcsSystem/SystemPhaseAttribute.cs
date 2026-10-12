using System;

namespace ECS
{
    /// <summary>
    /// 指定 System 属于哪个驱动相位。配合 <c>EcsNode.DriveEntityUpdate(phase)</c> 使用：
    /// 一次 <c>Step</c> 内可在多个相位点分别驱动，只执行该相位的系统，
    /// 从而在迁移期保留原有「多相位交错」的执行结构。
    /// <para>
    /// 未标注默认为相位 0。相位 + <see cref="SystemOrderAttribute"/> 共同显式声明执行结构：
    /// 相位决定「哪个驱动点」，Order 决定「相位内的先后」。
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SystemPhaseAttribute : Attribute
    {
        /// <summary>所属相位。</summary>
        public int Phase { get; }

        /// <summary>构造。</summary>
        public SystemPhaseAttribute(int phase)
        {
            Phase = phase;
        }
    }
}
