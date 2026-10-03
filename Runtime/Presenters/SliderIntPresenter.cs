using System;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// The render state of a <see cref="SliderIntPresenter"/>: the range, the position inside it, and the
    /// callback for changes the slider reports.
    /// </summary>
    /// <remarks>
    /// Read-only, so it is a snapshot: push a new instance through <c>SetModel</c> to move the slider or to
    /// change its range. The values are <c>long</c> but the slider stores <c>float</c>, so magnitudes past
    /// 2^24 lose precision at the view.
    /// </remarks>
    public class SliderIntModel
    {
        /// <summary>
        /// Invoked with the slider's new value — always a whole number, since the presenter turns on
        /// <c>wholeNumbers</c> — whenever the slider reports a change: the user dragging, or code assigning
        /// <c>Slider.value</c>. Never invoked by the presenter's own render. <c>null</c> means no callback.
        /// </summary>
        public Action<float> OnValueChanged { get; }

        /// <summary>The low end of the range, written to <c>Slider.minValue</c>.</summary>
        public long MinValue { get; }

        /// <summary>The high end of the range, written to <c>Slider.maxValue</c>.</summary>
        public long MaxValue { get; }

        /// <summary>The position to show. The slider clamps it into the range.</summary>
        public long Value { get; }

        /// <summary>
        /// Creates the state a <see cref="SliderIntPresenter"/> renders from.
        /// </summary>
        /// <param name="minValue">The low end of the range.</param>
        /// <param name="maxValue">The high end of the range. Not validated against
        /// <paramref name="minValue"/>.</param>
        /// <param name="value">The position to show. Not validated against the range; the slider clamps it.
        /// </param>
        /// <param name="onValueChanged">The callback for reported changes, or <c>null</c> for none.</param>
        public SliderIntModel(long minValue, long maxValue, long value, Action<float> onValueChanged = null)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Value = value;
            OnValueChanged = onValueChanged;
        }
    }

    /// <summary>
    /// Drives a <see cref="Slider"/> from a <see cref="SliderIntModel"/>: renders the range and the position
    /// as whole numbers, and forwards every change the slider reports to the model's callback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Whole numbers.</b> Every render sets <c>Slider.wholeNumbers</c>, then the range, then the value, so
    /// the slider rounds what the user drags to.
    /// </para>
    /// <para>
    /// <b>Rendering never echoes.</b> The value is written with <c>Slider.SetValueWithoutNotify</c>. Writing
    /// <c>wholeNumbers</c> or the range can make the slider re-clamp its current value and raise
    /// <c>onValueChanged</c>; the presenter ignores whatever is raised while it renders, so
    /// <see cref="SliderIntModel.OnValueChanged"/> never sees it. Other listeners on the slider do.
    /// </para>
    /// <para>
    /// A <c>null</c> model renders nothing. <b>One listener per attached view</b>, scoped to its
    /// <c>ViewLifetime</c>, so swapping or detaching the view moves or removes the listener.
    /// </para>
    /// </remarks>
    public class SliderIntPresenter : Presenter<Slider, SliderIntModel>
    {
        private bool _rendering;

        /// <summary>
        /// Adds the change listener to the attached slider, scoped to its <c>ViewLifetime</c>.
        /// </summary>
        protected override void OnViewAdded() => View.onValueChanged.Subscribe(ViewLifetime, OnValueChanged);

        /// <summary>
        /// Writes <c>wholeNumbers</c>, the range and the value to the slider. Does nothing for a <c>null</c>
        /// model. Idempotent.
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
        }
    }

    /// <summary>
    /// One-call construction of a <see cref="SliderIntPresenter"/>. To change the range or the position
    /// later, call <c>SetModel</c> on the presenter it returns.
    /// </summary>
    public static class SliderIntPresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="SliderIntPresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="model"/> and then its view to <paramref name="view"/>, which renders it once.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter
        /// closes no later than it does.</param>
        /// <param name="view">The slider. <c>null</c> attaches a presenter that renders when a view is set.
        /// </param>
        /// <param name="model">The range and position to show. <c>null</c> leaves the slider as it is.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached, or has
        /// closed.</exception>
        public static SliderIntPresenter AddSliderInt(this Presenter parent, Slider view, SliderIntModel model)
        {
            var presenter = parent.AddPresenter(new SliderIntPresenter());
            presenter.SetModel(model);
            presenter.SetView(view);
            return presenter;
        }
    }
}
