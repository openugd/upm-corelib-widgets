using System;
using System.Collections;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Sets a presenter's model from a callback on a timer, for a value with no change event of its own: a
    /// countdown, a clock.
    /// </summary>
    /// <remarks>
    /// Each call starts one coroutine and defines one scope nested in the presenter's
    /// <see cref="Presenter.Lifetime"/>; the callback receives that scope as an <see cref="IDisposable"/>, and
    /// disposing it, or closing the presenter, stops the timer. The interval is measured in unscaled real time,
    /// so the timer runs while <see cref="Time.timeScale"/> is <c>0</c>; updates land on frame boundaries. One
    /// wait object is allocated per timer. The coroutine stops for good if the
    /// <see cref="ICoroutineProvider"/>'s host is deactivated or destroyed.
    /// </remarks>
    public static class IntervalUpdateExtensions
    {
        /// <summary>
        /// The interval of the overloads that take none: 100 ms.
        /// </summary>
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// As <see cref="WithIntervalUpdate{TView,TModel}(Presenter{TView,TModel},ICoroutineProvider,Func{IDisposable,TModel},TimeSpan)"/>
        /// every <see cref="DefaultInterval"/>.
        /// </summary>
        /// <param name="presenter">The presenter to drive. Must be attached.</param>
        /// <param name="coroutines">Runs the timer.</param>
        /// <param name="model">Returns the model on every tick; receives the timer's scope.</param>
        /// <typeparam name="TView">The presenter's view type.</typeparam>
        /// <typeparam name="TModel">The presenter's model type.</typeparam>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>; nothing is started.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> is not attached.</exception>
        /// <exception cref="Exception">Whatever starting the coroutine, the first call of <paramref name="model"/>
        /// or the render of its result throws; the timer is stopped first.</exception>
        public static Presenter<TView, TModel> WithIntervalUpdate<TView, TModel>(
            this Presenter<TView, TModel> presenter, ICoroutineProvider coroutines, Func<IDisposable, TModel> model)
            where TView : class =>
            presenter.WithIntervalUpdate(coroutines, model, DefaultInterval);

        /// <summary>
        /// Sets <paramref name="presenter"/>'s model to what <paramref name="model"/> returns, now and then every
        /// <paramref name="interval"/>, until the presenter closes or the callback disposes the scope it receives.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The first update happens inside this call. If it throws, the timer is stopped and the exception reaches
        /// the caller. If a later update throws, the timer is stopped and Unity logs the exception.
        /// </para>
        /// <para>
        /// Disposing the scope from inside the callback still renders that call's result, and the callback is not
        /// called again. Two calls on one presenter run two timers. On a closed presenter nothing is started and
        /// the callback is not called.
        /// </para>
        /// </remarks>
        /// <param name="presenter">The presenter to drive. Must be attached.</param>
        /// <param name="coroutines">Runs the timer; typically injected into the calling presenter.</param>
        /// <param name="model">Returns the model on every tick, on the main thread; receives the timer's scope.
        /// </param>
        /// <param name="interval">The delay between updates in unscaled time; zero or less means every frame.
        /// </param>
        /// <typeparam name="TView">The presenter's view type.</typeparam>
        /// <typeparam name="TModel">The presenter's model type.</typeparam>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>; nothing is started.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> is not attached.</exception>
        /// <exception cref="Exception">Whatever <paramref name="coroutines"/> throws when it cannot start the
        /// coroutine (<see cref="InvalidOperationException"/> for an inactive or destroyed host, by the contract
        /// of <see cref="ICoroutineProvider.StartCoroutine"/>), or whatever the first call of
        /// <paramref name="model"/> or the render of its result throws. The timer is stopped first.</exception>
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
        /// Drives a text presenter from a callback returning a <c>string</c>, every
        /// <see cref="DefaultInterval"/>; see
        /// <see cref="WithIntervalUpdate{TView,TModel}(Presenter{TView,TModel},ICoroutineProvider,Func{IDisposable,TModel},TimeSpan)"/>.
        /// </summary>
        /// <param name="presenter">Any attached presenter whose model is a <see cref="TextModel"/>.</param>
        /// <param name="coroutines">Runs the timer.</param>
        /// <param name="text">Returns the text on every tick; receives the timer's scope.</param>
        /// <typeparam name="TView">The presenter's view type.</typeparam>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>; nothing is started.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> is not attached.</exception>
        /// <exception cref="Exception">Whatever starting the coroutine, the first call of <paramref name="text"/>
        /// or the render of its result throws; the timer is stopped first.</exception>
        public static Presenter<TView, TextModel> WithIntervalUpdate<TView>(this Presenter<TView, TextModel> presenter,
            ICoroutineProvider coroutines, Func<IDisposable, string> text)
            where TView : class =>
            presenter.WithIntervalUpdate(coroutines, text, DefaultInterval);

        /// <summary>
        /// Drives a text presenter from a callback returning a <c>string</c>; see
        /// <see cref="WithIntervalUpdate{TView,TModel}(Presenter{TView,TModel},ICoroutineProvider,Func{IDisposable,TModel},TimeSpan)"/>.
        /// </summary>
        /// <param name="presenter">Any attached presenter whose model is a <see cref="TextModel"/>.</param>
        /// <param name="coroutines">Runs the timer.</param>
        /// <param name="text">Returns the text on every tick; receives the timer's scope.</param>
        /// <param name="interval">The delay between updates in unscaled time; zero or less means every frame.
        /// </param>
        /// <typeparam name="TView">The presenter's view type.</typeparam>
        /// <returns><paramref name="presenter"/>.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>; nothing is started.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="presenter"/> is not attached.</exception>
        /// <exception cref="Exception">Whatever starting the coroutine, the first call of <paramref name="text"/>
        /// or the render of its result throws; the timer is stopped first.</exception>
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

                try
                {
                    presenter.SetModel(model(timer));
                }
                catch
                {
                    // The coroutine ends with this exception (Unity logs it); end the timer's scope with it.
                    timer.Terminate();
                    throw;
                }
            }
        }

        // A wait in unscaled time that re-arms itself when it completes, so one instance serves every tick. It reads
        // the double clock: WaitForSecondsRealtime reads the float one, on which a short interval rounds away to
        // nothing after a long session.
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
