using System;
using System.Collections;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Polling a key from inside a presenter's scope, without that presenter needing a <c>MonoBehaviour</c>
    /// of its own.
    /// </summary>
    public static class KeyboardPresenterExtensions
    {
        /// <summary>
        /// Watches <paramref name="keyCode"/> once per frame for as long as <paramref name="parent"/> is
        /// open, invoking whichever of the three callbacks the key's state calls for.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Order within a frame.</b> All three states are tested in the same frame, in this order:
        /// <paramref name="onKey"/> while the key is held, then <paramref name="onKeyUp"/> on the frame it
        /// is released, then <paramref name="onKeyDown"/> on the frame it is pressed. A press therefore
        /// delivers <paramref name="onKey"/> <i>before</i> <paramref name="onKeyDown"/> — the opposite of
        /// what the names suggest — while a release delivers <paramref name="onKeyUp"/> alone, the key
        /// already reading as up by then.
        /// </para>
        /// <para>
        /// <b>Start and stop.</b> The coroutine yields once before its first poll, so nothing can fire on
        /// the frame this is called. The scope is checked at the top of the loop but the poll it guards runs
        /// only after the yield that follows it, so one further round of callbacks can still run on the
        /// frame after <paramref name="parent"/> closes: write callbacks that tolerate being invoked once
        /// against a closed presenter. There is no handle to stop the watch early either — attach it to a
        /// child presenter you can close on its own if you need it to end before its parent does.
        /// </para>
        /// <para>
        /// <b>The coroutine host, not the scope, does the scheduling.</b> The loop runs on the
        /// <see cref="ICoroutineProvider"/> resolved from <see cref="Presenter.Context"/>, so deactivating
        /// or destroying that provider's <see cref="GameObject"/> ends the watch for good: Unity does not
        /// resume a coroutine when its host is reactivated, and nothing here restarts it.
        /// </para>
        /// <para>
        /// <b>Legacy input.</b> This reads <c>UnityEngine.Input</c>, so a project that has switched the
        /// active input handling to the Input System package alone does not merely see no keys here — the
        /// first poll throws, Unity logs it, and the watch stops at that frame.
        /// </para>
        /// <para>
        /// Resolving the coroutine provider throws a <c>ContextException</c> when none is registered; the
        /// watch is then not started at all.
        /// </para>
        /// </remarks>
        /// <param name="parent">The presenter whose scope bounds the watch and whose context supplies the
        /// coroutine runner. The watch is not tied to any view.</param>
        /// <param name="keyCode">The key to poll.</param>
        /// <param name="onKey">Invoked on every frame the key is held, the frame it is pressed included.
        /// <c>null</c> to ignore that state.</param>
        /// <param name="onKeyUp">Invoked on the frame the key is released. <c>null</c> to ignore.</param>
        /// <param name="onKeyDown">Invoked on the frame the key is pressed. <c>null</c> to ignore.</param>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached yet,
        /// so it has neither a context to resolve from nor a lifetime to bound the watch; or the coroutine
        /// provider refuses to start one, its host having been destroyed or its <see cref="GameObject"/>
        /// left inactive.</exception>
        public static void AddKeyboard(
            this Presenter parent,
            KeyCode keyCode,
            Action onKey = null,
            Action onKeyUp = null,
            Action onKeyDown = null
        )
        {
            parent.Context.Resolve<ICoroutineProvider>().StartCoroutine(
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
