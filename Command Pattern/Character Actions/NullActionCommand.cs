using UnityEngine;
using CommandPattern;

public class NullActionCommand : ICommand
{
    public NullActionCommand(GameObject _) { }

    public Coroutine CurrentActionCoroutine { get; set; } = null;

    public void Execute(int actorID, GameObject target, CharacterAction actionInfo)
    {
    }

    public void Stop()
    {
    }
}