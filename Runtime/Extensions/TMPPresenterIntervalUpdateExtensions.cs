using System;
using System.Collections;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Drives a <see cref="TMPPresenter"/> from a callback on a timer, for a label whose text depends on
    /// something that changes on its own — a countdown, a clock, an energy bar's refill time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Pull, not push.</b> Nothing here observes anything: the callback is asked for the current text
    /// every <c>interval</c>, and whatever it returns is set as the presenter's model. That is the right
    /// shape for a value derived from wall-clock time, which has no change event to subscribe to, and the
    /// wrong shape for a value that does — use a signal for those.
    /// </para>
    /// <para>
    /// <b>Scope.</b> Each call defines a nested scope on the presenter's <see cref="Presenter.Lifetime"/>
    /// and stops the coroutine when that scope ends, so the timer dies with the presenter and never outlives
    /// the label it writes to. The callback is handed that scope as an <see cref="IDisposable"/>, which is
    /// how it stops itself once there is nothing left to count down.
    /// </para>
    /// <para>
    /// <b>It is engine time, and engine scheduling.</b> The wait is a <see cref="WaitForSeconds"/>, so it is
    /// scaled by <see cref="Time.timeScale"/> — a paused game stops the ticking — and it resumes on a frame
    /// boundary, so an interval shorter than a frame just means "every frame". The coroutine also belongs to
    /// whatever host the <see cref="ICoroutineProvider"/> runs it on and stops if that host's
    /// <see cref="GameObject"/> is deactivated, which the presenter has no way to notice.
    /// </para>
    /// </remarks>
    public static class TMPPresenterIntervalUpdateExtensions
    {
        /// <summary>
        /// The interval used by the two overloads that do not take one: 100 ms, ten updates a second.
        /// </summary>
        /// <remarks>
        /// Chosen as a refresh cadence for a human reading a changing number, not as a simulation tick — it
        /// is fast enough that a seconds counter never visibly lags, and slow enough to cost nothing. Being
        /// a <see cref="TimeSpan"/> it is immutable, so this shared field cannot be altered by a caller.
        /// </remarks>
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Drives the label from a callback returning a plain string, every
        /// <see cref="DefaultInterval"/>.
        /// </summary>
        /// <remarks>
        /// The string becomes a <see cref="TextModel"/> with no substitutions, so it is still translated
        /// when a localisation is registered but never receives format arguments. For everything else —
        /// when the first update happens, how it stops, what it throws — see
        /// <see cref="WithIntervalUpdate(TMPPresenter, Func{IDisposable, TextModel}, TimeSpan)"/>.
        /// </remarks>
        /// <param name="parent">The presenter to drive. Must already be attached.</param>
        /// <param name="text">Asked for the current text on every tick; dispose the scope it is handed to
        /// stop. Returning <c>null</c> clears the label without stopping anything.</param>
        /// <returns><paramref name="parent"/>, so this chains onto the call that created it.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached yet,
        /// its lifetime has already terminated, or the coroutine host cannot run coroutines.</exception>
        /// <exception cref="NullReferenceException"><paramref name="parent"/> or <paramref name="text"/> is
        /// <c>null</c>; neither is validated.</exception>
        public static TMPPresenter WithIntervalUpdate(this TMPPresenter parent, Func<IDisposable, string> text)
        {
            return parent.WithIntervalUpdate((disposable) => (TextModel)text(disposable), DefaultInterval);
        }

        /// <summary>
        /// Drives the label from a callback returning a plain string, on an interval you choose.
        /// </summary>
        /// <remarks>
        /// The string becomes a <see cref="TextModel"/> with no substitutions. See
        /// <see cref="WithIntervalUpdate(TMPPresenter, Func{IDisposable, TextModel}, TimeSpan)"/> for the
        /// first-update-is-synchronous rule and for the three ways this stops.
        /// </remarks>
        /// <param name="parent">The presenter to drive. Must already be attached.</param>
        /// <param name="text">Asked for the current text on every tick; dispose the scope it is handed to
        /// stop. Returning <c>null</c> clears the label without stopping anything.</param>
        /// <param name="interval">The delay between updates, in scaled engine time. Rounded up to a frame,
        /// so zero or a negative value means "every frame".</param>
        /// <returns><paramref name="parent"/>, so this chains onto the call that created it.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached yet,
        /// its lifetime has already terminated, or the coroutine host cannot run coroutines.</exception>
        /// <exception cref="NullReferenceException"><paramref name="parent"/> or <paramref name="text"/> is
        /// <c>null</c>; neither is validated.</exception>
        public static TMPPresenter WithIntervalUpdate(this TMPPresenter parent, Func<IDisposable, string> text,
            TimeSpan interval)
        {
            return parent.WithIntervalUpdate((disposable) => (TextModel)text(disposable), interval);
        }

        /// <summary>
        /// Drives the label from a callback returning a full <see cref="TextModel"/>, every
        /// <see cref="DefaultInterval"/> — the overload to use when the text needs substitutions.
        /// </summary>
        /// <remarks>
        /// Identical to
        /// <see cref="WithIntervalUpdate(TMPPresenter, Func{IDisposable, TextModel}, TimeSpan)"/> with
        /// <see cref="DefaultInterval"/> passed explicitly.
        /// </remarks>
        /// <param name="parent">The presenter to drive. Must already be attached.</param>
        /// <param name="text">Asked for the current model on every tick; dispose the scope it is handed to
        /// stop. Returning a model whose <c>Format</c> is <c>null</c> clears the label but does not
        /// stop anything.</param>
        /// <returns><paramref name="parent"/>, so this chains onto the call that created it.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached yet,
        /// its lifetime has already terminated, or the coroutine host cannot run coroutines.</exception>
        /// <exception cref="NullReferenceException"><paramref name="parent"/> or <paramref name="text"/> is
        /// <c>null</c>; neither is validated.</exception>
        public static TMPPresenter WithIntervalUpdate(this TMPPresenter parent, Func<IDisposable, TextModel> text)
        {
            return parent.WithIntervalUpdate(text, DefaultInterval);
        }

        /// <summary>
        /// Starts a coroutine that sets <paramref name="parent"/>'s model to whatever
        /// <paramref name="text"/> returns, every <paramref name="interval"/>, until the presenter closes or
        /// the callback stops it. The overload the other three end up in.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The first update happens inside this call</b>, before it returns: a coroutine runs up to its
        /// first wait synchronously. The label is therefore never briefly empty, and a callback that throws
        /// straight away throws out of this method rather than out of a coroutine nobody is watching.
        /// </para>
        /// <para>
        /// <b>Stopping.</b> Two things end it, and both work by terminating the timer's scope: the presenter
        /// closing, since that scope is nested in <see cref="Presenter.Lifetime"/>, or the callback disposing
        /// the <see cref="IDisposable"/> it is handed. Terminating that scope is what stops the coroutine;
        /// the loop's own check of the scope before each update is a second line of defence, not a third way
        /// out. The disposable is the interesting one — it is what lets "count down, then stop" live entirely
        /// inside the callback with no state held outside it. Disposing during an invocation still renders
        /// that invocation's result; the loop simply never runs again. Disposing during the very first,
        /// synchronous invocation is safe too: the stop is registered a moment later on an
        /// already-terminated scope, and <c>Lifetime.AddAction</c> then runs it immediately.
        /// </para>
        /// <para>
        /// <b>Nothing is de-duplicated.</b> Calling this twice on one presenter leaves two coroutines both
        /// setting its model from their own callbacks; the label shows whichever ran last.
        /// </para>
        /// <para>
        /// If no <see cref="ICoroutineProvider"/> is registered, the resolve throws a <c>ContextException</c>
        /// and the nested scope defined a line earlier stays registered on the presenter until it closes. It
        /// holds nothing and does nothing, but it is not unwound.
        /// </para>
        /// </remarks>
        /// <param name="parent">The presenter to drive. Must already be attached — both its
        /// <see cref="Presenter.Lifetime"/> and its <see cref="Presenter.Context"/> are read here.</param>
        /// <param name="text">Asked for the current text on every tick, on Unity's main thread. It is handed
        /// the timer's own scope: dispose that to stop the updates. Returning a model whose <c>Format</c> is
        /// <c>null</c> clears the label without stopping anything.</param>
        /// <param name="interval">The delay between updates, in scaled engine time. Rounded up to a frame,
        /// so zero or a negative value means "every frame" rather than "as fast as possible".</param>
        /// <returns><paramref name="parent"/>, so this chains onto the call that created it.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached yet,
        /// its lifetime has already terminated, or the coroutine host cannot run coroutines — see
        /// <see cref="ICoroutineProvider.StartCoroutine"/>.</exception>
        /// <exception cref="NullReferenceException"><paramref name="parent"/> or <paramref name="text"/> is
        /// <c>null</c>; neither is validated.</exception>
        public static TMPPresenter WithIntervalUpdate(this TMPPresenter parent, Func<IDisposable, TextModel> text,
            TimeSpan interval)
        {
            IEnumerator IntervalCoroutine(Lifetime lifetime, IDisposable df)
            {
                while (!lifetime.IsTerminated)
                {
                    parent.SetModel(text(df));
                    yield return new WaitForSeconds((float)interval.TotalSeconds);
                }
            }

            var df = parent.Lifetime.DefineNested();
            var coroutineProvider = parent.Context.Resolve<ICoroutineProvider>();
            var coroutine = coroutineProvider.StartCoroutine(IntervalCoroutine(df.Lifetime, df));
            if (coroutine != null)
            {
                df.Lifetime.AddAction(() => coroutineProvider.StopCoroutine(coroutine));
            }

            return parent;
        }
    }
}
