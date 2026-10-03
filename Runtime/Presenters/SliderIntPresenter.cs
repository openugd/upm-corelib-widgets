using System;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// What a <see cref="SliderIntPresenter"/> renders: a whole-number range, a position in it, and a callback
    /// for reported changes.
    /// </summary>
    /// <remarks>
    /// Immutable; set a new instance to move the slider or change its range. The slider stores <c>float</c>, so
    /// values beyond ±2^24 lose precision. Nothing is validated: the slider clamps <see cref="Value"/>.
    /// </remarks>
    public class SliderIntModel
    {
        /// <summary>
        /// Called with the slider's new value, a whole number, for every change it reports — a drag, or code
        /// assigning <c>Slider.value</c> — before <see cref="SliderIntPresenter.ValueChanged"/>. Never called for
        /// the presenter's own render. <c>null</c> for none.
        /// </summary>
        public Action<float> OnValueChanged { get; }

        /// <summary>The low end of the range, written to <c>Slider.minValue</c>.</summary>
        public long MinValue { get; }

        /// <summary>The high end of the range, written to <c>Slider.maxValue</c>.</summary>
        public long MaxValue { get; }

        /// <summary>The position to show.</summary>
        public long Value { get; }

        /// <summary>
        /// Creates a slider model.
        /// </summary>
        /// <param name="minValue">The low end of the range.</param>
        /// <param name="maxValue">The high end of the range.</param>
        /// <param name="value">The position to show.</param>
        /// <param name="onValueChanged">The callback for reported changes, or <c>null</c>.</param>
        public SliderIntModel(long minValue, long maxValue, long value, Action<float> onValueChanged = null)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Value = value;
            OnValueChanged = onValueChanged;
        }
    }

    /// <summary>
    /// Renders a <see cref="SliderIntModel"/> on a <see cref="Slider"/> in whole numbers and reports the slider's
    /// changes to the model's callback and to <see cref="ValueChanged"/>.
    /// </summary>
    /// <remarks>
    /// A render sets <c>wholeNumbers</c>, the range, then the value with <c>SetValueWithoutNotify</c>. Writing
    /// the range can make the slider re-clamp and notify; whatever it raises during a render reaches neither the
    /// model's callback nor <see cref="ValueChanged"/> (other listeners on the slider do see it). A <c>null</c>
    /// model leaves the slider as it is. One listener is added per attached view, on its <c>ViewLifetime</c>.
    /// </remarks>
    public class SliderIntPresenter : Presenter<Slider, SliderIntModel>
    {
        private Signal<float> _valueChanged;
        private bool _rendering;

        /// <summary>
        /// Raised with the slider's value for every change it reports, after
        /// <see cref="SliderIntModel.OnValueChanged"/>. Never raised for the presenter's own render.
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
        /// Writes <c>wholeNumbers</c>, the range and the value. Does nothing for a <c>null</c> model.
        /// </summary>
        protected override void OnRefresh()
        {
            if (Model == null) return;

            _rendering = true;
            try
            {
                View.wholeNumbers = true;
                View.minValue = Model.MinValue;
                View.maxValue = Model.MaxValue;
                View.SetValueWithoutNotify(Model.Value);
            }
            finally
            {
                _rendering = false;
            }
        }

        private void OnValueChanged(float value)
        {
            if (_rendering) return;
            Model?.OnValueChanged?.Invoke(value);
            _valueChanged?.Fire(value);
        }
    }

    /// <summary>
    /// Creates <see cref="SliderIntPresenter"/>s.
    /// </summary>
    public static class SliderIntPresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="SliderIntPresenter"/> under <paramref name="parent"/>, then sets its model and its
        /// view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The slider, or <c>null</c> to render once a view is set.</param>
        /// <param name="model">The range and position, or <c>null</c> to leave the slider as it is.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> is not attached, or has closed.
        /// </exception>
        public static SliderIntPresenter AddSliderInt(this Presenter parent, Slider view, SliderIntModel model)
        {
            var presenter = parent.AddPresenter(new SliderIntPresenter());
            presenter.SetModel(model);
            presenter.SetView(view);
            return presenter;
        }
    }
}
