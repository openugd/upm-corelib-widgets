using System;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Drives a <see cref="Slider"/> whose model is its value, and reports every change the slider makes
    /// through the <see cref="ValueChanged"/> signal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="float.NaN"/> means "leave it alone".</b> A <c>float</c> has no unset state, so a NaN model
    /// renders nothing and the slider keeps the position the scene authored.
    /// </para>
    /// <para>
    /// <b>Rendering never echoes.</b> The value is written with <c>Slider.SetValueWithoutNotify</c>, so a
    /// render raises no <c>onValueChanged</c> and never reaches <see cref="ValueChanged"/>. Only the value is
    /// written: <c>minValue</c>, <c>maxValue</c> and <c>wholeNumbers</c> stay as authored, and the slider
    /// clamps and rounds the written value by them.
    /// </para>
    /// <para>
    /// <b>One listener per attached view</b>, scoped to its <c>ViewLifetime</c>, so swapping or detaching the
    /// view moves or removes the listener.
    /// </para>
    /// </remarks>
    public class SliderFloatPresenter : Presenter<Slider, float>
    {
        private Signal<float> _valueChanged;

        /// <summary>
        /// Fires with the slider's new value for every change the attached slider reports — the user dragging,
        /// or code assigning <c>Slider.value</c> or its range — and never for the presenter's own render. The
        /// value is the slider's own, already clamped and rounded, so it need not equal the model.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Created on first read and scoped to <see cref="Presenter.Lifetime"/>: every subscription ends at
        /// the earlier of the subscriber's lifetime and this presenter closing.
        /// </para>
        /// <para>
        /// <i>Changed in 2.0.0</i> — replaces the presenter itself implementing <c>ISignal&lt;float&gt;</c>:
        /// write <c>presenter.ValueChanged.Subscribe(lifetime, handler)</c> where you wrote
        /// <c>presenter.Subscribe(lifetime, handler)</c>. It no longer reports the presenter's own renders, and
        /// using it before attach throws <see cref="InvalidOperationException"/> instead of
        /// <see cref="NullReferenceException"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Read before the presenter is attached.</exception>
        public ISignal<float> ValueChanged => _valueChanged ??= new Signal<float>(Lifetime);

        /// <summary>
        /// Adds the change listener to the attached slider, scoped to its <c>ViewLifetime</c>.
        /// </summary>
        protected override void OnViewAdded() => View.onValueChanged.Subscribe(ViewLifetime, OnValueChanged);

        /// <summary>
        /// Writes the model to the slider without notification, unless it is <see cref="float.NaN"/>.
        /// Idempotent.
        /// </summary>
        protected override void OnRefresh()
        {
            if (!float.IsNaN(Model)) View.SetValueWithoutNotify(Model);
        }

        private void OnValueChanged(float value) => _valueChanged?.Fire(value);
    }

    /// <summary>
    /// One-call construction of a <see cref="SliderFloatPresenter"/>, optionally with a change handler.
    /// </summary>
    public static class SliderFloatPresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="SliderFloatPresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="value"/> and then its view to <paramref name="view"/>, which renders it once.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter
        /// closes no later than it does.</param>
        /// <param name="view">The slider. <c>null</c> attaches a presenter that renders when a view is set.
        /// </param>
        /// <param name="value">The value to show. The default, <see cref="float.NaN"/>, leaves the slider where
        /// the scene put it.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached, or has
        /// closed.</exception>
        public static SliderFloatPresenter AddSliderFloat(this Presenter parent, Slider view,
            float value = float.NaN)
        {
            var presenter = parent.AddPresenter(new SliderFloatPresenter());
            presenter.SetModel(value);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// As <see cref="AddSliderFloat(Presenter, Slider, float)"/>, and subscribes
        /// <paramref name="onChange"/> to <see cref="SliderFloatPresenter.ValueChanged"/> for the life of the
        /// presenter.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive.</param>
        /// <param name="view">The slider. <c>null</c> attaches a presenter that renders when a view is set.
        /// </param>
        /// <param name="value">The value to show, or <see cref="float.NaN"/> to leave the slider alone.</param>
        /// <param name="onChange">Receives every value the slider reports.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="onChange"/> is <c>null</c>. Nothing is
        /// attached.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached, or has
        /// closed.</exception>
        public static SliderFloatPresenter AddSliderFloat(this Presenter parent, Slider view, float value,
            Action<float> onChange)
        {
            if (onChange == null)
                throw new ArgumentNullException(nameof(onChange), $"{nameof(onChange)} can't be null");

            var presenter = parent.AddSliderFloat(view, value);
            presenter.ValueChanged.Subscribe(presenter.Lifetime, onChange);
            return presenter;
        }

        /// <summary>
        /// As <see cref="AddSliderFloat(Presenter, Slider, float, Action{float})"/> with
        /// <see cref="float.NaN"/>: reports changes and leaves the slider where the scene put it.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive.</param>
        /// <param name="view">The slider. <c>null</c> attaches a presenter that renders when a view is set.
        /// </param>
        /// <param name="onChange">Receives every value the slider reports.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="onChange"/> is <c>null</c>. Nothing is
        /// attached.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached, or has
        /// closed.</exception>
        public static SliderFloatPresenter AddSliderFloat(this Presenter parent, Slider view,
            Action<float> onChange) => parent.AddSliderFloat(view, float.NaN, onChange);
    }
}
