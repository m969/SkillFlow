using ECS;
using NetGame.Combat;

namespace NetGame.SkillFlow
{
    /// <summary>
    /// 技能编排解释器：无状态方法集合，读「数据图 Config + 运行时状态 NodeFrame」，
    /// 按 NodeType 分派控制语义并推进，写回 NodeFrame。
    /// 关键：状态全部在 Component，本类型无任何字段/属性（符合「System 只含方法」规范）；
    /// 时间由调用方按秒注入浮点 delta，不自行读取系统时钟。
    /// </summary>
    public static class SkillFlowInterpreter
    {
        // -------------------- 生命周期公开入口（由分端驱动调用） --------------------

        /// <summary>进入执行：构建运行时状态树（若未构建），随后推进根节点一次（处理纯同步技能）。</summary>
        public static NodeResult Enter(SkillExecuteComponent ec)
        {
            if (ec.RootFrame == null)
            {
                ec.RootFrame = BuildFrame(ec.Config.Root);
            }
            ec.IsStarted = true;
            return Step(ec.RootFrame, ec.Config.Root, ec.Context, ec.ActionHost, 0f);
        }

        /// <summary>每 tick / 每帧推进：根仍 Running 时递归推进；根已终态（如被取消）时驱动脱手子树（CancelScope 边界内仍运行的分支）。</summary>
        public static NodeResult Tick(SkillExecuteComponent ec, float delta)
        {
            if (ec.RootFrame.Status == NodeStatus.Running)
            {
                var result = Step(ec.RootFrame, ec.Config.Root, ec.Context, ec.ActionHost, delta);
                if (result.Status != NodeStatus.Running)
                {
                    ec.IsFinished = true;
                }
                return result;
            }

            // 根已终态：若存在脱手子树，继续驱动（如“火球发射后施法者死亡，火球流程继续”）
            if (HasRunningDetached(ec.RootFrame))
            {
                DriveDetached(ec.RootFrame, ec.Config.Root, ec, delta);
            }
            if (!HasRunningDetached(ec.RootFrame))
            {
                ec.IsFinished = true;
            }
            return NodeResult.Success();
        }

        /// <summary>退出执行：清理订阅句柄等资源。</summary>
        public static void Exit(SkillExecuteComponent ec, NodeResult result)
        {
            DisposeFrameSubscriptions(ec.RootFrame);
            DisposeContextSubscriptions(ec.Context);
            ec.IsFinished = true;
        }

        /// <summary>取消执行：递归取消节点树（先子后父，CancelScope 为边界）。若存在脱手子树（边界内仍运行）则不标记完成，交由驱动层继续推进。</summary>
        public static void Cancel(SkillExecuteComponent ec)
        {
            CancelFrame(ec.RootFrame);
            if (!HasRunningDetached(ec.RootFrame))
            {
                DisposeContextSubscriptions(ec.Context);
                ec.IsFinished = true;
            }
        }

        // -------------------- 构建运行时状态树（平行于数据图） --------------------

        private static NodeFrame BuildFrame(SkillFlowNodeConfig cfg)
        {
            var frame = new NodeFrame { Type = cfg.NodeType, Config = cfg };
            for (int i = 0; i < cfg.Children.Count; i++)
            {
                frame.ChildFrames.Add(BuildFrame(cfg.Children[i]));
            }
            return frame;
        }

        // -------------------- 统一推进：一个节点的一步 = 分派控制语义 --------------------

        private static NodeResult Step(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            // 节点首次进入：广播 Enter 事件（供表现层订阅；无执行载体则跳过）
            if (!frame.Entered)
            {
                frame.Entered = true;
                BroadcastNodeEnter(ctx, frame);
            }

            NodeResult result;
            switch (frame.Type)
            {
                case SkillFlowNodeType.Sequence: result = Sequence(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.Selector: result = Selector(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.Parallel: result = Parallel(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.Race: result = Race(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.If: result = If(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.Switch: result = Switch(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.Loop: result = Loop(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.ForEach: result = ForEach(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.Retry: result = Retry(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.Delay: result = Delay(frame, cfg, delta); break;
                case SkillFlowNodeType.WaitEvent: result = WaitEvent(frame, cfg, ctx, delta); break;
                case SkillFlowNodeType.Timeout: result = Timeout(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.CancelScope: result = CancelScope(frame, cfg, ctx, host, delta); break;
                case SkillFlowNodeType.SkillAction: result = host.Execute(ctx, cfg.StringParam, frame); break;
                case SkillFlowNodeType.AbilityEffect: result = AbilityEffect(frame, cfg, ctx); break;
                default: result = NodeResult.Failure("unknown node type " + frame.Type); break;
            }

            // 关键：同步推进节点的执行状态到其 NodeFrame，供 Parallel/Race 等组合语义读取「子节点是否完成」
            frame.Status = result.Status;

            // 节点达终态：广播 Exit 事件
            if (result.Status != NodeStatus.Running)
            {
                BroadcastNodeExit(ctx, frame, result);
            }
            return result;
        }

        /// <summary>
        /// Effect 节点：经 <c>ctx.EffectHost</c> 分派到接入方提供的效果宿主。
        /// 流程只传递效果载荷和运行状态；具体解释与执行策略由宿主定义。
        /// E4（配置驱动）：若节点未显式携带 <c>cfg.Effect</c>，则按 <c>ctx.SkillId</c> 从 <c>AbilitySkillLib</c>
        /// 读取 <c>AbilityConfigData.Effects</c>（支持单技能多效果），逐个执行 <c>Enabled</c> 的效果并聚合结果。
        /// </summary>
        private static NodeResult AbilityEffect(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx)
        {
            if (ctx.EffectHost == null)
            {
                return NodeResult.Failure("no ability effect host");
            }

            if (cfg.Effect != null)
            {
                return ctx.EffectHost.ExecuteEffect(ctx, cfg.Effect, frame);
            }

            // E4：配置驱动——按 SkillId 从 AbilitySkillLib 读取效果集合，逐个执行
            return ExecuteConfigEffects(frame, cfg, ctx);
        }

        /// <summary>
        /// E4：从 <c>AbilitySkillLib</c> 按 <c>ctx.SkillId</c> 读取 <c>AbilityConfigData.Effects</c>，
        /// 遍历执行所有 <c>Enabled</c> 的效果。任一 Failure 则整体 Failure，其余视为 Success（多效果顺序执行）。
        /// </summary>
        private static NodeResult ExecuteConfigEffects(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx)
        {
            if (!AbilitySkillLib.TryGet(ctx.SkillId, out var abilityConfig))
            {
                return NodeResult.Failure($"ability config not found for skill {ctx.SkillId}");
            }

            var effects = abilityConfig.Effects;
            if (effects == null || effects.Count == 0)
            {
                return NodeResult.Failure($"no effects registered for skill {ctx.SkillId}");
            }

            while (frame.EffectIndex < effects.Count)
            {
                var effect = effects[frame.EffectIndex];
                if (effect == null || !effect.Enabled)
                {
                    frame.EffectIndex++;
                    continue;
                }

                var result = ctx.EffectHost.ExecuteEffect(ctx, effect, frame);
                if (result.Status != NodeStatus.Success)
                {
                    return result;
                }

                frame.EffectIndex++;
            }
            return NodeResult.Success();
        }

        /// <summary>广播节点进入事件（经执行载体 Dispatch 到表现层；无执行载体则跳过）。</summary>
        private static void BroadcastNodeEnter(SkillFlowContext ctx, NodeFrame frame)
        {
            if (ctx.ExecutionEntity == null) return;
            ctx.ExecutionEntity.Dispatch<ISkillFlowNodeEnterHandle>(x => x.OnSkillFlowNodeEnter(frame, ctx));
        }

        /// <summary>广播节点退出事件（经执行载体 Dispatch 到表现层；无执行载体则跳过）。</summary>
        private static void BroadcastNodeExit(SkillFlowContext ctx, NodeFrame frame, NodeResult result)
        {
            if (ctx.ExecutionEntity == null) return;
            ctx.ExecutionEntity.Dispatch<ISkillFlowNodeExitHandle>(x => x.OnSkillFlowNodeExit(frame, ctx, result));
        }

        // -------------------- 控制语义：Sequence（示范"数据驱动、无多态"风格） --------------------

        /// <summary>顺序：子节点按序执行，任一失败则停止；全部 Success 则 Success。</summary>
        private static NodeResult Sequence(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            while (frame.ChildIndex < cfg.Children.Count)
            {
                var childCfg = cfg.Children[frame.ChildIndex];
                var childFrame = frame.ChildFrames[frame.ChildIndex];
                var r = Step(childFrame, childCfg, ctx, host, delta);

                // 子节点未完成 → 等待下一次 Tick
                if (r.Status == NodeStatus.Running)
                {
                    return NodeResult.Running();
                }

                // 子节点终止（失败/取消/超时）→ 透传结果并停止本序列
                if (r.Status != NodeStatus.Success)
                {
                    return r;
                }

                // 子节点成功 → 推进到下一个
                frame.ChildIndex++;
            }
            return NodeResult.Success();
        }

        // -------------------- 其余控制语义（骨架签名 + 最小占位，待实现遵循同一"分派"风格） --------------------

        /// <summary>选择：子节点按序尝试，直到一个成功即短路返回 Success；全部失败则 Failure。</summary>
        private static NodeResult Selector(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            while (frame.ChildIndex < cfg.Children.Count)
            {
                var childCfg = cfg.Children[frame.ChildIndex];
                var childFrame = frame.ChildFrames[frame.ChildIndex];
                var r = Step(childFrame, childCfg, ctx, host, delta);

                if (r.Status == NodeStatus.Running)
                {
                    return NodeResult.Running(); // 子节点未完成
                }
                if (r.Status == NodeStatus.Success)
                {
                    return NodeResult.Success(); // 成功短路
                }

                frame.ChildIndex++; // 失败/取消 → 尝试下一个
            }
            return NodeResult.Failure("selector: all failed");
        }

        /// <summary>并行：多子节点并发推进（每 tick 推进所有未完成分支）。完成策略由 cfg.BoolParam 决定：false=All（全部成功才结束），true=Any（任一成功即结束并取消其余）。</summary>
        private static NodeResult Parallel(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            bool any = cfg.BoolParam;
            int count = cfg.Children.Count;
            bool anySuccess = false;
            bool allTerminated = true;

            for (int i = 0; i < count; i++)
            {
                var child = frame.ChildFrames[i];
                if (child.Status == NodeStatus.Running)
                {
                    Step(child, cfg.Children[i], ctx, host, delta);
                }

                var st = child.Status;
                if (st == NodeStatus.Running) { allTerminated = false; }
                else if (st == NodeStatus.Success) { anySuccess = true; }
            }

            if (any)
            {
                // Any：任一成功 → 整体成功并取消其余仍在推进的分支
                if (anySuccess)
                {
                    CancelAllRunning(frame);
                    return NodeResult.Success();
                }
                if (allTerminated)
                {
                    return NodeResult.Failure("parallel any: no success");
                }
                return NodeResult.Running();
            }
            else
            {
                // All：任一终止（失败/取消/超时）→ 整体失败并取消其余；全部成功才 Success
                for (int i = 0; i < count; i++)
                {
                    var st = frame.ChildFrames[i].Status;
                    if (st != NodeStatus.Running && st != NodeStatus.Success)
                    {
                        CancelAllRunning(frame);
                        return new NodeResult(st);
                    }
                }
                if (allTerminated)
                {
                    return NodeResult.Success();
                }
                return NodeResult.Running();
            }
        }

        /// <summary>竞速：多分支竞争，最先达成目标结果者获胜并取消其余。由 cfg.BoolParam 决定：false=成功优先（RaceSuccess），true=失败优先（RaceFailure）。</summary>
        private static NodeResult Race(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            bool raceOnFailure = cfg.BoolParam;
            int count = cfg.Children.Count;

            for (int i = 0; i < count; i++)
            {
                var child = frame.ChildFrames[i];
                if (child.Status == NodeStatus.Running)
                {
                    Step(child, cfg.Children[i], ctx, host, delta);
                }

                var st = child.Status;
                bool win = raceOnFailure ? (st == NodeStatus.Failure)
                                         : (st == NodeStatus.Success);
                if (win)
                {
                    CancelAllRunning(frame);
                    return new NodeResult(st);
                }
            }

            // 无获胜者：所有分支都进入终态（无获胜者）→ 兜底失败；否则继续等待
            for (int i = 0; i < count; i++)
            {
                if (frame.ChildFrames[i].Status == NodeStatus.Running)
                {
                    return NodeResult.Running();
                }
            }
            return NodeResult.Failure("race no winner");
        }

        /// <summary>条件分支：读黑板布尔（cfg.StringParam 为键），true 走 then（Children[0]），false 走 else（Children[1]，可无）。
        /// 首次评估时锁定分支（frame.Subscribed），避免异步分支执行中途因条件变化而跳支。</summary>
        private static NodeResult If(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            // 首次：根据黑板条件锁定分支（0=then，1=else）
            if (!frame.Subscribed)
            {
                frame.Subscribed = true;
                bool cond = SkillFlowContextSystem.GetVariable<bool>(ctx, cfg.StringParam, false);
                frame.ChildIndex = cond ? 0 : 1;
            }

            // else 分支缺失 → 无事发生（直接完成）
            if (frame.ChildIndex == 1 && cfg.Children.Count < 2)
            {
                return NodeResult.Success();
            }
            if (frame.ChildIndex >= cfg.Children.Count)
            {
                return NodeResult.Failure("if branch missing");
            }

            // 推进选中分支
            return Step(frame.ChildFrames[frame.ChildIndex], cfg.Children[frame.ChildIndex], ctx, host, delta);
        }

        /// <summary>多路分支：读黑板整数值（cfg.StringParam 为键），匹配子节点 IntParam 相等的分支；无匹配时走最后一个 child（default 分支）。</summary>
        private static NodeResult Switch(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            // 首次：根据黑板选择器匹配分支
            if (!frame.Subscribed)
            {
                frame.Subscribed = true;
                int selector = SkillFlowContextSystem.GetVariable<int>(ctx, cfg.StringParam, 0);
                int idx = -1;
                for (int i = 0; i < cfg.Children.Count; i++)
                {
                    if (cfg.Children[i].IntParam == selector)
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx < 0)
                {
                    // 无匹配 → 约定最后一个 child 为 default 分支；若无 child 则无事发生
                    idx = cfg.Children.Count - 1;
                }
                frame.ChildIndex = idx;
            }

            if (frame.ChildIndex < 0 || frame.ChildIndex >= cfg.Children.Count)
            {
                return NodeResult.Success(); // 无匹配且无默认分支
            }

            return Step(frame.ChildFrames[frame.ChildIndex], cfg.Children[frame.ChildIndex], ctx, host, delta);
        }

        /// <summary>循环：重复执行子节点[0] 共 cfg.IntParam 次；循环体复用（ResetFrame），写入 _index 到局部。</summary>
        private static NodeResult Loop(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            if (cfg.Children.Count < 1)
            {
                return NodeResult.Failure("loop body missing");
            }

            var max = cfg.IntParam;
            var body = frame.ChildFrames[0];
            var bodyCfg = cfg.Children[0];

            while (frame.ChildIndex < max)
            {
                if (body.Status != NodeStatus.Running)
                {
                    frame.Locals["_index"] = frame.ChildIndex;
                    ResetFrame(body);
                }

                var r = Step(body, bodyCfg, ctx, host, delta);
                if (r.Status == NodeStatus.Running)
                {
                    return NodeResult.Running(); // 循环体未完成
                }
                if (r.Status != NodeStatus.Success)
                {
                    return r; // 失败 / 取消传播
                }

                frame.ChildIndex++; // 完成一次迭代
            }
            return NodeResult.Success();
        }

        /// <summary>遍历：从黑板读取集合（cfg.StringParam 为键），对每个元素执行子节点[0]；当前 item 写入局部黑板 `_item`。</summary>
        private static NodeResult ForEach(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            // 首次初始化：读取源集合并缓存到局部（避免每 tick 重复读黑板）
            if (!frame.Subscribed)
            {
                frame.Subscribed = true;
                frame.Locals["_items"] = SkillFlowContextSystem.GetVariable<System.Collections.IList>(ctx, cfg.StringParam);
            }

            var items = frame.Locals["_items"] as System.Collections.IList;
            if (items == null)
            {
                return NodeResult.Failure("foreach source missing: " + cfg.StringParam);
            }
            if (cfg.Children.Count == 0)
            {
                return NodeResult.Failure("foreach body missing");
            }

            var body = frame.ChildFrames[0];
            var bodyCfg = cfg.Children[0];

            while (frame.ChildIndex < items.Count)
            {
                // 循环体未开始或已结束上一迭代 → 进入新 item（写入 item + 重置循环体）
                if (body.Status != NodeStatus.Running)
                {
                    frame.Locals["_item"] = items[frame.ChildIndex];
                    ResetFrame(body);
                }

                // 推进循环体（当前 item）
                var r = Step(body, bodyCfg, ctx, host, delta);
                if (r.Status == NodeStatus.Running)
                {
                    return NodeResult.Running(); // 当前 item 未完成
                }
                if (r.Status != NodeStatus.Success)
                {
                    return r; // 失败 / 取消传播
                }

                frame.ChildIndex++; // 当前 item 完成 → 下一个
            }
            return NodeResult.Success();
        }

        /// <summary>重置节点运行时状态（ForEach 进入新迭代前复用循环体 frame）。</summary>
        private static void ResetFrame(NodeFrame f)
        {
            DisposeFrameSubscriptions(f);
            f.Status = NodeStatus.Running;
            f.ChildIndex = 0;
            f.EffectIndex = 0;
            f.Timer = 0f;
            f.RetryCount = 0;
            f.Subscribed = false;
            f.EventReceived = false;
            f.Locals.Clear();
            for (int i = 0; i < f.ChildFrames.Count; i++)
            {
                ResetFrame(f.ChildFrames[i]);
            }
        }

        /// <summary>重试：子节点失败后重试（最多 cfg.IntParam 次），成功即结束；超限透传失败。循环体重置，写入 _attempt 到局部。</summary>
        private static NodeResult Retry(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            if (cfg.Children.Count < 1)
            {
                return NodeResult.Failure("retry body missing");
            }

            var maxRetries = cfg.IntParam;
            var body = frame.ChildFrames[0];
            var bodyCfg = cfg.Children[0];

            while (true)
            {
                if (body.Status != NodeStatus.Running)
                {
                    frame.Locals["_attempt"] = frame.RetryCount;
                    ResetFrame(body);
                }

                var r = Step(body, bodyCfg, ctx, host, delta);
                if (r.Status == NodeStatus.Running)
                {
                    return NodeResult.Running(); // 异步循环体挂起
                }
                if (r.Status == NodeStatus.Success)
                {
                    return NodeResult.Success(); // 成功结束
                }

                if (frame.RetryCount >= maxRetries)
                {
                    return r; // 超过重试上限，透传失败
                }

                frame.RetryCount++; // 准备下次重试（循环继续）
            }
        }

        /// <summary>延迟：累计秒数，到点返回 Success；被取消时返回 Cancelled。</summary>
        private static NodeResult Delay(NodeFrame f, SkillFlowNodeConfig c, float d)
        {
            f.Timer += d;
            if (f.Timer >= c.FloatParam)
            {
                return NodeResult.Success();
            }
            return NodeResult.Running();
        }

        /// <summary>
        /// 事件等待：优先订阅注入的 <see cref="ISkillEventSource"/>，事件回调到达后完成；
        /// 未注入事件源时继续轮询黑板布尔信号，以兼容 headless 测试和旧调用方。
        /// </summary>
        private static NodeResult WaitEvent(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, float delta)
        {
            if (!frame.Subscribed)
            {
                frame.Subscribed = true;
                if (ctx.EventSource != null)
                {
                    frame.EventSubscription = ctx.EventSource.Subscribe(
                        cfg.StringParam,
                        () =>
                        {
                            CaptureEventTarget(ctx);
                            frame.EventReceived = true;
                        });
                }
            }

            bool signal = SkillFlowContextSystem.GetVariable<bool>(ctx, cfg.StringParam, false);
            if (frame.EventReceived || signal)
            {
                DisposeFrameSubscription(frame);
                return NodeResult.Success();
            }
            return NodeResult.Running();
        }

        /// <summary>
        /// 捕获事件源提供的目标载荷。
        /// 这里只更新 SkillFlow 上下文，不执行任何效果；具体效果仍由后续 AbilityEffect 节点和外部宿主处理。
        /// </summary>
        private static void CaptureEventTarget(SkillFlowContext ctx)
        {
            if (!(ctx.EventSource is ISkillEventTargetSource targetSource))
            {
                return;
            }

            if (targetSource.LastTargetEntity == null)
            {
                return;
            }

            ctx.PrimaryTarget = targetSource.LastTargetEntity;
            ctx.Variables["hitTarget"] = targetSource.LastTargetEntity;
        }

        /// <summary>超时包裹：子节点先结束 → 透传其结果；累计超时（cfg.FloatParam 秒）→ 取消子节点并返回 Timeout。</summary>
        private static NodeResult Timeout(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            if (cfg.Children.Count < 1)
            {
                return NodeResult.Failure("timeout body missing");
            }

            var body = frame.ChildFrames[0];
            var bodyCfg = cfg.Children[0];

            // 子节点先完成 → 透传其结果（成功/失败）
            var bodyResult = Step(body, bodyCfg, ctx, host, delta);
            if (bodyResult.Status != NodeStatus.Running)
            {
                return bodyResult;
            }

            // 子节点仍在推进：累计超时帧，超时则取消子节点并返回 Timeout
            frame.Timer += delta;
            if (frame.Timer >= cfg.FloatParam)
            {
                CancelFrame(body);
                return NodeResult.Timeout("timeout");
            }

            return NodeResult.Running();
        }

        /// <summary>取消作用域：仅作“包裹 + 取消传播边界”，正常推进交给子树；边界拦截逻辑见 CancelFrame。</summary>
        private static NodeResult CancelScope(NodeFrame frame, SkillFlowNodeConfig cfg, SkillFlowContext ctx, ISkillActionHost host, float delta)
        {
            if (cfg.Children.Count < 1)
            {
                return NodeResult.Failure("cancelscope body missing");
            }
            return Step(frame.ChildFrames[0], cfg.Children[0], ctx, host, delta);
        }

        /// <summary>取消当前仍处于 Running 的所有子节点（供 Parallel/Race 在获胜/失败时清理其余分支）。</summary>
        private static void CancelAllRunning(NodeFrame frame)
        {
            for (int i = 0; i < frame.ChildFrames.Count; i++)
            {
                var child = frame.ChildFrames[i];
                if (child.Status == NodeStatus.Running)
                {
                    CancelFrame(child);
                }
            }
        }

        /// <summary>释放单个节点的事件订阅句柄；重复调用安全。</summary>
        private static void DisposeFrameSubscription(NodeFrame frame)
        {
            if (frame.EventSubscription == null)
            {
                return;
            }

            frame.EventSubscription.Dispose();
            frame.EventSubscription = null;
            frame.EventReceived = false;
        }

        /// <summary>递归释放节点树中的所有事件订阅，包括取消作用域内的脱手分支。</summary>
        private static void DisposeFrameSubscriptions(NodeFrame frame)
        {
            DisposeFrameSubscription(frame);
            for (int i = 0; i < frame.ChildFrames.Count; i++)
            {
                DisposeFrameSubscriptions(frame.ChildFrames[i]);
            }
        }

        /// <summary>释放上下文登记的外部订阅句柄。</summary>
        private static void DisposeContextSubscriptions(SkillFlowContext context)
        {
            foreach (var subscription in context.Subscriptions)
            {
                subscription.Dispose();
            }
            context.Subscriptions.Clear();
        }

        /// <summary>是否存在仍 Running 的脱手子树（取消边界内继续运行的节点）。供驱动层判断是否继续推进。</summary>
        public static bool HasRunningDetached(NodeFrame frame)
        {
            if (frame.Status == NodeStatus.Running)
            {
                return true;
            }
            for (int i = 0; i < frame.ChildFrames.Count; i++)
            {
                if (HasRunningDetached(frame.ChildFrames[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>驱动取消边界内仍运行（脱手）的子树：遍历所有 CancelScope，推进其仍 Running 的子节点一次（Step 会递归推进该子树内部）。</summary>
        private static void DriveDetached(NodeFrame frame, SkillFlowNodeConfig cfg, SkillExecuteComponent ec, float delta)
        {
            if (frame.Type == SkillFlowNodeType.CancelScope)
            {
                for (int i = 0; i < frame.ChildFrames.Count; i++)
                {
                    if (frame.ChildFrames[i].Status == NodeStatus.Running)
                    {
                        Step(frame.ChildFrames[i], cfg.Children[i], ec.Context, ec.ActionHost, delta);
                    }
                }
                return;
            }

            for (int i = 0; i < frame.ChildFrames.Count; i++)
            {
                if (frame.ChildFrames[i].Status == NodeStatus.Running)
                {
                    DriveDetached(frame.ChildFrames[i], cfg.Children[i], ec, delta);
                }
            }
        }

        // -------------------- 取消传播：先子后父；CancelScope 为取消边界（子树不被取消） --------------------

        private static void CancelFrame(NodeFrame f)
        {
            if (f.Status != NodeStatus.Running)
            {
                DisposeFrameSubscription(f);
                return;
            }

            // 取消边界：本节点标记取消，但子树保持运行（由驱动继续 Tick 驱动脱手子树）
            if (f.Type == SkillFlowNodeType.CancelScope)
            {
                f.Status = NodeStatus.Cancelled;
                DisposeFrameSubscription(f);
                return;
            }

            // 递归取消仍 Running 的子节点（先子后父）
            for (int i = 0; i < f.ChildFrames.Count; i++)
            {
                CancelFrame(f.ChildFrames[i]);
            }
            DisposeFrameSubscription(f);
            f.Status = NodeStatus.Cancelled;
        }
    }
}
