using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Xeek
{
    public abstract class XeeksDogAnimationsBase : IEnumerable<AnimationClip>, IEnumerable
    {
        public abstract void SetupAnimations(Animator _animator);

        public IEnumerator<AnimationClip> GetEnumerator()
        {
            var thisObject = this;
            var fieldInfos = GetType().GetFields().Where(fi => fi.FieldType == typeof(AnimationClip));
            return fieldInfos.Select(fi => fi.GetValue(thisObject) as AnimationClip).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Dictionary<string, AnimationClip> ToDictionary()
        {
            var thisObject = this;
            var fieldInfos = GetType().GetFields().Where(fi => fi.FieldType == typeof(AnimationClip));
            return fieldInfos.ToDictionary(fi => fi.Name, fi => fi.GetValue(thisObject) as AnimationClip);
        }
    }

    /// <summary>
    /// These are the animation clips intended to be used with XeeksDogAnimationController and the order matters since
    /// these go into the blend tree in the order that they're declared.
    /// </summary>
    public class XeeksDogLocomotionAnimations : XeeksDogAnimationsBase
    {
        public AnimationClip walkForward;
        public AnimationClip walkLeft;
        public AnimationClip walkRight;
        public AnimationClip trotForward;
        public AnimationClip trotLeft;
        public AnimationClip trotRight;
        public AnimationClip runForward;
        public AnimationClip runLeft;
        public AnimationClip runRight;
        public AnimationClip turnRight;
        public AnimationClip turnLeft;
        public AnimationClip walkBack;
        public AnimationClip walkBackLeft;
        public AnimationClip walkBackRight;
        public AnimationClip idle;

        public override void SetupAnimations(Animator _animator)
        {
            AnimatorController controller;
            AnimatorControllerLayer baseLayer;
            ChildAnimatorState locomotionState;
            BlendTree blendTree = null;

            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("Unable to configure locomotion animations because the animator lacks a controller", _animator);
            }

            if (null == (controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(_animator.runtimeAnimatorController))))
            {
                Debug.LogWarning("Unable to configure locomotion animations because the animation controller couldn't be located in your assets", _animator);
                return;
            }

            try
            {
                baseLayer = controller.layers.FirstOrDefault();
                locomotionState = baseLayer.stateMachine.states.FirstOrDefault(s => string.Compare(s.state.name, "Locomotion", ignoreCase: true) == 0);
                blendTree = (BlendTree)locomotionState.state.motion;
            }
            catch
            {
                Debug.LogWarning($"The animation controller in this object's animator isn't the correct controller", _animator);
                return;
            }

            if (blendTree.children.Length < 15) Debug.LogWarning("The blend tree for this animation doesn't have the right number of motion children", _animator);

            // The children cannot be set directly as the array in the tree is just a copy
            var blendTreeChildren = blendTree.children;
            int index = 0;
            foreach (var clip in this)
            {
                if (index < blendTreeChildren.Length) blendTreeChildren[index].motion = clip;
                index++;
            }
            blendTree.children = blendTreeChildren;
        }
    }

    public class XeeksDogIdleAnimations : XeeksDogAnimationsBase
    {
        public AnimationClip idle1;
        public AnimationClip idle2;
        public AnimationClip idle3;
        public AnimationClip idle4;
        public AnimationClip idle5;

        public override void SetupAnimations(Animator _animator)
        {
            AnimatorController controller;
            AnimatorControllerLayer baseLayer;
            ChildAnimatorStateMachine idleState;

            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("Unable to configure idle animations because the animator lacks a controller", _animator);
            }

            if (null == (controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(_animator.runtimeAnimatorController))))
            {
                Debug.LogWarning("Unable to configure idle animations because the animation controller couldn't be located in your assets", _animator);
                return;
            }

            try
            {
                baseLayer = controller.layers.FirstOrDefault();
                idleState = baseLayer.stateMachine.stateMachines.FirstOrDefault(s => string.Compare(s.stateMachine.name, "Idle", ignoreCase: true) == 0);

                var updatedStateMachineChildren = idleState.stateMachine.states.Select(child =>
                {
                    var motion = ToDictionary().FirstOrDefault(x => x.Key.ToLower().Contains(child.state.name.ToLower())).Value;
                    if (motion != null) child.state.motion = motion;
                    return child;
                });

                // The children cannot be set directly as the array in the tree is just a copy
                idleState.stateMachine.states = updatedStateMachineChildren.ToArray();
            }
            catch
            {
                Debug.LogWarning($"The animation controller in this object's animator isn't the correct controller", _animator);
                return;
            }
        }
    }
}
