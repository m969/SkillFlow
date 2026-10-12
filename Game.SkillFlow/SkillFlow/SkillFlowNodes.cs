using System;
using System.Collections.Generic;
using NetGame.Combat;

namespace NetGame.SkillFlow
{
    /// <summary>用于组合可序列化 SkillFlow 节点配置的工厂方法。</summary>
    public static class SkillFlowNodes
    {
        public static SkillFlowNodeConfig Sequence(params SkillFlowNodeConfig[] children)
        {
            return Composite(SkillFlowNodeType.Sequence, children);
        }

        public static SkillFlowNodeConfig Selector(params SkillFlowNodeConfig[] children)
        {
            return Composite(SkillFlowNodeType.Selector, children);
        }

        public static SkillFlowNodeConfig ParallelAll(params SkillFlowNodeConfig[] children)
        {
            return Parallel(false, children);
        }

        public static SkillFlowNodeConfig ParallelAny(params SkillFlowNodeConfig[] children)
        {
            return Parallel(true, children);
        }

        public static SkillFlowNodeConfig RaceSuccess(params SkillFlowNodeConfig[] children)
        {
            return Race(false, children);
        }

        public static SkillFlowNodeConfig RaceFailure(params SkillFlowNodeConfig[] children)
        {
            return Race(true, children);
        }

        public static SkillFlowNodeConfig If(
            string conditionKey,
            SkillFlowNodeConfig whenTrue,
            SkillFlowNodeConfig whenFalse)
        {
            ValidateKey(conditionKey, nameof(conditionKey));
            var node = Composite(SkillFlowNodeType.If, whenTrue, whenFalse);
            node.StringParam = conditionKey;
            return node;
        }

        public static SkillFlowNodeConfig Switch(
            string selectorKey,
            SkillFlowNodeConfig defaultBranch,
            params (int Value, SkillFlowNodeConfig Branch)[] cases)
        {
            ValidateKey(selectorKey, nameof(selectorKey));
            if (defaultBranch == null)
            {
                throw new ArgumentNullException(nameof(defaultBranch));
            }
            if (cases == null)
            {
                throw new ArgumentNullException(nameof(cases));
            }

            var caseValues = new HashSet<int>();
            var children = new SkillFlowNodeConfig[cases.Length + 1];
            for (var i = 0; i < cases.Length; i++)
            {
                var (value, branch) = cases[i];
                if (branch == null)
                {
                    throw new ArgumentException("Switch case branches cannot be null.", nameof(cases));
                }
                if (!caseValues.Add(value))
                {
                    throw new ArgumentException("Switch case values must be unique.", nameof(cases));
                }

                branch.IntParam = value;
                children[i] = branch;
            }

            children[children.Length - 1] = defaultBranch;
            var node = Composite(SkillFlowNodeType.Switch, children);
            node.StringParam = selectorKey;
            return node;
        }

        public static SkillFlowNodeConfig Loop(int count, SkillFlowNodeConfig body)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Loop count cannot be negative.");
            }

            var node = Unary(SkillFlowNodeType.Loop, body);
            node.IntParam = count;
            return node;
        }

        public static SkillFlowNodeConfig ForEach(string collectionKey, SkillFlowNodeConfig body)
        {
            return KeyedUnary(SkillFlowNodeType.ForEach, collectionKey, body);
        }

        public static SkillFlowNodeConfig Retry(int maxRetries, SkillFlowNodeConfig body)
        {
            if (maxRetries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "Retry count cannot be negative.");
            }

            var node = Unary(SkillFlowNodeType.Retry, body);
            node.IntParam = maxRetries;
            return node;
        }

        public static SkillFlowNodeConfig Delay(float seconds)
        {
            ValidateDuration(seconds, nameof(seconds));
            var node = new SkillFlowNodeConfig { NodeType = SkillFlowNodeType.Delay };
            node.FloatParam = seconds;
            return node;
        }

        public static SkillFlowNodeConfig WaitEvent(string eventKey)
        {
            ValidateKey(eventKey, nameof(eventKey));
            var node = new SkillFlowNodeConfig { NodeType = SkillFlowNodeType.WaitEvent };
            node.StringParam = eventKey;
            return node;
        }

        public static SkillFlowNodeConfig Timeout(float seconds, SkillFlowNodeConfig body)
        {
            ValidateDuration(seconds, nameof(seconds));
            var node = Unary(SkillFlowNodeType.Timeout, body);
            node.FloatParam = seconds;
            return node;
        }

        public static SkillFlowNodeConfig CancelScope(SkillFlowNodeConfig body)
        {
            return Unary(SkillFlowNodeType.CancelScope, body);
        }

        public static SkillFlowNodeConfig Action(string actionKey)
        {
            ValidateKey(actionKey, nameof(actionKey));
            var node = new SkillFlowNodeConfig { NodeType = SkillFlowNodeType.SkillAction };
            node.StringParam = actionKey;
            return node;
        }

        public static SkillFlowNodeConfig AbilityEffect()
        {
            return new SkillFlowNodeConfig { NodeType = SkillFlowNodeType.AbilityEffect };
        }

        public static SkillFlowNodeConfig AbilityEffect(Effect effect)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            var node = AbilityEffect();
            node.Effect = effect;
            return node;
        }

        private static SkillFlowNodeConfig Parallel(bool completeOnAny, params SkillFlowNodeConfig[] children)
        {
            var node = Composite(SkillFlowNodeType.Parallel, children);
            node.BoolParam = completeOnAny;
            return node;
        }

        private static SkillFlowNodeConfig Race(bool failOnFailure, params SkillFlowNodeConfig[] children)
        {
            var node = Composite(SkillFlowNodeType.Race, children);
            node.BoolParam = failOnFailure;
            return node;
        }

        private static SkillFlowNodeConfig Composite(
            SkillFlowNodeType nodeType,
            params SkillFlowNodeConfig[] children)
        {
            if (children == null)
            {
                throw new ArgumentNullException(nameof(children));
            }

            var node = new SkillFlowNodeConfig { NodeType = nodeType };
            foreach (var child in children)
            {
                if (child == null)
                {
                    throw new ArgumentException("Child nodes cannot be null.", nameof(children));
                }

                node.Children.Add(child);
            }

            return node;
        }

        private static SkillFlowNodeConfig Unary(SkillFlowNodeType nodeType, SkillFlowNodeConfig child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            return Composite(nodeType, child);
        }

        private static SkillFlowNodeConfig KeyedUnary(
            SkillFlowNodeType nodeType,
            string key,
            SkillFlowNodeConfig child)
        {
            ValidateKey(key, nameof(key));
            var node = Unary(nodeType, child);
            node.StringParam = key;
            return node;
        }

        private static void ValidateKey(string key, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or whitespace.", parameterName);
            }
        }

        private static void ValidateDuration(float seconds, string parameterName)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Duration must be a finite, non-negative value.");
            }
        }
    }
}
