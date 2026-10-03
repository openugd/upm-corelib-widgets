using System;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// Renders a <c>float</c> on a <see cref="Slider"/> and reports the slider's changes through
    /// <see cref="ValueChanged"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="float.NaN"/> model leaves the slider where it is. The value is written with
    /// <c>SetValueWithoutNotify</c>, so a render is never reported; <c>minValue</c>, <c>maxValue</c> and
    /// <c>wholeNumbers</c> are left as authored and clamp the written value. One listener is added per attached
    /// view, on its <c>ViewLifetime</c>.
    /// </remarks>
    public class SliderFloatPresenter : Presenter<Slider, float>
    {
        private Signal<float> _valueChanged;

        /// <summary>
        /// Raised with the slider's value for every change it reports — a drag, or code assigning
        /// <c>Slider.value</c> or its range — and never for the presenter's own render. The value is the slider's,
        /// after clamping, so it can differ from the model.
        /// </summary>
        /// <remarks>
        /// Created on first read and scoped to <see cref="Presenter.Lifetime"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Read before the presenter is attached.</exception>
        public ISignal<float> ValueChanged => _valueChanged ??= new Signal<float>(Lifetime);

        /// <summary>
        /// Adds the change listener to the attached slider, scoped to its <c>ViewLifetime</c>.
        /// </summary>
        protected override void OnViewAdded() => View.onValueChanged.Subscribe(ViewLifetime, OnValueChanged);

        /// <summary>
        /// Writes the model without notification, unless it is <see cref="float.NaN"/>.
        /// </summary>
        protected override void OnRefresh()
        {
            if (!float.IsNaN(Model)) View.SetValueWithoutNotify(Model);
        }

        private void OnValueChanged(float value) => _valueChanged?.Fire(value);
    }

    /// <summary>
    /// Creates <see cref="SliderFloatPresenter"/>s.
    /// </summary>
    public static class SliderFloatPresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="SliderFloatPresenter"/> under <paramref name="parent"/>, then sets its model and
        /// its view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The slider, or <c>null</c> to render once a view is set.</param>
        /// <param name="value">The value to show; <see cref="float.NaN"/>, the default, leaves the slider alone.
        /// </param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> is not attached, or has closed.
        /// </exception>
        public static SliderFloatPresenter AddSliderFloat(this Presenter parent, Slider view,
            float value = float.NaN)
        {
            var presenter = parent.AddPresenter(new SliderFloatPresenter());
            presenter.SetModel(value);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// As <see cref="AddSliderFloat(Presenter, Slider, float)"/>, and subscribes <paramref name="onChange"/> to
        /// <see cref="SliderFloatPresenter.ValueChanged"/> for the life of the presenter.
        /// </summary>
        /// <param name="parent">An attached, live presenter.</param>
        /// <param name="view">The slider, or <c>null</c> to render once a view is set.</param>
        /// <param name="value">The value to show, or <see cref="float.NaN"/> to leave the slider alone.</param>
        /// <param name="onChange">Receives every value the slider reports.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="onChange"/> is <c>null</c>; nothing is attached.
        /// </exception>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> is not attached, or has closed.
        /// </exception>
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
        /// <see cref="float.NaN"/>: reports changes and leaves the slider where it is.
        /// </summary>
        /// <param name="parent">An attached, live presenter.</param>
        /// <param name="view">The slider, or <c>null</c> to listen once a view is set.</param>
        /// <param name="onChange">Receives every value the slider reports.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="onChange"/> is <c>null</c>; nothing is attached.
        /// </exception>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> is not attached, or has closed.
        /// </exception>
        public static SliderFloatPresenter AddSliderFloat(this Presenter parent, Slider view,
            Action<float> onChange) => parent.AddSliderFloat(view, float.NaN, onChange);
    }
}
