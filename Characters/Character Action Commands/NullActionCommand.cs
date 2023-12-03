using UnityEngine;
using CommandPattern;

public class NullActionCommand : CharacterActionCommand
{
    public NullActionCommand(GameObject _) { }

    public override void Execute(int actorID, GameObject target, CharacterAction actionInfo)
    {
    }

    public override void Stop()
    {
    }
}