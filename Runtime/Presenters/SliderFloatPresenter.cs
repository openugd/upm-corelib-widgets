using System;
using UnityEngine.UI;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// Drives a <see cref="Slider"/> whose model <i>is</i> its value, and which is itself the
    /// <see cref="ISignal{T1}"/> reporting changes — callers subscribe to the presenter, with no separate
    /// event object to reach for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Lifecycle.</b> <see cref="OnInitialize"/> creates the signal, <see cref="OnViewAdded"/> adds the
    /// Unity listener once per attached view and registers its removal on <see cref="Presenter.Lifetime"/>,
    /// and <see cref="OnRefresh"/> renders. Nothing here is ever unsubscribed by hand — closing this
    /// presenter, or any ancestor of it, unwires the view and drops every subscription.
    /// </para>
    /// <para>
    /// <b><see cref="float.NaN"/> means "leave it alone".</b> A <c>float</c> model has no unset state, so
    /// NaN is the sentinel for one: <see cref="OnRefresh"/> skips the write entirely and the slider keeps
    /// whatever position the scene authored. The presenter therefore never writes NaN to a view.
    /// </para>
    /// <para>
    /// <b>Rendering is not silent.</b> There is no without-notify path here — <see cref="OnRefresh"/>
    /// assigns <c>Slider.value</c>, so a render that actually moves the slider raises
    /// <c>onValueChanged</c> and fires this signal, carrying the slider's clamped value rather than the
    /// model's. Subscribers must tolerate that echo.
    /// </para>
    /// <para>
    /// <b>The range is never touched.</b> Only the value is written, so <c>minValue</c>, <c>maxValue</c>
    /// and <c>wholeNumbers</c> stay exactly as authored — and it is those that clamp and round what
    /// subscribers actually receive.
    /// </para>
    /// </remarks>
    public class SliderFloatPresenter : Presenter<Slider, float>, ISignal<float>
    {
        private Signal<float> _onChange;

        /// <summary>
        /// Creates the signal behind <see cref="Subscribe"/> and scopes it to
        /// <see cref="Presenter.Lifetime"/>. Runs when the presenter is attached, which is why
        /// <see cref="Subscribe"/> is unusable before then.
        /// </summary>
        protected override void OnInitialize()
        {
            _onChange = new Signal<float>(Lifetime);
        }

        /// <summary>
        /// Subscribes <paramref name="listener"/> to this slider's changes for as long as
        /// <paramref name="lifetime"/> is alive, or until the presenter closes — whichever comes first.
        /// </summary>
        /// <remarks>
        /// Listeners run in subscription order and receive the slider's own value: already clamped to its
        /// range and rounded by its <c>wholeNumbers</c> setting, and so not necessarily equal to
        /// <see cref="Presenter{TView,TModel}.Model"/>. They also see the echo of a render that moved the
        /// slider, not only the user's drags. One that throws does not stop the others; the failures
        /// surface together as an <see cref="AggregateException"/> thrown out of the Unity callback.
        /// </remarks>
        /// <param name="lifetime">The <i>subscriber's</i> scope, not the presenter's — the listener is
        /// detached when it terminates. Must not be <c>null</c>.</param>
        /// <param name="listener">Receives each new value. Must not be <c>null</c>.</param>
        /// <returns><c>true</c> if and only if the listener is registered and live when this returns;
        /// <c>false</c> when either scope was already dead, in which case nothing was registered.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="lifetime"/> or
        /// <paramref name="listener"/> is <c>null</c>.</exception>
        /// <exception cref="NullReferenceException">The presenter has not been attached yet, so
        /// <see cref="OnInitialize"/> has not run and there is no signal to subscribe to.</exception>
        public bool Subscribe(Lifetime lifetime, Action<float> listener) => _onChange.Subscribe(lifetime, listener);


        /// <summary>
        /// Wires <c>Slider.onValueChanged</c> once per attached view, and unwires it when this presenter's
        /// scope ends.
        /// </summary>
        /// <remarks>
        /// The removal reads <see cref="Presenter{TView}.View"/> at termination time rather than closing
        /// over the view attached here, so closing a presenter whose view has been detached throws out of
        /// the clean-up itself.
        /// </remarks>
        protected override void OnViewAdded()
        {
            View.onValueChanged.AddListener(ValueChangedHandler);
            Lifetime.AddAction(() => View.onValueChanged.RemoveListener(ValueChangedHandler));
        }

        /// <summary>
        /// Writes the model to <c>Slider.value</c> unless it is <see cref="float.NaN"/>, in which case the
        /// slider is left alone. Idempotent, and called both when the view attaches and on every model
        /// change — which is why the render body no longer exists in two places.
        /// </summary>
        protected override void OnRefresh()
        {
            if (!float.IsNaN(Model)) View.value = Model;
        }

        private void ValueChangedHandler(float newValue)
        {
            _onChange.Fire(newValue);
        }
    }

    /// <summary>
    /// Building a <see cref="SliderFloatPresenter"/> in one call. Three overloads, differing only in
    /// whether they seed a value, hook up a listener, or both.
    /// </summary>
    /// <remarks>
    /// All three set the model before attaching the view, so a presenter built here renders at most once —
    /// inside the call, and not at all without a view — before any listener passed here can be subscribed,
    /// which is why none of them deliver the initial value to <c>onChange</c>.
    /// </remarks>
    public static class SliderPresenterExtensions
    {
        /// <summary>
        /// Constructs a <see cref="SliderFloatPresenter"/>, attaches it under <paramref name="parent"/>,
        /// and renders <paramref name="value"/> unless it is left at NaN. Subscribe afterwards with
        /// <see cref="SliderFloatPresenter.Subscribe"/>, choosing your own scope.
        /// </summary>
        /// <remarks>
        /// The new presenter closes with <paramref name="parent"/>; call <see cref="Presenter.Close"/> on
        /// it to end it sooner.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached under. Must already be attached
        /// itself and still alive.</param>
        /// <param name="view">The slider to drive. A <c>null</c> view leaves the presenter attached but
        /// inert: nothing renders and nothing is wired.</param>
        /// <param name="value">The value to render. Defaults to <see cref="float.NaN"/>, which leaves the
        /// slider at whatever position the scene authored.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached
        /// yet, or its lifetime has already terminated.</exception>
        public static SliderFloatPresenter AddFloatSlider(this Presenter parent, Slider view, float value = float.NaN)
        {
            var presenter = new SliderFloatPresenter();
            parent.AddPresenter(presenter);

            presenter.SetModel(value);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// As <see cref="AddFloatSlider(Presenter, Slider, float)"/>, and subscribes
        /// <paramref name="onChange"/> for the whole life of the presenter.
        /// </summary>
        /// <remarks>
        /// <paramref name="onChange"/> is subscribed after the view is attached, so it never sees the
        /// render of <paramref name="value"/> — only what happens afterwards. It is scoped to the
        /// presenter's own <see cref="Presenter.Lifetime"/>, so it is dropped when the presenter closes and
        /// there is nothing to unsubscribe; use <see cref="SliderFloatPresenter.Subscribe"/> directly when
        /// the listener needs a shorter scope than that.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached under. Must already be attached
        /// itself and still alive.</param>
        /// <param name="view">The slider to drive. A <c>null</c> view leaves the presenter attached but
        /// inert.</param>
        /// <param name="value">The value to render, or <see cref="float.NaN"/> to leave the slider
        /// alone.</param>
        /// <param name="onChange">Receives each subsequent value, already clamped and rounded by the
        /// slider. Must not be <c>null</c>.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached
        /// yet, or its lifetime has already terminated.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="onChange"/> is <c>null</c>.</exception>
        public static SliderFloatPresenter AddFloatSlider(this Presenter parent, Slider view, float value,
            Action<float> onChange)
        {
            var presenter = new SliderFloatPresenter();
            parent.AddPresenter(presenter);

            presenter.SetModel(value);
            presenter.SetView(view);
            presenter.Subscribe(presenter.Lifetime, onChange);

            return presenter;
        }

        /// <summary>
        /// Attaches a <see cref="SliderFloatPresenter"/> that reports changes to
        /// <paramref name="onChange"/> but leaves the slider's authored position untouched — the model is
        /// <see cref="float.NaN"/>, so the first render writes nothing.
        /// </summary>
        /// <remarks>
        /// The overload for a slider whose starting position belongs to the scene rather than to code.
        /// Nothing reaches the view until something calls <c>SetModel</c> with a real number.
        /// <paramref name="onChange"/> is scoped to the presenter's own <see cref="Presenter.Lifetime"/>;
        /// see <see cref="AddFloatSlider(Presenter, Slider, float, Action{float})"/>.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached under. Must already be attached
        /// itself and still alive.</param>
        /// <param name="view">The slider to drive. A <c>null</c> view leaves the presenter attached but
        /// inert.</param>
        /// <param name="onChange">Receives each value the slider reports. Must not be <c>null</c>.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached
        /// yet, or its lifetime has already terminated.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="onChange"/> is <c>null</c>.</exception>
        public static SliderFloatPresenter AddFloatSlider(this Presenter parent, Slider view, Action<float> onChange)
        {
            var presenter = new SliderFloatPresenter();
            parent.AddPresenter(presenter);

            presenter.SetModel(float.NaN);
            presenter.SetView(view);
            presenter.Subscribe(presenter.Lifetime, onChange);

            return presenter;
        }
    }
}
