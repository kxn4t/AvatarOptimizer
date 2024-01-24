using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    /// <summary>
    /// This class represents a node in the property modification tree.
    ///
    /// In AnimatorParser V2, Modifications of each property are represented as a tree to make it possible to
    /// remove modifications of a property.
    ///
    /// This class is the abstract class for the nodes.
    ///
    /// Most nodes are immutable but some nodes are mutable.
    /// </summary>
    internal abstract class PropModNode<T> : IErrorContext
    {
        /// <summary>
        /// Returns true if this node is always applied. For inactive nodes, this returns false.
        /// </summary>
        public abstract bool AppliedAlways { get; }
        public abstract ValueInfo<T> Value { get; }
        public abstract IEnumerable<ObjectReference> ContextReferences { get; }
    }

    /// <summary>
    /// The abstract information about actual value of PropModNode.
    ///
    /// This struct currently only handles single constant value, but will handle more complex values in the future.
    /// </summary>
    internal readonly struct ValueInfo<T>
    {
        public bool IsConstant { get; }
        private readonly T _value;

        public T ConstantValue
        {
            get
            {
                if (!IsConstant) throw new InvalidOperationException("Not Constant");
                return _value;
            }
        }

        public static ValueInfo<T> Variable => default;

        public ValueInfo(T value)
        {
            _value = value;
            IsConstant = true;
        }

        public bool TryGetConstantValue(out T o)
        {
            o = _value;
            return IsConstant;
        }

        public bool Equals(ValueInfo<T> other) =>
            IsConstant == other.IsConstant &&
            (!IsConstant || EqualityComparer<T>.Default.Equals(_value, other._value));

        public override bool Equals(object obj) => obj is ValueInfo<T> other && Equals(other);
        public override int GetHashCode() => !IsConstant ? 0 : EqualityComparer<T>.Default.GetHashCode(_value);
    }

    internal static class NodeImplUtils
    {
        public static ValueInfo<T> ConstantInfoForSideBySide<T>(IEnumerable<PropModNode<T>> nodes)
        {
            using (var enumerator = nodes.GetEnumerator())
            {
                Debug.Assert(enumerator.MoveNext());

                if (!enumerator.Current.Value.TryGetConstantValue(out var value))
                    return ValueInfo<T>.Variable;

                while (enumerator.MoveNext())
                {
                    if (!enumerator.Current.Value.TryGetConstantValue(out var otherValue))
                        return ValueInfo<T>.Variable;

                    if (!EqualityComparer<T>.Default.Equals(value, otherValue))
                        return ValueInfo<T>.Variable;
                }

                return new ValueInfo<T>(value);
            }
        }

        public static bool AlwaysAppliedForOverriding<T, TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer<T>
        {
            return layersReversed.Any(x =>
                x.Weight == AnimatorWeightState.AlwaysOne && x.BlendingMode == AnimatorLayerBlendingMode.Override &&
                x.Node.AppliedAlways);
        }

        public static ValueInfo<T> ConstantInfoForOverriding<T, TLayer>(IEnumerable<TLayer> layersReversed)
            where TLayer : ILayer<T>
        {
            T value = default;
            bool initialized = false;

            foreach (var layer in layersReversed)
            {
                switch (layer.Weight)
                {
                    case AnimatorWeightState.AlwaysOne:
                    case AnimatorWeightState.EitherZeroOrOne:
                        if (!layer.Node.Value.TryGetConstantValue(out var otherValue)) return ValueInfo<T>.Variable;

                        if (layer.Node.AppliedAlways && layer.Weight == AnimatorWeightState.AlwaysOne &&
                            layer.BlendingMode == AnimatorLayerBlendingMode.Override)
                        {
                            // the layer is always applied at the highest property.
                            return new ValueInfo<T>(otherValue);
                        }

                        // partially applied constants so save that value and continue.
                        if (!initialized)
                        {
                            value = otherValue;
                            initialized = true;
                        }
                        else
                        {
                            if (!EqualityComparer<T>.Default.Equals(value, otherValue))
                                return ValueInfo<T>.Variable;
                        }

                        break;
                    case AnimatorWeightState.Variable:
                        return ValueInfo<T>.Variable;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new ValueInfo<T>(value);
        }
    }

    internal interface ILayer<T>
    {
        AnimatorWeightState Weight { get; }
        AnimatorLayerBlendingMode BlendingMode { get; }
        PropModNode<T> Node { get; }
    }

    internal sealed class RootPropModNode<T> : PropModNode<T>, IErrorContext
    {
        readonly struct ComponentInfo
        {
            public readonly ComponentPropModNode<T> Node;
            public readonly bool AlwaysApplied;

            public bool AppliedAlways => AlwaysApplied && Node.AppliedAlways;
            public IEnumerable<ObjectReference> ContextReferences => Node.ContextReferences;
            public Component Component => Node.Component;

            public ComponentInfo(ComponentPropModNode<T> node, bool alwaysApplied)
            {
                Node = node;
                AlwaysApplied = alwaysApplied;
            }
        }

        private readonly List<ComponentInfo> _children = new List<ComponentInfo>();

        public override bool AppliedAlways => _children.All(x => x.AppliedAlways);
        public override IEnumerable<ObjectReference> ContextReferences => _children.SelectMany(x => x.ContextReferences);
        public override ValueInfo<T> Value => NodeImplUtils.ConstantInfoForSideBySide(_children.Select(x => x.Node));

        public bool IsEmpty => _children.Count == 0;

        public IEnumerable<Component> SourceComponents => _children.Select(x => x.Component);

        public void Add(ComponentPropModNode<T> node, bool alwaysApplied)
        {
            _children.Add(new ComponentInfo(node, alwaysApplied));
            DestroyTracker.Track(node.Component, OnDestroy);
        }
        
        public void Add([NotNull] RootPropModNode<T> toAdd)
        {
            if (toAdd == null) throw new ArgumentNullException(nameof(toAdd));
            foreach (var child in toAdd._children)
                Add(child.Node, child.AppliedAlways);
        }

        private void OnDestroy(int objectId)
        {
            _children.RemoveAll(x => x.Component.GetInstanceID() == objectId);
        }

        public void Invalidate()
        {
            foreach (var componentInfo in _children)
                DestroyTracker.Untrack(componentInfo.Component, OnDestroy);
            _children.Clear();
        }

        [CanBeNull] public RootPropModNode<T> Normalize() => IsEmpty ? null : this;
    }

    internal abstract class ImmutablePropModNode<T> : PropModNode<T>
    {
    }

    internal class FloatAnimationCurveNode : ImmutablePropModNode<float>
    {
        public AnimationCurve Curve { get; }
        public AnimationClip Clip { get; }

        [CanBeNull]
        public static FloatAnimationCurveNode Create([NotNull] AnimationClip clip, EditorCurveBinding binding)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null) return null;
            if (curve.keys.Length == 0) return null;
            return new FloatAnimationCurveNode(clip, curve);
        }

        private FloatAnimationCurveNode(AnimationClip clip, AnimationCurve curve)
        {
            Debug.Assert(curve.keys.Length > 0);
            Clip = clip;
            Curve = curve;
            _constantInfo = new Lazy<ValueInfo<float>>(() => ParseProperty(curve), isThreadSafe: false);
        }

        private readonly Lazy<ValueInfo<float>> _constantInfo;

        public override bool AppliedAlways => true;
        public override ValueInfo<float> Value => _constantInfo.Value;
        public override IEnumerable<ObjectReference> ContextReferences => new []{ ObjectRegistry.GetReference(Clip) };

        private static ValueInfo<float> ParseProperty(AnimationCurve curve)
        {
            if (curve.keys.Length == 1) return new ValueInfo<float>(curve.keys[0].value);

            float constValue = 0;
            foreach (var (preKey, postKey) in curve.keys.ZipWithNext())
            {
                var preWeighted = preKey.weightedMode == WeightedMode.Out || preKey.weightedMode == WeightedMode.Both;
                var postWeighted = postKey.weightedMode == WeightedMode.In || postKey.weightedMode == WeightedMode.Both;

                if (preKey.value.CompareTo(postKey.value) != 0) return ValueInfo<float>.Variable;
                constValue = preKey.value;
                // it's constant
                if (float.IsInfinity(preKey.outWeight) || float.IsInfinity(postKey.inTangent)) continue;
                if (preKey.outTangent == 0 && postKey.inTangent == 0) continue;
                if (preWeighted && postWeighted && preKey.outWeight == 0 && postKey.inWeight == 0) continue;
                return ValueInfo<float>.Variable;
            }

            return new ValueInfo<float>(constValue);
        }
    }

    internal class BlendTreeNode<T> : ImmutablePropModNode<T>
    {
        private readonly IEnumerable<ImmutablePropModNode<T>> _children;
        private readonly BlendTreeType _blendTreeType;
        private readonly bool _partial;

        public BlendTreeNode(IEnumerable<ImmutablePropModNode<T>> children, BlendTreeType blendTreeType, bool partial)
        {
            // expected to pass list or array
            // ReSharper disable once PossibleMultipleEnumeration
            Debug.Assert(children.Any());
            // ReSharper disable once PossibleMultipleEnumeration
            _children = children;
            _blendTreeType = blendTreeType;
            _partial = partial;
        }


        private bool WeightSumIsOne => _blendTreeType != BlendTreeType.Direct;

        public override bool AppliedAlways => WeightSumIsOne && !_partial && _children.All(x => x.AppliedAlways);
        public override ValueInfo<T> Value => !WeightSumIsOne
            ? ValueInfo<T>.Variable
            : NodeImplUtils.ConstantInfoForSideBySide(_children);

        public override IEnumerable<ObjectReference> ContextReferences =>
            _children.SelectMany(x => x.ContextReferences);
    }

    abstract class ComponentPropModNode<T> : PropModNode<T>
    {
        protected ComponentPropModNode([NotNull] Component component)
        {
            if (!component) throw new ArgumentNullException(nameof(component));
            Component = component;
        }

        public Component Component { get; }

        public override IEnumerable<ObjectReference> ContextReferences => new [] { ObjectRegistry.GetReference(Component) };
    }

    class VariableComponentPropModNode<T> : ComponentPropModNode<T>
    {
        public VariableComponentPropModNode([NotNull] Component component) : base(component)
        {
        }

        public override bool AppliedAlways => false;
        public override ValueInfo<T> Value => ValueInfo<T>.Variable;
    }

    class AnimationComponentPropModNode<T> : ComponentPropModNode<T>
    {
        private readonly ImmutablePropModNode<T> _animation;

        public AnimationComponentPropModNode([NotNull] Component component, ImmutablePropModNode<T> animation) : base(component)
        {
            _animation = animation;
            _constantInfo = new Lazy<ValueInfo<T>>(() => animation.Value, isThreadSafe: false);
        }

        private readonly Lazy<ValueInfo<T>> _constantInfo;

        public override bool AppliedAlways => true;
        public override ValueInfo<T> Value => _constantInfo.Value;

        public override IEnumerable<ObjectReference> ContextReferences =>
            base.ContextReferences.Concat(_animation.ContextReferences);
    }
}
