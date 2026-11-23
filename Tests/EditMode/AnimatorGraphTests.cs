using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Guards the death animation: every layer must be able to reach its death state from anywhere.
public class AnimatorGraphTests
{
    [TestCase("Assets/Animations/PlayerAnimatorController.controller")]
    [TestCase("Assets/Animations/EnemyAnimatorController.controller")]
    public void EveryLayerHasAnAnyStateTransitionOnDead(string path)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        Assert.That(controller, Is.Not.Null);

        foreach (var layer in controller.layers)
        {
            var toDead = layer.stateMachine.anyStateTransitions.Where(t =>
                t.conditions.Any(c => c.parameter == "Dead" && c.mode == AnimatorConditionMode.If)).ToList();
            Assert.That(toDead, Is.Not.Empty, $"layer '{layer.name}' cannot reach death from AnyState");
            Assert.That(toDead.All(t => t.destinationState && t.destinationState.transitions.Length == 0),
                $"layer '{layer.name}' can leave its death state");
        }
    }

    [Test]
    public void SkeletonOverridesEveryEnemyClip()
    {
        var animator = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(
            "Assets/Animations/SkeletonWarriorAnimator.overrideController");
        Assert.That(animator, Is.Not.Null);

        var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
        animator.GetOverrides(overrides);
        var missing = overrides.Where(o => o.Value == null && o.Key.name != "Mage@Attack02 - Load")
            .Select(o => o.Key.name).ToList(); // the cast loop is boss-only
        Assert.That(missing, Is.Empty, "skeleton would reuse the boss motion for these clips");
    }
}
