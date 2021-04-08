using UnityEngine;

public sealed class DisablenessIndicator : MonoBehaviour
{
    public void Disable()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    public void Enable()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }
}
