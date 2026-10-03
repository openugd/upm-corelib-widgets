using System;
using System.Collections;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Drives a presenter's model from a callback on a timer, for a view that shows something that changes on its
    /// own — a countdown, a clock, a refill timer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Pull, not push.</b> The callback is asked for the current model every interval and its result is set
    /// as the presenter's model. That suits a value derived from time, which has no change event; for a value
    /// that does, subscribe to its signal instead.
    /// </para>
    /// <para>
    /// <b>Any presenter with a model.</b> The general overloads take any <see cref="Presenter{TView,TModel}"/> and
    /// a callback returning its <c>TModel</c>. A text presenter — anything whose model is a
    /// <see cref="TextModel"/> — also takes a callback returning a plain <c>string</c>.
    /// </para>
    /// <para>
    /// <b>Scope.</b> Each call defines a scope nested in the presenter's <see cref="Presenter.Lifetime"/> and
    /// stops the coroutine when that scope ends, so the timer never outlives the presenter. The callback is handed
    /// that scope as an <see cref="IDisposable"/>: disposing it stops the timer.
    /// </para>
    /// <para>
    /// <b>Real time, engine scheduling.</b> The wait counts unscaled time, so the timer keeps running while
    /// <see cref="Time.timeScale"/> is <c>0</c> — a pause menu does not freeze a countdown. Updates happen on a
    /// frame boundary, so an interval shorter than a frame means every frame. One wait object is created per
    /// timer and reused by every tick. The coroutine runs on the <see cref="ICoroutineProvider"/> you pass, and
    /// stops for good if that provider's host is deactivated or destroyed.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — the class was <c>TMPPresenterIntervalUpdateExtensions</c> and drove only a
    /// <see cref="TMPPresenter"/>. Each tick allocated a new <see cref="WaitForSeconds"/>, which counts scaled
    /// time, so the label froze while the game was paused (audit WG-13, UH-23). The coroutine provider is a
    /// parameter: it was resolved from the removed <c>Presenter.Context</c>; inject an
    /// <see cref="ICoroutineProvider"/> into the presenter that calls this and pass it on.
    /// </para>
    /// </remarks>
    public static class IntervalUpdateExtensions
    {
        /// <summary>
        /// The interval used by the overloads that do not take one: 100 ms.
        /// </summary>
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Drives a presenter from a callback returning its model, every <see cref="DefaultInterval"/>. See
        /// <see cref="WithIntervalUpdate{TView,TModel}(Presenter{TView,TModel},ICoroutineProvider,Func{IDisposable,TModel},TimeSpan)"/>.
        /// </summary>
        /// <param name="presenter">The presenter to drive. It must be attached.</param>
        /// <param name="coroutines">Runs the timer.</param>
        /// <param name="model">Asked for the current model on every tick; dispose the scope it is handed to stop.
        /// </param>
        /// <typeparam name="TView">The presenter's view type.</typeparam>
        /// <typeparam name="TModel">The presenter's model type.</typeparam>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>. Nothing is started.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> has not been attached.
        /// </exception>
        /// <exception cref="Exception">Whatever <paramref name="coroutines"/> or the first call of
        /// <paramref name="model"/> throws, after the timer has been stopped.</exception>
        public static Presenter<TView, TModel> WithIntervalUpdate<TView, TModel>(
            this Presenter<TView, TModel> presenter, ICoroutineProvider coroutines, Func<IDisposable, TModel> model)
            where TView : class =>
            presenter.WithIntervalUpdate(coroutines, model, DefaultInterval);

        /// <summary>
        /// Starts a coroutine on <paramref name="coroutines"/> that sets <paramref name="presenter"/>'s model to
        /// whatever <paramref name="model"/> returns, now and then every <paramref name="interval"/>, until the
        /// presenter closes or the callback disposes the scope it is handed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The first update happens inside this call</b>, once the coroutine has started, so the view shows the
        /// callback's model before this method returns. If that first call throws, the timer is stopped and the
        /// exception propagates to the caller. An exception from a later tick ends the coroutine, and Unity logs
        /// it; the timer's scope then stays until the presenter closes, and closing it is still safe.
        /// </para>
        /// <para>
        /// Disposing the scope from inside the callback still renders that call's result; the callback is not
        /// called again. Calling this twice on one presenter runs two timers that both set its model. If the
        /// coroutine cannot be started, the timer's scope is ended and the callback is never called. On a
        /// presenter that has already closed this does nothing: no coroutine is started and the callback is never
        /// called.
        /// </para>
        /// </remarks>
        /// <param name="presenter">The presenter to drive. It must be attached.</param>
        /// <param name="coroutines">Runs the timer. Typically injected into the presenter that calls this.
        /// </param>
        /// <param name="model">Asked for the current model on every tick, on Unity's main thread, and handed the
        /// timer's scope.</param>
        /// <param name="interval">The delay between updates, in unscaled time. Zero or negative means every
        /// frame.</param>
        /// <typeparam name="TView">The presenter's view type.</typeparam>
        /// <typeparam name="TModel">The presenter's model type.</typeparam>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>. Nothing is started.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> has not been attached.
        /// </exception>
        /// <exception cref="Exception">Whatever <paramref name="coroutines"/> throws when it cannot start the
        /// coroutine — <see cref="InvalidOperationException"/> for an inactive or destroyed host, by the contract of
        /// <see cref="ICoroutineProvider.StartCoroutine"/> — or whatever the first call of <paramref name="model"/>
        /// throws. Either way the timer has been stopped first.</exception>
        public static Presenter<TView, TModel> WithIntervalUpdate<TView, TModel>(
            this Presenter<TView, TModel> presenter, ICoroutineProvider coroutines, Func<IDisposable, TModel> model,
            TimeSpan interval)
            where TView : class
        {
            if (presenter == null)
                throw new ArgumentNullException(nameof(presenter), $"{nameof(presenter)} can't be null");
            if (coroutines == null)
                throw new ArgumentNullException(nameof(coroutines), $"{nameof(coroutines)} can't be null");
            if (model == null)
                throw new ArgumentNullException(nameof(model), $"{nameof(model)} can't be null");

            // On a closed presenter the scope is born terminated: there is nothing to drive.
            var timer = presenter.Lifetime.DefineNested();
            if (timer.IsTerminated) return presenter;

            Coroutine coroutine;
            try
            {
                // Tick's first step only waits, so the coroutine is always still running when StartCoroutine
                // returns: Unity never hands back the null it returns for a coroutine that ended inside the call.
                coroutine = coroutines.StartCoroutine(Tick(presenter, timer, model, interval));
            }
            catch
            {
                timer.Terminate();
                throw;
            }

            // A provider must not return null (ICoroutineProvider's contract); if one does, there is nothing to
            // stop, and StopCoroutine(null) would throw when the presenter closes.
            if (coroutine != null)
            {
                timer.Lifetime.AddAction(() => coroutines.StopCoroutine(coroutine));
            }

            try
            {
                presenter.SetModel(model(timer));
            }
            catch
            {
                timer.Terminate();
                throw;
            }

            return presenter;
        }

        /// <summary>
        /// Drives a text presenter from a callback returning a plain string, every <see cref="DefaultInterval"/>.
        /// See <see cref="WithIntervalUpdate{TView,TModel}(Presenter{TView,TModel},ICoroutineProvider,Func{IDisposable,TModel},TimeSpan)"/>.
        /// </summary>
        /// <param name="presenter">The presenter to drive — <see cref="TextPresenter"/>, <see cref="TMPPresenter"/>,
        /// <see cref="HyperlinkTextPresenter"/> or any other whose model is a <see cref="TextModel"/>. It must be
        /// attached.</param>
        /// <param name="coroutines">Runs the timer.</param>
        /// <param name="text">Asked for the current text on every tick; dispose the scope it is handed to stop.
        /// </param>
        /// <typeparam name="TView">The presenter's view type.</typeparam>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>. Nothing is started.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> has not been attached.
        /// </exception>
        /// <exception cref="Exception">Whatever <paramref name="coroutines"/> or the first call of
        /// <paramref name="text"/> throws, after the timer has been stopped.</exception>
        public static Presenter<TView, TextModel> WithIntervalUpdate<TView>(this Presenter<TView, TextModel> presenter,
            ICoroutineProvider coroutines, Func<IDisposable, string> text)
            where TView : class =>
            presenter.WithIntervalUpdate(coroutines, text, DefaultInterval);

        /// <summary>
        /// Drives a text presenter from a callback returning a plain string, on an interval you choose.
        /// See <see cref="WithIntervalUpdate{TView,TModel}(Presenter{TView,TModel},ICoroutineProvider,Func{IDisposable,TModel},TimeSpan)"/>.
        /// </summary>
        /// <param name="presenter">The presenter to drive — any whose model is a <see cref="TextModel"/>. It must
        /// be attached.</param>
        /// <param name="coroutines">Runs the timer.</param>
        /// <param name="text">Asked for the current text on every tick; dispose the scope it is handed to stop.
        /// </param>
        /// <param name="interval">The delay between updates, in unscaled time. Zero or negative means every
        /// frame.</param>
        /// <typeparam name="TView">The presenter's view type.</typeparam>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>. Nothing is started.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> has not been attached.
        /// </exception>
        /// <exception cref="Exception">Whatever <paramref name="coroutines"/> or the first call of
        /// <paramref name="text"/> throws, after the timer has been stopped.</exception>
        public static Presenter<TView, TextModel> WithIntervalUpdate<TView>(this Presenter<TView, TextModel> presenter,
            ICoroutineProvider coroutines, Func<IDisposable, string> text, TimeSpan interval)
            where TView : class
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text), $"{nameof(text)} can't be null");

            return presenter.WithIntervalUpdate<TView, TextModel>(coroutines, scope => text(scope), interval);
        }

        private static IEnumerator Tick<TView, TModel>(Presenter<TView, TModel> presenter, Lifetime.Definition timer,
            Func<IDisposable, TModel> model, TimeSpan interval)
            where TView : class
        {
            // One wait for the whole timer; null waits one frame.
            var wait = interval > TimeSpan.Zero ? new RealtimeWait(interval.TotalSeconds) : null;

            while (!timer.IsTerminated)
            {
                yield return wait;
                if (timer.IsTerminated) yield break;

                presenter.SetModel(model(timer));
            }
        }

        // A wait in unscaled time that re-arms itself when it completes, so one instance serves every tick.
        // WaitForSecondsRealtime would do the same, but it measures with the float Time.realtimeSinceStartup: after
        // a long session (the editor's clock counts from editor start) a short interval rounds away to nothing, and
        // a wait that ends as soon as it is yielded can resume the coroutine within the same frame, over and over.
        // This one uses the double clock.
        internal sealed class RealtimeWait : CustomYieldInstruction
        {
            private static readonly Func<double> RealtimeClock = () => Time.realtimeSinceStartupAsDouble;

            private readonly double _seconds;
            private readonly Func<double> _clock;
            private bool _armed;
            private double _until;

            public RealtimeWait(double seconds) : this(seconds, RealtimeClock)
            {
            }

            internal RealtimeWait(double seconds, Func<double> clock)
            {
                _seconds = seconds;
                _clock = clock;
            }

            public double Seconds => _seconds;

            public override bool keepWaiting
            {
                get
                {
                    var now = _clock();
                    if (!_armed)
                    {
                        _armed = true;
                        _until = now + _seconds;
                    }

                    if (now < _until) return true;

                    _armed = false;
                    return false;
                }
            }

            public override void Reset() => _armed = false;
        }
    }
}
