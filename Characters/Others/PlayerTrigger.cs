using UnityEngine;
using UnityEngine.Events;

public class PlayerTrigger : MonoBehaviour
{
    public UnityAction ActionOnTriggerEnter;

    private void OnTriggerEnter()
    {
        ActionOnTriggerEnter?.Invoke();   
    }
}