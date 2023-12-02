using UnityEngine;

namespace CommandPattern
{
    public interface ICommand
    {
        void Execute(int actorID, GameObject target, CharacterAction actionInfo);
        void Stop();
        Coroutine CurrentActionCoroutine { get; set; }
    }
}