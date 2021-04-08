using UnityEngine;

namespace CommandPattern
{
    public interface ICommand
    {
        void Execute(int actorID, GameObject target, ActionInfo actionInfo);
        void Stop();
        Coroutine CurrentActionCoroutine { get; set; }
    }
}
