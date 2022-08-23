using System;
using System.Collections;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Core.Widgets
{
    public static class KeyboardWidgetExtensions
    {
        public static void AddKeyboard(
            this Widget parent,
            KeyCode keyCode,
            Action onKey = null,
            Action onKeyUp = null,
            Action onKeyDown = null
        )
        {
            parent.Resolve<ICoroutineProvider>().StartCoroutine(
                KeyboardCoroutine(
                    parent.Lifetime,
                    keyCode,
                    onKey,
                    onKeyUp,
                    onKeyDown)
            );
        }

        private static IEnumerator KeyboardCoroutine(
            Lifetime lifetime,
            KeyCode keyCode,
            Action onKey,
            Action onKeyUp,
            Action onKeyDown
        )
        {
            while (!lifetime.IsTerminated)
            {
                yield return null;
                if (Input.GetKey(keyCode))
                {
                    onKey?.Invoke();
                }

                if (Input.GetKeyUp(keyCode))
                {
                    onKeyUp?.Invoke();
                }

                if (Input.GetKeyDown(keyCode))
                {
                    onKeyDown?.Invoke();
                }
            }
        }
    }
}
