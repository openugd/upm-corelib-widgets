using System;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// The render state of a <see cref="TogglePresenter"/>: the checked state to show, and the callback for
    /// changes the toggle reports.
    /// </summary>
    /// <remarks>
    /// Read-only, so it is a snapshot: to show a different state, hand the presenter a new instance through
    /// <c>SetModel</c>. The presenter never writes back — toggling the view leaves <see cref="Value"/> as it
    /// was.
    /// </remarks>
    public class ToggleModel
    {
        /// <summary>
        /// Invoked with the new checked state whenever the toggle reports a change — the user's click, a
        /// <see cref="ToggleGroup"/> switching this toggle off, or code assigning <c>Toggle.isOn</c> — before
        /// any <see cref="TogglePresenter.Toggled"/> subscriber. Never invoked by the presenter's own render.
        /// <c>null</c> means no callback.
        /// </summary>
        public Action<bool> OnChanged { get; }

        /// <summary>
        /// The checked state the presenter shows.
        /// </summary>
        public bool Value { get; }

        /// <summary>
        /// Creates the state a <see cref="TogglePresenter"/> renders from.
        /// </summary>
        /// <param name="onChanged">The callback for reported changes, or <c>null</c> for none.</param>
        /// <param name="value">The checked state to show. Defaults to unchecked.</param>
        public ToggleModel(Action<bool> onChanged, bool value = false)
        {
            OnChanged = onChanged;
            Value = value;
        }
    }

    /// <summary>
    /// Drives a <see cref="Toggle"/> from a <see cref="ToggleModel"/>: renders the checked state, and reports
    /// every change the toggle makes to the model's callback and to the <see cref="Toggled"/> signal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Rendering never echoes.</b> The checked state is written with <c>Toggle.SetIsOnWithoutNotify</c>,
    /// so a render raises no <c>onValueChanged</c>, never reaches <see cref="ToggleModel.OnChanged"/> or
    /// <see cref="Toggled"/>, and is idempotent.
    /// </para>
    /// <para>
    /// A <c>null</c> model renders nothing — the toggle keeps its current state — and reported changes still
    /// reach <see cref="Toggled"/>.
    /// </para>
    /// <para>
    /// <b>One listener per attached view</b>, scoped to its <c>ViewLifetime</c>, so swapping or detaching the
    /// view moves or removes the listener.
    /// </para>
    /// </remarks>
    public class TogglePresenter : Presenter<Toggle, ToggleModel>
    {
        private Signal<bool> _toggled;

        /// <summary>
        /// Fires with the new checked state for every change the attached toggle reports, after
        /// <see cref="ToggleModel.OnChanged"/>. Never fires for the presenter's own render.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Created on first read and scoped to <see cref="Presenter.Lifetime"/>: every subscription ends at
        /// the earlier of the subscriber's lifetime and this presenter closing.
        /// </para>
        /// <para>
        /// <i>Changed in 2.0.0</i> — replaces <c>SubscribeOnClick(Lifetime, Action&lt;bool&gt;)</c>:
        /// write <c>Toggled.Subscribe(lifetime, handler)</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Read before the presenter is attached.</exception>
        public ISignal<bool> Toggled => _toggled ??= new Signal<bool>(Lifetime);

        /// <summary>
        /// Adds the change listener to the attached toggle, scoped to its <c>ViewLifetime</c>.
        /// </summary>
        protected override void OnViewAdded() => View.onValueChanged.Subscribe(ViewLifetime, OnValueChanged);

        /// <summary>
        /// Writes <see cref="ToggleModel.Value"/> to the view without notification. Does nothing for a
        /// <c>null</c> model.
        /// </summary>
        protected override void OnRefresh()
        {
            if (Model != null) View.SetIsOnWithoutNotify(Model.Value);
        }

        private void OnValueChanged(bool isOn)
        {
            Model?.OnChanged?.Invoke(isOn);
            _toggled?.Fire(isOn);
        }
    }

    /// <summary>
    /// One-call construction of a <see cref="TogglePresenter"/>, and joining its view to a
    /// <see cref="ToggleGroup"/>.
    /// </summary>
    public static class TogglePresenterExtensions
    {
        /// <summary>
        /// Creates a <see cref="TogglePresenter"/> under <paramref name="parent"/>, sets its model to
        /// <paramref name="model"/> and then its view to <paramref name="view"/>, which renders it once.
        /// </summary>
        /// <param name="parent">The presenter to attach to. It must be attached and alive; the new presenter
        /// closes no later than it does.</param>
        /// <param name="view">The toggle. <c>null</c> attaches a presenter that renders when a view is set.
        /// </param>
        /// <param name="model">The state to render. <c>null</c> leaves the toggle as it is.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> has not been attached, or has
        /// closed.</exception>
        public static TogglePresenter AddToggle(this Presenter parent, Toggle view, ToggleModel model)
        {
            var presenter = parent.AddPresenter(new TogglePresenter());
            presenter.SetModel(model);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// Puts the presenter's view into <paramref name="group"/>, so the group's mutual exclusion covers it.
        /// </summary>
        /// <remarks>
        /// Does nothing when the presenter has no view, its view has been destroyed, or
        /// <paramref name="group"/> is <c>null</c>. Closing the presenter does not take the toggle out of the
        /// group.
        /// </remarks>
        /// <param name="toggle">The presenter whose view joins the group.</param>
        /// <param name="group">The group to join, or <c>null</c> to do nothing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="toggle"/> is <c>null</c>.</exception>
        public static void RegisterToggleInGroup(this TogglePresenter toggle, ToggleGroup group)
        {
            if (toggle == null)
                throw new ArgumentNullException(nameof(toggle), $"{nameof(toggle)} can't be null");

            if (toggle.View == null || group == null)
            {
                return;
            }

            group.RegisterToggle(toggle.View);
            toggle.View.group = group;
        }
    }
}
