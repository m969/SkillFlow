using System;
using System.Collections.Generic;

namespace NetGame.Combat
{
    /// <summary>
    /// Ability 配置注册表：按 ID 注册和查询 <c>AbilityConfigData</c>。
    /// 接入方在加载配置数据后注册；本类只负责保存和查询，不依赖具体资源格式。
    /// 纯静态工具类，无状态。
    /// </summary>
    public static class AbilitySkillLib
    {
        /// <summary>AbilityId → 技能配置数据。</summary>
        private static readonly Dictionary<int, AbilityConfigData> Id2Configs = new Dictionary<int, AbilityConfigData>();

        /// <summary>注册表同步锁：<c>World</c> 构造 / 并行测试会在多线程写入，需保证静态字典线程安全。</summary>
        private static readonly object SyncRoot = new object();

        /// <summary>注册配置（同 Id 覆盖）。为 null 抛异常，避免掩盖配置缺失。</summary>
        public static void Register(AbilityConfigData config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            lock (SyncRoot)
            {
                Id2Configs[config.Id] = config;
            }
        }

        /// <summary>按 Id 注销。</summary>
        public static void Unregister(int abilityId)
        {
            lock (SyncRoot)
            {
                Id2Configs.Remove(abilityId);
            }
        }

        /// <summary>尝试获取配置。</summary>
        public static bool TryGet(int abilityId, out AbilityConfigData config)
        {
            lock (SyncRoot)
            {
                return Id2Configs.TryGetValue(abilityId, out config);
            }
        }

        /// <summary>清空全部注册（测试 / 热更重载时使用）。</summary>
        public static void Clear()
        {
            lock (SyncRoot)
            {
                Id2Configs.Clear();
            }
        }

        /// <summary>当前注册数量。</summary>
        public static int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return Id2Configs.Count;
                }
            }
        }
    }
}
