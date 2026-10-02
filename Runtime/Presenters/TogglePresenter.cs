using System;
using UnityEngine.UI;

namespace OpenUGD.Core.Presenters
{
    /// <summary>
    /// The render state of a <see cref="TogglePresenter"/>: the checked state to show, and the callback to
    /// run when the user changes it.
    /// </summary>
    /// <remarks>
    /// Every property is read-only, so this is a snapshot rather than a live object: to show a different
    /// state, hand the presenter a new instance through <c>SetModel</c>, which re-renders. The presenter
    /// never writes back — toggling the view leaves <see cref="Value"/> exactly as it was.
    /// </remarks>
    public class ToggleModel
    {
        /// <summary>
        /// Invoked with the new checked state whenever the toggle raises <c>onValueChanged</c>, and before
        /// any <see cref="TogglePresenter.SubscribeOnClick"/> listener. That covers the user's clicks, a
        /// <see cref="ToggleGroup"/> deselecting this toggle, and code assigning <c>Toggle.isOn</c> — but
        /// never the presenter's own render, which suppresses notification. <c>null</c> means "no callback".
        /// </summary>
        public Action<bool> OnChanged { get; }

        /// <summary>
        /// The checked state pushed onto the view on every render, with notification suppressed — so a
        /// render can never be mistaken for a user click.
        /// </summary>
        public bool Value { get; }

        /// <summary>
        /// Creates the state a <see cref="TogglePresenter"/> renders from.
        /// </summary>
        /// <param name="onChanged">Handler for the changes the toggle reports, or <c>null</c> for none. Not
        /// validated here, and never invoked by rendering.</param>
        /// <param name="value">The checked state to show. Defaults to unchecked.</param>
        public ToggleModel(Action<bool> onChanged, bool value = false)
        {
            OnChanged = onChanged;
            Value = value;
        }
    }

    /// <summary>
    /// Drives a <see cref="Toggle"/> from a <see cref="ToggleModel"/>: renders the checked state, and turns
    /// every change the toggle reports into the model's callback plus, optionally, a signal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Lifecycle.</b> <see cref="OnViewAdded"/> adds the Unity listener once per attached view and
    /// registers its removal on <see cref="Presenter.Lifetime"/>; <see cref="OnRefresh"/> pushes
    /// <see cref="ToggleModel.Value"/> onto the view. Nothing here is ever unsubscribed by hand — closing
    /// this presenter, or any ancestor of it, unwires the view and drops every listener.
    /// </para>
    /// <para>
    /// <b>No feedback loop.</b> Rendering goes through <c>Toggle.SetIsOnWithoutNotify</c>, so pushing the
    /// model never raises <c>onValueChanged</c> and therefore never re-enters the model's callback. That is
    /// what makes <see cref="OnRefresh"/> idempotent: it writes the same state the second time and emits
    /// nothing either time.
    /// </para>
    /// <para>
    /// <b>The model must arrive before the view.</b> <see cref="OnRefresh"/> dereferences
    /// <see cref="Presenter{TView,TModel}.Model"/> unguarded, and attaching a view renders immediately, so
    /// <c>SetView</c> before <c>SetModel</c> throws <see cref="NullReferenceException"/>.
    /// </para>
    /// </remarks>
    public class TogglePresenter : Presenter<Toggle, ToggleModel>
    {
        private Signal<bool> _onClick;

        /// <summary>
        /// Wires <c>Toggle.onValueChanged</c> for the view just attached, and registers the matching
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
            View.onValueChanged.AddListener(ClickHandler);
            Lifetime.AddAction(() => { View.onValueChanged.RemoveListener(ClickHandler); });
        }

        /// <summary>
        /// Pushes <see cref="ToggleModel.Value"/> onto the view without notification. Idempotent, and
        /// called both when the view attaches and on every model change.
        /// </summary>
        /// <exception cref="NullReferenceException">No model has been set yet.</exception>
        protected override void OnRefresh()
        {
            View.SetIsOnWithoutNotify(Model.Value);
            base.OnRefresh();
        }

        /// <summary>
        /// Adds a listener for the changes the toggle reports, on top of
        /// <see cref="ToggleModel.OnChanged"/>, for as long as <paramref name="lifetime"/> is alive.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The backing <see cref="Signal{T1}"/> is created on the first call and scoped to
        /// <see cref="Presenter.Lifetime"/>, so a toggle nobody listens to allocates nothing. A listener is
        /// dropped when <paramref name="lifetime"/> ends or when the presenter closes, whichever comes
        /// first; there is no unsubscribe to call. Whether the listener actually took — both scopes could
        /// already be dead — is not reported back.
        /// </para>
        /// <para>
        /// Listeners run after <see cref="ToggleModel.OnChanged"/>, in subscription order, and see exactly
        /// what it sees: every change the toggle reports, never the presenter's own render. One that throws
        /// does not stop the others; the failures surface together as an <see cref="AggregateException"/>
        /// thrown out of the Unity callback that raised them.
        /// </para>
        /// </remarks>
        /// <param name="lifetime">The <i>subscriber's</i> scope, not the presenter's. Must not be
        /// <c>null</c>.</param>
        /// <param name="listener">Receives the new checked state. Must not be <c>null</c>.</param>
        /// <exception cref="InvalidOperationException">The presenter has not been attached yet, so it has
        /// no lifetime to scope the signal to.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="lifetime"/> or
        /// <paramref name="listener"/> is <c>null</c>.</exception>
        public void SubscribeOnClick(Lifetime lifetime, Action<bool> listener)
        {
            if (_onClick == null)
            {
                _onClick = new Signal<bool>(Lifetime);
            }

            _onClick.Subscribe(lifetime, listener);
        }

        private void ClickHandler(bool state)
        {
            Model.OnChanged?.Invoke(state);

            _onClick?.Fire(state);
        }
    }

    /// <summary>
    /// Building a <see cref="TogglePresenter"/> in one call, and putting one into a
    /// <see cref="ToggleGroup"/>.
    /// </summary>
    public static class TogglePresenterExtensions
    {
        /// <summary>
        /// Constructs a <see cref="TogglePresenter"/>, attaches it under <paramref name="parent"/>, and
        /// hands it <paramref name="view"/> and <paramref name="model"/>.
        /// </summary>
        /// <remarks>
        /// The new presenter closes with <paramref name="parent"/>; call <see cref="Presenter.Close"/> on
        /// it to end it sooner.
        /// </remarks>
        /// <param name="parent">The presenter the new one is attached under. Must already be attached
        /// itself and still alive.</param>
        /// <param name="view">The toggle to drive. A <c>null</c> view leaves the presenter attached but
        /// inert: nothing renders and nothing is wired.</param>
        /// <param name="model">The state to render.</param>
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
        public static TogglePresenter AddToggle(this Presenter parent, Toggle view, ToggleModel model)
        {
            var presenter = new TogglePresenter();
            parent.AddPresenter(presenter);

            presenter.SetView(view);
            presenter.SetModel(model);

            return presenter;
        }

        /// <summary>
        /// Puts the presenter's view into <paramref name="group"/> — registering it and pointing it back at
        /// the group — so the group's mutual exclusion covers it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A no-op, not an error, when the presenter has no view (or Unity has destroyed the one it had) or
        /// when <paramref name="group"/> is <c>null</c>, so the call site never needs a guard of its own.
        /// </para>
        /// <para>
        /// Nothing is registered on <see cref="Presenter.Lifetime"/>, so closing the presenter does not
        /// take the toggle back out of the group; the two Unity objects are left to sort that out between
        /// themselves.
        /// </para>
        /// </remarks>
        /// <param name="toggle">The presenter whose view joins the group.</param>
        /// <param name="group">The group to join, or <c>null</c> to do nothing.</param>
        /// <exception cref="NullReferenceException"><paramref name="toggle"/> is <c>null</c>.</exception>
        public static void RegisterToggleInGroup(this TogglePresenter toggle, ToggleGroup group)
        {
            if (toggle.View == null || group == null)
            {
                return;
            }

            group.RegisterToggle(toggle.View);
            toggle.View.group = group;
        }
    }
}
