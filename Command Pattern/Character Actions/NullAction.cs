using UnityEngine;
using CommandPattern;

public class NullAction : ICommand
{
    private readonly IActable actorIActable;

    public NullAction(GameObject actor)
    {
        actorIActable = actor.GetComponent<IActable>();
    }

    public Coroutine CurrentActionCoroutine { get; set; } = null;

    public void Execute(int actorID, GameObject target, ActionInfo actionInfo)
    {
        // actorIActable.ActionToTake = 0;
    }

    public void Stop()
    {
    }
}