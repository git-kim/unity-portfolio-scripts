using Managers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UserInterface
{
    public class ErrorMessageDisplay : MonoBehaviour
    {
        [SerializeField] private List<ErrorMessageObject> errorMessageObjects;
        [SerializeField] private TMP_Text label;
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
            ShowMessage(errorMessageObjects[errMsgID].ErrorMessageText);
        }

        public void ShowMessage(string message)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            label.alpha = 0f;
            label.text = message;
            coroutine = StartCoroutine(ShowErrorMessageWithTransition());
        }

        private const float FadeTime = 0.16f; // was 8 fixed steps of 0.02 s

        IEnumerator ShowErrorMessageWithTransition()
        {
            while (label.alpha < 1f)
            {
                label.alpha = Mathf.Min(1f, label.alpha + Time.deltaTime / FadeTime);
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            while (label.alpha > 0f)
            {
                label.alpha = Mathf.Max(0f, label.alpha - Time.deltaTime / FadeTime);
                yield return null;
            }
        }
    }
}