using System;
using System.Collections;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Drives a <see cref="TMPPresenter"/> from a callback on a timer, for a label whose text depends on
    /// something that changes on its own — a countdown, a clock, a refill timer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Pull, not push.</b> The callback is asked for the current text every interval and its result is set
    /// as the presenter's model. That suits a value derived from time, which has no change event; for a value
    /// that does, subscribe to its signal instead.
    /// </para>
    /// <para>
    /// <b>Scope.</b> Each call defines a scope nested in the presenter's <see cref="Presenter.Lifetime"/> and
    /// stops the coroutine when that scope ends, so the timer never outlives the label. The callback is handed
    /// that scope as an <see cref="IDisposable"/>: disposing it stops the timer.
    /// </para>
    /// <para>
    /// <b>Engine time and engine scheduling.</b> The wait is a <see cref="WaitForSeconds"/>, so it is scaled by
    /// <see cref="Time.timeScale"/> and resumes on a frame boundary; an interval shorter than a frame means
    /// "every frame". The coroutine runs on the <see cref="ICoroutineProvider"/> you pass, and stops for good
    /// if that provider's host is deactivated or destroyed.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — the coroutine provider is a parameter. It was resolved from the removed
    /// <c>Presenter.Context</c>; inject an <see cref="ICoroutineProvider"/> into the presenter that calls this
    /// and pass it on.
    /// </para>
    /// </remarks>
    public static class TMPPresenterIntervalUpdateExtensions
    {
        /// <summary>
        /// The interval used by the overloads that do not take one: 100 ms.
        /// </summary>
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Drives the label from a callback returning a plain string, every <see cref="DefaultInterval"/>.
        /// See <see cref="WithIntervalUpdate(TMPPresenter, ICoroutineProvider, Func{IDisposable, TextModel}, TimeSpan)"/>.
        /// </summary>
        /// <param name="presenter">The presenter to drive. It must be attached and alive.</param>
        /// <param name="coroutines">Runs the timer.</param>
        /// <param name="text">Asked for the current text on every tick; dispose the scope it is handed to stop.
        /// </param>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> has not been attached, or
        /// <paramref name="coroutines"/> cannot start a coroutine.</exception>
        public static TMPPresenter WithIntervalUpdate(this TMPPresenter presenter, ICoroutineProvider coroutines,
            Func<IDisposable, string> text) =>
            presenter.WithIntervalUpdate(coroutines, Wrap(text), DefaultInterval);

        /// <summary>
        /// Drives the label from a callback returning a plain string, on an interval you choose.
        /// See <see cref="WithIntervalUpdate(TMPPresenter, ICoroutineProvider, Func{IDisposable, TextModel}, TimeSpan)"/>.
        /// </summary>
        /// <param name="presenter">The presenter to drive. It must be attached and alive.</param>
        /// <param name="coroutines">Runs the timer.</param>
        /// <param name="text">Asked for the current text on every tick; dispose the scope it is handed to stop.
        /// </param>
        /// <param name="interval">The delay between updates, in scaled engine time.</param>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> has not been attached, or
        /// <paramref name="coroutines"/> cannot start a coroutine.</exception>
        public static TMPPresenter WithIntervalUpdate(this TMPPresenter presenter, ICoroutineProvider coroutines,
            Func<IDisposable, string> text, TimeSpan interval) =>
            presenter.WithIntervalUpdate(coroutines, Wrap(text), interval);

        /// <summary>
        /// Drives the label from a callback returning a <see cref="TextModel"/>, every
        /// <see cref="DefaultInterval"/>.
        /// See <see cref="WithIntervalUpdate(TMPPresenter, ICoroutineProvider, Func{IDisposable, TextModel}, TimeSpan)"/>.
        /// </summary>
        /// <param name="presenter">The presenter to drive. It must be attached and alive.</param>
        /// <param name="coroutines">Runs the timer.</param>
        /// <param name="text">Asked for the current model on every tick; dispose the scope it is handed to
        /// stop.</param>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> has not been attached, or
        /// <paramref name="coroutines"/> cannot start a coroutine.</exception>
        public static TMPPresenter WithIntervalUpdate(this TMPPresenter presenter, ICoroutineProvider coroutines,
            Func<IDisposable, TextModel> text) =>
            presenter.WithIntervalUpdate(coroutines, text, DefaultInterval);

        /// <summary>
        /// Starts a coroutine on <paramref name="coroutines"/> that sets <paramref name="presenter"/>'s model to
        /// whatever <paramref name="text"/> returns, every <paramref name="interval"/>, until the presenter
        /// closes or the callback disposes the scope it is handed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The first update happens as the coroutine starts.</b> Unity runs a coroutine up to its first wait
        /// inside <c>StartCoroutine</c>, so with <c>CoroutineProvider</c> the label is set before this method
        /// returns.
        /// </para>
        /// <para>
        /// Disposing the scope from inside the callback still renders that call's result; the loop does not
        /// run again. Calling this twice on one presenter runs two timers that both set its model. If the
        /// coroutine cannot be started, the timer's scope is ended before the exception propagates.
        /// </para>
        /// </remarks>
        /// <param name="presenter">The presenter to drive. It must be attached and alive.</param>
        /// <param name="coroutines">Runs the timer. Typically injected into the presenter that calls this.
        /// </param>
        /// <param name="text">Asked for the current model on every tick, on Unity's main thread, and handed the
        /// timer's scope.</param>
        /// <param name="interval">The delay between updates, in scaled engine time. Zero or negative means
        /// every frame.</param>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>. Nothing is started.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> has not been attached, or
        /// <paramref name="coroutines"/> cannot start a coroutine — see
        /// <see cref="ICoroutineProvider.StartCoroutine"/>.</exception>
        public static TMPPresenter WithIntervalUpdate(this TMPPresenter presenter, ICoroutineProvider coroutines,
            Func<IDisposable, TextModel> text, TimeSpan interval)
        {
            if (presenter == null)
                throw new ArgumentNullException(nameof(presenter), $"{nameof(presenter)} can't be null");
            if (coroutines == null)
                throw new ArgumentNullException(nameof(coroutines), $"{nameof(coroutines)} can't be null");
            if (text == null)
                throw new ArgumentNullException(nameof(text), $"{nameof(text)} can't be null");

            var timer = presenter.Lifetime.DefineNested();
            Coroutine coroutine;
            try
            {
                coroutine = coroutines.StartCoroutine(Tick(presenter, timer, text, interval));
            }
            catch
            {
                timer.Terminate();
                throw;
            }

            timer.Lifetime.AddAction(() => coroutines.StopCoroutine(coroutine));
            return presenter;
        }

        private static IEnumerator Tick(TMPPresenter presenter, Lifetime.Definition timer,
            Func<IDisposable, TextModel> text, TimeSpan interval)
        {
            while (!timer.IsTerminated)
            {
                presenter.SetModel(text(timer));
                yield return new WaitForSeconds((float)interval.TotalSeconds);
            }
        }

        private static Func<IDisposable, TextModel> Wrap(Func<IDisposable, string> text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text), $"{nameof(text)} can't be null");
            return scope => text(scope);
        }
    }
}
