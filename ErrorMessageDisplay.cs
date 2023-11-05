using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErrorMessageDisplay: MonoBehaviour
{
    [SerializeField]
    List<ErrorMessageObject> errorMessageObjects;

    UILabel label;
    Coroutine coroutine;

    void Awake()
    {
        coroutine = null;
        label = GameObject.Find("Error Message Display").GetComponent<UILabel>();
        label.text = "";
    }

    public void ShowErrorMessage(int errMsgID)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);

        label.alpha = 0f;
        label.text = errorMessageObjects[errMsgID].ErrorMessageText;
        coroutine = StartCoroutine(ShowErrorMessageWithTransition());
    }

    IEnumerator ShowErrorMessageWithTransition()
    {
        while (label.alpha < 1f)
        {
            label.alpha += 0.125f;
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(1f);

        while (label.alpha > 0f)
        {
            label.alpha -= 0.125f;
            yield return new WaitForFixedUpdate();
        }
    }
}
