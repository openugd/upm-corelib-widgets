using System;
using UnityEngine.UI;

namespace OpenUGD.Presenters
{
    /// <summary>
    /// What a <see cref="TogglePresenter"/> renders: the checked state, and a callback for reported changes.
    /// </summary>
    /// <remarks>
    /// Immutable. The presenter never writes back to it; to show another state, set a new instance.
    /// </remarks>
    public class ToggleModel
    {
        /// <summary>
        /// Called with the new state for every change the toggle reports — a click, a <see cref="ToggleGroup"/>
        /// turning it off, or code assigning <c>Toggle.isOn</c> — before <see cref="TogglePresenter.Toggled"/>.
        /// Never called for the presenter's own render. <c>null</c> for none.
        /// </summary>
        public Action<bool> OnChanged { get; }

        /// <summary>
        /// The checked state to show.
        /// </summary>
        public bool Value { get; }

        /// <summary>
        /// Creates a toggle model.
        /// </summary>
        /// <param name="onChanged">The callback for reported changes, or <c>null</c>.</param>
        /// <param name="value">The checked state to show.</param>
        public ToggleModel(Action<bool> onChanged, bool value = false)
        {
            OnChanged = onChanged;
            Value = value;
        }
    }

    /// <summary>
    /// Renders a <see cref="ToggleModel"/> on a <see cref="Toggle"/> and reports the toggle's changes to the
    /// model's callback and to <see cref="Toggled"/>.
    /// </summary>
    /// <remarks>
    /// The state is written with <c>SetIsOnWithoutNotify</c>, so a render is never reported. A <c>null</c> model
    /// leaves the toggle as it is; reported changes still reach <see cref="Toggled"/>. One listener is added per
    /// attached view, on its <c>ViewLifetime</c>.
    /// </remarks>
    public class TogglePresenter : Presenter<Toggle, ToggleModel>
    {
        private Signal<bool> _toggled;

        /// <summary>
        /// Raised with the new state for every change the attached toggle reports, after
        /// <see cref="ToggleModel.OnChanged"/>. Never raised for the presenter's own render.
        /// </summary>
        /// <remarks>
        /// Created on first read and scoped to <see cref="Presenter.Lifetime"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Read before the presenter is attached.</exception>
        public ISignal<bool> Toggled => _toggled ??= new Signal<bool>(Lifetime);

        /// <summary>
        /// Adds the change listener to the attached toggle, scoped to its <c>ViewLifetime</c>.
        /// </summary>
        protected override void OnViewAdded() => View.onValueChanged.Subscribe(ViewLifetime, OnValueChanged);

        /// <summary>
        /// Writes <see cref="ToggleModel.Value"/> without notification. Does nothing for a <c>null</c> model.
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
    /// Creates <see cref="TogglePresenter"/>s and adds their views to a <see cref="ToggleGroup"/>.
    /// </summary>
    public static class TogglePresenterExtensions
    {
        /// <summary>
        /// Attaches a <see cref="TogglePresenter"/> under <paramref name="parent"/>, then sets its model and its
        /// view.
        /// </summary>
        /// <param name="parent">An attached, live presenter. The new presenter closes no later than it does.</param>
        /// <param name="view">The toggle, or <c>null</c> to render once a view is set.</param>
        /// <param name="model">The state to render, or <c>null</c> to leave the toggle as it is.</param>
        /// <returns>The attached presenter.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="parent"/> is not attached, or has closed.
        /// </exception>
        public static TogglePresenter AddToggle(this Presenter parent, Toggle view, ToggleModel model)
        {
            var presenter = parent.AddPresenter(new TogglePresenter());
            presenter.SetModel(model);
            presenter.SetView(view);
            return presenter;
        }

        /// <summary>
        /// Adds the presenter's view to <paramref name="group"/>.
        /// </summary>
        /// <remarks>
        /// Does nothing when the presenter has no live view or <paramref name="group"/> is <c>null</c>. Closing the
        /// presenter does not remove the toggle from the group.
        /// </remarks>
        /// <param name="toggle">The presenter whose view joins the group.</param>
        /// <param name="group">The group, or <c>null</c>.</param>
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
