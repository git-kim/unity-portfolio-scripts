using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UserInterface
{
    public class ErrorMessageDisplay : MonoBehaviour
    {
        [SerializeField] private List<ErrorMessageObject> errorMessageObjects;
        [SerializeField] private UILabel label;
        private Coroutine coroutine;

        void Awake()
        {
            coroutine = null;
            label.text = string.Empty;
        }

        private void Start()
        {
            GameManager.Instance.SetErrorMessageDisplay(this);
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
}