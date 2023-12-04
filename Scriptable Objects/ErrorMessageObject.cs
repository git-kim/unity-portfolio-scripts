using UnityEngine;

[CreateAssetMenu(fileName = "Error Message", menuName = "Scriptable Object/Error Message", order = 1)]
public class ErrorMessageObject : ScriptableObject
{
    [SerializeField] private int errorMessageID;
    public int ErrorMessageID => errorMessageID;

    [SerializeField] private string errorMessageText;
    public string ErrorMessageText => errorMessageText;
}
