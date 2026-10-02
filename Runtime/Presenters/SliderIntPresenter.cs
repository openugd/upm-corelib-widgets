using System;
using UnityEngine.UI;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// The render state of a <see cref="SliderIntPresenter"/>: the range, the position inside it, and the
    /// callback for changes. The range and the position are written to the view together on every render;
    /// the callback stays on this side and is never handed to the slider.
    /// </summary>
    /// <remarks>
    /// Read-only throughout, so it is a snapshot rather than a live object: push a new instance through
    /// <c>SetModel</c> (or <see cref="SliderIntPresenterExtensions.UpdateSliderInt"/>) to move the slider or
    /// to widen its range.
    /// </remarks>
    public class SliderIntModel
    {
        /// <summary>
        /// Invoked with the slider's own value — a <c>float</c>, not a rounded <c>long</c> — whenever that
        /// value changes. <c>null</c> means "no callback".
        /// </summary>
        /// <remarks>
        /// Fires for renders as well as for drags: the presenter assigns <c>Slider.value</c> rather than
        /// calling <c>SetValueWithoutNotify</c>, and Unity's setter notifies whenever the stored value
        /// actually moves. Handlers that push a new model back in must therefore tolerate re-entry.
        /// </remarks>
        public Action<float> OnValueChanged { get; }

        /// <summary>
        /// The low end of the range, written to <c>Slider.minValue</c> on every render. Held as a
        /// <c>long</c> but narrowed to <c>float</c> at the view, so magnitudes past 2^24 lose precision.
        /// </summary>
        public long MinValue { get; }

        /// <summary>
        /// The high end of the range, written to <c>Slider.maxValue</c> on every render, after
        /// <see cref="MinValue"/>. Same <c>float</c> narrowing as <see cref="MinValue"/>.
        /// </summary>
        public long MaxValue { get; }

        /// <summary>
        /// The position to render, written last so it lands inside the range set just before it. The slider
        /// clamps it, and rounds it when its own <c>wholeNumbers</c> is on — which this presenter never
        /// sets, so despite the name a slider left in float mode reports fractions.
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// Creates the state a <see cref="SliderIntPresenter"/> renders from.
        /// </summary>
        /// <param name="minValue">The low end of the range.</param>
        /// <param name="maxValue">The high end of the range. Not validated against
        /// <paramref name="minValue"/>; an inverted range is handed to the slider as-is.</param>
        /// <param name="value">The position to render. Not validated against the range either — the slider
        /// clamps it.</param>
        /// <param name="onValueChanged">Handler for value changes, or <c>null</c> for none.</param>
        public SliderIntModel(long minValue, long maxValue, long value, Action<float> onValueChanged = null)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Value = value;
            OnValueChanged = onValueChanged;
        }
    }

    /// <summary>
    /// Drives a <see cref="Slider"/> from a <see cref="SliderIntModel"/>: renders range and position, and
    /// forwards every change to the model's callback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Lifecycle.</b> <see cref="OnViewAdded"/> adds the Unity listener once per attached view and
    /// registers its removal on <see cref="Presenter.Lifetime"/>; <see cref="OnRefresh"/> renders. Nothing
    /// here is ever unsubscribed by hand — closing this presenter, or any ancestor of it, unwires the view.
    /// </para>
    /// <para>
    /// <b>Rendering is not silent.</b> Unlike <see cref="TogglePresenter"/>, this presenter has no
    /// without-notify path: it assigns <c>Slider.value</c>, so a render that actually moves the slider
    /// raises <c>onValueChanged</c> and reaches <see cref="SliderIntModel.OnValueChanged"/> — carrying the
    /// slider's clamped <c>float</c>, which need not equal <see cref="SliderIntModel.Value"/>. Callbacks
    /// that respond by setting a new model must be written for that.
    /// </para>
    /// <para>
    /// <b>The model must arrive before the view.</b> <see cref="OnRefresh"/> dereferences
    /// <see cref="Presenter{TView,TModel}.Model"/> unguarded, and attaching a view renders immediately, so
    /// <c>SetView</c> before <c>SetModel</c> throws <see cref="NullReferenceException"/>.
    /// </para>
    /// <para>
    /// <b>"Int" is the model's word, not the view's.</b> Nothing sets <c>Slider.wholeNumbers</c>; the
    /// integral range is a promise about what is written, never about what comes back.
    /// </para>
    /// </remarks>
    public class SliderIntPresenter : Presenter<Slider, SliderIntModel>
    {
        /// <summary>
        /// Wires <c>Slider.onValueChanged</c> for the view just attached, and registers the matching
        /// removal on <see cref="Presenter.Lifetime"/> so a surviving view is never left holding a listener
        /// into a closed presenter.
        /// </summary>
        /// <remarks>
        /// The removal closes over <see cref="Presenter{TView}.View"/> as read at termination time, not
        /// over the view attached here, and one is registered per attach. A presenter that swaps or detaches
        /// its view — rather than living and dying with it — cannot rely on this to unwire the right one,
        /// and closing one whose view has been detached throws out of the clean-up itself.
        /// </remarks>
        protected override void OnViewAdded()
        {
            View.onValueChanged.AddListener(OnValueChanged);
            Lifetime.AddAction(() => { View.onValueChanged.RemoveListener(OnValueChanged); });
        }
        /// <summary>
        /// Renders the model onto the slider. Idempotent, and called both when the view attaches and on every
        /// model change — 1.x split this between OnReady (range + value) and OnAfterModelChanged (value
        /// only), so a range change after the first render was silently ignored.
        /// </summary>
        protected override void OnRefresh()
        {
            View.minValue = Model.MinValue;
            View.maxValue = Model.MaxValue;
            View.value    = Model.Value;
        }

        private void OnValueChanged(float value)
        {
            Model.OnValueChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// Building a <see cref="SliderIntPresenter"/> in one call, and re-rendering one afterwards.
    /// </summary>
    public static class SliderIntPresenterExtensions
    {
        /// <summary>
        /// Constructs a <see cref="SliderIntPresenter"/>, attaches it under <paramref name="parent"/>, and
        /// hands it <paramref name="view"/> and <paramref name="model"/>.
        /// </summary>
        /// <remarks>
        /// The new presenter closes with <paramref name="parent"/>; call <see cref="Presenter.Close"/> on
        /// it to end it sooner.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached under. Must already be attached
        /// itself and still alive.</param>
        /// <param name="view">The slider to drive. A <c>null</c> view leaves the presenter attached but
        /// inert: nothing renders and nothing is wired.</param>
        /// <param name="model">The range and position to render.</param>
        /// <returns>The attached presenter. It has rendered nothing: the only case that returns at all is
        /// the <c>null</c> view, and the other throws.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached
        /// yet, or its lifetime has already terminated.</exception>
        /// <exception cref="NullReferenceException">
        /// For any non-<c>null</c> <paramref name="view"/>: the view is attached before the model is set,
        /// and the render that the attach triggers dereferences a model that is still <c>null</c>. Build
        /// the presenter by hand — <c>AddPresenter</c>, then <c>SetModel</c>, then <c>SetView</c> — to
        /// avoid it.
        /// </exception>
        public static SliderIntPresenter AddSliderInt(this Presenter parent, Slider view, SliderIntModel model)
        {
            var presenter = new SliderIntPresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(model);

            return presenter;
        }

        /// <summary>
        /// Replaces the model and re-renders — range included, so this is how a slider's bounds change
        /// after the first render.
        /// </summary>
        /// <remarks>
        /// Exactly <c>SetModel</c>, under a name that reads as the counterpart of
        /// <see cref="AddSliderInt"/>. Setting the same instance again still re-renders; if the render moves
        /// the slider, <see cref="SliderIntModel.OnValueChanged"/> on the <i>new</i> model fires as part of
        /// this call.
        /// </remarks>
        /// <param name="presenter">The presenter to re-render.</param>
        /// <param name="model">The new range and position. Must not be <c>null</c>.</param>
        /// <exception cref="NullReferenceException"><paramref name="presenter"/> is <c>null</c>, or
        /// <paramref name="model"/> is <c>null</c> and the presenter is in a state that renders — a view
        /// attached and its own scope still alive. The old model is already gone by then.</exception>
        public static void UpdateSliderInt(this SliderIntPresenter presenter, SliderIntModel model)
        {
            presenter.SetModel(model);
        }
    }
}
