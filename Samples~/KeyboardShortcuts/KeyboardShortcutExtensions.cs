using System;
using System.Collections;
using OpenUGD.Core.Presenters;
using OpenUGD.Utils;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && OPENUGD_INPUT_SYSTEM_PACKAGE
using UnityEngine.InputSystem;
#endif

namespace OpenUGD.Samples.KeyboardShortcuts
{
    /// <summary>
    /// Keyboard shortcuts scoped to a presenter. The replacement for <c>AddKeyboard</c>, removed from
    /// <c>com.openugd.corelib.widgets</c> in 2.0.0. Copy this file into your project and own it.
    /// </summary>
    public static class KeyboardShortcutExtensions
    {
        /// <summary>
        /// Invokes <paramref name="onKeyDown"/> on each frame <paramref name="key"/> goes down, from the frame
        /// after this call until <paramref name="presenter"/> closes, and never after it has closed.
        /// </summary>
        /// <remarks>
        /// Reads the Input System when Active Input Handling enables it and <c>com.unity.inputsystem</c> is
        /// installed (<c>OPENUGD_INPUT_SYSTEM_PACKAGE</c> comes from this sample's <c>versionDefines</c>),
        /// otherwise the Input Manager when that is enabled, and otherwise starts nothing. The poll runs on
        /// <paramref name="coroutines"/>, so deactivating or destroying its host ends it for good.
        /// </remarks>
        /// <param name="presenter">The scope: the poll stops when its <c>Lifetime</c> terminates.</param>
        /// <param name="coroutines">Runs the poll once per frame. Inject it into the presenter.</param>
        /// <param name="key">The key. Under the Input System it is looked up by name in <c>Key</c>, so a
        /// name the two enums do not share (<c>Return</c>, <c>Alpha1</c>, <c>Keypad1</c>, <c>LeftControl</c>,
        /// <c>Mouse0</c>) throws <see cref="ArgumentException"/> here instead of never firing.</param>
        /// <param name="onKeyDown">Invoked on the main thread, at most once per frame.</param>
        /// <exception cref="ArgumentNullException"><paramref name="coroutines"/> or <paramref name="onKeyDown"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> is not attached yet, or
        /// <paramref name="coroutines"/> cannot start a coroutine because its host is destroyed or inactive.
        /// </exception>
        public static void SubscribeOnKeyDown(this Presenter presenter, ICoroutineProvider coroutines,
            KeyCode key, Action onKeyDown)
        {
            if (coroutines == null) throw new ArgumentNullException(nameof(coroutines));
            if (onKeyDown == null) throw new ArgumentNullException(nameof(onKeyDown));
            var lifetime = presenter.Lifetime;
#if ENABLE_INPUT_SYSTEM && OPENUGD_INPUT_SYSTEM_PACKAGE
            var inputKey = (Key)Enum.Parse(typeof(Key), key.ToString());
            Func<bool> wentDown = () => Keyboard.current != null && Keyboard.current[inputKey].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            Func<bool> wentDown = () => Input.GetKeyDown(key);
#else
            Func<bool> wentDown = null; // no input backend this code can read: the shortcut never fires
#endif
            if (wentDown == null || lifetime.IsTerminated) return;
            var poll = coroutines.StartCoroutine(Poll(lifetime, wentDown, onKeyDown));
            lifetime.AddAction(() => coroutines.StopCoroutine(poll));
        }

        private static IEnumerator Poll(Lifetime lifetime, Func<bool> wentDown, Action onKeyDown)
        {
            while (true)
            {
                yield return null;                      // nothing fires on the frame of the call
                if (lifetime.IsTerminated) yield break; // checked after the wait, right before the read
                if (wentDown()) onKeyDown();
            }
        }
    }
}
