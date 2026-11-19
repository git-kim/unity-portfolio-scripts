using Characters;
using UnityEngine;
using UnityEngine.Events;

public class PlayerTrigger : MonoBehaviour
{
    public UnityAction ActionOnTriggerEnter;

    private void OnTriggerEnter(Collider other)
    {
        // Only the player opens or closes doors; enemies chasing the player must not.
        if (other.TryGetComponent<Player>(out _))
            ActionOnTriggerEnter?.Invoke();
    }
}
