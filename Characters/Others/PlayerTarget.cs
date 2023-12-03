using UnityEngine;
using UserInterface;

public class PlayerTarget : MonoBehaviour
{
    [SerializeField] private TargetIndicator targetIndicator;
    public TargetIndicator TargetIndicator => targetIndicator;
}